using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using Digi;
using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Scripts;
using Scripts.Specials;
using VRage.Game;
using Scripts.Specials.Blocks.StackableMultipliers;

namespace ServerMod.Specials {

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), true, new string[] {"AfterburnerSmall", "AfterburnerLarge"})]
    public class Afterburner2 : LimitedOnOffBlock {
        private static Dictionary<int, int> AFTERBURNER = LimitsChecker.From(LimitsChecker.TYPE_WEAPONPOINTS, 5, LimitsChecker.TYPE_AFTERBURNER, 1);
        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            base.Init(objectBuilder);
            SetOptions(AFTERBURNER, false);
        }

        public override bool IsDrainingPoints()
        {
            return true;
        }
    }
    
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), true, new string[] {"AfterburnerSmall", "AfterburnerLarge"})]
    public class Afterburner3 : EMPEffectOnOff { }


    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), true, new string[] { "AfterburnerSmall", "AfterburnerLarge" })]
    public class Afterburner : CooldownBlockOnOff  {
        protected override void InitDuration() {
            activeDuration = 1800;
            cooldownDuration = 5400;
        }

		private List<MultiplierEffect> effects = new List<MultiplierEffect>();

		public void Logic(bool enabled) {
            try {

				if (!enabled)
				{
					foreach (var x in effects) {
						x.RemoveEffect();
					}
					effects.RemoveAll((x)=>true);
					return;
				}

                var ship = myBlock.CubeGrid.GetShip();
                if (ship != null) {
                    float scale = (ship.grid.GridSizeEnum == MyCubeSize.Small ? 0.3f : 1f) * 4.5f;

                    foreach (var v in ship.thrusters)
                    {
                        if (v.CustomName.Contains("[NoAfterburn]")) continue;

						var effect = new WhileOnMultiplierEffect(0, myBlock.EntityId, v.EntityId, 3f, 7f);
						effects.Add (effect);
						v.GetAs<ThrusterBase>().AddEffect (effect);

						if (!MyAPIGateway.Session.isTorchServer())
						{
							var scale2 = scale * (v.SlimBlock.BlockDefinition as MyCubeBlockDefinition).Size.AbsMin();
							var sn = v.SlimBlock.BlockDefinition.Id.SubtypeName;
							if (!sn.Contains("Heli") && !sn.Contains("Hover"))
							{
								ParticleDispatcher.AddEffect(new Particle()
								{
									time = activeDuration + 6 * 60,
									ttp = activeDuration,
									scale = scale2,
									effectId = "Afterburner",
									translation = v.WorldMatrix.Translation,
									forward = v.WorldMatrix.Forward,
									up = v.WorldMatrix.Up,
									entityId = v.EntityId,
									specialType = Particle.SPECIAL_AFTERBURNER,
									color = (v.SlimBlock.BlockDefinition as MyThrustDefinition).FlameFullColor,
									disableOnBlockOff = true
								});
							}
						}
                    }
                }
            } catch (Exception e) {
                Log.Error(e);
            }
        }
        
        public override void OnActivated() {
            base.OnActivated();
            Logic(true);
        }

        public override void OnCooldown() {
            base.OnCooldown();
            Logic(false);
        }

        public override bool CheckCanTickTimer()
        {
            return base.CheckCanTickTimer() && myBlock.IsFunctional;
        }
    }
}
