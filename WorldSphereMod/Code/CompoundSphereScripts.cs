using CompoundMeshes;
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
        public static float StaticTileTexture(int Tile)
        {
            return Core.Sphere.WorldTileTexture(Core.Sphere.GetTile(Tile).SphereToWorld());
        }
        public static float Scale(this StaticTile tile)
        {
            return Scales[tile.Index];
        }
        public static Vector2 GetPosition(int index)
        {
            var tile = Core.Sphere.GetTile(index);
            return new Vector2(tile.X, tile.Y);
        }
        public static float[] Scales;
        public static float StaticTileHeight(StaticTile Tile)
        {
            WorldTile tile = Tile.SphereToWorld();
            float Height = Tools.TrueHeight(tile.GetHeight(), tile.main_type.render_z) * Core.Sphere.HeightMult;
            if (Core.Sphere.PerlinNoise)
            {
                Height *= Tools.PerlinNose(Tile.X, Tile.Y, Tile.Manager.Rows, Tile.Manager.Cols, 20);
            }
            Scales[Tile.Index] = Height / 2;
            return Height;
        }
        public static Vector3 StaticTileScaleFlat(int Index)
        {
            float Height = StaticTileHeight(Core.Sphere.GetTile(Index));
            return new Vector3(1, 1, Height);
        }
        public static Vector3 StaticTileScaleCube(StaticTile Tile)
        {
            float Height = StaticTileHeight(Tile);
            return new Vector3(1, 1, Height);
        }
        public static float IsWave(int Index) {
            return Core.Sphere.GetTile(Index).SphereToWorld().Type.ocean ? 1 : 0;
        }
        public static Vector3 StaticTileScaleCylindrical(int Index)
        {
            float Height = StaticTileHeight(Core.Sphere.GetTile(Index));
            return new Vector3(1, 1 + (Height * YConst), Height);
        }
        public static Quaternion CylindricalRotation(StaticTile tile)
        {
            return CylindricalRotation(new Vector2(tile.X, tile.Y));
        }
        public static Quaternion CylindricalRotation(Vector2 pos)
        {
            return Quaternion.AngleAxis(Tools.MathStuff.Angle(pos.y, pos.x), Vector3.forward) * ConstRot;
        }
        public static Quaternion FlatRotation(StaticTile _)
        {
            return ConstRot * ToUpright;
        }
        public static Quaternion CubeRotation(Vector2 _)
        {
            return ConstRot * ToUpright;
        }
        public static Quaternion FlatRotation(Vector2 _)
        {
            return ConstRot * ToUpright;
        }
        public static Vector3 CartesianToFlat(StaticHandler manager, float X, float Y, float Height = 0)
        {
            return new Vector3(X, Height, Y + ZDisplacement);
        }
        public static Vector3 FlatToCartesian(StaticHandler manager, float x, float y, float z)
        {
            return new Vector3(x, z - ZDisplacement, y);
        }
        public static Vector2 FlatToCartesianFast(StaticHandler manager, float x, float y, float z)
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
        public static Vector3 CartesianToCylindrical(StaticHandler manager, float X, float Y, float Height = 0)
        {
            Vector2 xy = Tools.MathStuff.PointOnCircle(-X, Core.Sphere.Radius, Height);
            float z = Y + ZDisplacement;
            return new Vector3(xy.x, xy.y, z);
        }
        public static Vector3 CylindricalToCartesian(StaticHandler manager, float x, float y, float z)
        {
            float X = manager.Clamp(Tools.MathStuff.Flip(Mathf.Atan2(y, x) / (2f * Mathf.PI) * manager.Rows, manager.Rows), 0);
            float Y = z - ZDisplacement;
            float Height = Mathf.Sqrt((x * x) + (y * y)) - Core.Sphere.Radius;
            return new Vector3(X, Y, Height);
        }
        //doesnt calculate height
        public static Vector2 CylindricalToCartesianFast(StaticHandler manager, float x, float y, float z)
        {
            float X = manager.Clamp(Tools.MathStuff.Flip(Mathf.Atan2(y, x) / (2f * Mathf.PI) * manager.Rows, manager.Rows), 0);
            float Y = z - ZDisplacement;
            return new Vector2Int((int)X, (int)Y);
        }
        public static void CylindricalInitiation(StaticHandler Manager)
        {
            GameObject Cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Cylinder.transform.SetPositionAndRotation(new Vector3(0, 0, (Manager.Cols / 2) + ZDisplacement), Quaternion.Euler(-90, 0, 0));
            Cylinder.transform.localScale = new Vector3(Core.Sphere.Radius*2, Manager.Cols / 2, Core.Sphere.Radius*2);
            Object.Destroy(Cylinder.GetComponent<CapsuleCollider>());
            Object.Destroy(Cylinder.GetComponent<MeshRenderer>());
            Cylinder.AddComponent<MeshCollider>();
            Cylinder.transform.parent = Manager.Manager.transform;
        }
        public static void FlatInitiation(StaticHandler Manager)
        {
            GameObject Quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Quad.transform.SetPositionAndRotation(new Vector3((Manager.Rows / 2) - 0.5f, 0, (Manager.Cols / 2) - 0.5f + ZDisplacement), Quaternion.Euler(90, 0, 0));
            Quad.transform.localScale = new Vector3(Manager.Rows, Manager.Cols, 1);
            Object.Destroy(Quad.GetComponent<MeshRenderer>());
            Quad.GetComponent<MeshCollider>().convex = true; //why the fuck?
            Quad.transform.parent = Manager.Manager.transform;
        }
        public static int getdisplaymode()
        {
            return World.world.quality_changer.isLowRes() ? 0 : 1;
        }
        static float RangeMult => Core.savedSettings.RenderRange;
        static float BaseRange => 4 - (1 / RangeMult);
        public static void RenderRange(StaticHandler StaticHandler, out Range X, out Range Y)
        {
            float Devide = BaseRange + (CameraManager.Manager.orthographic_size_max / CameraManager.Height / RangeMult);

            float Rows = StaticHandler.Rows;
            float Cols = StaticHandler.Cols;
            int Min = (int)-(Rows / Devide);
            int Max = (int)(Rows / Devide);
            X = new Range(Min, Max);

            Min = (int)-(Cols / Devide) * 4;
            Max = (int)(Cols / Devide) * 4;
            Y = new Range(Min, Max);
        }
        public static void RenderRangeFlat(StaticHandler StaticHandler, out Range X, out Range Y)
        {
            float Devide = (BaseRange + (CameraManager.Manager.orthographic_size_max / CameraManager.Height / RangeMult)) / 4;

            float Rows = StaticHandler.Rows;
            int Min = Mathf.Max((int)-(Rows / Devide), -(int)CameraManager.Position.x);
            int Max = Mathf.Min((int)(Rows / Devide), Core.Sphere.Width - (int)CameraManager.Position.x);
            X = new Range(Min, Max);

            float Cols = StaticHandler.Cols;
            Min = (int)-(Cols / Devide);
            Max = (int)(Cols / Devide);
            Y = new Range(Min, Max);
        }
        public static void RenderRangeCube(StaticHandler StaticHandler, out Range X, out Range Y)
        {
            float Devide = (BaseRange + (CameraManager.Manager.orthographic_size_max / CameraManager.Height / RangeMult)) / 4;

            float Rows = StaticHandler.Rows;
            int Min = Mathf.Max((int)-(Rows / Devide), -(int)CameraManager.Position.x);
            int Max = Mathf.Min((int)(Rows / Devide), Core.Sphere.Width - (int)CameraManager.Position.x);
            X = new Range(Min, Max);

            float Cols = StaticHandler.Cols;
            Min = (int)-(Cols / Devide);
            Max = (int)(Cols / Devide);
            Y = new Range(Min, Max);
        }
        public static Vector3 CubeToCartesian(StaticHandler manager, float x, float y, float z)
        {
            return Tools.Cube.ToWorld(new Vector2(x, y), z);
        }
        public static Vector3 CartesianToCube(StaticHandler manager, float x, float y, float z)
        {
            return Tools.Cube.To2D(new Vector3(x, y, z));
        }
        public static Vector2 CartesianToCubeFast(StaticHandler manager, float x, float y, float z)
        {
            return Tools.Cube.To2D(new Vector3(x, y, z));
        }
        public static Quaternion CubeRotation(StaticTile tile)
        {
            return Tools.Cube.GetRegion(new Vector2(tile.X, tile.Y)).Direction;
        }
        public static void CubeInitiation(StaticHandler Manager)
        {
            GameObject Cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Cube.transform.position = new Vector3(0, 0, ZDisplacement);
            Cube.transform.localScale = new Vector3(Tools.Cube.Size, Tools.Cube.Size, Tools.Cube.Size);
            Object.Destroy(Cube.GetComponent<MeshRenderer>());
        }
    }
}