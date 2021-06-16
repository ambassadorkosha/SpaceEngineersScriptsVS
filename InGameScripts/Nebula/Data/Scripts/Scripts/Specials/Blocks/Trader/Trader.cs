using Digi;
using Sandbox.ModAPI;
using Scripts.Base;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ProtoBuf;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage;
using ServerMod;
using VRage.ModAPI;
using Sandbox.ModAPI.Ingame;
using Scripts.Shared;
using Slime;
using IMyCargoContainer = Sandbox.ModAPI.IMyCargoContainer;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;
using Sandbox.Game;
using Sandbox.Common.ObjectBuilders;
using Scripts.Specials.Messaging;
using VRageMath;

namespace Scripts.Specials.Trader
{



    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), true, new string[] { "Trader" })]
    public class AbstractTrader : MyGameLogicComponent {
        private static String CARGO_SHOP = "TRADER-SHOP";
        private static String CARGO_CUSTOMER = "TRADER-CUSTOMER";
        private static Regex bracketsRegex = new Regex("([\\w\\\\_-]+)\\/([\\w\\\\_-]+) ?{([^}]*)}");
        private static Regex tradeRegex = new Regex("([\\w\\\\_-]+)\\/([\\w\\\\_-]+)\\|([\\d,.]+)\\|([\\w,]+)\\|\\[([\\d.,\\|]+)\\](?:\\|\\[([\\d.,\\|]*)\\])?");
        private static Connection<BuySellRequest> connection;

        [ProtoContract]
        private class BuySellRequest
        {
            [ProtoMember(1)] public string What;
            [ProtoMember(2)] public string For;
            [ProtoMember(3)] public double ExpectedWhat;
            [ProtoMember(4)] public double ExpectedFor;
            [ProtoMember(5)] public double Wanted;
            [ProtoMember(7)] public long EntityId;
        }

        private IMyCargoContainer cargo_shop;
        private IMyCargoContainer cargo_customer;
        private Sandbox.ModAPI.IMyUpgradeModule block;

        private bool trigger = true;
        public Dictionary<Pair<MyDefinitionId, MyDefinitionId>, TradeOptions> TradeOptions = new Dictionary<Pair<MyDefinitionId, MyDefinitionId>, TradeOptions>();


        public IMyCargoContainer GetShopCargo ()
        {
            return cargo_shop;
        }

        internal IMyCargoContainer GetCustomerCargo()
        {
            return cargo_customer; 
        }


        public bool isReady ()
        {
            return block.Enabled && findCargo();
        }

        public String NotReadyDescription()
        {
            if (!block.Enabled)
            {
                return "Trader is not enabled";
            }
            if (cargo_shop == null)
            {
                return "Shop cargo not found. It must have name `TRADER-SHOP` and be in 100 m\nAlso it should have same Owner as Cargo";
            }
            if (cargo_customer == null)
            {
                return "Customer cargo not found `TRADER-CUSTOMER` and be in 25 m\nAlso it should have same Owner as Cargo";
            }

            return null;
        }

        public static void Init ()
        {
            connection = new Connection<BuySellRequest>(24352, HandleBuySellRequest);
        }

        private static void HandleBuySellRequest (BuySellRequest request, ulong PlayerSteamId, bool isFromServer)
        {
            try
            {
                if (isFromServer)
                {
                    //Log.ChatError("HandleBuySellRequest: isFromServer");
                    return;
                }

                var ent = request.EntityId.As<IMyEntity>();
                if (ent == null)
                {
                    //Log.ChatError("HandleBuySellRequest: ent is null");
                    return;
                }
                var tr = ent.GetAs<AbstractTrader>();
                if (tr == null)
                {
                    //Log.ChatError("HandleBuySellRequest: trader is null");
                    return;
                }

                tr.tryBuyOrSell(MyDefinitionId.Parse(request.What), MyDefinitionId.Parse(request.For), request.Wanted, true, request.ExpectedWhat, request.ExpectedFor);
            } catch (Exception e) { 
                Log.ChatError (request.What+" " + request.For);    
            }
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
            block = Entity as Sandbox.ModAPI.IMyUpgradeModule;
            block.CustomDataChanged += CustomDataChanged;
            CustomDataChanged(block);
        }

        public void CustomDataChanged (IMyTerminalBlock block)
        {
            Parse();
        }

        public override void UpdateBeforeSimulation100() {
            base.UpdateBeforeSimulation100();
            Parse();
            if (!findCargo()) return;
            updatePrices ();
        }

        string lastCustomData;

        bool LOG = false;
        bool LOG_LCD_CHECK = false;
        bool LOGTRADE = false;
        
        private void Parse() {
            if (lastCustomData == block.CustomData) return;

            lastCustomData = block.CustomData;

            TradeOptions.Clear();
            try
            {
                var ma = bracketsRegex.Matches(block.CustomData);
                var errors = new List<string>();
                for (var x = 0; x < ma.Count; x++)
                {
                    var b = ma[x];
                    try
                    {
                        var _typeFor = b.Groups[1].Value;
                        var _subtypeFor = b.Groups[2].Value;
                        MyDefinitionId Сurrency;

                        if (!InventoryUtils.ParseHumanDefinition(_typeFor, _subtypeFor, out Сurrency))
                        {
                            errors.Add(b.Value + " : couldn't find item with type/subtype");
                            continue;
                        }

                        var ma2 = tradeRegex.Matches(b.Groups[3].Value);
                        for (var y = 0; y < ma2.Count; y++)
                        {
                            var match = ma2[y];
                            var _type = match.Groups[1].Value;
                            var _subtype = match.Groups[2].Value;
                            var _tax = match.Groups[3].Value;
                            var _buysell = match.Groups[4].Value;
                            var _prices = match.Groups[5].Value;
                            var _amounts = match.Groups[6].Value ?? "";

                            double tax = 0;
                            if (!Double.TryParse(_tax, out tax))
                            {
                                errors.Add(match.Value + " : tax");
                                continue;
                            }
                            MyDefinitionId What;
                            if (!InventoryUtils.ParseHumanDefinition(_type, _subtype, out What))
                            {
                                errors.Add(match.Value + " : couldn't find item with type/subtype");
                                continue;
                            }

                            _buysell = _buysell.ToLowerInvariant();
                            bool canBuy = _buysell.Contains("b");
                            bool canSell = _buysell.Contains("s");

                            if (!canBuy && !canSell)
                            {
                                continue;
                            }

                            var _amounts2 = _amounts.Split(new char[] { '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            var _prices2 = _prices.Split(new char[] { '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);


                            if (_amounts2.Length + 1 != _prices2.Length)
                            {
                                errors.Add(match.Value + " : amounts count must be less than prices by 1 item");
                            }

                            double[] amounts = new double[_amounts2.Length];
                            double[] prices = new double[_prices2.Length];

                            for (var i = 0; i < amounts.Length; i++)
                            {
                                double v;
                                if (!double.TryParse(_amounts2[i], out v))
                                {
                                    errors.Add(match.Value + " : Couldn't parse number: " + _amounts2[i]);
                                }

                                amounts[i] = v;
                            }

                            for (var i = 0; i < prices.Length; i++)
                            {
                                double v;
                                if (!double.TryParse(_prices2[i], out v))
                                {
                                    errors.Add(match.Value + " : Couldn't parse number: " + _prices2[i]);
                                }

                                prices[i] = v;
                            }


                            if (prices.Length > 1)
                            {
                                errors.Add(match.Value + " : Wrong prices amount. Currently only 1 price availiable");
                            }

                            if (errors.Count > 0)
                            {
                                continue;
                            }

                            try
                            {
                                if (canBuy)
                                {
                                    var pricesBuy = new double[prices.Length];
                                    for (var z = 0; z < prices.Length; z++)
                                    {
                                        pricesBuy[z] = prices[z]*(1-tax);
                                    }
                                    var to = new TradeOptions(What, Сurrency, What, false, amounts, pricesBuy, tax);
                                    var p1 = new Pair<MyDefinitionId, MyDefinitionId>(What, Сurrency);
                                    TradeOptions.Add(p1, to);
                                }

                                if (canSell)
                                {
                                    var pricesSell = new double[prices.Length];
                                    for (var z = 0; z < prices.Length; z++)
                                    {
                                        pricesSell[z] = (1-tax)/prices[z];
                                    }
                                    var to = new TradeOptions(Сurrency, What, What, false, amounts, pricesSell, tax);
                                    var p2 = new Pair<MyDefinitionId, MyDefinitionId>(Сurrency, What);
                                    TradeOptions.Add(p2, to);
                                }
                            } catch (Exception e)
                            {
                                Log.ChatError (e);
                            }
                        }
                    } catch (Exception e) {
                        Log.Error(e, b.Value);
                    }
                }

                foreach (var r in errors)
                {
                    Log.ChatError(r);
                }
            } catch (Exception e)
            {
                Log.Error (e);
            }
            
        }
        
        private int Compare (Vector3 a, Vector3 b)
        {
            var d1 = (a - block.WorldMatrix.Translation).LengthSquared();
            var d2 = (b - block.WorldMatrix.Translation).LengthSquared();

            return d1 > d2 ? 1 : d1 == d2 ? 0 : -1;
        }

        private bool findCargo () {
            if (cargo_customer != null && (cargo_customer.Closed || cargo_customer.MarkedForClose)) {
                cargo_customer = null;
            }

            if (cargo_shop != null && (cargo_shop.Closed || cargo_shop.MarkedForClose))
            {
                cargo_shop = null;
            }

            if (cargo_customer == null)
            {
                var cargos = MyAPIGateway.Entities.GetEntitiesInSphere(block.WorldMatrix.Translation, 20d, (x) => {
                    var y = x as IMyCargoContainer;
                    return y != null && y.OwnerId == block.OwnerId && y.CustomName == CARGO_CUSTOMER;
                });
                cargos.Sort ((a,b)=> Compare (a.WorldMatrix.Translation, b.WorldMatrix.Translation));
                cargo_customer = cargos.Count > 0 ? cargos[0] as IMyCargoContainer : null;
            }

            if (cargo_shop == null)
            {
                var traders = MyAPIGateway.Entities.GetEntitiesInSphere(block.WorldMatrix.Translation, 100d, (x) => {
                    var y = x as IMyCargoContainer;
                    return y != null && y.OwnerId == block.OwnerId && y.CustomName == CARGO_SHOP;
                });
                traders.Sort((a, b) => Compare(a.WorldMatrix.Translation, b.WorldMatrix.Translation));
                cargo_shop = traders.Count > 0 ? traders[0] as IMyCargoContainer : null;
            }
            
            return cargo_customer != null && cargo_shop != null;
        }

        

        public void updatePrices () {
            try {
                if (!findCargo()) return;
                var items = cargo_customer.GetInventory().CountItems();
                foreach (var trade in TradeOptions) {
                    var what = items.GetOr(trade.Value.GetWhat(), (MyFixedPoint)0d);
                    var forr = items.GetOr(trade.Value.GetFor(), (MyFixedPoint)0d);
                    trade.Value.refreshAmount((double)what, (double)forr);
                }
            } catch (Exception e) {
                Log.Error(e, "Prices");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="What">What customer BUY</param>
        /// <param name="For">What customer give for `WHAT`</param>
        /// <param name="wanted">Amount of `WHAT` is wanted</param>
        /// <param name="realTransact"></param>
        /// <param name="expectedWhat"></param>
        /// <param name="expectedFor"></param>
        /// <returns></returns>
        public Pair<double,double> tryBuyOrSell (MyDefinitionId What, MyDefinitionId For, double wanted, bool realTransact=false, double expectedWhat=-1.0, double expectedFor = -1.0) {

            bool nLOG = LOG;
            if (!realTransact && !LOG_LCD_CHECK) nLOG = false;

            if (!findCargo()) {
                return new Pair<double, double>(0,0);
            }

            Parse();

            var trade = TradeOptions.GetOr(new Pair<MyDefinitionId, MyDefinitionId>(What, For), null);
            
            var cItems = cargo_customer.GetInventory().CountItems();
            var customerWhatAmount = (double)cItems.GetOr(What, (MyFixedPoint)0d);
            var customerForAmount = (double)cItems.GetOr(For, (MyFixedPoint)0d);
            
            var items = cargo_shop.GetInventory().CountItems();
            var shopWhatAmount = (double)items.GetOr(What, (MyFixedPoint)0d);
            var shopForAmount = (double)items.GetOr(For, (MyFixedPoint)0d);


            if (customerForAmount <= 0)
            {
                if (nLOG) Log.ChatError("forAmount < 0");
                return new Pair<double, double>(0, 0);
            }

            if (shopWhatAmount <= 0)
            {
                if (nLOG) Log.ChatError("shopWhatAmount < 0");
                return new Pair<double, double>(0, 0);
            }

            if (trade == null)
            {
                if (nLOG) Log.ChatError("No trade options");
                return new Pair<double, double>(0, 0);
            }

            //if (nLOG) Log.ChatError($"START TRADE: {realTransact} {wanted} ({shopWhatAmount})x{What}->({customerForAmount}?){For} (shop have: {shopForAmount})");
            if (nLOG) Log.ChatError($"START TRADE: {customerWhatAmount} {customerForAmount} -> {shopWhatAmount} ({shopForAmount})");



            var beforeTradeWhat = shopWhatAmount;
            var beforeTradeFor = shopForAmount;

            try
            {
                trade.refreshAmount(shopWhatAmount, shopForAmount);


                var gotWHAT = 0d;
                var usedFOR = 0d;

                while (true)
                {
                    if (customerForAmount <= 0)
                    {
                        if (nLOG) Log.ChatError($"Can buy <= 0 {customerForAmount}");
                        break;
                    }


                    var leftWHAT = trade.isBuy ? trade.leftToBuy : trade.leftToSell;
                    leftWHAT = Math.Min(leftWHAT, wanted - gotWHAT);

                    var canAffordWHAT = customerForAmount * trade.buyRatio;
                    canAffordWHAT = Math.Min(canAffordWHAT, leftWHAT);
                    if (trade.isFixed)
                    {
                        canAffordWHAT = (long)canAffordWHAT;
                        if (canAffordWHAT == 0)
                        {
                            if (LOG) Log.ChatError("wtb == 0");
                            return new Pair<double, double>(0, 0);
                        }
                    }

                    if (nLOG) Log.ChatError($"canAfford={canAffordWHAT}");

                    
                    gotWHAT += canAffordWHAT;
                    usedFOR += canAffordWHAT / trade.buyRatio;

                    customerWhatAmount += canAffordWHAT;
                    customerForAmount -= canAffordWHAT / trade.buyRatio;

                    shopWhatAmount -= canAffordWHAT;
                    shopForAmount += canAffordWHAT / trade.buyRatio;

                    //if (nLOG) Log.ChatError($"cart : -({canAffordWHAT / trade.buyRatio}) => +({canAffordWHAT})");
                    if (nLOG) Log.ChatError($"START TRADE: {customerWhatAmount} {customerForAmount} -> {shopWhatAmount} ({shopForAmount})");

                    if (gotWHAT >= wanted)
                    {
                        if (nLOG) Log.ChatError($"cart >= wanted {gotWHAT} {wanted}");
                        break;
                    }

                    trade.setAmount(shopWhatAmount, shopForAmount);


                    if (trade.isBuy)
                    {
                        if (!trade.decreaseStep())
                        {
                            if (nLOG) Log.ChatError($"Cant decrease step! {gotWHAT} {wanted}");
                            break;
                        }
                    }
                    else
                    {
                        if (!trade.increaseStep())
                        {
                            if (nLOG) Log.ChatError($"Cant decrease step! {gotWHAT} {wanted}");
                            break;
                        }
                    }
                }

                if (nLOG) Log.ChatError("TRADE BUY: " + gotWHAT + "/" + What + " FOR " + usedFOR + "/" + For);

                var traderInv = cargo_shop.GetInventory() as MyInventory;
                var cargoInv = cargo_customer.GetInventory() as MyInventory;
                if (gotWHAT <= 0 || usedFOR <= 0)
                {
                    return new Pair<double, double>(0, 0);
                }

                if (!traderInv.CanItemsBeAdded((MyFixedPoint)gotWHAT, What) || !cargoInv.CanItemsBeAdded((MyFixedPoint)usedFOR, For))
                {
                    return new Pair<double, double>(-gotWHAT, -usedFOR);
                }


                if (realTransact)
                {
                    if (MyAPIGateway.Session.IsServer)
                    {
                        if (Math.Abs(expectedWhat - gotWHAT) < 0.000001 && Math.Abs(expectedFor - usedFOR) < 0.000001 && expectedWhat > 0 && expectedFor > 0 || !MyAPIGateway.Session.isTorchServer())
                        {
                            if (nLOG) Log.ChatError("Do trade");

                            traderInv.RemoveAmount(What, gotWHAT);
                            traderInv.AddItem(For, usedFOR);

                            cargoInv.RemoveAmount(For, usedFOR);
                            cargoInv.AddItem(What, gotWHAT);

                            trade.refreshAmount(shopWhatAmount, shopForAmount);
                        }
                        else
                        {
                            if (nLOG) Log.ChatError("Handle message from server: Validation failed " + (expectedWhat + "/" + usedFOR) + " " + (expectedFor));
                            //Don't do anything
                        }
                    }
                    else
                    {
                        connection.SendMessageToServer(new BuySellRequest
                        {
                            What = What.ToString(),
                            For = For.ToString(),
                            ExpectedWhat = gotWHAT,
                            ExpectedFor = usedFOR,
                            EntityId = Entity.EntityId,
                            Wanted = wanted
                        });
                        //Send Request
                    }
                }

                return new Pair<double, double>(gotWHAT, usedFOR);
            } finally
            {
                if (!realTransact)
                {
                    trade.refreshAmount(beforeTradeWhat, beforeTradeFor);
                }
            }
        }
    }
}