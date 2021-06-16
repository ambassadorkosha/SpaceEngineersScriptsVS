using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRageMath;
using Digi;
using Scripts;
using System;
using System.Text.RegularExpressions;
using SpaceEngineers.Game.ModAPI;
using Scripts.Specials.Messaging;

namespace ServerMod.Specials {
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_ShipConnector), true, new string[] { "Connector", "ConnectorSmall" })]
    public class AutoConnector : MyGameLogicComponent  {
        private IMyShipConnector myBlock;
        private String lockedTimer;
        private String unlockedTimer;
        private static Regex regex = new Regex("\\[(LOCK|UNLOCK)\\:([\\w\\,\\/]+)\\]");
        
        private int state = 0; 

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            myBlock = (Entity as IMyShipConnector);
            if (!MyAPIGateway.Session.IsServer) return;
            myBlock.CustomNameChanged += MyBlock_CustomNameChanged;
            myBlock.OnMarkForClose += MyBlock_OnMarkForClose;
            Parse ();
            NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        private void MyBlock_OnMarkForClose(VRage.ModAPI.IMyEntity obj) {
             myBlock.CustomNameChanged -= MyBlock_CustomNameChanged;
             myBlock.OnMarkForClose -= MyBlock_OnMarkForClose;
        }

        private void MyBlock_CustomNameChanged(IMyTerminalBlock obj) {
            Parse ();
        }

        public override void UpdateOnceBeforeFrame() {
            if (state ==0) {
                NeedsUpdate &= ~VRage.ModAPI.MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                state = myBlock.IsConnected ? 1 : -1;
                Parse ();
            }
            base.UpdateOnceBeforeFrame();
        }
        private void Parse () {
            lockedTimer = null;
            unlockedTimer = null;
            foreach (Match m in regex.Matches(myBlock.CustomName)) {
                var what = m.Groups[1].Value;
                var name = m.Groups[2].Value;
                if (what == "LOCK") {
                    lockedTimer = name;
                } else if (what == "UNLOCK") {
                    unlockedTimer = name;
                }
            }
            if (lockedTimer != null || unlockedTimer != null) {
                NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.EACH_10TH_FRAME;
            } else {
                NeedsUpdate &= ~VRage.ModAPI.MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
        }

        public override void UpdateAfterSimulation10() {
            if (myBlock.IsConnected && state == -1 && lockedTimer != null) {
                var timer = myBlock.CubeGrid.FindBlock<IMyTimerBlock>(lockedTimer);   
                if (timer == null) {
                    timer = myBlock.OtherConnector.CubeGrid.FindBlock<IMyTimerBlock>(lockedTimer);
                }
                if (timer != null) {
                    timer.Trigger();
                }
            } else if (!myBlock.IsConnected && state == 1 && unlockedTimer != null) {
                var timer = myBlock.CubeGrid.FindBlock<IMyTimerBlock>(unlockedTimer); 
                if (timer != null) {
                    timer.Trigger();
                }
            }
            state = myBlock.IsConnected ? 1 : -1;
        }
    }
}
