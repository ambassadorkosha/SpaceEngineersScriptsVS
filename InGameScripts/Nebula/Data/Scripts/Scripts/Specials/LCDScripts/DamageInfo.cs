using System.Collections.Generic;
using System.Text;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scripts.Specials.LCDScripts
{
    [MyTextSurfaceScript("DamageInfoNebula", "Damage Info Nebula")]
    class DamageInfo : MyTSSCommon
    {
        private float Damage;
        private float TempDamage;
        private float InDPS;
        private float DPS;

        private int i;
        private bool upd;
        private readonly Vector2 Size;
        private readonly Vector2 TextSize;

        public DamageInfo(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            var m_sb = new StringBuilder();
            m_sb.Append("[");
            TextSize = surface.MeasureStringInPixels(m_sb, m_fontId, m_fontScale);
            Size = surface.SurfaceSize;
            MyAPIGateway.Session.DamageSystem.RegisterAfterDamageHandler(11, DamageCollector);
        }

        private void DamageCollector(object target, MyDamageInformation damage)
        {
            TempDamage += damage.Amount;
            upd = true;
        }
        
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        public override void Run()
        {
            using (var frame = m_surface.DrawFrame())
            {
                if (upd)
                {
                    Damage = TempDamage;
                    TempDamage = 0;
                    InDPS += Damage;
                    upd = false;
                }
                i++;
                if (i >= 6) 
                {
                    if ((int) InDPS != 0) DPS = InDPS; 
                    InDPS = i = 0;
                }

                var SizeDif = Size.Y / 512;
                var DPSSprite = MySprite.CreateText("Damage: " + Damage + "\n\n" + "DPS: " + DPS, "Monospace", m_foregroundColor,  m_fontScale * SizeDif);
                DPSSprite.Position = new Vector2(Size.X / 2, Size.Y / 2 - TextSize.Y);
                frame.Add(DPSSprite);
            }
        }
    }
}