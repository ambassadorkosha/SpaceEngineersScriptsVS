using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace Scripts.Shared {
    public static class EconomyHelper {
        public static long changeMoney (this IMyPlayer pl, double perc, long amount = 0) {
             long bal;
             if (pl.TryGetBalanceInfo (out bal)) {
                var fee = (long)(bal * perc + amount);
                var take = fee - bal > 0? bal : fee;
                pl.RequestChangeBalance (fee);
                return fee;
             }
             return 0;
        }

        public static long changeMoney (this IMyFaction pl, double perc, long amount = 0) {
             long bal;
             if (pl.TryGetBalanceInfo (out bal)) {
                var fee = (long)(bal * perc + amount) ;
                var take = fee - bal > 0? bal : fee;
                pl.RequestChangeBalance (take);
                return take;
             }
             return 0;
        }
    }
}
