using CompoundSpheres;
using UnityEngine;
using WorldSphereMod.NewCamera;
using static WorldSphereMod.Constants;
namespace WorldSphereMod
{
    public static class CompoundSphereScripts
    {
        public static int SphereTileTexture(SphereTile Tile)
        {
            return Core.Sphere.WorldTileTexture(Tile.SphereToWorld());
        }
        public static Vector3 SphereTileScale(SphereTile Tile)
        {
            float Height = Tools.TrueHeight(Tile.SphereToWorld().GetHeight(), Tile.SphereToWorld().main_type.render_z);
            return new Vector3(1, 1+(Height*YConst), Height*ZConst);
        }
        public static Vector3 SphereTileAddedColor(SphereTile Tile) {
            return (Vector4)Core.Sphere.GetAddedColor(Tile.Index());
        }
        public static Quaternion CylindricalRotation(SphereTile SphereTile)
        {
            return Quaternion.AngleAxis(Tools.MathStuff.Angle(SphereTile.Position.y, SphereTile.Position.x), Vector3.forward) * ConstRot;
        }
        public static Quaternion FlatRotation(SphereTile SphereTile)
        {
            return ConstRot* ToUpright;
        }
        public static Color32 SphereTileColor(SphereTile SphereTile)
        {
            return Core.Sphere.GetColor(SphereTile.Index());
        }
        public static Vector3 CartesianToFlat(SphereManager manager, float X, float Y, float Height = 0)
        {
            return new Vector3(-X, Height, Y+ZDisplacement);
        }
        public static Vector3 FlatToCartesian(SphereManager manager, float x, float y, float z)
        {
            return new Vector3(-x, z-ZDisplacement, y);
        }
        public static Vector2 FlatToCartesianFast(SphereManager manager, float x, float y, float z)
        {
            return new Vector2(-x, z-ZDisplacement);
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
        public static void CylindricalInitiation(SphereManager Manager)
        {
            GameObject Cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Cylinder.transform.SetPositionAndRotation(new Vector3(0, 0, (Manager.Cols / 2)+ZDisplacement), Quaternion.Euler(-90, 0, 0));
            Cylinder.transform.localScale = new Vector3(Manager.Diameter, Manager.Cols / 2, Manager.Diameter);
            Object.Destroy(Cylinder.GetComponent<CapsuleCollider>());
            Object.Destroy(Cylinder.GetComponent<MeshRenderer>());
            Cylinder.AddComponent<MeshCollider>();
            Cylinder.transform.parent = Manager.transform;
        }
        public static void FlatInitiation(SphereManager Manager)
        {
            GameObject Quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Quad.transform.SetPositionAndRotation(new Vector3(-(Manager.Rows / 2) + 0.5f, 0, (Manager.Cols / 2) - 0.5f+ZDisplacement), Quaternion.Euler(90, 0, 0));
            Quad.transform.localScale = new Vector3(Manager.Rows, Manager.Cols, 1);
            Object.Destroy(Quad.GetComponent<MeshRenderer>());
            Quad.GetComponent<MeshCollider>().convex = true; //why the fuck?
            Quad.transform.parent = Manager.transform;
        }
        public static DisplayMode getdisplaymode(SphereManager _)
        {
            return World.world.quality_changer.isLowRes() ? DisplayMode.ColorOnly : DisplayMode.TextureOnly;
        }
        static float RangeMult => Core.savedSettings.RenderRange;
        static float BaseRange => 4 - (1/RangeMult);
        public static void RenderRange(SphereManager SphereManager, out int Min, out int Max) 
        {
           float Devide = BaseRange + (CameraManager.Manager.orthographicSizeMax / CameraManager.Height / RangeMult);
           float Rows = SphereManager.Rows;
           Min = (int)-(Rows / Devide);
           Max = (int)(Rows / Devide); 
        }
        public static void RenderRangeFlat(SphereManager SphereManager, out int Min, out int Max)
        {
            float Devide = (BaseRange + (CameraManager.Manager.orthographicSizeMax / CameraManager.Height / RangeMult))/4;
            
            float Rows = SphereManager.Rows;
            Min = Mathf.Max((int)-(Rows / Devide), -(int)CameraManager.Position.x);
            Max = Mathf.Min((int)(Rows / Devide), Core.Sphere.Width - (int)CameraManager.Position.x);
        }
    }
}
