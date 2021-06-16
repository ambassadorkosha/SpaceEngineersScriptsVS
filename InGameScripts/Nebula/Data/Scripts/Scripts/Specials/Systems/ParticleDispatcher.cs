using System;
using System.Collections.Generic;
using Digi;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game;
using Slime;
using ProtoBuf;
using Sandbox.Definitions;
using Scripts.Shared;
using ServerMod.Specials;
using VRage.ModAPI;
using VRageMath;
using Scripts.Specials.Messaging;
using Scripts.Specials.Blocks.StackableMultipliers;

namespace ServerMod {
    
    [ProtoContract]
    public class Particle {
        [ProtoMember(1)] public int time = Int32.MaxValue;
        [ProtoMember(2)] public Vector3D translation;
        [ProtoMember(3)] public Vector3D forward;
        [ProtoMember(4)] public Vector3D up;
        [ProtoMember(5)] public Vector3D position;
        [ProtoMember(6)] public Vector3 velocity;
        [ProtoMember(7)] public float scale = 1.0f;
        [ProtoMember(8)] public float scaleAdd;
        [ProtoMember(9)] public String effectId = "Smoke_DrillDust_Ice";
        [ProtoMember(10)] public Vector4 color = Vector4.One;
        [ProtoMember(11)] public long entityId;
        [ProtoMember(12)] public int showT = 0;
        [ProtoMember(13)] public int hideT = 0;
        [ProtoMember(14)] public int specialType = 0;
        [ProtoMember(15)] public int ttp = Int32.MaxValue; //Time to Pause
        [ProtoMember(16)] public bool disableOnBlockOff = false;
        public const int SPECIAL_AFTERBURNER = 1; 
        
        internal int ttl = 0; //Time to Pause
        
        //[ProtoMember(11)] public bool loopable = false;

        public MyParticleEffect effect;
        
        public MyParticleEffect createEffect() {
            if (effect == null) {
                MyParticleEffect eff;
                var m = MatrixD.CreateWorld(translation, forward, up);
                if (!MyParticlesManager.TryCreateParticleEffect(effectId, ref m, ref position, uint.MaxValue, out eff)) {
                    Log.ChatError("Couldn't create effect:"+effectId);
                    return null;
                }
                effect = eff;
                ttl = time;
                //effect.Loop = loopable;
                effect.Velocity = velocity;
                effect.UserScale = scale;
                effect.UserColorMultiplier = color;
            }
            return effect;
        }

        public static Random random = new Random ();

        public void tick() {
            var eee = effect;
            ttl--;
            ttp--;
            scale += scaleAdd;


            IMyEntity ent = null;
            if (entityId != 0) {
				ent = entityId.As<IMyEntity>();
                if (ent != null) {
					var wm = ent.WorldMatrix;
                    eee.WorldMatrix = MatrixD.CreateWorld(wm.Translation, wm.Forward, wm.Up);
					if (specialType != 0) {
                        switch (specialType) {
                            case SPECIAL_AFTERBURNER: {
                                var th = ent as IMyThrust;
                                eee.WorldMatrix = MatrixD.CreateWorld(wm.Translation+wm.Forward*(th.SlimBlock.BlockDefinition as MyCubeBlockDefinition).Size.Z * th.CubeGrid.GridSize/2, wm.Forward, wm.Up);
                                //PhysicsHelper.Draw(Color.Aqua, eee.WorldMatrix.Translation, wm.Forward, 0.3f);
                                break;
                            }
                        }
                    } else {
						eee.WorldMatrix = MatrixD.CreateWorld(ent.WorldMatrix.Translation, ent.WorldMatrix.Forward, ent.WorldMatrix.Up);
                    }
                }   else {
                    Die();
                    return;
                }
            }



			if (eee == null) return;
            eee.UserScale = scale;

			var extraAlpha = 1f;
            bool stopEmmiting = false;


            if (disableOnBlockOff)
            {
                var block = (ent as IMyFunctionalBlock);
                if (!block.Enabled)
                {
                    stopEmmiting = true;
                }
            }

            if (specialType != 0) {
                switch (specialType) {
                    case SPECIAL_AFTERBURNER: {
                        var thruster = (ent as IMyThrust);
						var tb = thruster.GetAs<ThrusterBase>();
                        if (!tb.HasEffect(0))
                        {
                            stopEmmiting = true;
                        } else if (thruster.CurrentThrust <= 0.05f * thruster.MaxThrust) {
                            stopEmmiting = true;
                        } else {
                            if (thruster.MaxEffectiveThrust > 0)
                            {
                                var v = thruster.CurrentThrust / thruster.MaxEffectiveThrust;
                                if (random.NextDouble() > v)
                                {
                                    stopEmmiting = true;
                                }
                            }
                        }
                        break;
                    }
                }
            }

			var specialAlpha = 1f; //hideT == 0 ? (hide ? 0 : 1) : (float)showHideTimer / hideT;
            
            if (ttl < hideT) {
                specialAlpha *= (float)ttl / hideT;
            } else if (time - showT < ttl) {
                specialAlpha *= (float)(time - ttl) / showT;
            }

			if (specialAlpha != 1f) {
                eee.UserColorMultiplier = color * new Vector4 (specialAlpha,specialAlpha,specialAlpha,specialAlpha);
            } else {
                eee.UserColorMultiplier = color;
            }

			if (stopEmmiting || ttp <=0) {
                if (!effect.IsEmittingStopped) { effect.StopEmitting(); }
            } else {
                if (effect.IsEmittingStopped) { effect.Play(); }
            }
		}

        public bool NeedDie() {
            return ttl <= 0;
        }

        public bool Die() {
            ttl = 0;
            effect?.Stop();
            return true;
        }
    }
    
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class ParticleDispatcher : MySessionComponentBase {
        public const ushort HANDLER = 36603;
        private static List<Particle> particles = new List<Particle>();

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent) {
            base.Init(sessionComponent);
            if (!MyAPIGateway.Session.isTorchServer()) {
                MyAPIGateway.Multiplayer.RegisterMessageHandler(HANDLER, HandleData);
            }
        }

        protected override void UnloadData() {
            if (!MyAPIGateway.Session.isTorchServer()) {
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(HANDLER, HandleData);
            }
        }

        private void HandleData(byte[] obj) {
            List<Particle> data;
            try { 
                data = MyAPIGateway.Utilities.SerializeFromBinary<List<Particle>>(obj);
            } catch (Exception e) {
                return;
            }

            foreach (var x in data) {
                AddEffect(x);
            }
        }
       
        
        
        public static void AddEffect(Particle particle) {
            if (!MyAPIGateway.Session.isTorchServer()) {
                particles.Add(particle);
                particle.createEffect();
            }
            
        }
        
        public static void AddEffect(List<Particle> particle) {
            if (!MyAPIGateway.Session.isTorchServer()) {
                foreach (var p in particle) {
                    particles.Add(p);
                    p.createEffect();
                }
            }
        }

        public static void AddEffectToOthers(Particle particle) {
            MyAPIGateway.Multiplayer.SendMessageToOthersProto(HANDLER, new List<Particle>() {particle});
        }
        
        public static void AddEffectToOthers(List<Particle> particles) {
            MyAPIGateway.Multiplayer.SendMessageToOthersProto(HANDLER, particles);
        }
        
        public override void Draw() {
            base.Draw();
            if (MyAPIGateway.Session.isTorchServer()) return;

			try { 
				foreach (var x in particles) {
					x.tick();
				}
			} catch (Exception e) {

			}
            //if (x.ttl > x.scale) w

            particles.RemoveAll(particle => particle.NeedDie() && particle.Die());
        }
        
        public static List<String> effects = new List<string>()
            {
                "DEL_ParticleEffect",
                "Damage_Electrical_Damaged_Antenna",
                "Damage_Electrical_Damaged_SolarPanels", //Damage_Electrical_Damaged_SolarPanels
                "Damage_Electrical_Damaged",
                "Damage_GravGen_Damaged", //
                "Damage_HeavyMech_Damaged",
                "Damage_Reactor_Damaged",
                "OxyLeakLarge",
                "OxyVent",
                "Meteory_Fire_Atmosphere",
                "Meteory_Fire_Space",
                "Shell_Casings",
                "Muzzle_Flash",
                "MaterialHit_Rock",
                "AlienGreenGrass",
                "CharacterStepAlienYellow",
                "CharacterStepGrassAlienOrange",
                "CharacterStepFlora",
                "CharacterStepIce",
                "CharacterStepGrassOld",
                "CharacterStepMarsSoil",
                "CharacterStepSoil",
                "CharacterStepMoonSoil",
                "CharacterStepSand",
                "CharacterStepSnow",
                "MaterialHit_GrassGreen",
                "MaterialHit_Ice",
                "MaterialHit_Sand",
                "MaterialHit_MoonSoil",
                "MaterialHit_MarsSoil",
                "MaterialHit_Soil",
                "MaterialHit_Metal",
                "MaterialHit_Wood",
                "MaterialHit_Snow",
                "MaterialHit_Glass",
                "MaterialHit_GrassOrange",
                "MaterialHit_AlienGreenGrass",
                "MaterialHit_AlienYellowGrass",
                "MaterialHit_GrassYellow",
                "Grinder_Character",
                "Dummy",
                "Warp",
                "Hit_BasicAmmoSmall",
                "MaterialHit_Character",
                "Blood_Spider",
                "Tree_Drill",
                "MaterialHit_Metal_GatlingGun",
                "MaterialHit_Rock_GatlingGun",
                "Tree Destruction",
                "Damage_WeapExpl_Damaged",
                "WheelDust_GrassGreen",
            };
       
    }
}