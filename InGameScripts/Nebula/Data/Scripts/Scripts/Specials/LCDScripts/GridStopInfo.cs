using System;
using System.Collections.Generic;
using Digi;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scripts.Specials {
    //AUTHOR: Stridemann
    [MyTextSurfaceScriptAttribute("GridStopInfo", "Grid Stopping Information")]
    public class GridStopInfo : MyTSSCommon {
        private const int THRUSTER_BLOCKS_UPDATE_EACH_N_FRAMES = 60 * 2 / 10;
        private readonly IMyCubeBlock _cubeBlock;
        private readonly IMyTextSurface _lcd;
        private readonly Vector2 _screenSize;
        private int _blocksUpdateCounter;
        private IMyCockpit _cockpit;
        private List<IMyThrust> _thrusters;

        public GridStopInfo(IMyTextSurface surface, IMyCubeBlock block, Vector2 screenSize) : base(surface, block, screenSize) {
            _screenSize = screenSize;
            _cubeBlock = block;
            _lcd = surface;

            try {
                UpdateThrusterBlocks();
            } catch (Exception e) {
                Log.Error(e);
            }
            
        }

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        public override void Run() {
            base.Run();
            try {
                if (++_blocksUpdateCounter >= THRUSTER_BLOCKS_UPDATE_EACH_N_FRAMES) {
                    _blocksUpdateCounter = 0;
                    UpdateThrusterBlocks();
                }

                var info = string.Empty;

                if (_cockpit != null && _thrusters.Count > 0) info = GetStoppingInfo();
                else if (_cockpit == null) info = "No cockpit";
                else if (_thrusters.Count == 0) info = "No thrusters";

                var minScreenSide = Math.Min(_screenSize.X, _screenSize.Y);

                var viewRectangle = new RectangleF((m_surface.TextureSize - m_surface.SurfaceSize) / 2 + Vector2.One * 5, m_surface.SurfaceSize);

                using (var frame = m_surface.DrawFrame()) {
                    var sprite = MySprite.CreateText(info, "Monospace", m_foregroundColor, minScreenSide / 500, TextAlignment.LEFT);

                    sprite.Size = new Vector2(512f, 80f);
                    sprite.Position = new Vector2(viewRectangle.X + 10, viewRectangle.Y + 10);

                    frame.Add(sprite);
                }
            } catch (Exception e) {
                Log.Error(e);
            }
        }

        private string GetStoppingInfo() {
            _lcd.WriteText(string.Empty);

            if (_cockpit.CubeGrid.IsStatic) return "Grid is Station";

            var linearVelocity = _cockpit.GetShipVelocities().LinearVelocity;

            if (linearVelocity.Length() < 0.1) return "Stopped";

            if (!_cockpit.DampenersOverride) return "Dampeners disabled!";

            var gravityVector = _cockpit.GetNaturalGravity();
            var totalThrustInDirection = GetTotalThrustInDirection(-linearVelocity);

            var mass = _cockpit.CalculateShipMass().PhysicalMass;

            //how much the gravity affect stopping
            var gravityDot = Vector3D.Normalize(gravityVector).Dot(Vector3D.Normalize(-linearVelocity));

            //subtracting the force needed for counteraction to gravity
            totalThrustInDirection += gravityDot * mass * gravityVector.Length();

            if (totalThrustInDirection < 0) return "No way to stop this :(";

            var speed = linearVelocity.Length();
            var stoppingTime = speed * mass / totalThrustInDirection;
            var stopDistance = mass * (speed * speed) / (2 * totalThrustInDirection);

            return $"Stop time: {stoppingTime:F0} s{Environment.NewLine}Distance : {FormatDist(stopDistance)}";
        }

        private void UpdateThrusterBlocks() {
            var thrusterSlimBlocks = new List<IMySlimBlock>();
            _cubeBlock.CubeGrid.GetBlocks(thrusterSlimBlocks, block => block?.FatBlock is IMyTerminalBlock);
            _thrusters = new List<IMyThrust>();
            _cockpit = _cubeBlock as IMyCockpit; //can be cockpit or PB

            foreach (var myTerminalBlock in thrusterSlimBlocks) {
                var terminalBlock = myTerminalBlock.FatBlock as IMyTerminalBlock;

                if (terminalBlock != null) {
                    var truster = terminalBlock as IMyThrust;

                    if (truster != null) _thrusters.Add(truster);
                    else if (_cockpit == null) _cockpit = terminalBlock as IMyCockpit;
                }
            }
        }

        /// <summary>
        /// Calculates the total thrust grid can produce in defined direction.
        /// </summary>
        private double GetTotalThrustInDirection(Vector3D direction) {
            var thrust = 0.0;
            direction.Normalize();
            var invDirection = -direction;

            foreach (var t in _thrusters) {
                if (t.IsWorking && t.ThrustOverridePercentage == 0) {
                    var projection = t.WorldMatrix.Forward.Dot(invDirection);

                    if (projection > 0) thrust += t.MaxEffectiveThrust * projection;
                }
            }

            return thrust;
        }

        private static string FormatDist(double distance) {
            var distLong = (long) distance;
            var km = distLong / 1000;

            if (km > 1000) return $"{km:F0} km";

            if (km > 0) {
                var m = distance % 1000 / 100;
                return $"{km:F0}.{(int) m} km";
            }

            return $"{distLong} m";
        }
    }
}