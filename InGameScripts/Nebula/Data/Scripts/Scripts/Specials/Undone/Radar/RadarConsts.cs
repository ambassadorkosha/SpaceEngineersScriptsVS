using Digi;
using Sandbox.Game.Entities;
using ServerMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Scripts.Specials.Radar {
    public static class RadarConsts {
        public static readonly float ENERGY_UPPER = 600f; //MW
        public static readonly float ENERGY_LOWER = 30f; //MW
        public static readonly float ENERGY_COEF = 1f; //MW
        public static readonly float ENERGY_PENALTY = ENERGY_UPPER; //MW

        public static readonly float SPEED_UPPER = 500f; //m/s
        public static readonly float SPEED_LOWER = 100f; //m/s
        public static readonly float SPEED_COEF = 1f; //MW

        
        public static readonly float MASS_UPPER = 2000000f; //kg
        public static readonly float MASS_LOWER = 0f; //kg
        public static readonly float MASS_COEF = 1f; //kg
        public static readonly float MASS_PENALTY = 0; //kg
       

        public static readonly float DISTANCE_COEF = 3000f; //meters

        public static readonly float INFO_1 = 1f; //signature
        public static readonly float INFO_2 = 2f; //signature
        public static readonly float INFO_3 = 3f; //signature

        public static readonly double HEIGHT_STEP = 200d; //meters
        public static readonly double HEIGHT_ZERO_POINTS = 10d; //meters
        public static readonly double HEIGHT_PER_STEP = 5d; //meters
        public static readonly double HEIGHT_MAX_POINTS = 100; //meters
        public static readonly double HEIGHT_COEF = 3; //meters


        public static float calculateEnergy(float value) {
            if (value < ENERGY_LOWER) return 0;
            return value / (ENERGY_UPPER - ENERGY_LOWER) * ENERGY_COEF;
        }

        public static float calculateSpeed(float value) {
            if (value < SPEED_LOWER) return 0;
            return value / (SPEED_UPPER - SPEED_LOWER) * SPEED_COEF;
        }

        public static float calculateMass(float value) {
            if (value < MASS_LOWER) return 0;
            return value / (MASS_UPPER - MASS_LOWER) * MASS_COEF;
        }

        public static float calculateDistance(double value) { return (float)(DISTANCE_COEF / value); }

        public static int calculateSpotLevel(double value) {
            if (value < INFO_1) return 0;
            if (value < INFO_2) return 1;
            if (value < INFO_3) return 2;
            return 3;
        }

        

        public static double calculateHeightPoints(MyPlanet planet, double elevation) {
            var d = (int)(elevation / HEIGHT_STEP);
            return Math.Min(HEIGHT_ZERO_POINTS + d * HEIGHT_PER_STEP, HEIGHT_MAX_POINTS);
        }


        public static double calculateHeight(double heightPoints) {
            return (heightPoints / HEIGHT_MAX_POINTS)*(HEIGHT_COEF);
        }

        public static Spotted GetSpot(ShipInfo info, Vector3 pos, MyPlanet planet) {
            var gridPlanet = info.position.GetPlanet();
            if (gridPlanet != planet) {
                return null;//new Spotted(info.position, "Invis [Another planet]" , "");
            }


            var isSpace = planet == null;

            var timesTaken = info.times;

            var avrgE = info.electricity / timesTaken;
            var avrgS = info.speed.Length() / timesTaken;
            var avrgH = info.height / timesTaken;
            var avrgM = info.mass / timesTaken;
            var distance = (info.position - pos).Length();

            //Log.Info("CALC SPOT:" + info + " " + avrgE + " " + avrgS + " " + avrgH + " " + avrgM + " " + distance);

            var mltE = calculateEnergy(avrgE);
            var mltS = calculateSpeed(avrgS);
            var mltH = calculateHeight(avrgH);
            var mltM = calculateMass(avrgM);

            var mltD = calculateDistance(distance);


            

            var mltSum = 1+mltE + mltS + mltH + mltM;

            var visibleRange = mltSum * DISTANCE_COEF;

            var result = mltD * mltSum;

            var spotLevel = calculateSpotLevel(result); 
            //var pinfo = String.Format("[lvl={0} | sig={1:0.##} | sumk {2:0.##} e={3:0.##} spd={4:0.#} m={5:0} h={6:0.#}]", spotLevel, result, mltSum, avrgE, avrgS, avrgM, avrgH);
            //var kinfo = String.Format("[lvl={0} | sig={1:0.####} | sumk {2:0.#####} e={3:0.#####} s={4:0.#####} h={5:0.#####} m={6:0.#####} | d={7:0.#####} m={8:0.#####} spd={9:0.#####}  h={10:0.#}]", spotLevel, result, mltSum, mltE, mltS, mltH, mltM, mltD, avrgS, avrgM, avrgH);

            switch (spotLevel) {
                case 0: return null;// new Spotted(info.position, pinfo + " Invis ",kinfo);
                case 1: return new Spotted(info.position, "???", ""); //new Spotted(info.position, pinfo + " Unknown ",kinfo);
                case 2: return new Spotted(info.position, info.name, ""); // new Spotted(info.position, pinfo + " " + info.name, kinfo);
                case 3: {
                        var trgPos = info.position + info.speed / timesTaken;
                        var relSpeed = (info.position - pos).Length() - (trgPos - pos).Length();
                        var extra = String.Format(" [d={0:0.#}][spd={1:0} m/s rel spd={2:0} m/s] ", visibleRange/1000d, + avrgS, relSpeed);
                        return new Spotted(info.position, info.name + extra, "");//new Spotted(info.position, pinfo + info.name + extra, kinfo);
                    }
                default: return null;
            }
        }


    }
}
