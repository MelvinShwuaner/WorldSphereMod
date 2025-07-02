using CompoundSpheres;
using UnityEngine;
using WorldSphereMod.NewCamera;

namespace WorldSphereMod
{
    public static class CompoundSphereScripts
    {
        public const int ZDisplacement = 100;
        public static int SphereTileTexture(SphereTile Tile)
        {
            return Core.Sphere.WorldTileTexture(Tile.SphereToWorld());
        }
        //a constant multiplier for tile heights
        const float YConst = 1f/(81/ZConst);
        public const float ZConst = 2f;
        public static Vector3 SphereTileScale(SphereTile Tile)
        {
            float Height = Tools.TrueHeight(Tile.SphereToWorld().GetHeight());
            return new Vector3(1, 1+(Height*YConst), Height*ZConst);
        }
        public static Vector3 SphereTileAddedColor(SphereTile Tile) {
            return (Vector4)Core.Sphere.GetAddedColor(Tile.Index());
        }
        public static Quaternion CylindricalRotation(SphereTile SphereTile)
        {
            return Tools.GetRotation(SphereTile.Position.x, SphereTile.Position.y);
        }
        public static Color SphereTileColor(SphereTile SphereTile)
        {
            return Core.Sphere.GetColor(SphereTile.Index());
        }
        public static Vector3 CartesianToCylindrical(SphereManager manager, float X, float Y, float Height = 0)
        {
            Vector2 xy = Tools.MathStuff.PointOnCircle(X, manager.Radius, Height);
            float z = Y+ZDisplacement;
            return new Vector3(xy.x, xy.y, z);
        }
        public static Vector3 CylindricalToCartesian(SphereManager manager, float x, float y, float z)
        {
            float X = manager.Clamp(Mathf.Atan2(y, x) / (2f * Mathf.PI) * manager.Rows, 0);
            float Y = z-ZDisplacement;
            float Height = Mathf.Sqrt((x * x) + (y * y)) - manager.Radius;
            return new Vector3(X, Y, Height);
        }
        //doesnt calculate height
        public static Vector2 CylindricalToCartesianFast(SphereManager manager, float x, float y, float z)
        {
            float X = manager.Clamp(Mathf.Atan2(y, x) / (2f * Mathf.PI) * manager.Rows, 0);
            float Y = z-ZDisplacement;
            return new Vector2Int((int)X, (int)Y);
        }
        public static void Initiation(SphereManager Manager)
        {
            GameObject Cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Cylinder.transform.SetPositionAndRotation(new Vector3(0, 0, (Manager.Cols / 2)+ZDisplacement), Quaternion.Euler(-90, 0, 0));
            Cylinder.transform.localScale = new Vector3(Manager.Diameter, Manager.Cols / 2, Manager.Diameter);
            Object.Destroy(Cylinder.GetComponent<CapsuleCollider>());
            Object.Destroy(Cylinder.GetComponent<MeshRenderer>());
            Cylinder.AddComponent<MeshCollider>();
            Cylinder.transform.parent = Manager.transform;
        }
        public static DisplayMode getdisplaymode(SphereManager Manager)
        {
            return World.world.quality_changer.isLowRes() ? DisplayMode.ColorOnly : DisplayMode.TextureOnly;
        }
        static float RangeMult => 2;
        static float BaseRange => 3 + (1-(1/RangeMult));
        public static void RenderRange(SphereManager SphereManager, out int Min, out int Max) 
        {
           float Devide = BaseRange + (CameraManager.cam.orthographicSizeMax / CameraManager.Height / RangeMult);
           float Rows = SphereManager.Rows;
           Min = (int)-(Rows / Devide);
           Max = (int)(Rows / Devide); 
        }
    }
}
