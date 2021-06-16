using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Slime;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace ServerMod {
     public static class Ext {
        public static T FindAndMove<T> (this List<T> list, int newPos, Func<T, bool> x) {
            var ind = list.FindIndex ((y) => x.Invoke(y));
            if (ind != -1) {
                var tt = list[ind];
                list.Move (ind, newPos);
                return tt;
            } else { return default(T); }
        }
        public static Ship GetShip(this IMyCubeGrid grid) {
            var x = GameBase.instance.gridToShip;
            if (x.ContainsKey(grid.EntityId)) return x[grid.EntityId];
            else return null;
        }

        public static bool inSpace (this IMyCubeGrid grid) {
            var aabb = grid.WorldAABB;

            foreach (var pl in GameBase.instance.planets) {
                if (pl.Value.IntersectsWithGravityFast(ref aabb)) {
                    return false;
                }
            }

            return true;
        }

        public static Dictionary<MyDefinitionId, int> GetBlockPrice (this IMySlimBlock slim, Dictionary<MyDefinitionId, int> dict = null) {
            if (dict == null) dict = new Dictionary<MyDefinitionId, int>();

            var cmps = (slim.BlockDefinition as MyCubeBlockDefinition).Components;

            foreach (var xx in cmps) {
                var id = xx.Definition.Id;
                var c = xx.Count;
                if (dict.ContainsKey(id)) {
                    dict[id] += c;
                } else {
                    dict.Add (id, c);
                }
            }

            return dict;
        }

        public static Dictionary<MyDefinitionId, int> GetBlockLeftNeededComponents (this IMySlimBlock slim, Dictionary<MyDefinitionId, int> dict = null, Dictionary<MyDefinitionId, int> temp = null) {
            if (dict == null) dict = new Dictionary<MyDefinitionId, int>();

            var cmps = (slim.BlockDefinition as MyCubeBlockDefinition).Components;

            temp.Clear();
            foreach (var xx in cmps) {
                var id = xx.Definition.Id;
                var c = xx.Count;
                if (temp.ContainsKey(id)) {
                    temp[id] += c;
                } else {
                    temp.Add (id, c);
                }
            }

            foreach (var x in temp) {
                var id = x.Key;
                var has = slim.GetConstructionStockpileItemAmount (id);
                var need = x.Value;
                var left = need - has;
                if (left > 0) {
                    if (dict.ContainsKey(id)) {
                        dict[id] += left;
                    } else {
                        dict.Add (id, left);
                    }
                }
            }

            return dict;
        }

        public static Vector3D GetWorldPosition (this IMySlimBlock block) {
            var box = new BoundingBoxD (); 
            block.GetWorldBoundingBox(out box);
            return box.Center;
        }

        public static bool Contains (this MySafeZone __instance, Vector3 point) {
            if (__instance.Shape == MySafeZoneShape.Sphere) {
				BoundingSphereD boundingSphereD = new BoundingSphereD(__instance.PositionComp.GetPosition(), (double)__instance.Radius);
				return boundingSphereD.Contains(point) == ContainmentType.Contains;
			} else {
				MyOrientedBoundingBoxD myOrientedBoundingBoxD = new MyOrientedBoundingBoxD(__instance.PositionComp.LocalAABB, __instance.PositionComp.WorldMatrix);
				return myOrientedBoundingBoxD.Contains(ref point);
			}
        }

        public static bool inSpace(this Vector3D position) {
            var aabb = new BoundingBoxD(position, position);
            foreach (var pl in GameBase.instance.planets) {
                if (pl.Value.IntersectsWithGravityFast(ref aabb)) {
                    return false;
                }
            }

            return true;
        }
        
        public static bool isOwnerOnline(this IMySlimBlock block) {
            if (block.BuiltBy == 0) {
                var owners = block.CubeGrid.BigOwners;
                if (owners.Count > 0) {
                    return OnlineFactions.isOnlineFaction(owners[0]);
                } else {
                    return true; // Fix ungrindable blocks
                }
            }

            if (block.OwnerId != 0) {
                return OnlineFactions.isOnlineFaction(block.OwnerId);
            } else {
                return OnlineFactions.isOnlineFaction(block.BuiltBy);
            }
        }        

        public static MyPlanet GetPlanet(this Vector3D position) {
            var aabb = new BoundingBoxD(position, position);
            foreach (var pl in GameBase.instance.planets) {
                if (pl.Value.IntersectsWithGravityFast(ref aabb)) {
                    return pl.Value;
                }
            }

            return null;
        }
    }
}
