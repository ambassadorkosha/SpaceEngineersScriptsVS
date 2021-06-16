using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Scripts.Shared;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Scripts.Specials.Systems
{
    public class HungerGame
    {
        public MyStringId SQUARE = MyStringId.GetOrCompute("Square");

        public HungerGameParams gameParams = new HungerGameParams();
        public HungerGameSettings gameSettings = new HungerGameSettings();


        public HungerGame()
        {

        }

        public void Tick()
        {
            if (gameParams.DamageSphere.Radius > gameParams.NoneDamageSphere.Radius)
            {
                gameParams.DamageSphere.Radius -= 0.16f*3;
            }
            

            if (FrameExecutor.currentFrame % 10 == 0)
            {
                ships.Clear();
                chars.Clear();

                GetAllInSphere(gameSettings.GameSphere, ships, chars, true);
                GetAllInSphere(gameParams.DamageSphere, ships, chars, false);

                foreach (var x in chars)
                {
                    x.DoDamage(0.1f, MyDamageType.Radioactivity, true);
                }

                foreach (var x in ships)
                {
                    var list = new List<IMySlimBlock>();
                    x.grid.GetBlocks(list);
                    foreach (var y in list)
                    {
                        var dmg = y.MaxIntegrity / 100;
                        y.DoDamage(dmg, MyDamageType.Radioactivity, true);
                    }
                }
            }
        }




        public void Draw()
        {
            //if (gameParams.State != HungerGameState.WRONG_STATE)
            {
                if (gameParams.DamageSphere != gameSettings.GameSphere)
                {
                    var c1 = Color.Blue * 0.7f;
                    var zzz = MatrixD.CreateWorld(gameParams.DamageSphere.Center, Vector3D.Forward, Vector3D.Up);
                    VRage.Game.MySimpleObjectDraw.DrawTransparentSphere(ref zzz, (float)gameParams.DamageSphere.Radius, ref c1, VRage.Game.MySimpleObjectRasterizer.Solid, 60, SQUARE);
                }

                //if (gameParams.State == HungerGameState.RUNNING_SPHERE_STATIC || gameParams.State == HungerGameState.RUNNING_SPHERE_RESIZING)
                {
                    if (gameParams.NoneDamageSphere != gameParams.DamageSphere && gameParams.DamageSphere != gameSettings.GameSphere)
                    {
                        var c2 = Color.White * 0.7f;
                        var zzz2 = MatrixD.CreateWorld(gameParams.NoneDamageSphere.Center, Vector3D.Forward, Vector3D.Up);
                        VRage.Game.MySimpleObjectDraw.DrawTransparentSphere(ref zzz2, (float)gameParams.NoneDamageSphere.Radius, ref c2, VRage.Game.MySimpleObjectRasterizer.Solid, 60, SQUARE);
                    }
                }

                var c3 = Color.Red * 0.2f;
                var zzz3 = MatrixD.CreateWorld(gameSettings.GameSphere.Center, Vector3D.Forward, Vector3D.Up);
                VRage.Game.MySimpleObjectDraw.DrawTransparentSphere(ref zzz3, (float)gameSettings.GameSphere.Radius, ref c3, VRage.Game.MySimpleObjectRasterizer.Solid, 60, SQUARE);
            }
        }

        List<Ship> ships = new List<Ship>();
        List<IMyCharacter> chars = new List<IMyCharacter>();

        public void Prepare()
        {
            ships.Clear();
            chars.Clear();
            GetAllInSphere(gameSettings.GameSphere, ships, chars, true);
            GetAllInSphere(gameParams.DamageSphere, ships, chars, false);

            foreach (var sh in ships)
            {
                foreach (var cargo in sh.CargoBoxes)
                {
                    //var treasure = cargo.GetAs<TreasureChest>()
                    //if ()
                }
            }
        }

        private void GetAllInSphere(BoundingSphereD sphere, List<Ship> ships, List<IMyCharacter> chars, bool add)
        {
            foreach (var ship in GameBase.instance.gridToShip)
            {
                if (sphere.Contains(ship.Value.grid.WorldAABB) != ContainmentType.Disjoint)
                {
                    if (add) ships.Add(ship.Value);
                    else ships.Remove(ship.Value);
                }
            }

            foreach (var ch in GameBase.instance.characters)
            {
                if (!ch.Value.IsDead && sphere.Contains(ch.Value.WorldMatrix.Translation) != ContainmentType.Disjoint)
                {
                    if (add) chars.Add(ch.Value);
                    else chars.Remove(ch.Value);
                }
            }
        }
    }
}
