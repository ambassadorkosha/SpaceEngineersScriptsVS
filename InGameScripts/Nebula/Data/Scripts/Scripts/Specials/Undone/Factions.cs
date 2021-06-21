using ServerMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Specials.Faction {


    public class FactionInfo {
        public HashSet<long> activatedZones = new HashSet<long>();
        public HashSet<long> activatedPortals = new HashSet<long>();
        public HashSet<long> activatedAssemblers = new HashSet<long>();
        public HashSet<long> activatedRefineries = new HashSet<long>();

        float TAX_FACTION = 50000f;
        float TAX_ZONES = 25000f;
        float TAX_PORTALS = 50000f;
        float TAX_ASSEMBLERS = 100000f;
        float TAX_REFINERIES = 100000f;

        int RefineryPoints = 0;
        int AssemblerPoints = 0;

        int ThrusterThrustPoints = 0;
        int ThrusterPowerPoints = 0;

        int GyroPowerPoints = 0;

        int BulletDamagePoints = 0;
        int MissleDamagePoints = 0;

        int BulletDefencePoints = 0;
        int MissleDefencePoints = 0;

        public long calculatePrice (long currentBalance, StringBuilder sb) {
            var pay = 0l; 
            var total = 0l; 
            

            if (sb !=null) sb.Append ("Faction pay for:\n");

            pay = (long)(TAX_ZONES * activatedZones.Count);
            if (sb !=null) sb.Append ("- ").Append (pay).Append(" SC for activated safezones\n");
            total += pay;


            pay = (long)(TAX_PORTALS * activatedPortals.Count);
            if (sb !=null) sb.Append ("- ").Append (pay).Append(" SC for activated portals\n");
            total += pay;

            pay = (long)(TAX_ASSEMBLERS * activatedAssemblers.Count);
            if (sb !=null) sb.Append ("- ").Append (pay).Append(" SC for faction assemblers\n");
            total += pay;

            pay = (long)(TAX_REFINERIES * activatedRefineries.Count);
            if (sb !=null) sb.Append ("- ").Append (pay).Append(" SC for faction refineries\n");
            total += pay;

            pay = (long)TAX_FACTION;
            if (sb !=null) sb.Append ("- ").Append (pay).Append(" SC for faction tax\n");
            total += pay;

            if (sb !=null) {
                 var times = ((double)(currentBalance-total)) / total;

                 var timeNotDie = BankingSystem.INTERVAL_SECONDS * times; 

                 var hours_left = String.Format("{0:0.00}", timeNotDie / 3600);
                
                 sb.Append ("Total: ").Append (total).Append(" SC. Hours left: ").Append (hours_left);

                 sb.Append ("\nBalance: ").Append (currentBalance);
            }

            return total;
        }
    }

    public static class Factions {
        static Dictionary<long, FactionInfo> blocks = new Dictionary<long, FactionInfo>();

        public static FactionInfo getInfo (long factionId) {
            if (!blocks.ContainsKey(factionId)) {
                blocks.Add (factionId, new FactionInfo());
            }

            return blocks[factionId];
        }
    }
}