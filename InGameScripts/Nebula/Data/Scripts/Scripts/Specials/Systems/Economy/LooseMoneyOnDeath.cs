using Digi;
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
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Scripts.Specials.Economy {
    public static class LooseMoneyOnDeath {
        public static int BOUNTY_FOR_FOUNDER = 500000;
        public static int BOUNTY_FOR_LEADER = 50000;

        static Dictionary<MyStringHash, double> moneyLoose = new Dictionary<MyStringHash, double>() {
            {  MyDamageType.Grind, 0.15d },
            {  MyDamageType.Drill, 0.15d },
            {  MyDamageType.Weld, 0.15d },

            {  MyDamageType.Asphyxia, 0.1d },
            {  MyDamageType.Temperature, 0.1d },
            {  MyDamageType.LowPressure, 0.1d },

            {  MyDamageType.Bullet, 0.05d },
            {  MyDamageType.Weapon, 0.05d },
            {  MyDamageType.Explosion, 0.05d },
            {  MyDamageType.Rocket, 0.05d },
            {  MyDamageType.Spider, 0.05d },
            {  MyDamageType.Wolf, 0.05d },
           
            {  MyDamageType.Fire, 0.05d },
            {  MyDamageType.Unknown, 0.05d },

           
            {  MyDamageType.Suicide, 0.00d }, // DC - 
             //Often lags, will be annoying
            {  MyDamageType.Environment, 0.0d },
            {  MyDamageType.Fall, 0.0d },
            {  MyDamageType.Squeez, 0.0d },


            {  MyDamageType.Bolt, 0d },
            
            {  MyDamageType.Debug, 0d },
            {  MyDamageType.Deformation, 0d },
            {  MyDamageType.Destruction, 0d },
            
            {  MyDamageType.Mine, 0d },
            {  MyDamageType.Radioactivity, 0d }
        };

        public static void Init () {
             //if (!MyAPIGateway.Multiplayer.IsServer) return; 
             //MyAPIGateway.Session.DamageSystem.RegisterDestroyHandler( 0, DestroyHandler);
        }
        
        public static void DestroyHandler(object target, MyDamageInformation info) {
            try {
                if (!(target is IMyCharacter)) return;

                var character = (IMyEntity)target;
                var player = MyAPIGateway.Players.GetPlayerControllingEntity(character);
                if (player == null) return;

                //find out what killed the player
                IMyEntity attacker = null;
                if (!MyAPIGateway.Entities.TryGetEntityById( info.AttackerId, out attacker)) {
                    HandleDeath (player, 0, 0, moneyLoose[info.Type]);
                    return;
                }

                var dd = info.getDamageDealer ();
                var ddRelation = dd == 0 ? 0 : Relations.GetRelation (dd, player.IdentityId);

                HandleDeath (player, dd, ddRelation, moneyLoose[info.Type]);
            } catch (Exception e) {
                //Common.SendChatMessage ("Error : on handle death: " + e.Message + " " + e.StackTrace);
            }
            
        }


        private static void HandleDeath (IMyPlayer player, long fromPlayer, int relation, double drainMoney) { 
            if (drainMoney <= 0) return;

            var took = player.changeMoney (-drainMoney);

            bool leader = false;
            bool founder = false;

            var relationToFaction = Relations.getFactionMemberShip (player.IdentityId);

            if (relationToFaction >= Relations.MEMBERSHIP_LEADER) {
                var fact = player.getFaction ();
                if (fact != null) {

                    if (relationToFaction == Relations.MEMBERSHIP_FOUNDER) {
                        founder = true;
                        took += fact.changeMoney (0, (long)(-BOUNTY_FOR_FOUNDER * drainMoney));
                    } else {
                        took += fact.changeMoney (0, (long)(-BOUNTY_FOR_LEADER * drainMoney));
                        leader = true;
                    }
                }
            }

            var victumFaction = Relations.getFaction (player);
            StringBuilder sb = new StringBuilder();
            if (fromPlayer != 0 && relation <= 0) {

                var ids = new List<IMyIdentity>();
                MyAPIGateway.Players.GetAllIdentites (ids, (x)=> x.IdentityId == fromPlayer);
                var killer = ids.FirstOrDefault();

                if (killer != null) {
                    var killerFaction = Relations.getFaction (killer.IdentityId);
                    if (killer != null) {
                        sb.Append (killer, killerFaction).Append (" kills ");
                        if (leader) sb.Append ("leader ");
                        if (founder) sb.Append ("founder ");
                        sb.Append (player, victumFaction).Append (" and get ").Append (-took);
                        if (leader || founder) sb.Append (" from him and his faction balance");
                        else sb.Append (" from his balance");

                        Common.SendChatMessage (sb.ToString(), "System", font:"Red");

                        MyAPIGateway.Players.RequestChangeBalance (killer.IdentityId, -took);
                        return;
                    }
                }
            }

            if (leader) sb.Append ("Leader ");
            if (founder) sb.Append ("Founder ");
            sb.Append (player, victumFaction).Append (" dies and loses ").Append (-took);
            if (leader) sb.Append (" from him and his faction balance");
            else sb.Append (" from his balance");
            Common.SendChatMessage (sb.ToString(), "System", font:"Red");
            return;
        }
    }
}
