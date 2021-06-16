using Digi;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scripts.Shared;
using Scripts.Specials.Blocks.Reactions;
using ServerMod;
using Slime;
using System;
using System.Text;
using System.Text.RegularExpressions;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace Scripts.Specials.Blocks
{
    public abstract class BlockReactionsOnKeys : MyGameLogicComponent
	{
		private static readonly Guid guid = new Guid("c88b4f1d-3642-4db6-b8e8-5935a6c02c58"); //SERVER ONLY
        private static Sync<string, BlockReactionsOnKeys> sync;
        private static Regex RemoveSpaceDuplicatesRegex = new Regex("[ ]{2,}", RegexOptions.None);

        protected string reactionString = null;
		public ReactionSet keyReactions;

		public static void Init()
		{
			sync = new Sync<string, BlockReactionsOnKeys>(55530, x => x.reactionString, (x, newsettings, PlayerSteamId, isFromServer) => {
				x.reactionString = newsettings;
                x.OnOptionsChanged();
                if (!isFromServer)
                {
                    x.SendOrSave();
                }
			});
		}

		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
            if (MyAPIGateway.Session.IsServer) {
                if (Entity.TryGetStorageData<string>(guid, out reactionString))
                {
                    OnOptionsChanged();
                }
            } else {
                sync.RequestData(Entity.EntityId);
            }
        }
        
        public abstract void OnOptionsChanged ();
        public static IMyTerminalControlTextbox InitBlockReactionsActions<Z, T>(string id) where Z : BlockReactionsOnKeys
        {
            return MyAPIGateway.TerminalControls.CreateTextbox<Z, T>(id + "_ReactionKeys", "Reaction keys", "When you press this key on Keyboard, block is enabled",
                (x) => new StringBuilder(x.reactionString ?? ""),
                (x, sb) => {
                    x.reactionString = sb.ToString();
                    x.OnOptionsChanged();
                    x.SendOrSave();
                }, (x) => x.GetAs<BlockReactionsOnKeys>() != null,
                (x) => x.GetAs<BlockReactionsOnKeys>() != null);
        }

        public void SendOrSave ()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                Entity.SetStorageData<string>(guid, reactionString);
                sync.SendMessageToOthers(Entity.EntityId, reactionString, true);
            }
            else
            {
                FrameExecutor.addDelayedLogic (DATA_SEND_DELAY, new DelayedDataSend(this));
                sync.SendMessageToServer(Entity.EntityId, reactionString, true);
            }
        }

        private long lastFrameSent = 0;
        private const int DATA_SEND_DELAY = 30;
        private class DelayedDataSend : Action1<long>
        {
            BlockReactionsOnKeys logic;
            public DelayedDataSend (BlockReactionsOnKeys logic)
            {
                this.logic = logic;
            }
            public void run(long t)
            {
                if (logic == null)
                {
                    return;
                }
                if (FrameExecutor.currentFrame - logic.lastFrameSent < DATA_SEND_DELAY-1) return;
                logic.lastFrameSent = FrameExecutor.currentFrame;
                sync.SendMessageToServer(logic.Entity.EntityId, logic.reactionString, true);
            }
        }

        public abstract IMyTerminalControlTextbox GetControl();

        public static string RemoveSpaceDuplicates(string s)
        {
            return RemoveSpaceDuplicatesRegex.Replace(s, " ");
        }

        protected static void AddSliderForDoubleMouse<T>(string id, string name, string tooltip, string MOUSE, float min, float max, bool shift = false)
        {
            MyAPIGateway.TerminalControls.CreateSlider<BlockReactionsOnKeys, T>(id, name, tooltip, min, max, (x) => GetValueForMouse(x, MOUSE, shift), (x, v) => GetDescriptionForMouse(x, MOUSE, v, shift), (x, v) => SetReactionForMouse(x, MOUSE, v, shift));
        }

        protected static void AddSliderForDoubleKeys<T>(string id, string name, string tooltip, string KEY, float min, float max, bool shift = false)
        {
            MyAPIGateway.TerminalControls.CreateSlider<BlockReactionsOnKeys, T>(id + "_" + KEY, name, tooltip, min, max, (x) => GetValueForKeys(x, KEY, shift), (x, v) => GetDescriptionForKeys(x, KEY, v, shift), (x, v) => SetReactionForKeys(x, KEY, v, shift));
        }

        public static void SetReactionForKeys(BlockReactionsOnKeys logic, string name, float value, bool shift)
        {
            try
            {
                var names = name.Split('/');
                var name1 = (shift ? "SHIFT+" : "") + names[0];
                var name2 = (shift ? "SHIFT+" : "") + names[1];
                string newV = logic.reactionString ?? "";
                if (value == 0)
                {
                    newV = ReactionSet.Set(newV, name1, "");
                    newV = ReactionSet.Set(newV, name2, "");
                }
                else
                {
                    newV = ReactionSet.Set(newV, name1, name1 + "=" + value);
                    newV = ReactionSet.Set(newV, name2, name2 + "=" + (-value));
                }

                newV = ReactionSet.Set(newV, "NONE", "NONE=0");
                newV = RemoveSpaceDuplicates(newV);

                logic.reactionString = newV;
                logic.SendOrSave();
                logic.GetControl()?.UpdateVisual();
                logic.OnOptionsChanged();
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }

        public static float GetValueForKeys(BlockReactionsOnKeys logic, string name, bool shift)
        {
            try
            {
                if (logic.reactionString == null || logic.reactionString == "")
                {
                    return 0;
                }

                var s = (shift ? "SHIFT+" : "") + name.Split('/')[0];
                var v = ReactionSet.GetValue(logic.reactionString, s);
                if (v.HasValue) return v.Value;
                return 0;
            }
            catch (Exception e)
            {
                Log.ChatError(e);
                return 0;
            }
        }

        public static void GetDescriptionForKeys(BlockReactionsOnKeys logic, string name, StringBuilder sb, bool shift)
        {
            try
            {
                var v = GetValueForKeys(logic, name, shift);
                if (v == 0)
                {
                    sb.Append("0 (Disabled)");
                }
                else
                {
                    sb.Append(v);
                }
            }
            catch (Exception e)
            {
                Log.ChatError(e);
                sb.Append("Error");
            }
        }


        protected static void AddSliderForSingleKey<T>(string id, string name, string tooltip, string KEY, float min, float max, bool shift = false, float disabledValue = 0)
        {
            MyAPIGateway.TerminalControls.CreateSlider<BlockReactionsOnKeys, T>(id + "_" + KEY, name, tooltip, min, max, (x) => GetValueForSingleKey(x, KEY, shift, disabledValue), (x, v) => GetDescriptionForSingleKey(x, KEY, v, shift, disabledValue), (x, v) => SetReactionForSingleKey(x, KEY, v, shift, disabledValue));
        }

        public static void SetReactionForSingleKey(BlockReactionsOnKeys logic, string name, float value, bool shift, float disabledValue)
        {
            try
            {
                var name1 = (shift ? "SHIFT+" : "") + name;
                string newV = logic.reactionString ?? "";
                if (value == disabledValue)
                {
                    newV = ReactionSet.Set(newV, name1, "");
                }
                else
                {
                    newV = ReactionSet.Set(newV, name1, name1 + "=" + value);
                }

                newV = RemoveSpaceDuplicates(newV);

                logic.reactionString = newV;
                logic.SendOrSave();
                logic.GetControl()?.UpdateVisual();
                logic.OnOptionsChanged();
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }

        public static float GetValueForSingleKey(BlockReactionsOnKeys logic, string name, bool shift, float disabledValue)
        {
            try
            {
                if (logic.reactionString == null || logic.reactionString == "")
                {
                    return disabledValue;
                }

                var s = (shift ? "SHIFT+" : "") + name;
                var v = ReactionSet.GetValue(logic.reactionString, s);
                if (v.HasValue) return v.Value;
                return disabledValue;
            }
            catch (Exception e)
            {
                Log.ChatError(e);
                return disabledValue;
            }
        }

        public static void GetDescriptionForSingleKey(BlockReactionsOnKeys logic, string name, StringBuilder sb, bool shift, float disabledValue)
        {
            try
            {
                var v = GetValueForSingleKey(logic, name, shift, disabledValue);
                if (v == disabledValue)
                {
                    sb.Append("Disabled (" + v + ")");
                }
                else
                {
                    sb.Append(v);
                }
            }
            catch (Exception e)
            {
                Log.ChatError(e);
                sb.Append("Error");
            }
        }


        public static void SetReactionForMouse(BlockReactionsOnKeys logic, string name, float value, bool shift)
        {
            try
            {
                string vv1 = (shift ? "SHIFT+" : "") + name;
                string vv2 = (shift ? "SHIFT+I" : "") + name;

                string newV = logic.reactionString ?? "";
                if (value == 0)
                {
                    newV = ReactionSet.Set(newV, vv1, "");
                    newV = ReactionSet.Set(newV, vv2, "");
                }
                else
                {
                    newV = ReactionSet.Set(newV, vv1, vv1 + ":0.5=" + value);
                    newV = ReactionSet.Set(newV, vv2, vv2 + ":0.5=" + (-value));
                    newV = ReactionSet.Set(newV, "NONE", "NONE=0");
                }

                newV = RemoveSpaceDuplicates(newV);

                logic.reactionString = newV;
                logic.SendOrSave();
                logic.GetControl()?.UpdateVisual();
                logic.OnOptionsChanged();
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }

        public static float GetValueForMouse(BlockReactionsOnKeys logic, string name, bool shift)
        {
            try
            {
                if (logic.reactionString == null || logic.reactionString == "")
                {
                    return 0;
                }
                var v = ReactionSet.GetValue(logic.reactionString, (shift ? "SHIFT+" : "") + name);
                if (v.HasValue) return v.Value;
                return 0;
            }
            catch (Exception e)
            {
                Log.ChatError(e);
                return 0;
            }
        }

        public static void GetDescriptionForMouse(BlockReactionsOnKeys logic, string name, StringBuilder sb, bool shift)
        {
            try
            {
                var v = GetValueForMouse(logic, name, shift);
                if (v == 0)
                {
                    sb.Append("0 (Disabled)");
                }
                else
                {
                    sb.Append(v);
                }
            }
            catch (Exception e)
            {
                Log.ChatError(e);
                sb.Append("Error");
            }
        }
    }
}
