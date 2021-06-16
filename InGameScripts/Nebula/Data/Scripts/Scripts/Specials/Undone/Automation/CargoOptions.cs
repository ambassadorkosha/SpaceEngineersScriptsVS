using Sandbox.Definitions;
using Scripts.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.Utils;

namespace Scripts.Specials.Automation {

    public class ProductionTarget {
        public MyDefinitionId target;

        public double ratio;
        public double time;

        public double minimum;
        public double maximum;
        public double fillAmount;
    }


    public class ProductionVariants {
        public HashSet<MyDefinitionId> excludedIngots = new HashSet<MyDefinitionId>();
        public Dictionary<MyDefinitionId, List<Pair<MyBlueprintDefinitionBase, MyBlueprintDefinitionBase.ProductionInfo>>> produced =  new Dictionary<MyDefinitionId, List<Pair<MyBlueprintDefinitionBase, MyBlueprintDefinitionBase.ProductionInfo>>>();
        public Dictionary<MyDefinitionId, List<Pair<MyBlueprintDefinitionBase, MyBlueprintDefinitionBase.ProductionInfo>>> resources = new Dictionary<MyDefinitionId, List<Pair<MyBlueprintDefinitionBase, MyBlueprintDefinitionBase.ProductionInfo>>>();

        internal List<Pair<MyBlueprintDefinitionBase, MyBlueprintDefinitionBase.ProductionInfo>> GetAvailibleRecipes(MyDefinitionId target) {
            if (excludedIngots.Contains(target)) return null;
            if (!produced.ContainsKey(target)) return null;
            return produced[target];
        }

        internal List<Pair<MyBlueprintDefinitionBase, MyBlueprintDefinitionBase.ProductionInfo>> GetAvailibleRecipesForOre(MyDefinitionId target) {
            if (excludedIngots.Contains(target)) return null;
            if (!resources.ContainsKey(target)) return null;
            return resources[target];
        }
    }
    

    public class CargoOptions {
        public List<CargoTypeOptions> pullItems=new List<CargoTypeOptions>();

        public int getRawPriority (string type, string subtype) {
            foreach (var x in pullItems) {
                if(x.type == type && x.subtype == subtype) {
                    return x.priority;
                }
            }

            return 11;
        }

        public CargoTypeOptions getOptions(string type, string subtype) {
             foreach (var x in pullItems) {
                if(x.type == type && (x.subtype == null || x.subtype.Length == 0 || x.subtype == subtype)) {
                    return x;
                }
            }
            return null;
        }

        public int getPriority (string type, string subtype) {
            foreach (var x in pullItems) {
                if(x.type == type && (x.subtype == null || x.subtype.Length == 0 || x.subtype == subtype)) {
                    return x.priority;
                }
            }
            return 11;
        }
    }

    public class CargoTypeOptions {
        public String type;
        public String subtype;
        public int priority = 5;
        public int min=-1;
        public int max=int.MaxValue;

        public override string ToString() {
            return (subtype == null ? type : type + "/" + subtype) + ":"+priority;//+":"+min+":"+max;
        }

        public String getString () { return subtype == null ? type : type + "/" + subtype; }
    }
}
