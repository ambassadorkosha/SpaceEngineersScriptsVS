using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Digi;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using ServerMod;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

namespace Scripts.Specials.LCDScripts
{
  [MyTextSurfaceScript("TSS_EnergyHydrogen", "Energy and Fuel Nebula")]
  public class MyTSSEnergyHydrogenNebula : MyTSSCommon
  {
    private const float ASPECT_RATIO = 3f;
    private const float DECORATION_RATIO = 0.25f;
    private const float TEXT_RATIO = 0.25f;
    private const string ENERGY_ICON = "IconEnergy";
    private const string HYDROGEN_ICON = "IconHydrogen";
    private readonly StringBuilder m_sb = new StringBuilder();
    private HashSet<IMyGasTank> m_TotalTanks = new HashSet<IMyGasTank>();
    private HashSet<IMyPowerProducer> m_PowerProducers = new HashSet<IMyPowerProducer>();
    private readonly List<IMyGasTank> m_HydTankBlocks = new List<IMyGasTank>();
    private readonly List<IMyGasTank> m_KeroTankBlocks = new List<IMyGasTank>();
    private readonly List<IMyGasTank> m_OilTankBlocks = new List<IMyGasTank>();
    private readonly List<IMyBatteryBlock> m_Battery = new List<IMyBatteryBlock>();
    
    private readonly Vector2 m_innerSize;
    private readonly float m_firstLine;
    private readonly float m_secondLine;
    private readonly float m_thirdLine;
    private readonly float m_fourthLine;
    private readonly MyCubeGrid m_grid;
    private float m_maxHydrogen;
    private float m_maxKerosene;
    private float m_MaxStoredPower;
    private float m_maxPowerOutput;
    private float m_maxOil;

    private float LargestRechargeTime;
    private float LastStoredPower;

    private float LastStoredHydrogen;
    private float LastStoredKerosene;
    private float LastStoredOil;
    private readonly Color barBgColor;
    private readonly float x,num1;
    private readonly Vector2 vector_text,vector_image;
    
    private Color DopInfoColor;

    public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update100;

    public MyTSSEnergyHydrogenNebula(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
    {
      m_innerSize = new Vector2(ASPECT_RATIO, 1f);
      FitRect(surface.SurfaceSize, ref m_innerSize);
      var mDecorationSize = new Vector2(0.012f * m_innerSize.X, DECORATION_RATIO * m_innerSize.Y);
      m_sb.Clear();
      m_sb.Append("Power Usage: 00.000");
      var vector2 = surface.MeasureStringInPixels(m_sb, m_fontId, 1f);
      var val2 = TEXT_RATIO * m_innerSize.Y / vector2.Y;
      m_fontScale = Math.Min(m_innerSize.X * 0.72f / vector2.X, val2);
      m_firstLine = m_halfSize.Y - (mDecorationSize.Y * 0.35f)*3.125f;
      m_secondLine = m_halfSize.Y - (mDecorationSize.Y * 0.35f);
      m_thirdLine = m_halfSize.Y + (mDecorationSize.Y * 0.35f);
      m_fourthLine = m_halfSize.Y + (mDecorationSize.Y * 0.35f)*3.125f;
      barBgColor = new Color(m_foregroundColor, 0.1f);
      x = m_innerSize.X * 0.5f;
      num1 = x * 0.06f;
      m_sb.Clear();
      m_sb.Append("[");
      vector_text = m_surface.MeasureStringInPixels(m_sb, m_fontId, m_fontScale);
      vector_image = m_surface.MeasureStringInPixels(m_sb, m_fontId, mDecorationSize.Y / m_surface.MeasureStringInPixels(m_sb, m_fontId, 1f).Y);
      if (m_block == null) return;
      m_grid = m_block.CubeGrid as MyCubeGrid;
    }

    public override void Run()
    {
      base.Run();
      
      if (m_grid?.GetShip() == null) return;
      RecalculateGas();
      RecalculateBat();

      DopInfoColor = new Color(255 - m_foregroundColor.R, 255 - m_foregroundColor.G, 255 - m_foregroundColor.B);
      
      
      using (var frame = m_surface.DrawFrame())
      {
        AddBackground(frame, new Color(m_backgroundColor, 0.66f));
        
        if (m_OilTankBlocks.Count != 0)
        {
          DrawEnergy(frame, m_firstLine);
          DrawOil(frame,m_secondLine);
        }
        else if(m_TotalTanks.Count == 0) DrawEnergy (frame, m_halfSize.Y);
        else DrawEnergy(frame, m_secondLine);
        
        if (m_KeroTankBlocks.Count != 0 && m_HydTankBlocks.Count != 0)
        {
          DrawHydrogen(frame, m_thirdLine);
          DrawKerosene(frame, m_fourthLine);
        }
        else if (m_HydTankBlocks.Count != 0) DrawHydrogen(frame, m_thirdLine);
        else if (m_KeroTankBlocks.Count != 0) DrawKerosene(frame, m_thirdLine);

        AddBrackets(frame, new Vector2(64f, 256f), (float) (m_innerSize.Y / 256.0 * 0.899999976158142), (float) ((m_size.X - (double) m_innerSize.X) / 2.0));
      }
    }
    private void DrawEnergy(MySpriteDrawFrame frame, float Line)
    {
      frame.Add(new MySprite(SpriteType.TEXTURE, ENERGY_ICON, new Vector2(m_halfSize.X - x * 0.6f - num1, Line), new Vector2(vector_text.Y), m_foregroundColor));
      
      var CurrentStoredPower = m_Battery.Sum(bat => bat.CurrentStoredPower);
      var TotalBatRatio = m_MaxStoredPower > 0.0 ? CurrentStoredPower / m_MaxStoredPower : 0.0f;
      AddProgressBar(frame, new Vector2(m_halfSize.X - num1, Line - (vector_image.Y * 0.3f)/1.5f), new Vector2(x, vector_image.Y * 0.1f/2), TotalBatRatio, barBgColor, new Color(0, 200, 0));
      AddProgressBar(frame, new Vector2(m_halfSize.X - num1, Line + (vector_image.Y * 0.3f)/1.5f), new Vector2(x, vector_image.Y * 0.1f/2), TotalBatRatio, barBgColor, new Color(0, 200, 0));

      var PowRatio = m_PowerProducers.Sum(PProd => PProd.CurrentOutput);
      var TotalPowRatio = (double) m_maxPowerOutput > 0.0 ? PowRatio / m_maxPowerOutput : 0.0f;
      AddProgressBar(frame, new Vector2(m_halfSize.X - num1, Line), new Vector2(x, vector_image.Y * 0.3f), TotalPowRatio, barBgColor, m_foregroundColor);

      LastStoredPower = DrawCentralText(frame, Line, CurrentStoredPower, LastStoredPower, m_MaxStoredPower, true, LargestRechargeTime);

      frame.Add(new MySprite(SpriteType.TEXT, $"{TotalPowRatio * 100.0:0}",new Vector2(m_halfSize.X + x * 0.6f - num1, Line - vector_text.Y * 0.5f),new Vector2(m_innerSize.X, m_innerSize.Y),m_foregroundColor,m_fontId,TextAlignment.LEFT,m_fontScale*0.9f));
    }
    private void DrawOil(MySpriteDrawFrame frame, float Line)
    {
      var OilRatio = m_OilTankBlocks.Sum(tankBlock => (float) tankBlock.FilledRatio * tankBlock.Capacity);
      var TotalOilRatio = (double) m_maxOil > 0.0 ? OilRatio / m_maxOil : 0.0f;
      frame.Add(new MySprite(SpriteType.TEXT, "Oil", new Vector2(m_halfSize.X - x * 0.6f - num1, Line - vector_text.Y * 0.5f), new Vector2(m_innerSize.X, m_innerSize.Y), m_foregroundColor,m_fontId,TextAlignment.CENTER,m_fontScale*0.8f));
      AddProgressBar(frame, new Vector2(m_halfSize.X - num1, Line), new Vector2(x, vector_image.Y * 0.4f), TotalOilRatio, barBgColor, m_foregroundColor);
      LastStoredOil = DrawCentralText(frame, Line, OilRatio,LastStoredOil,m_maxOil);
      frame.Add(new MySprite(SpriteType.TEXT,$"{TotalOilRatio * 100.0:0}",new Vector2(m_halfSize.X + x * 0.6f - num1, Line - vector_text.Y * 0.5f),new Vector2(m_innerSize.X, m_innerSize.Y),m_foregroundColor,m_fontId,TextAlignment.LEFT,m_fontScale*0.9f));
    }
    private void DrawHydrogen(MySpriteDrawFrame frame, float Line)
    {
      var HydRation = m_HydTankBlocks.Sum(tankBlock => (float) tankBlock.FilledRatio * tankBlock.Capacity);
      var TotalHydRatio = (double) m_maxHydrogen > 0.0 ? HydRation / m_maxHydrogen : 0.0f;
      frame.Add(new MySprite(SpriteType.TEXTURE, HYDROGEN_ICON, new Vector2(m_halfSize.X - x * 0.6f - num1, Line), new Vector2(vector_text.Y), m_foregroundColor));
      AddProgressBar(frame, new Vector2(m_halfSize.X - num1, Line), new Vector2(x, vector_image.Y * 0.4f), TotalHydRatio, barBgColor, m_foregroundColor);
      LastStoredHydrogen = DrawCentralText(frame, Line, HydRation,LastStoredHydrogen,m_maxHydrogen);
      frame.Add(new MySprite(SpriteType.TEXT,$"{TotalHydRatio * 100.0:0}",new Vector2(m_halfSize.X + x * 0.6f - num1, Line - vector_text.Y * 0.5f),new Vector2(m_innerSize.X, m_innerSize.Y),m_foregroundColor,m_fontId,TextAlignment.LEFT,m_fontScale*0.9f));
    }
    private void DrawKerosene(MySpriteDrawFrame frame, float Line)
    {
      var KerRatio = m_KeroTankBlocks.Sum(KeroTankBlock => (float) KeroTankBlock.FilledRatio * KeroTankBlock.Capacity);
      var TotalKerRatio = (double) m_maxKerosene > 0.0 ? KerRatio / m_maxKerosene : 0.0f;
      frame.Add(new MySprite(SpriteType.TEXT, "Ker", new Vector2(m_halfSize.X - x * 0.6f - num1, Line - vector_text.Y * 0.5f), new Vector2(m_innerSize.X, m_innerSize.Y),m_foregroundColor,m_fontId,TextAlignment.CENTER,m_fontScale*0.8f));
      AddProgressBar(frame, new Vector2(m_halfSize.X - num1, Line), new Vector2(x, vector_image.Y * 0.4f), TotalKerRatio, barBgColor, m_foregroundColor);
      LastStoredKerosene = DrawCentralText(frame, Line, KerRatio,LastStoredKerosene,m_maxKerosene);
      frame.Add(new MySprite(SpriteType.TEXT,$"{TotalKerRatio * 100.0:0}",new Vector2(m_halfSize.X + x * 0.6f - num1, Line - vector_text.Y * 0.5f),new Vector2(m_innerSize.X, m_innerSize.Y),m_foregroundColor,m_fontId,TextAlignment.LEFT,m_fontScale*0.9f));
    }
    private void RecalculateGas()
    {
      try
      {
        m_maxHydrogen = 0.0f;
        m_maxKerosene = 0.0f;
        m_maxOil = 0.0f;
        m_HydTankBlocks.Clear();
        m_KeroTankBlocks.Clear();
        m_OilTankBlocks.Clear();
        m_TotalTanks = m_grid.GetShip().GasTank;
        foreach (var myGasTank in m_TotalTanks)
        {
          if (myGasTank == null) return;
          var _myGasTank = myGasTank.SlimBlock;
          var StoredGas = _myGasTank.BlockDefinition as MyGasTankDefinition;
          switch (StoredGas?.StoredGasId.SubtypeName)
          {
            case "Hydrogen":
              m_maxHydrogen += myGasTank.Capacity;
              m_HydTankBlocks.Add(myGasTank);
              break;
            case "Kerosene":
              m_maxKerosene += myGasTank.Capacity;
              m_KeroTankBlocks.Add(myGasTank);
              break;
            case "Oil":
              m_maxOil += myGasTank.Capacity;
              m_OilTankBlocks.Add(myGasTank);
              break;
          }
        }
      }
      catch (Exception e)
      {
        Log.ChatError("MyTSSEnergyHydrogenNew::RecalculateGas: " + e);
      }
    }
    private void RecalculateBat()
    {
      try
      {
        m_MaxStoredPower = 0.0f;
        LargestRechargeTime = 0.0f;
        m_Battery.Clear();
        
        m_PowerProducers = m_grid.GetShip().PowerProducers;
        m_maxPowerOutput = m_PowerProducers.Sum(PProd => PProd.MaxOutput);
        foreach (var bat in m_PowerProducers.Select(PProd => PProd.SlimBlock.FatBlock).OfType<IMyBatteryBlock>())
        {
          var input = bat.CurrentInput / 3600f;
          var output = bat.CurrentOutput / 3600f;
          
          float TimeToCharge;
          if (input > 0) TimeToCharge = (bat.MaxStoredPower - bat.CurrentStoredPower) / input ;
          else TimeToCharge = bat.CurrentStoredPower / output ;
          
          if (LargestRechargeTime < TimeToCharge) LargestRechargeTime = TimeToCharge;
          m_MaxStoredPower += bat.MaxStoredPower;
          m_Battery.Add(bat);
        }
      }
      catch (Exception e)
      {
        Log.ChatError("MyTSSEnergyHydrogenNew::RecalculateBat: " + e);
      }
    }
    
    private float DrawCentralText(MySpriteDrawFrame frame, float Line, float current, float last, float max, bool battery = false, float inTimeSeconds = 0)
    {
      double Dif = 0;
      double Left = 0;
      if (last != current && current != last )
      {
        Dif = current - last;
        Left = last < current ? max - current : max - (max - current);
        last = current;
      }

      if (Dif == 0) return last;
      var text = Dif > 0 ? "Full in:" : "Empty in: ";
      Dif *= 0.6;
      var _dif = battery ? Dif.toHumanQuantityEnergy() : Dif.toHumanQuantityVolume();
      if(!battery)frame.Add(new MySprite(SpriteType.TEXT, $"{_dif}/S {text}{(Left / Math.Abs(Dif)).toHumanTime()}", new Vector2(m_halfSize.X - num1, Line - (vector_text.Y * 0.5f - vector_text.Y / 4)), new Vector2(m_innerSize.X, m_innerSize.Y), DopInfoColor, m_fontId, TextAlignment.CENTER, m_fontScale * 0.45f));
      else frame.Add(new MySprite(SpriteType.TEXT, $"{_dif}/S {text}{((int)inTimeSeconds).toHumanTime2()}", new Vector2(m_halfSize.X - num1, Line - (vector_text.Y * 0.5f - vector_text.Y / 4)), new Vector2(m_innerSize.X, m_innerSize.Y), DopInfoColor, m_fontId, TextAlignment.CENTER, m_fontScale * 0.45f));
      return last;
    }
  }
}
