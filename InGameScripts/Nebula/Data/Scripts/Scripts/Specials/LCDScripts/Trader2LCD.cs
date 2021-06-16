using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using System;
using VRage.Game.ModAPI;
using VRageMath;
using System.Collections.Generic;
using Digi;
using Scripts.Shared.GUI;
using Scripts.Specials.Trader;
using ServerMod;
using Slime;
using VRage.Game;
using Scripts.Base;
using System.Text;
using Sandbox.Definitions;
using VRage;

namespace MyMod.Specials
{
    [MyTextSurfaceScript("_Trader_", "_Trader_")]
    internal class TraderLCD : MyTSSCommon {
        private enum CurrentScreen
        {
            LIST,
            BUYSELL
        }

        private GUIBase mainScreen = new GUIBase();
        private GUIBase buySellScreen = new GUIBase();
        private GUIBase itemScreen = new GUIBase();
        private GUIBase noTraderScreen = new GUIBase();

        private CStaticText inputedNumberControl;
        private CStaticText currentTradeItemTitle;
        private CStaticText currentTradeItemTitleLine2;
        private CStaticText traderErrorInfo;
        private CStaticText tradeInfoText;
        private CButton buySellSwitch;

        public TradeOptions currentTrade = null;

        int inputedNumber = 0;

        CurrentScreen SCREEN = CurrentScreen.LIST;


        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;
        
        private AbstractTrader trader;
        bool canTrade = false;
        
        public TraderLCD(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size) {
            try {
                surface.ScriptForegroundColor = Color.White;
                InitScreens();
            } catch (Exception exception) { Log.ChatError (exception); }
        }


        
        private void InitScreens () {
            traderErrorInfo = new CStaticText(new RectangleF(20.0f, 20.0f, 200.0f, 30.0f), "Trader not found", m_foregroundColor);
            noTraderScreen.AddControl (traderErrorInfo);
            
            var s = (int)m_size.X / 3 * 2;
            var e = (int)m_size.X;


            var close = new CButton(new RectangleF(0, 20, 30, 30), "X", m_foregroundColor, null, CloseBuy);
            buySellSwitch = new CButton(new RectangleF(40, 20, 160, 30), "Switch", m_foregroundColor, null, OnBuySellToggled);

            var submit = new CButton(new RectangleF((int)m_size.X - (e - s), (int)m_size.Y - 60, e - s, 60), "SUBMIT TRADE", m_foregroundColor, null, SubmitTransaction);

            currentTradeItemTitle = new CStaticText(new RectangleF(220, 20, s - 240, 30), "", m_foregroundColor);
            currentTradeItemTitle.TextColor = Color.Gold;

            currentTradeItemTitleLine2 = new CStaticText(new RectangleF(220, 60, s - 240, 30), "", m_foregroundColor);
            currentTradeItemTitleLine2.TextColor = Color.Gold;

            tradeInfoText = new CStaticText(new RectangleF(20, 120, s - 40, 0), "", m_foregroundColor);
            tradeInfoText.Text.TextSize = 22;

            inputedNumberControl = new CStaticText(new RectangleF(s, 20, e - s, 60), "0", m_foregroundColor);
            inputedNumberControl.FillColor = Color.White;
            inputedNumberControl.TextColor = Color.Yellow;
            inputedNumberControl.BorderColor = Color.Gray;
            
            
            buySellScreen.AddControl (inputedNumberControl);
            buySellScreen.AddControl (submit);
            buySellScreen.AddControl (currentTradeItemTitle);
            buySellScreen.AddControl (currentTradeItemTitleLine2);
            buySellScreen.AddControl (tradeInfoText);
            buySellScreen.AddControl (buySellSwitch);
            
            buySellScreen.AddControl (close);

            var list = new List<string>() { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "<" };
            buySellScreen.ShowInGrid(list, (i, rect, d) => d == null ? null : new CButton(rect, d, m_foregroundColor, d, OnNumberInputPressed), 3, new Rectangle(s, 100, e-s, 0), 60, 15, 15);
        }

        public override void Run()
        {
            base.Run();
            try
            {
                if (m_block == null) return;
                if (MyAPIGateway.Session == null) return;
                if (MyAPIGateway.Session.Camera == null) return;

                if (!FindTrader() || !trader.isReady() || trader.TradeOptions.Count == 0)
                {
                    using (var frame = Surface.DrawFrame())
                    {
                        if (trader != null)
                        {
                            if (trader.TradeOptions.Count == 0)
                            {
                                traderErrorInfo.Text.Title = "No trade options set: Open custom data of trader";
                            } else
                            {
                                traderErrorInfo.Text.Title = trader.NotReadyDescription();
                            }
                            
                        } else
                        {
                            traderErrorInfo.Text.Title = "Trader not found";
                        }

                        noTraderScreen.Draw(frame);
                    }
                } else {

                    SCREEN = currentTrade != null ? CurrentScreen.BUYSELL : CurrentScreen.LIST;

                    switch (SCREEN)
                    {
                        case CurrentScreen.LIST:
                            UpdateItemScreen();
                            itemScreen.DrawInto (this, m_size);
                            break;
                        case CurrentScreen.BUYSELL:
                            UpdateBuySellScreen();
                            buySellScreen.DrawInto(this, m_size);
                            break;
                    }
                }

            } catch (Exception e) {
                Log.ChatError(e);
            }
        }

        public bool FindTrader(bool forceNew = false)
        {
            if (trader == null || forceNew || trader.MarkedForClose || trader.Closed)
            {
                trader = null;
                var entities = MyAPIGateway.Entities.GetEntitiesInSphere(Block.WorldMatrix.Translation, 30d, x => x.GetAs<AbstractTrader>() != null);
                if (entities.Count > 0)
                {
                    trader = entities[0].GetAs<AbstractTrader>();
                }
            }

            return trader != null;
        }
        
        
        public void UpdateBuySellScreen()
        {
            trader.updatePrices();
            UpdateTradeInfo();
        }

        public void UpdateItemScreen ()
        {
            trader.updatePrices();
            itemScreen.ClearControls();
            itemScreen.AddControl(new CStaticText(new RectangleF(20, 20, m_size.X - 40, 40), "Select item with Mouse middle button", m_foregroundColor));

            var list = new List<KeyValuePair<Pair<MyDefinitionId,MyDefinitionId>, TradeOptions>>();
            var dict = new Dictionary<Pair<MyDefinitionId, MyDefinitionId>, TradeOptions>();
            var dict2 = new Dictionary<MyDefinitionId, int>();

            foreach (var t in trader.TradeOptions)
            {
                dict2.Sum(t.Key.k, 1);
                dict2.Sum(t.Key.v, 1);

                var p = t.Key;
                var p2 = new Pair<MyDefinitionId, MyDefinitionId>(p.v, p.k);
                if (dict.ContainsKey (p2)) continue;
                dict.Add (p, t.Value);
                list.Add (new KeyValuePair<Pair<MyDefinitionId, MyDefinitionId>, TradeOptions>(t.Key, t.Value));
            }

            itemScreen.ShowInGrid (list, (i, rect, data)=>
            {
                MyPhysicalItemDefinition itemDefinition1 = null;
                MyPhysicalItemDefinition itemDefinition2 = null;
                if (MyDefinitionManager.Static.TryGetDefinition(data.Key.k, out itemDefinition1) && MyDefinitionManager.Static.TryGetDefinition(data.Key.v, out itemDefinition2))
                {
                    var i1 = itemDefinition1.Id.ToString();
                    var i2 = itemDefinition2.Id.ToString();

                    if (dict2[data.Key.k] > dict2[data.Key.v])
                    {
                        var i3 = i2;
                        i2 = i1;
                        i1 = i3;
                    }

                    var button = new CDoubleImage(rect, i1, i2);
                    button.UserData = data.Value;
                    button.OnDownDelegate = OnTradeSelected;
                    return button;
                } else
                {
                    var name = InventoryUtils.GetHumanName(data.Key.k) + " " + data.Value.GetAmount().toHumanQuantityCeiled();
                    var button = new CButton(rect, name, m_foregroundColor);
                    button.UserData = data.Value;
                    button.OnDownDelegate = OnTradeSelected;
                    return button;
                }

            }, 10, new Rectangle(20, 80, (int)m_size.X - 40, 0), rowOffset:10, columnInterval:10);
        }

        

        public void OnTradeSelected(Control control)
        {
            currentTrade = (TradeOptions)control.UserData;
            UpdateTradeInfo();
        }

        public void CloseBuy (Control control)
        {
            currentTrade = null;
        }

        public void SubmitTransaction(Control control)
        {
            trader.tryBuyOrSell (currentTrade.GetWhat(), currentTrade.GetFor(), (double)inputedNumber, true);
        }

        public void OnBuySellToggled(Control control)
        {
            var inverted = new Pair<MyDefinitionId, MyDefinitionId>(currentTrade.GetFor(), currentTrade.GetWhat());
            TradeOptions trade = null;
            if (trader.TradeOptions.TryGetValue(inverted, out trade))
            {
                currentTrade = trade;
                UpdateTradeInfo();
            }

            UpdateTradeInfo();
        }

        public void UpdateTradeInfo ()
        {
            var v = trader.tryBuyOrSell(currentTrade.GetWhat(), currentTrade.GetFor(), inputedNumber, false);

            if (v.k < 0)
            {
                currentTradeItemTitle.Text.Title = $"No space for {-v.v:0.#########} {InventoryUtils.GetHumanName(currentTrade.GetFor())}";
            } else
            {
                currentTradeItemTitle.Text.Title = $"You Pay {v.v:0.#########} {InventoryUtils.GetHumanName(currentTrade.GetFor())}";
            }


            if (v.v < 0)
            {
                currentTradeItemTitleLine2.Text.Title = $"No space for {-v.k:0.#########} {InventoryUtils.GetHumanName(currentTrade.GetWhat())}";
            }
            else
            {
                currentTradeItemTitleLine2.Text.Title = $"You Get {v.k:0.#########} {InventoryUtils.GetHumanName(currentTrade.GetWhat())}";
            }

           
            tradeInfoText.Text.Title = GetBuyInfo(currentTrade);//isBuying ?  : currentTrade.getSellInfo();
        }


        public string GetBuyInfo(TradeOptions options)
        {
            var items = trader.GetShopCargo().GetInventory().CountItems();
            var shopWhatAmount = (double)items.GetOr(options.GetWhat(), (MyFixedPoint)0d);
            var shopForAmount = (double)items.GetOr(options.GetFor(), (MyFixedPoint)0d);

            var items2 = trader.GetCustomerCargo().GetInventory().CountItems();
            var youWhatAmount = (double)items2.GetOr(options.GetWhat(), (MyFixedPoint)0d);
            var youForAmount = (double)items2.GetOr(options.GetFor(), (MyFixedPoint)0d);

            var sb = new StringBuilder();
            if (options.prices.Length == 1)
            {
                sb.Append(">").AppendFormat("1 : {0:0.###.###.###} {1}", options.getBuyRatio(0), InventoryUtils.GetHumanName(options.For)).Append("\n");
            } 
            else
            {
                for (var x = 0; x < options.prices.Length; x++)
                {
                    sb.Append(x == options.step ? ">" : " ").AppendFormat("1 : {0:0.###.###.###} {1}", options.getBuyRatio(x), InventoryUtils.GetHumanName(options.For));
                    if (x >= options.steps.Length)
                    {
                        sb.AppendFormat(" ({0:0.##} / Infinity)", options.step == x ? options.amount - options.steps[x - 1] : 0);
                    }
                    else
                    {
                        if (x < options.step)
                        {
                            sb.AppendFormat(" ({0:0.##} / {1:0.##})", options.steps[x], options.steps[x]);
                        }
                        else if (x == options.step)
                        {
                            sb.AppendFormat(" ({0:0.##} / {1:0.##})", options.leftToBuy, options.steps[x]);
                        }
                        else
                        {
                            sb.AppendFormat(" ({0:0.##} / {1:0.##})", 0, options.steps[x]);
                        }
                    }
                    sb.Append("\n");
                }
            }
            

            sb.Append("-----------------\nShop have:\n");
            //sb.Append("Tax:" + (100d * options.charge) + " %\n");
            sb.Append(InventoryUtils.GetHumanName(options.GetWhat())).Append(":").Append(shopWhatAmount).Append("\n");
            sb.Append(InventoryUtils.GetHumanName(options.GetFor())).Append(":").Append(shopForAmount).Append("\n");
            sb.Append("-----------------\nYou have:\n");
            //sb.Append("Tax:" + (100d * options.charge) + " %\n");
            sb.Append(InventoryUtils.GetHumanName(options.GetWhat())).Append(":").Append(youWhatAmount).Append("\n");
            sb.Append(InventoryUtils.GetHumanName(options.GetFor())).Append(":").Append(youForAmount);
            return sb.ToString();
        }

        public void OnNumberInputPressed(Control control)
        {
            var txt = (string)control.UserData;
            int num;
            if (int.TryParse(txt, out num) && inputedNumber < int.MaxValue / 100)
            {
                inputedNumber = inputedNumber * 10 + num;
            }
            else
            {
                if (txt == "<")
                {
                    inputedNumber = inputedNumber / 10;
                }
                if (txt == "C")
                {
                    inputedNumber = 0;
                }
            }
            UpdateTradeInfo();
            inputedNumberControl.Text.Title = $"{inputedNumber:0.#}";
        }
    }
}