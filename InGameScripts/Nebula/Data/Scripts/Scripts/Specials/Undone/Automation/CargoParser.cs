using Digi;
using Sandbox.ModAPI;
using Scripts.Base;
using Scripts.Specials.Automation;
using Scripts.Specials.Messaging;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Item = VRage.Game.ModAPI.Ingame.MyInventoryItem;

namespace Scripts.Specials.Automation {
    static class CargoParser {
        private static Regex regex = new Regex("\\(PULL\\:([a-zA-Z\\,/\\:\\d]+)\\)");
        //private static Regex regex = new Regex("\\(PULL\\:([a-zA-Z\\,/\\:\\d]+)\\)");

        public static Dictionary<string, string> registeredSubtypeAliases = new Dictionary<string, string>() {
            {  "COBALT", "Cobalt" },
            {  "GRAVEL", "Stone" },
            {  "URANIUM", "Uranium" },
            {  "ICE", "Ice" },
            {  "LARGETUBE", "LargeTube" },
            {  "SMALLTUBE", "SmallTube" },
            {  "STEELPLATE", "SteelPlate" },
            {  "NICKEL", "Nickel" },
            {  "IRON", "Iron" },
        };//ice -> Ice, SteelPlates -> SteelPlates, 


        //ConsumableItem
        public static Dictionary<string, string> registeredTypeAliases = new Dictionary<string, string>(); //ICE -> MyObjectBuilder_Ore/Ice
        public static bool GetTypeSubtype (String what, out string type, out string subtype) {
            var d = what.Split ('/');

            type = null;
            subtype = null;

            try {
                  switch (d[0].ToUpper()) {
                    case "ORE": 
                        type = "MyObjectBuilder_Ore";
                        break;

                    case "INGOT": 
                    case "INGOTS": 
                        type = "MyObjectBuilder_Ingot";
                        break;

                    case "COMPONENT":
                    case "COMPONENTS": 
                        type = "MyObjectBuilder_Component";
                        break; 

                    case "TOOLS": 
                        type = "MyObjectBuilder_PhysicalGunObject";
                        break; 
                    default: {
                        Common.SendChatMessage ("Validate: Wrong What" + "["+what+"]");
                        return false;
                    }
                }

                if (d.Length >= 2) {
                    if (d[1].Length == 0) return true;
                    var upper = d[1].ToUpper();
                    if (registeredSubtypeAliases.ContainsKey (upper)) {
                        subtype = registeredSubtypeAliases[upper];
                        return true;
                    } else {    
                        subtype = d[1];
                        return true;
                    }
                } else {
                    return true;
                }
            } catch (Exception e) {
                Common.SendChatMessage ("WTF?" + e.Message);
                return false;
            }
           
        }

        public static bool tryAnalyzeName (IMyCargoContainer cargo, out CargoOptions options) {
            options = null;
            Log.Info ("Validate:" + cargo.CustomName);
            var match = regex.Match (cargo.CustomName);
            if (match.Success) {
                Log.Info ("Validate: Match" + match.Groups.Count);
                var opt = new CargoOptions ();
                var what = match.Groups[1].Value;

                Log.Info ("Validate: What" + "["+what+"]");

                var parts = what.Split(',');
                
                foreach (var p in parts) {

                    var cargoType = new CargoTypeOptions();
                    var info = p.Split(':');
                    var name = info[0];


                    string type, subtype; 
                    if (GetTypeSubtype (name, out type, out subtype)) {
                        cargoType.type = type;
                        cargoType.subtype = subtype;

                        var priority = (info.Length >1) ? info[1] : null;
                        var max = (info.Length >2) ? info[2] : null;
                        var min = (info.Length >3) ? info[3] : null;
                        
                        
                        int _priority = cargoType.priority;
                        int _max = cargoType.min;
                        int _min = cargoType.max;
                        
                        if (int.TryParse (priority, out _priority)) { cargoType.priority = _priority;  }
                        if (int.TryParse (max, out _max)) { cargoType.priority = _max;  }
                        if (int.TryParse (min, out _min)) { cargoType.priority = _min;  }

                        opt.pullItems.Add (cargoType);
                    }
                }

                options = opt;
                return true;
            }

            return false;
        }

        public static Dictionary<MyDefinitionId, ProductionTarget> parsePriorities (string customData) {
            var lines = customData.Split('\n');
            var data = new Dictionary<MyDefinitionId, ProductionTarget>();

            foreach (var x in lines) {
                try {
                    if (x.Length ==0) continue;

                    var parts = x.Split(':');   
                    var pt = new ProductionTarget();

                

                    string type, subtype; 
                    MyDefinitionId id;

                    double ratio = 1;
                    double minimum = 0; //parts[2];
                    double maximum = Double.MaxValue; //parts[3];

                    if (GetTypeSubtype (parts[0], out type, out subtype) && type!=null && subtype != null) {
                        if (!MyDefinitionId.TryParse (type+"/" + subtype, out id)) {
                            Common.SendChatMessage ("Couldn't parse: " + type+"/" + subtype);
                            continue;
                        }
                        if (parts.Length >= 2) { Double.TryParse(parts[1], out ratio); }
                        if (parts.Length >= 3) { Double.TryParse(parts[2], out minimum); }
                        if (parts.Length >= 4) { Double.TryParse(parts[3], out maximum); }
                    
                        pt.target = id;
                        pt.ratio = ratio;
                        pt.minimum = minimum;
                        pt.maximum = maximum;


                        if (data.ContainsKey (pt.target)) {
                            data.Remove (pt.target);
                            Common.SendChatMessage ("Already exists:" + pt.target);
                        }
                        data.Add (pt.target, pt);
                    } else {
                        Common.SendChatMessage ("Error at:" + parts[0] +" " + type + " | " + subtype);
                    }

                } catch (Exception e) {
                    Common.SendChatMessage ("Error at:" + e.Message + " " +e.StackTrace);
                }
               
            }

            return data;
        }
    }
}
