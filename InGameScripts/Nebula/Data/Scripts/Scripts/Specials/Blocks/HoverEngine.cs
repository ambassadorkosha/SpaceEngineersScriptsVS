using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRageMath;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI.Interfaces.Terminal;
using ServerMod;
using Scripts.Specials.Blocks.StackableMultipliers;
using Scripts.Specials;
using Digi;
using Scripts.Shared;

namespace Shame.HoverEngine
{
	#region HoverEngine attribute

	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false, 
                                 "SmallHoverEngine_SmallBlockV1T1", 
                                 "LargeHoverEngine_SmallBlockV1T1", 
                                 "SmallHoverEngine_LargeBlockV1T1", 
                                 "LargeHoverEngine_LargeBlockV1T1",
                                 "SmallHoverEngine_SmallBlockV2T1",
                                 "LargeHoverEngine_SmallBlockV2T1", 
                                 "SmallHoverEngine_LargeBlockV2T1", 
                                 "LargeHoverEngine_LargeBlockV2T1", "SmallHoverEngine_SmallBlockV1T2", "LargeHoverEngine_SmallBlockV1T2", "SmallHoverEngine_LargeBlockV1T2", "LargeHoverEngine_LargeBlockV1T2", "SmallHoverEngine_SmallBlockV2T2", "LargeHoverEngine_SmallBlockV2T2", "SmallHoverEngine_LargeBlockV2T2", "LargeHoverEngine_LargeBlockV2T2", "SmallHoverEngine_SmallBlockV1T3", "LargeHoverEngine_SmallBlockV1T3", "SmallHoverEngine_LargeBlockV1T3", "LargeHoverEngine_LargeBlockV1T3", "SmallHoverEngine_SmallBlockV2T3", "LargeHoverEngine_SmallBlockV2T3", "SmallHoverEngine_LargeBlockV2T3", "LargeHoverEngine_LargeBlockV2T3", "SmallHoverEngine_SmallBlockV1T4", "LargeHoverEngine_SmallBlockV1T4", "SmallHoverEngine_LargeBlockV1T4", "LargeHoverEngine_LargeBlockV1T4", "SmallHoverEngine_SmallBlockV2T4", "LargeHoverEngine_SmallBlockV2T4", "SmallHoverEngine_LargeBlockV2T4", "LargeHoverEngine_LargeBlockV2T4", "SmallHoverEngine_SmallBlockV1T5", "LargeHoverEngine_SmallBlockV1T5", "SmallHoverEngine_LargeBlockV1T5", "LargeHoverEngine_LargeBlockV1T5", "SmallHoverEngine_SmallBlockV2T5", "LargeHoverEngine_SmallBlockV2T5", "SmallHoverEngine_LargeBlockV2T5", "LargeHoverEngine_LargeBlockV2T5", "SmallHoverEngine_SmallBlockV1T6", "LargeHoverEngine_SmallBlockV1T6", "SmallHoverEngine_LargeBlockV1T6", "LargeHoverEngine_LargeBlockV1T6", "SmallHoverEngine_SmallBlockV2T6", "LargeHoverEngine_SmallBlockV2T6", "SmallHoverEngine_LargeBlockV2T6", "LargeHoverEngine_LargeBlockV2T6", "SmallHoverEngine_SmallBlockV1T7", "LargeHoverEngine_SmallBlockV1T7", "SmallHoverEngine_LargeBlockV1T7", "LargeHoverEngine_LargeBlockV1T7", "SmallHoverEngine_SmallBlockV2T7", "LargeHoverEngine_SmallBlockV2T7", "SmallHoverEngine_LargeBlockV2T7", "LargeHoverEngine_LargeBlockV2T7", "SmallHoverEngine_SmallBlockV1T8", "LargeHoverEngine_SmallBlockV1T8", "SmallHoverEngine_LargeBlockV1T8", "LargeHoverEngine_LargeBlockV1T8", "SmallHoverEngine_SmallBlockV2T8", "LargeHoverEngine_SmallBlockV2T8", "SmallHoverEngine_LargeBlockV2T8", "LargeHoverEngine_LargeBlockV2T8", "SmallHoverEngine_SmallBlockV1T9", "LargeHoverEngine_SmallBlockV1T9", "SmallHoverEngine_LargeBlockV1T9", "LargeHoverEngine_LargeBlockV1T9", "SmallHoverEngine_SmallBlockV2T9", "LargeHoverEngine_SmallBlockV2T9", "SmallHoverEngine_LargeBlockV2T9", "LargeHoverEngine_LargeBlockV2T9", "SmallHoverEngine_SmallBlockV1T10", "LargeHoverEngine_SmallBlockV1T10", "SmallHoverEngine_LargeBlockV1T10", "LargeHoverEngine_LargeBlockV1T10", "SmallHoverEngine_SmallBlockV2T10", "LargeHoverEngine_SmallBlockV2T10", "SmallHoverEngine_LargeBlockV2T10", "LargeHoverEngine_LargeBlockV2T10", "SmallHoverEngine_SmallBlockV1T11", "LargeHoverEngine_SmallBlockV1T11", "SmallHoverEngine_LargeBlockV1T11", "LargeHoverEngine_LargeBlockV1T11", "SmallHoverEngine_SmallBlockV2T11", "LargeHoverEngine_SmallBlockV2T11", "SmallHoverEngine_LargeBlockV2T11", "LargeHoverEngine_LargeBlockV2T11", "SmallHoverEngine_SmallBlockV1T12", "LargeHoverEngine_SmallBlockV1T12", "SmallHoverEngine_LargeBlockV1T12", "LargeHoverEngine_LargeBlockV1T12", "SmallHoverEngine_SmallBlockV2T12", "LargeHoverEngine_SmallBlockV2T12", "SmallHoverEngine_LargeBlockV2T12", "LargeHoverEngine_LargeBlockV2T12")]

    #endregion

    public class HoverEngine : MyGameLogicComponent {
        private static bool isInited = false;
        public static int hoverRaycastStartingIndexOffset = 0;

        bool init = false;
        bool initShowState = false;
        bool showStateNextFrame = true;


        public IMyThrust block { get; private set; }

        public Color m_color = new Color(0, 255, 0, 255); // lighter green after Keens graphic overhaoul 2/2018

        public float heightTargetMax { get; private set; }
        public float maxAltitude { get; private set; }
        public double distanceToSurfaceCached { get; private set; }
        public bool rayHitHappened { get; private set; }

        public bool autoHoverEnabled { get; private set; }


        public Vector3D currentRaycastPoint { get; private set; }
        public bool isCurrentRaycastPointValid() { return currentRaycastPoint != Vector3D.NegativeInfinity; }
        public Vector3D prevRaycastPoint { get; private set; }
        public bool isPreviousRaycastPointValid() { return prevRaycastPoint != Vector3D.NegativeInfinity; }

        public static int RaycastInterval = 3;
        
        float blockSizeSpecial;

        public HoverProperties properties;

        enum StoredProperties {
            MinAltitude = 0,
            AutoHover = 1,
            StorageSize
        }

        protected string[] Storage;
        public static double Cos60Deg = 0.5;
        public static double Cos75Deg = 0.75;
        public static double MaxOperationalAngleCos = Cos75Deg;


		public EndlessMultiplierEffect heightEffect;

		// Gamelogic initialization
		public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
			base.Init(objectBuilder);
            if (!isInited) {
              Buttons_AllBlock();
              isInited = true;
            }
			heightEffect = new EndlessMultiplierEffect(3, Entity.EntityId, Entity.EntityId, 1, 1, 1);

			Storage = new string[(int) StoredProperties.StorageSize];

            prevRaycastPoint = Vector3D.NegativeInfinity;
            currentRaycastPoint = Vector3D.NegativeInfinity;

            block = Entity as IMyThrust;

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

            if (block == null) return;

            block.IsWorkingChanged += Block_IsWorkingChanged;
            block.EnabledChanged += BlockOnEnabledChanged;
            block.CustomDataChanged += BlockOnCustomDataChanged;
            block.OnMarkForClose += BlockOnOnMarkForClose;

            if (MyAPIGateway.Session == null) return;

            init = true;

            var subtype = block.BlockDefinition.SubtypeId;

            var lid = subtype.Substring(subtype.IndexOf("_") + 1);
            properties = HoverProperties.Levels[lid]; 
            m_color = properties.Color;
            maxAltitude = properties.AltitudeMax;

            if (!Deserialize()) {
                // Load defaults
                heightTargetMax = maxAltitude;
                autoHoverEnabled = true;
                Serialize();
                UpdateCustomData();
            }

            if (subtype.StartsWith("SmallHoverEngine_SmallBlockV1")) { 
                blockSizeSpecial = -0.15f; 
            } else if (subtype.StartsWith("LargeHoverEngine_SmallBlockV1")) { 
                blockSizeSpecial = 0.26f; 
            } else if (subtype.StartsWith("SmallHoverEngine_LargeBlockV1")) { 
                blockSizeSpecial = -0.4f;
            } else if (subtype.StartsWith("LargeHoverEngine_LargeBlockV1")) { 
                blockSizeSpecial = 1.3f;
            } else if (subtype.StartsWith("SmallHoverEngine_SmallBlockV2")) { 
                blockSizeSpecial = -0.18f;
            } else if (subtype.StartsWith("LargeHoverEngine_SmallBlockV2")) { 
                blockSizeSpecial = 0.01f; 
            } else if (subtype.StartsWith("SmallHoverEngine_LargeBlockV2")) { 
                blockSizeSpecial = -0.8f; 
            } else if (subtype.StartsWith("LargeHoverEngine_LargeBlockV2")) { 
                blockSizeSpecial = 0.01f; 
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            block.GetAs<ThrusterBase>().AddEffect(heightEffect);
            base.UpdateOnceBeforeFrame();
        }

		public void SetThustMultiplier (float value)
		{
			heightEffect.m1 = value;
			block.GetAs<ThrusterBase>().Recalculate ();
		}

		private void BlockOnOnMarkForClose(IMyEntity obj) {
            block.IsWorkingChanged -= Block_IsWorkingChanged;
            block.EnabledChanged -= BlockOnEnabledChanged;
            block.CustomDataChanged -= BlockOnCustomDataChanged;
            block.OnMarkForClose -= BlockOnOnMarkForClose;
            block = null;
        }

        private void BlockOnEnabledChanged(IMyTerminalBlock b) { showStateNextFrame = true; }

        private void Block_IsWorkingChanged(IMyCubeBlock obj) { showStateNextFrame = true; }
        
        private void BlockOnCustomDataChanged(IMyTerminalBlock obj) {
            Deserialize();
        }
        
        private void UpdateCustomData() {
            if (block != null) {
                var s = "";
                for (int i = 0; i < (int) StoredProperties.StorageSize; i++) { s += Storage[i]; }
                block.CustomData = s;
            }
        }
        
        private void Serialize() {
            if (block != null) {
                Storage[(int) StoredProperties.AutoHover] = autoHoverEnabled ? "1\n" : "0\n";
                Storage[(int) StoredProperties.MinAltitude] = String.Format("{0}\n", heightTargetMax);
            }
        }

        private bool Deserialize() {
            if (block != null && block.CustomData.Length > 0) {
                string[] serializedData = block.CustomData.Split('\n');
                if (serializedData.Length < (int) StoredProperties.StorageSize) {
                    //MyAPIGateway.Utilities.ShowNotification("serialized data = " + serializedData.Length.ToString() + " expected " + ((int)StoredProperties.StorageSize - 1).ToString(), 10000, MyFontEnum.Red);
                    return false;
                }
                var ahe = autoHoverEnabled;
                autoHoverEnabled = serializedData[(int) StoredProperties.AutoHover] == "1";
                if (autoHoverEnabled == false && ahe == true)
                {
                    block.ThrustOverridePercentage = 0;
                }


                float tempValue = 0;
                try {
                    if (float.TryParse(serializedData[(int) StoredProperties.MinAltitude], out tempValue)) {
                        heightTargetMax = tempValue;
                    } else {
                        return false;
                    }
                } catch {
                    return false;
                }
                return true;
            }
            return false;
        }

        public Vector3D TryGetRaycastElevationDifference() {
            // If both previous and most fresh raycasts hit the ground
            if (isPreviousRaycastPointValid() && isCurrentRaycastPointValid()) {
                // Get the terrain change vector direction (so we can determine world vector of terrain change along the ship trajectory)
                Vector3D Dir = currentRaycastPoint - prevRaycastPoint;
                if (Dir != Vector3D.Zero) return Vector3D.Normalize(Dir);
                else return Vector3D.Zero;
            } else return Vector3D.Zero;
        }

        public void CheckDistanceToClosestSurface(ref Vector3D NGravityDown, ref Vector3D Velocity) {
            if (FrameExecutor.currentFrame % RaycastInterval == Entity.EntityId % RaycastInterval) {
                distanceToSurfaceCached = 0;
                rayHitHappened = false;

                MyCubeGrid grid = block.CubeGrid as MyCubeGrid;
                IHitInfo hitInfo;
                Vector3D StartPoint = block.GetPosition() + blockSizeSpecial * block.WorldMatrix.Forward;
                double distanceToRaycast = maxAltitude * 10;
                Vector3D RayDirection = NGravityDown * distanceToRaycast + Velocity * 2;
                Vector3D EndPoint = StartPoint + RayDirection;
                Vector3D NRayDirection = RayDirection;
                distanceToRaycast = NRayDirection.Normalize();

                if (MyAPIGateway.Physics.CastRay(StartPoint, EndPoint, out hitInfo)) {
                    // Check self-intersection
                    var hitGrid = hitInfo.HitEntity.GetTopMostParent() as IMyCubeGrid;
                    if (hitGrid != null && MyAPIGateway.GridGroups.HasConnection(hitGrid as IMyCubeGrid, grid, GridLinkTypeEnum.Physical | GridLinkTypeEnum.NoContactDamage)) {
                        distanceToSurfaceCached = -1;
                    } else if (hitInfo.HitEntity as IMyCharacter != null) { // Check if we see a player
                        distanceToSurfaceCached = -1;
                    } else { // Setup resulting distance to surface
                        //PhysicsHelper.Draw(Color.Green, StartPoint, EndPoint);
                        distanceToSurfaceCached = Vector3D.Dot(NGravityDown, NRayDirection) * Math.Max(Vector3D.Distance(hitInfo.Position, StartPoint), 0.01);
                        prevRaycastPoint = currentRaycastPoint;
                        currentRaycastPoint = hitInfo.Position;
                        rayHitHappened = true;
                    }
                } else { // Raycast didn't hit anything, surface is too far away, no thrust here
                    distanceToSurfaceCached = -1;
                    prevRaycastPoint = currentRaycastPoint;
                    currentRaycastPoint = Vector3D.NegativeInfinity;
                }
            }
        }

        //fix for missing initailizing on DS in special case, maybe a keen prob
        public override void UpdateAfterSimulation100() {
            if (!MyAPIGateway.Utilities.IsDedicated) { showStateNextFrame = true; }
        }

        // Gamelogic update (each frame after simulation)
        public override void UpdateAfterSimulation() {
            if (block == null || block.MarkedForClose || block.Closed) return;

            if (!init) {
                Init(null);
                return;
            }

            if (MyAPIGateway.Session.isTorchServer()) return;

            if (!initShowState) {
                showstate();
                initShowState = true;
            }

            //fix for not changing color after welding
            if (showStateNextFrame) {
                showStateNextFrame = false;
                showstate();
            }

            if (!block.IsWorking) return; // no update if block down, off or damaged
        }

        //------------------------------------------ show state
        public void showstate() {
            if (block == null) return;

            if (!block.Enabled || !block.IsWorking) {
                block.SetEmissiveParts("emissive10", Color.Red, 0.0f);
                block.SetEmissiveParts("emissive11", Color.Red, 0.0f);
            } else {
                block.SetEmissiveParts("emissive10", m_color, 1.0f);
                block.SetEmissiveParts("emissive11", Color.DarkRed, 1.0f);
            }
        }

        static bool isHover(IMyTerminalBlock b) { return b.GetAs<HoverEngine>() != null; }

        public static void Buttons_AllBlock() {
            var prop = new HoverProperties(12, false);

            // Altitude Min slider control and action---------------------------
            var L_altitudemin = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyThrust>("Hover_MinHeight");
            L_altitudemin.Title = MyStringId.GetOrCompute("Altitude Min");
            L_altitudemin.Tooltip = MyStringId.GetOrCompute("minimum altitude at 0m/s speed");
            L_altitudemin.Writer = (b, t) => t.AppendFormat("{0:N1}", L_altitudemin.Getter(b)).Append(" m");
            L_altitudemin.SetLimits(0, prop.AltitudeMax);
            L_altitudemin.Getter = (b) => b.GetAs<HoverEngine>().heightTargetMax;
            L_altitudemin.Setter = (b, v) => { b.GetAs<HoverEngine>().SetHeight(v, L_altitudemin); };
            L_altitudemin.Enabled = isHover;
            L_altitudemin.Visible = isHover;

            var checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyThrust>("Hover_AutoAltitude");
            checkbox.Title = MyStringId.GetOrCompute("Auto altitude");
            checkbox.Tooltip = MyStringId.GetOrCompute("Preserve altitude at certain level automatically by thrust");
            checkbox.OnText = MyStringId.GetOrCompute("Hover_AutoAltitude_On");
            checkbox.OffText = MyStringId.GetOrCompute("Hover_AutoAltitude_Off");
            checkbox.Enabled = isHover;
            checkbox.Visible = isHover;
            checkbox.Getter = (b) => b.GetAs<HoverEngine>().autoHoverEnabled;
            checkbox.Setter = (block, value) => { block.GetAs<HoverEngine>().SetAutoHover(value, checkbox); };

            var altitudeMinActionInc = MyAPIGateway.TerminalControls.CreateAction<IMyThrust>("MinAlt_Increase");
            altitudeMinActionInc.Action = (b) => {
                HoverEngine HE = b.GetAs<HoverEngine>();
                if (HE != null) {
                    HE.SetHeight(HE.heightTargetMax > 4.0f ? HE.heightTargetMax + 1.0f : HE.heightTargetMax + 0.5f, null);
                }
            };
            altitudeMinActionInc.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            altitudeMinActionInc.Writer = (b, sb) => sb.Append(((double) b.GetAs<HoverEngine>().heightTargetMax).toHumanQuantity()).Append(" m");
            altitudeMinActionInc.Name = new StringBuilder("Min Altitude Increase");
            altitudeMinActionInc.Enabled = isHover;
            MyAPIGateway.TerminalControls.AddAction<IMyThrust>(altitudeMinActionInc);

            var altitudeMinActionDec = MyAPIGateway.TerminalControls.CreateAction<IMyThrust>("MinAlt_Decrease");
            altitudeMinActionDec.Action = (b) => {
                HoverEngine HE = b.GetAs<HoverEngine>();
                if (HE != null) {
                    HE.SetHeight(HE.heightTargetMax > 4.0f ? HE.heightTargetMax - 1.0f : HE.heightTargetMax - 0.5f, null);
                }
            };
            altitudeMinActionDec.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            altitudeMinActionDec.Name = new StringBuilder("Min Altitude Decrease");
            altitudeMinActionDec.Writer = (b, sb) => sb.Append(((double) b.GetAs<HoverEngine>().heightTargetMax).toHumanQuantity()).Append(" m");
            altitudeMinActionDec.Enabled = isHover;
            MyAPIGateway.TerminalControls.AddAction<IMyThrust>(altitudeMinActionDec);
            
            
            var AutoAlt_Toggle = MyAPIGateway.TerminalControls.CreateAction<IMyThrust>("AutoAlt_Toggle");
            AutoAlt_Toggle.Action = (b) => { b.GetAs<HoverEngine>().SetAutoHover(!b.GetAs<HoverEngine>().autoHoverEnabled, checkbox); };
            AutoAlt_Toggle.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            AutoAlt_Toggle.Name = new StringBuilder("Auto Altitude Toggle");
            AutoAlt_Toggle.Writer = (b, sb) => sb.Append(b.GetAs<HoverEngine>().autoHoverEnabled ? "On" : "Off");
            AutoAlt_Toggle.Enabled = isHover;
            MyAPIGateway.TerminalControls.AddAction<IMyThrust>(AutoAlt_Toggle);
            
            var AutoAlt_On = MyAPIGateway.TerminalControls.CreateAction<IMyThrust>("AutoAlt_On");
            AutoAlt_On.Action = (b) => { b.GetAs<HoverEngine>().SetAutoHover(true, checkbox); };
            AutoAlt_On.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            AutoAlt_On.Name = new StringBuilder("Auto Altitude On");
            AutoAlt_On.Writer = (b, sb) => sb.Append(b.GetAs<HoverEngine>().autoHoverEnabled ? "On" : "Off");
            AutoAlt_On.Enabled = isHover;
            MyAPIGateway.TerminalControls.AddAction<IMyThrust>(AutoAlt_On);
            
            var AutoAlt_Off = MyAPIGateway.TerminalControls.CreateAction<IMyThrust>("AutoAlt_Off");
            AutoAlt_Off.Action = (b) => { b.GetAs<HoverEngine>().SetAutoHover(false, checkbox); };
            AutoAlt_Off.Icon = @"Textures\GUI\Icons\Actions\SwitchOff.dds";
            AutoAlt_Off.Name = new StringBuilder("Auto Altitude Off");
            AutoAlt_Off.Writer = (b, sb) => sb.Append(b.GetAs<HoverEngine>().autoHoverEnabled ? "On" : "Off");
            AutoAlt_Off.Enabled = isHover;
            MyAPIGateway.TerminalControls.AddAction<IMyThrust>(AutoAlt_Off);

            var propertyDistanceToSurface = MyAPIGateway.TerminalControls.CreateProperty<double, IMyThrust>("DistanceToSurface");
            propertyDistanceToSurface.SupportsMultipleBlocks = false;
            propertyDistanceToSurface.Getter = (block) => { return block.GetAs<HoverEngine>().distanceToSurfaceCached; };
            MyAPIGateway.TerminalControls.AddControl<IMyThrust>(propertyDistanceToSurface);
            
            MyAPIGateway.TerminalControls.AddControl<IMyThrust>(L_altitudemin);
            MyAPIGateway.TerminalControls.AddControl<IMyThrust>(checkbox);
        }

        public void SetHeight(float v, IMyTerminalControlSlider view) {
            heightTargetMax = MathHelper.Clamp(v, 0, properties.AltitudeMax);
            Serialize();
            UpdateCustomData();
            if (view != null) view.UpdateVisual();
        }

        public void SetAutoHover(bool v, IMyTerminalControlCheckbox view) {
            autoHoverEnabled = v;
            if (!autoHoverEnabled) {
                block.ThrustOverridePercentage = 0f; // set override to very small value to prevent automatic dampening by defauly SE dampening system
            }
            Serialize();
            UpdateCustomData();
            try
            {
                view.UpdateVisual();
            } catch (Exception e) {  }
        }

}


    public class HoverProperties {
        public static Color[] Colors = new Color[] {new Color(50, 255, 50, 255), new Color(10, 255, 10, 255), new Color(0, 255, 0, 255), new Color(80, 150, 175, 255), new Color(10, 32, 40, 255), new Color(5, 15, 20, 255), new Color(0, 50, 255, 255), new Color(0, 12, 255, 255), new Color(0, 0, 255, 255), new Color(255, 255, 50, 255), new Color(255, 255, 12, 255), new Color(255, 255, 0, 255),};

        public static Dictionary<String, HoverProperties> Levels = new Dictionary<string, HoverProperties>() {
             {"SmallBlockV1T1", new HoverProperties(1, true)},
             {"SmallBlockV2T1", new HoverProperties(1, true)},
             {"LargeBlockV1T1", new HoverProperties(1, false)},
             {"LargeBlockV2T1", new HoverProperties(1, false)},
             {"SmallBlockV1T2", new HoverProperties(2, true)},
             {"SmallBlockV2T2", new HoverProperties(2, true)},
             {"LargeBlockV1T2", new HoverProperties(2, false)},
             {"LargeBlockV2T2", new HoverProperties(2, false)},
             {"SmallBlockV1T3", new HoverProperties(3, true)},
             {"SmallBlockV2T3", new HoverProperties(3, true)},
             {"LargeBlockV1T3", new HoverProperties(3, false)},
             {"LargeBlockV2T3", new HoverProperties(3, false)},
             {"SmallBlockV1T4", new HoverProperties(4, true)},
             {"SmallBlockV2T4", new HoverProperties(4, true)},
             {"LargeBlockV1T4", new HoverProperties(4, false)},
             {"LargeBlockV2T4", new HoverProperties(4, false)},
             {"SmallBlockV1T5", new HoverProperties(5, true)},
             {"SmallBlockV2T5", new HoverProperties(5, true)},
             {"LargeBlockV1T5", new HoverProperties(5, false)},
             {"LargeBlockV2T5", new HoverProperties(5, false)},
             {"SmallBlockV1T6", new HoverProperties(6, true)},
             {"SmallBlockV2T6", new HoverProperties(6, true)},
             {"LargeBlockV1T6", new HoverProperties(6, false)},
             {"LargeBlockV2T6", new HoverProperties(6, false)},
             {"SmallBlockV1T7", new HoverProperties(7, true)},
             {"SmallBlockV2T7", new HoverProperties(7, true)},
             {"LargeBlockV1T7", new HoverProperties(7, false)},
             {"LargeBlockV2T7", new HoverProperties(7, false)},
             {"SmallBlockV1T8", new HoverProperties(8, true)},
             {"SmallBlockV2T8", new HoverProperties(8, true)},
             {"LargeBlockV1T8", new HoverProperties(8, false)},
             {"LargeBlockV2T8", new HoverProperties(8, false)},
             {"SmallBlockV1T9", new HoverProperties(9, true)},
             {"SmallBlockV2T9", new HoverProperties(9, true)},
             {"LargeBlockV1T9", new HoverProperties(9, false)},
             {"LargeBlockV2T9", new HoverProperties(9, false)},
             {"SmallBlockV1T10", new HoverProperties(10, true)},
             {"SmallBlockV2T10", new HoverProperties(10, true)},
             {"LargeBlockV1T10", new HoverProperties(10, false)},
             {"LargeBlockV2T10", new HoverProperties(10, false)},
             {"SmallBlockV1T11", new HoverProperties(11, true)},
             {"SmallBlockV2T11", new HoverProperties(11, true)},
             {"LargeBlockV1T11", new HoverProperties(11, false)},
             {"LargeBlockV2T11", new HoverProperties(11, false)},
             {"SmallBlockV1T12", new HoverProperties(12, true)},
             {"SmallBlockV2T12", new HoverProperties(12, true)},
             {"LargeBlockV1T12", new HoverProperties(12, false)},
             {"LargeBlockV2T12", new HoverProperties(12, false)},
         };

        public Color Color;
        public float AltitudeMax;


        public HoverProperties(int level, bool isSmall) {
            AltitudeMax = 20f + (float) Math.Pow(1.3, level - 1);
            if (isSmall) { AltitudeMax *= 0.8f; }
            Color = Colors[level - 1];
        }
    }
}