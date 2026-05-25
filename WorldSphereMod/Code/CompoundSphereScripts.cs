using CompoundSpheres;
using System.Runtime.CompilerServices;
using UnityEngine;
using WorldSphereMod.NewCamera;
using static UnityEngine.UI.CanvasScaler;
using static WorldSphereMod.Constants;
namespace WorldSphereMod
{
    public delegate float Gate(float a, float b, float c);
    public struct PhaseGate
    {
        public Gate GetDist;
        public Gate GetChange;
    }
    public static class CompoundSphereScripts
    {
        public static readonly PhaseGate DefaultGate = new()
        {
            GetDist = (a, b, c) => a - b,
            GetChange = (a, b, c) => a + b
        };
        public static readonly PhaseGate WrappedGate = new()
        {
            GetDist = (a, b, c) => Tools.MathStuff.WrappedDist(a, b, c),
            GetChange = (a, b, c) => Tools.MathStuff.WrappedChange(a, b, c)
        };
        public static int SphereTileTexture(SphereTile Tile)
        {
            return Core.Sphere.WorldTileTexture(Tile.SphereToWorld());
        }
        public static float SphereTileHeight(SphereTile Tile)
        {
            WorldTile tile = Tile.SphereToWorld();
            float Height = Tools.TrueHeight(tile.GetHeight(), tile.main_type.render_z);
            if (Core.Sphere.PerlinNoise)
            {
                Height *= Tools.PerlinNose(Tile.X, Tile.Y, Tile.Manager.Rows, Tile.Manager.Cols, 20);
            }
            return Height;
        }
        public static Vector3 SphereTileScaleFlat(SphereTile Tile)
        {
            float Height = SphereTileHeight(Tile);
            return new Vector3(1, 1, Height * Core.Sphere.HeightMult);
        }
        public static Vector3 SphereTileScaleCube(SphereTile Tile)
        {
            float Height = SphereTileHeight(Tile);
            return new Vector3(1, 1, Height * Core.Sphere.HeightMult);
        }
        public static Vector3 SphereTileScaleCylindrical(SphereTile Tile)
        {
            float Height = SphereTileHeight(Tile);
            return new Vector3(1, 1 + (Height * YConst), Height * Core.Sphere.HeightMult);
        }
        public static Vector3 SphereTileAddedColor(SphereTile Tile)
        {
            Color32 color = Core.Sphere.GetAddedColor(Tile.Index());
            return new Vector3(color.r, color.g, color.b) / 255;
        }
        public static Quaternion CylindricalRotation(Vector2 Pos)
        {
            return Quaternion.AngleAxis(Tools.MathStuff.Angle(Pos.y, Pos.x), Vector3.forward) * ConstRot;
        }
        public static Quaternion FlatRotation(Vector2 Pos)
        {
            return ConstRot * ToUpright;
        }
        public static Color32 SphereTileColor(SphereTile SphereTile)
        {
            return Core.Sphere.GetColor(SphereTile.Index());
        }
        public static Vector3 CartesianToFlat(SphereManager manager, float X, float Y, float Height = 0)
        {
            return new Vector3(X, Height, Y + ZDisplacement);
        }
        public static Vector3 FlatToCartesian(SphereManager manager, float x, float y, float z)
        {
            return new Vector3(x, z - ZDisplacement, y);
        }
        public static Vector2 FlatToCartesianFast(SphereManager manager, float x, float y, float z)
        {
            return new Vector2(x, z - ZDisplacement);
        }
        public static Vector2 GetMovementVectorSpherical(float Speed, bool Vertical)
        {
            Vector3 vector;
            float yRotation = RotateCamera.Rotation.y;

            float rad = yRotation * Mathf.Deg2Rad;

            bool Inversed = Core.savedSettings.InvertedCameraMovement;
            if (Core.savedSettings.CameraRotatesWithWorld)
            {
                Inversed = !Inversed;
            }

            if (Inversed ? !Vertical : Vertical)
            {
                vector = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)).normalized;
                if (Core.savedSettings.CameraRotatesWithWorld)
                {
                    vector.z *= RotateCamera.InvertMult;
                }
            }
            else
            {
                vector = new Vector3(Mathf.Cos(rad), 0, -Mathf.Sin(rad)).normalized;
                vector.x *= RotateCamera.InvertMult;
                if (Core.savedSettings.CameraRotatesWithWorld)
                {
                    vector *= -1;
                }
                else
                {
                    vector.z *= RotateCamera.InvertMult;
                }
            }
            return new Vector2(vector.x * Speed, vector.z * Speed * RotateCamera.InvertMult);
        }
        public static Vector2 GetMovementVectorFlat(float Speed, bool Vertical)
        {
            Vector3 vector = GetMovementVectorSpherical(Speed, Vertical);
            if (!Core.savedSettings.CameraRotatesWithWorld)
            {
                vector.x *= RotateCamera.InvertMult;
            }
            return vector;
        }
        public static Vector2 GetMovementVectorCube(float Speed, bool Vertical)
        {
            Vector3 vector = GetMovementVectorSpherical(Speed, Vertical);
            if (!Core.savedSettings.CameraRotatesWithWorld)
            {
                vector.x *= RotateCamera.InvertMult;
            }
            return vector;
        }
        public static Vector3 CartesianToCylindrical(SphereManager manager, float X, float Y, float Height = 0)
        {
            Vector2 xy = Tools.MathStuff.PointOnCircle(-X, manager.Radius, Height);
            float z = Y + ZDisplacement;
            return new Vector3(xy.x, xy.y, z);
        }
        public static Vector3 CylindricalToCartesian(SphereManager manager, float x, float y, float z)
        {
            float X = manager.Clamp(Tools.MathStuff.Flip(Mathf.Atan2(y, x) / (2f * Mathf.PI) * manager.Rows, manager.Rows), 0);
            float Y = z - ZDisplacement;
            float Height = Mathf.Sqrt((x * x) + (y * y)) - manager.Radius;
            return new Vector3(X, Y, Height);
        }
        //doesnt calculate height
        public static Vector2 CylindricalToCartesianFast(SphereManager manager, float x, float y, float z)
        {
            float X = manager.Clamp(Tools.MathStuff.Flip(Mathf.Atan2(y, x) / (2f * Mathf.PI) * manager.Rows, manager.Rows), 0);
            float Y = z - ZDisplacement;
            return new Vector2Int((int)X, (int)Y);
        }
        public static void CylindricalInitiation(SphereManager Manager)
        {
            GameObject Cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Cylinder.transform.SetPositionAndRotation(new Vector3(0, 0, (Manager.Cols / 2) + ZDisplacement), Quaternion.Euler(-90, 0, 0));
            Cylinder.transform.localScale = new Vector3(Manager.Diameter, Manager.Cols / 2, Manager.Diameter);
            Object.Destroy(Cylinder.GetComponent<CapsuleCollider>());
            Object.Destroy(Cylinder.GetComponent<MeshRenderer>());
            Cylinder.AddComponent<MeshCollider>();
            Cylinder.transform.parent = Manager.transform;
        }
        public static void FlatInitiation(SphereManager Manager)
        {
            GameObject Quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Quad.transform.SetPositionAndRotation(new Vector3((Manager.Rows / 2) - 0.5f, 0, (Manager.Cols / 2) - 0.5f + ZDisplacement), Quaternion.Euler(90, 0, 0));
            Quad.transform.localScale = new Vector3(Manager.Rows, Manager.Cols, 1);
            Object.Destroy(Quad.GetComponent<MeshRenderer>());
            Quad.GetComponent<MeshCollider>().convex = true; //why the fuck?
            Quad.transform.parent = Manager.transform;
        }
        public static DisplayMode getdisplaymode(SphereManager _)
        {
            return World.world.quality_changer.isLowRes() ? DisplayMode.ColorOnly : DisplayMode.TextureOnly;
        }
        static float RangeMult => Core.savedSettings.RowRange;
        static float BaseRange => 4 - (1 / RangeMult);
        public static void RenderRange(SphereManager SphereManager, out int Min, out int Max)
        {
            float Devide = BaseRange + (CameraManager.Manager.orthographic_size_max / CameraManager.Height / RangeMult);
            float Rows = SphereManager.Rows;
            Min = (int)-(Rows / Devide);
            Max = (int)(Rows / Devide);
        }
        public static void RenderRangeFlat(SphereManager SphereManager, out int Min, out int Max)
        {
            float Devide = (BaseRange + (CameraManager.Manager.orthographic_size_max / CameraManager.Height / RangeMult)) / 4;

            float Rows = SphereManager.Rows;
            Min = Mathf.Max((int)-(Rows / Devide), -(int)CameraManager.Position.x);
            Max = Mathf.Min((int)(Rows / Devide), Core.Sphere.Width - (int)CameraManager.Position.x);
        }
        public static void RenderRangeCube(SphereManager SphereManager, out int Min, out int Max)
        {
            float Devide = (BaseRange + (CameraManager.Manager.orthographic_size_max / CameraManager.Height / RangeMult)) / 4;

            float Rows = SphereManager.Rows;
            Min = Mathf.Max((int)-(Rows / Devide), -(int)CameraManager.Position.x);
            Max = Mathf.Min((int)(Rows / Devide), Core.Sphere.Width - (int)CameraManager.Position.x);
        }
        public static Vector3 CubeToCartesian(SphereManager manager, float x, float y, float z)
        {
            return Tools.Cube.ToWorld(new Vector2(x, y), z);
        }
        public static Vector3 CartesianToCube(SphereManager manager, float x, float y, float z)
        {
            return Tools.Cube.To2D(new Vector3(x, y, z));
        }
        public static Vector2 CartesianToCubeFast(SphereManager manager, float x, float y, float z)
        {
            return Tools.Cube.To2D(new Vector3(x, y, z));
        }
        public static Quaternion CubeRotation(Vector2 Pos)
        {
            return Tools.Cube.GetRegion(Pos).Direction;
        }
        public static void CubeInitiation(SphereManager Manager)
        {
            GameObject Cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Cube.transform.position = new Vector3(0, 0, ZDisplacement);
            Cube.transform.localScale = new Vector3(Tools.Cube.Size, Tools.Cube.Size, Tools.Cube.Size);
            Object.Destroy(Cube.GetComponent<MeshRenderer>());
            Cube.GetComponent<MeshCollider>().convex = true;
        }
    }
}