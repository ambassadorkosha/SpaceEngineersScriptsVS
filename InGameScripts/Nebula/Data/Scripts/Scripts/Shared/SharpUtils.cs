using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using Sandbox.Game.EntityComponents;

namespace ServerMod {
    static class SharpUtils {

        public static DateTime utcZero = new DateTime(1970, 1, 1);
		public static DateTime y2020 = new DateTime(2020, 1, 1);

        public static float Lerp2 (float Current, float Desired, float Speed)
        {
            if (Current < Desired)
            {
                Current += Speed;
                if (Current > Desired)
                {
                    Current = Desired;
                }
            }
            else
            {
                Current -= Speed;
                if (Current < Desired)
                {
                    Current = Desired;
                }
            }
            return Current;
        }

        public static float toRadian (this float v)
        {
            return (float)(v * Math.PI / 180d);
        }

        public static double toRadian(this double v)
        {
            return (v * Math.PI / 180d);
        }

        public static float toDegree (this float v)
        {
            return (float)(v / Math.PI * 180d);
        }

        public static double toDegree (this double v)
        {
            return (v / Math.PI * 180d);
        }

        public static int DayOfWeek (DateTime time) //Saturday = 6, Sunday = 7
		{
			var utcZero = new DateTime(1970, 1, 1);
			if (time < utcZero) return 1;
			var d = (y2020 - utcZero).TotalDays;
			var dd = (int)d + d%1>0 ? 1 : 0;
			dd = dd- (dd / 7)*7 + 4; //1970 was Thursday
			if (dd > 7) dd -=7;
			return dd;
		}


        public static double Degree (Vector3D v1, Vector3D v2)
        {
            return Math.Acos(v1.Dot(v2) / (v1.Length() * v2.Length())).toDegree();
        }

        public static double Degree2 (Vector3D v1, Vector3D v2)
        {
            var d = Degree(v1, v2);

            if ((v1+v2).LengthSquared () < (v1 - v2).LengthSquared())
            {
                d*=-1;
            }
            return d;
        }

        public static long timeStamp () {
            return (long)(DateTime.UtcNow.Subtract(utcZero)).TotalSeconds;
        }

        public static long timeUtcDif()
        {
            return Math.Abs((long)DateTime.UtcNow.Subtract(DateTime.Now).TotalSeconds);
        }

        public static bool HasFlags(this int x, int f)
        {
            return (x | f) == x;
        }

        public static long msTimeStamp () {
            return (long)(DateTime.UtcNow.Subtract(utcZero)).TotalMilliseconds;
        }

        public static TimeSpan StripMilliseconds(this TimeSpan time)
        {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }


        public static void AddOrRemove<T>(this HashSet<T> set, T data, bool add) {
            if (add) { set.Add(data); } else { set.Remove(data);  }
        }


        public static string Print<T, K>(this Dictionary<T, K> dict) {
            StringBuilder sb = new StringBuilder();
            sb.Append("Dict[");
            foreach (var x in dict) {
                sb.Append(x.Key).Append("->").Append(x.Value).Append("\n");
            }
            sb.Append("]");
            return sb.ToString();
        }

        public static string Print<T>(this List<T> list)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("List[");
            foreach (var x in list)
            {
                sb.Append(x).Append(" ");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static Dictionary<MyObjectBuilderType, string> alliases = new Dictionary<MyObjectBuilderType, string>() {
            { MyObjectBuilderType.Parse("MyObjectBuilder_Ingot"), "i/" },
            { MyObjectBuilderType.Parse("MyObjectBuilder_Ore"), "o/" },
            { MyObjectBuilderType.Parse("MyObjectBuilder_Component"), "" }
        };

        
        public static string toHumanWeight (this double num) {
            if (num <1000) return String.Format ("{0:N2} kg", num);
            if (num <1000000) return String.Format ("{0:N2} t", num /1000);
            if (num <1000000000) return String.Format ("{0:N2} kt", num /1000000);
            if (num <1000000000000) return String.Format ("{0:N2} Mt", num /1000000000);
            if (num <1000000000000000) return String.Format ("{0:N2} Gt", num /1000000000000);
            return "TONS";
        }

        public static string toHumanQuantity (this double num) {
            if (num <1000) return String.Format ("{0:N2}", num);
            if (num <1000000) return String.Format ("{0:N2} K", num /1000);
            if (num <1000000000) return String.Format ("{0:N2} M", num /1000000);
            return "TONS";
        }

        public static string toPhysicQuantity (this double num, String s) {
            var k = 1000d;
            if (num <k) return String.Format ("{0:N2} ", num)+s;
            if (num <k*k) return String.Format ("{0:N2} k", num / k)+s;
            if (num <k*k*k) return String.Format ("{0:N2} M", num /(k*k))+s;
            if (num <k*k*k*k) return String.Format ("{0:N2} G", num / (k*k*k))+s;
            if (num <k*k*k*k*k) return String.Format ("{0:N2} T", num / (k*k*k*k))+s;
            return String.Format ("{0:N2} P", num / (k*k*k*k*k));
        }

        public static string toHumanQuantityCeiled (this double num) {
            if (num <1000) return String.Format ("{0:N0}", num);
            if (num <1000000) return String.Format ("{0:N2} K", num /1000);
            if (num <1000000000) return String.Format ("{0:N2} M", num /1000000);
            if (num <1000000000) return String.Format ("{0:N2} B", num /1000000);
            return "TONS";
        }
        
        public static string toHumanQuantityEnergy (this double num)
        {
            if (Math.Abs(num) > 1000) return $"{num / 1000 :N0} GW";
            if (Math.Abs(num) > 1) return $"{num :N2} MW";
            if (Math.Abs(num) > 0.001) return $"{num * 1000 :N0} KW";
            return $"{num * 1000000 :N0} W";
        }
        
        public static string toHumanQuantityVolume (this double num)
        {
            if (Math.Abs(num) > 1000000000) return $"{num / 1000000000 :N2} GL";
            if (Math.Abs(num) > 1000000) return $"{num / 1000000 :N2} ML";
            if (Math.Abs(num) > 1000) return $"{num / 1000 :N2} kL";
            if (Math.Abs(num) > 100) return $"{num / 100 :N2} hL";
            if (Math.Abs(num) > 10) return $"{num / 10 :N2} daL";
            if (Math.Abs(num) > 1) return $"{num :N2} L";
            if (Math.Abs(num) > 0.1) return $"{num * 10 :N2} dL";
            if (Math.Abs(num) > 0.01) return $"{num * 100 :N2} cL";
            return $"{num * 1000 :N2} mL";
        }


        public static string toHumanTime (this double num) {
            if (num <120) return String.Format ("{0:N0} s", num);
            if (num <3600) return String.Format ("{0:N0} min", num /60);
            if (num <3600*24) return String.Format ("{0:N0} h", num /3600);
            return String.Format ("{0:N0} days", num/3600/24);
        }
        public static string toHumanTime2 (this int num, bool isFullWithDays = false) {
            if (num < 120) return num % 60 + "s";
            if (num < 3600) return num / 60 + "m " + num % 60 + "s";
            if (num < 3600 * 24) return num / 3600 + "h " + (num / 60) % 60 + "m " + num % 60 + "s";
            if (num / 3600 / 24 == 1) return isFullWithDays ? "1 day " + num / 3600 + "h " + (num / 60) % 60 + "m " + num % 60 + "s" : "1 day";
            return isFullWithDays ? num / 3600 / 24 + " days " + num / 3600 % 24 + "h " + (num / 60) % 60 + "m " + num % 60 + "s" : num / 3600 / 24 + " days";
        }

        public static string fixZero (this double num) {
            return String.Format ("{0:N2}", num);
        }

        public static string fixZero(this float num)
        {
            return String.Format("{0:N2}", num);
        }

        public static string toHumanString (this MyDefinitionId id) {
            if (alliases.ContainsKey (id.TypeId)) {
                return alliases[id.TypeId] +id.SubtypeName;
            } else {
                return id.TypeId.ToString().Substring (17) + "/"+id.SubtypeName;
            }
        }
        public static float CurrentPowerInput(this IMyCubeBlock block) => block?.ResourceSink?.CurrentInputByType(MyResourceDistributorComponent.ElectricityId) ?? 0; 
        public static float MinRequiredPowerInput(this IMyCubeBlock block) => block?.ResourceSink?.RequiredInputByType(MyResourceDistributorComponent.ElectricityId) ?? 0;
        public static float MaxRequiredPowerInput(this IMyCubeBlock block) => block?.ResourceSink?.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId) ?? 0;
        
        public static bool IsOneKeyMoreThan<T> (this Dictionary<T, int> buffer, Dictionary<T, int> maxLimits)
        {
            foreach (var y in buffer)
            {
                if (y.Value > maxLimits[y.Key])
                {
                    return true;
                }
            }
            return false;
        }
        

        public static void Sum <T> (this Dictionary<T,double> dict, T key, double value) {
            if (!dict.ContainsKey(key)) {
                dict[key] = value;
            } else {
                dict[key] = dict[key] + value;
            }
        }

        public static void Sum <T> (this Dictionary<T,MyFixedPoint> dict, T key, MyFixedPoint value) {
            if (!dict.ContainsKey(key)) {
                dict[key] = value;
            } else {
                dict[key] = dict[key] + value;
            }
        }

        public static void Sum <T> (this Dictionary<T,int> dict, T key, int value) {
            if (!dict.ContainsKey(key)) {
                dict[key] = value;
            } else {
                dict[key] = dict[key] + value;
            }
        }

        public static string printContent<T>(this List<T> dict) {
            StringBuilder sb = new StringBuilder();
            sb.Append("List[");
            foreach (var x in dict) {
                sb.Append(x).Append(",\n");
            }
            sb.Append("]");
            return sb.ToString();
        }


        /*public static byte[] toBytes(this List<long> list) {
            using (MemoryStream ms = new MemoryStream()) {
                using (BinaryWriter bw = new BinaryWriter(ms)) {
                    if (list == null) { bw.Write (-1); }
                    bw.Write (list.Count);
                    foreach (var x in list) {
                        bw.Write (x);
                    }
                    bw.Flush();
                    return ms.ToArray();
                }
            }
        }

        public static List<long> toListLong(this byte[] bytes) {
            using (MemoryStream ms = new MemoryStream(bytes)) {
                using (BinaryReader bw = new BinaryReader(ms)) {

                    var count = bw.ReadInt32 ();
                    if (count == -1) return null;

                    var list = new List<long>();
                    for (var x=0; x<count; x++) {
                        var l = bw.ReadInt64 ();
                        list.Add (l);
                    }
                    return list;
                }
            }
        }*/
        
        public static string printContent(this List<IMyPlayer> dict) {
            StringBuilder sb = new StringBuilder();
            sb.Append("List[");
            foreach (var x in dict) {
                sb.Append(x.DisplayName  + "/" + x.PlayerID).Append(", ");
            }
            sb.Append("]");
            return sb.ToString();
        }
        

        public static StringBuilder Append(this StringBuilder sb, IMyPlayer player, IMyFaction faction) {
            sb.Append (player.DisplayName);
            if (faction != null) {
                sb.Append("[").Append(faction.Tag).Append("]");
            }

            return sb;
        }

        public static StringBuilder Append(this StringBuilder sb, IMyIdentity player, IMyFaction faction) {
            sb.Append (player.DisplayName);
            if (faction != null) {
                sb.Append("[").Append(faction.Tag).Append("]");
            }

            return sb;
        }

        public static string printContent(this List<IMyFaction> dict) {
            StringBuilder sb = new StringBuilder();
            sb.Append("List[");
            foreach (var x in dict) {
                sb.Append(x.Name + "/" +x.FactionId).Append(", ");
            }
            sb.Append("]");
            return sb.ToString();
        }


        public static string printContent(this List<MyProductionQueueItem> dict) {
            StringBuilder sb = new StringBuilder();
            sb.Append("List[");
            foreach (var x in dict) {
                sb.Append("{").Append(x.ItemId).Append("/").Append(x.Amount).Append("/").Append(x.Blueprint).Append("},\n");
            }
            sb.Append("]");
            return sb.ToString();
        }
       

        public static K GetOr<T, K>(this Dictionary<T, K> dict, T t, K k) {
            if (dict.ContainsKey(t)) {
                return dict[t];
            } else {
                return k;
            }
        }

        public static List<K> GetOrCreate<T, K>(this Dictionary<T, List<K>> dict, T t) {
            if (!dict.ContainsKey(t)) {
                dict.Add (t, new List<K>());
            }
            return dict[t];
        }

        public static HashSet<K> GetOrCreate<T, K>(this Dictionary<T, HashSet<K>> dict, T t) {
            if (!dict.ContainsKey(t)) {
                dict.Add (t, new HashSet<K>());
            }
            return dict[t];
        }

        public static Dictionary<K,V> GetOrCreate<T, K, V>(this Dictionary<T, Dictionary<K,V>> dict, T t) {
            if (!dict.ContainsKey(t)) {
                dict.Add (t, new Dictionary<K,V>());
            }
            return dict[t];
        }

        public static K Set<T, K>(this Dictionary<T, K> dict, T t, K k) {
            K old = default(K);
            if (dict.ContainsKey(t)) {
                old = dict[t];
                dict.Remove(t);
            }
            dict.Add(t, k);
            return old;
        }
    }
}
