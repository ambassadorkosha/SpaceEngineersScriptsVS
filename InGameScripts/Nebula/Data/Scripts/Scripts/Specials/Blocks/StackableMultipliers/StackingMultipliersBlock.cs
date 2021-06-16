using Digi;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scripts.Base;
using Scripts.Specials.Messaging;
using ServerMod;
using Slime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.ModAPI;

namespace Scripts.Specials
{
	public abstract class MultiplierEffect {
		public Pair<int, long> key;
		public long effectOwner;
		public float m1 = 1f;
		public float m2 = 1f;
		public float m3 = 1f;

		public MultiplierEffect (int type, long effectCreator, long effectOwner, float m1 = 1f, float m2 = 1f, float m3 = 1f)
		{
			this.key = new Pair<int, long>(type, effectCreator);
			
			this.effectOwner = effectOwner;
			this.m1 = m1;
			this.m2 = m2;
			this.m3 = m3;
		}

		public override int GetHashCode()
		{
			return (int)effectOwner;
		}

		public override bool Equals(object obj)
		{
			var me = (obj as MultiplierEffect);
			return key == me.key && effectOwner == me.effectOwner;
		}

		public bool EqualEffects(MultiplierEffect obj)
		{
			return obj.GetType() == GetType() && obj.m1 == m1 && obj.m2 == m2 && obj.m3 == m3;
		}

		public void RemoveEffect ()
		{
			var x = effectOwner.As<IMyEntity>()?.GetAs<StackingMultipliersBlock>();
			x?.RemoveMultiplierEffect(this);
		}

		public abstract bool Check ();
	}

	public class EndlessMultiplierEffect : MultiplierEffect
	{
		public EndlessMultiplierEffect(int type, long effectCreator, long effectOwner, float m1 = 1, float m2 = 1, float m3 = 1) : base(type, effectCreator, effectOwner, m1, m2, m3)
		{
		}

		public override bool Check()
		{
			return effectOwner.As<IMyEntity>() != null;
		}
	}

	public class WhileOnMultiplierEffect : MultiplierEffect
	{
		public WhileOnMultiplierEffect(int type, long effectCreator, long effectOwner, float m1 = 1, float m2 = 1, float m3 = 1) : base(type, effectCreator, effectOwner, m1, m2, m3)
		{
		}

		public override bool Check()
		{
			var block = effectOwner.As<IMyFunctionalBlock>();
			if (block == null) return false;
			return block.Enabled && block.IsFunctional;
		}
	}

    public class WhileOnAndOnSafeGridMultiplierEffect : MultiplierEffect
    {
        public WhileOnAndOnSafeGridMultiplierEffect(int type, long effectCreator, long effectOwner, float m1 = 1, float m2 = 1, float m3 = 1) : base(type, effectCreator, effectOwner, m1, m2, m3)
        {
        }

        public override bool Check()
        {
            var block = effectOwner.As<IMyFunctionalBlock>();
            var creator = effectOwner.As<IMyFunctionalBlock>();
            if (block == null) return false;
            if (creator == null) return false;
            if (!block.Enabled) return false;
            if (!block.IsFunctional) return false;

            var ship1 = block.CubeGrid.GetShip();
            var ship2 = creator.CubeGrid.GetShip();

            return ship1.connectedGrids.Contains (creator.CubeGrid);
        }
    }

    public class WhileOnAndConnectedMultiplierEffect : MultiplierEffect
    {
        MyCubeGrid myGrid;
        List<VRage.Game.ModAPI.IMyCubeGrid>  buffer = new List<VRage.Game.ModAPI.IMyCubeGrid>();
        public WhileOnAndConnectedMultiplierEffect(int type, MyCubeGrid grid, long effectCreator, long effectOwner, float m1 = 1, float m2 = 1, float m3 = 1) : base(type, effectCreator, effectOwner, m1, m2, m3)
        {
            this.myGrid = grid;
        }

        public override bool Check()
        {
            var block = effectOwner.As<IMyFunctionalBlock>();
            if (block == null || block.MarkedForClose) return false;
            if (!block.Enabled || !block.IsFunctional) return false;

            if (block.CubeGrid == myGrid)
            {
                return true;
            }

            myGrid.GetConnectedGrids(VRage.Game.ModAPI.GridLinkTypeEnum.Physical, buffer, false);

            return buffer.Contains(block.CubeGrid);
        }
    }



    public abstract class StackingMultipliersBlock : MyGameLogicComponent
	{
        private StackingMultipliers stacking = new StackingMultipliers();

        public StackingMultipliersBlock ()
        {
            stacking.onRecalculate = () => {
                Apply(stacking.m1, stacking.m2, stacking.m3);
            };
        }

		public void AddEffect (MultiplierEffect effect)
		{
            stacking.AddEffect (effect);
		}

		public void RemoveMultiplierEffect(MultiplierEffect effect)
		{
            stacking.RemoveMultiplierEffect(effect);
		}

		internal bool HasEffect(int v)
		{
            return stacking.HasEffect(v);
		}

		public void RemoveMultiplierEffect(int type, long creatorId)
		{
            stacking.RemoveMultiplierEffect(type, creatorId);
		}

		public void Recalculate ()
		{
            stacking.Recalculate();
		}

		public abstract void Apply (float m1, float m2, float m3);
	}

    public class StackingMultipliers
    {
        private List<Pair<int, long>> toRemove = new List<Pair<int, long>>();
        Dictionary<Pair<int, long>, MultiplierEffect> effects = new Dictionary<Pair<int, long>, MultiplierEffect>();
        private Pair<int, long> buffer = new Pair<int, long>(0, 0);

        public float m1;
        public float m2;
        public float m3;

        public Action onRecalculate = null;

        public void AddEffect(MultiplierEffect effect)
        {
            MultiplierEffect prevValue = null;
            if (effects.ContainsKey(effect.key))
            {
                prevValue = effects[effect.key];
                effects[effect.key] = effect;
            }
            else
            {
                effects[effect.key] = effect;
            }

            if (prevValue == null || !prevValue.EqualEffects(effect))
            {
                Recalculate();
            }
        }

        public void RemoveMultiplierEffect(MultiplierEffect effect)
        {
            if (effects.Remove(effect.key))
            {
                Recalculate();
            }
        }

        internal bool HasEffect(int v)
        {
            foreach (var x in effects)
            {
                if (x.Key.k == v) return true;
            }
            return false;
        }

        public void RemoveMultiplierEffect(int type, long creatorId)
        {
            buffer.k = type;
            buffer.v = creatorId;
            if (effects.Remove(buffer))
            {
                Recalculate();
            }
        }

        public void Recalculate()
        {
            m1 = 1f;
            m2 = 1f;
            m3 = 1f;
            
            foreach (var x in effects)
            {
                m1 *= x.Value.m1;
                m2 *= x.Value.m2;
                m3 *= x.Value.m3;
            }

            onRecalculate?.Invoke();
        }
        
    }
}
