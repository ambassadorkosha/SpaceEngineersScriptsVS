using VRageMath;

namespace Scripts.Specials.Safezones
{
    public struct RestictedArea
    {
        public RestictedArea(Vector3D vec, string name, float rad)
        {
            Center = vec;
            Name = name;
            RadiusSqr = rad;
        }
        public Vector3D Center;
        public string Name;
        public float RadiusSqr;
    }
}
