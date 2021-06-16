using Digi;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces.Terminal;
using ServerMod;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Scripts.Specials.Production {

    public abstract class Adjustable : MyGameLogicComponent {
        public static Dictionary<long, Adjustable> allRefs = new Dictionary<long, Adjustable>();
        public static Func<IMyTerminalBlock, bool> isVisibleEnabled = (b)=>b.GetAs<Adjustable>() != null;

        protected IMyProductionBlock  block;
        public int pspeed = 0;
        public int ppower = 0; 
        public int pyeild = 0;

        private float BASE_POWER;
        private bool inited = false;

        public static void SetPoints(IMyTerminalControlSlider slider, IMyTerminalControlSlider slider2, IMyTerminalControlSlider slider3) {
            slider.UpdateVisual();
            slider2.UpdateVisual();
            slider3?.UpdateVisual();
        }

        public static IMyTerminalControlSlider CreateControl<T>(string id, string title, string tooltip) {
            var Control = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(id);
            
            Control.Title = MyStringId.GetOrCompute(title);
            Control.Tooltip = MyStringId.GetOrCompute(tooltip);
            Control.SetLimits(-10f, 110f);
            Control.Enabled = isVisibleEnabled;
            Control.Visible = isVisibleEnabled;

            MyAPIGateway.TerminalControls.AddControl<T>(Control);
            return Control;
        }
        
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            block = (Entity as IMyProductionBlock);

            try
            {
                block.AddUpgradeValue("AllUpgrades", 0);
                block.OnUpgradeValuesChanged += Block_OnUpgradeValuesChanged;
                block.OnClose += Block_OnClose;

                BASE_POWER = (block.SlimBlock.BlockDefinition as MyProductionBlockDefinition).OperationalPowerConsumption;
                Init();
            } catch (Exception e)
            {
                Log.ChatError (e);
            }
            
        }

        public override sealed void OnAddedToContainer() {
            try {
                base.OnAddedToContainer();

                if (block == null) { block = (Entity as IMyProductionBlock); }
                var n = (block.Storage != null);
                if (block.Storage == null) { block.Storage = new MyModStorageComponent(); }
            } catch (Exception e) {
                Log.Error (e, " N? " + (block == null));
            }
        }

        public void Init()
        {
            if (inited) return;
            inited = true;

            allRefs.Add(block.EntityId, this);

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                if (block.Storage != null)
                {
                    String speed, power, yeild;
                    if (block.Storage.TryGetValue(AdjustableProductionMod.Guid1, out speed)
                        && block.Storage.TryGetValue(AdjustableProductionMod.Guid2, out power)
                        && block.Storage.TryGetValue(AdjustableProductionMod.Guid3, out yeild)
                        && int.TryParse(speed, out pspeed)
                        && int.TryParse(power, out ppower)
                        && int.TryParse(yeild, out pyeild))
                    {
                        //Success
                    }
                    else
                    {
                        ppower = 100;
                        pspeed = 0;
                        pyeild = 0;
                    }
                    UpdatePoints();
                }
                else
                {
                    ppower = 100;
                    pspeed = 0;
                    pyeild = 0;
                    UpdatePoints();
                }
            }
            else
            {
                AdjustableProductionMod.RequestValuesFromServer(block.EntityId, MyAPIGateway.Multiplayer.MyId);
            }
        }

        private void Block_OnClose(IMyEntity obj) {
            try {
                allRefs.Remove (block.EntityId);
                block.OnUpgradeValuesChanged -= Block_OnUpgradeValuesChanged;
                block.OnClose -= Block_OnClose;
            } catch (Exception e){
                Log.Error (e, "AdjustableRefs->OnClose");
            }
        }

        private void Block_OnUpgradeValuesChanged() {
            UpdatePoints (); 
        }

        public abstract float getMAX_SPEED_POINTS();
        public abstract float getMAX_POWER_POINTS();
        public abstract float getMAX_YEILD_POINTS();
        public abstract float getBASE_SPEED();
        public abstract float getSPEEDBonus();
        public abstract void Refresh();

        
        
        public void UpdatePoints () {
            
            try { 
                var maxPoints = getBASE_SPEED() + block.UpgradeValues["AllUpgrades"] * getSPEEDBonus();
               
                var pointsSpeed = 1f + (pspeed / 100f) * getMAX_SPEED_POINTS();
                var pointsPower = 1f + (ppower / 100f) * getMAX_POWER_POINTS();
                var pointsYeild = 1f + (pyeild / 100f) * getMAX_YEILD_POINTS();
                
                var speedMod = maxPoints * pointsSpeed;
                var energy = BASE_POWER * speedMod / pointsPower * Math.Sqrt (pointsSpeed) * Math.Sqrt (pointsYeild);
                var powerMod = speedMod * BASE_POWER / (float)energy;
                var yeildMod = pointsYeild;

                float speedBoost = 1f;
                if (block.UpgradeValues.ContainsKey("Boost"))
                {
                    speedBoost = block.UpgradeValues["Boost"];
                    if (speedBoost < 1f)
                    {
                        speedBoost = 1f;
                    }
                }
                //MyLog.Default.Info($"[Adjustable.Update] : [{speedBoost}]");




                block.UpgradeValues["Productivity"] = (speedMod/yeildMod * speedBoost) -1;
                block.UpgradeValues["PowerEfficiency"] = powerMod/yeildMod;
                block.UpgradeValues["Effectiveness"] = yeildMod;

                Refresh();
                block.RefreshCustomInfo();
            } catch (Exception e){ 
                Log.Error (e);
                Refresh();
                block.UpgradeValues["Productivity"] = 0f;
                block.UpgradeValues["PowerEfficiency"] = 1f;
                block.UpgradeValues["Effectiveness"] = 1f;
            }
        }

        public void SetExactPoints (int speed, int power, int yeild, bool update, bool save) {

            SetPointsWithValidation (speed, power, yeild);

            if (save) { 
			   block.Storage.SetValue(AdjustableProductionMod.Guid1, ""+pspeed);
               block.Storage.SetValue(AdjustableProductionMod.Guid2, ""+ppower);
               block.Storage.SetValue(AdjustableProductionMod.Guid3, ""+pyeild);
            }

            if (update) {
                UpdatePoints ();
            }
        }
        //pspeed + " " + ppower + " " + pyeild +
        public void SetPointsWithValidation (int speed, int power, int yeild) {
            var max = 100;
            int left;
            
            left = max - pspeed - pyeild - ppower;
            if (speed >= 0) { 
                pspeed = Math.Max(0, Math.Min (pspeed + left, speed));
            }

            left = max - pspeed - pyeild - ppower;
            if (power >= 0) { 
                ppower = Math.Max(0, Math.Min (ppower + left, power));
            }

            left = max - pspeed - pyeild - ppower;
            if (yeild >= 0) { 
                pyeild = Math.Max(0, Math.Min (pyeild + left, yeild));
            }
        }

        public void SetPointsClient (int speed, int power, int yeild) {
            SetPointsWithValidation (speed, power, yeild);
            AdjustableProductionMod.SendValuesToServer (block.EntityId, pspeed, ppower, pyeild);
        }
    }

   
}
