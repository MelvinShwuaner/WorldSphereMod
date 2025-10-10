using CompoundSpheres;
using System;
using UnityEngine;
using HarmonyLib;
using WorldSphereMod.NewCamera;
using System.Reflection;
using static WorldSphereMod.Tools.MathStuff;
using System.Runtime.CompilerServices;
using static WorldSphereMod.Constants;
using System.Collections.Concurrent;
using WorldSphereMod.General;
using System.Collections.Generic;
using System.Linq;
using static UnityEngine.UI.CanvasScaler;
namespace WorldSphereMod
{
    public static class Tools
    {
        public static float PerlinNose(float x, float y, float width, float height, float Scale)
        {
            float tX = x / width;
            float tY = y / height;
            return Mathf.PerlinNoise(tX * Scale, tY * Scale) + 0.5f;
        }
        public static T[] ExpandArray<T>(T[] Array, int NewLength)
        {
            T[] NewArray = new T[NewLength];
            for(int i = 0; i < NewLength; i++)
            {
                if (i < Array.Length)
                {
                    NewArray[i] = Array[i];
                }
                else
                {
                    NewArray[i] = default;
                }
            }
            return NewArray;
        }
        public static void ResetTexture(this Texture2D texture)
        {
            Color[] clearPixels = new Color[texture.width * texture.height];
            for (int i = 0; i < clearPixels.Length; i++)
            {
                clearPixels[i] = Color.clear;
            }
            texture.SetPixels(clearPixels);
            texture.Apply();
        }
        public static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);

            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                if (property.CanWrite)
                {
                    property.SetValue(copy, property.GetValue(original, null), null);
                }
            }

            return copy as T;
        }
        public static void AddRotation(this Transform Transform, Quaternion rot)
        {
            Transform.rotation *= rot;
        }
        public static void AddChildrenToList(this Transform Transform, ref List<Transform> list)
        {
            foreach(Transform transform in Transform)
            {
                list.Add(transform);
                AddChildrenToList(transform, ref list);
            }
        }
        public static List<Transform> GetAllChildren(this Transform transform)
        {
            List<Transform> Children = new List<Transform>();
            AddChildrenToList(transform, ref Children);
            return Children;
        }
        public static void Transpile(this Harmony harmony, MethodInfo Method, HarmonyMethod Transpiler)
        {
            harmony.Patch(Method, null, null, Transpiler);
        }
        public static void Transpile(this Harmony harmony, MethodInfo Method, Transpiler Transpiler)
        {
            harmony.Transpile(Method, new HarmonyMethod(Transpiler.Method));
        }
        public static Vector3 RotateLocalPointAroundPivot(ref Vector3 point, ref Vector3 pivot, ref Vector3 angles)
        {
            return Quaternion.Euler(angles) * point + pivot;
        }
        public static bool FindNext(this CodeMatcher Matcher, params CodeMatch[] matches)
        {
            Matcher.Advance(1);
            Matcher.MatchForward(false, matches);
            return Matcher.IsValid;
        }
        public static Color32[] PixelsFromSpriteAtlas(this Sprite sprite)
        {
            Color32[] Colors = new Color32[64];
            Color32[] Texture = sprite.texture.GetPixels32();
            Rect SpriteLocation = sprite.textureRect;
            int Y = (int)SpriteLocation.y;
            int X = (int)SpriteLocation.x;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Colors[(i * 8) + j] = Texture[((i+Y) * sprite.texture.width) + j + X];
                }
            }
            return Colors;
        }
        public static void Add<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            dict.TryAdd(key, value);
        }
        public static float GetDistSpeedMult(this Actor Actor)
        {
            if (!Core.IsWorld3D)
            {
                return 1;
            }
            WorldTile tile = GetTile(Actor.next_step_position.AsInt());
            if(tile == null)
            {
                return 1;
            }
            float Dif = Mathf.Abs(tile.TileHeight() - Actor.current_tile.TileHeight());
            return 1 - Mathf.Clamp(Dif / TileHeightDiffSpeed, 0, 0.9f);
        }
        public static WorldTile GetTile(Vector2Int Pos)
        {
            return World.world.GetTile(Pos.x, Pos.y);
        }
        public static bool IsUpright(this Building building)
        {
            return !PerpBuildings.ContainsKey(building.asset.id);
        }
        public static bool IsUpright(this Actor actor)
        {
            return !actor.isLying() && !PerpActors.ContainsKey(actor.asset.id);
        }
        public static void MoveLabels(CodeInstruction PrevInstruction, CodeInstruction NewInstruction)
        {
            PrevInstruction.MoveLabelsTo(NewInstruction);
        }
        public static bool ScreenPointToRay(this Camera Camera, Vector2 Pos, out Ray Ray)
        {
            Vector2 viewportPos = Camera.ScreenToViewportPoint(Pos);
            return ViewPortToRay(Camera, viewportPos, out Ray);
        }
        public static bool ViewPortToRay(this Camera Camera, Vector2 viewportPos, out Ray Ray)
        {
            Ray = default;
            if (viewportPos.x < 0f || viewportPos.x > 1f || viewportPos.y < 0f || viewportPos.y > 1f)
            {
                return false;
            }
            try
            {
                Ray = Camera.ViewportPointToRay(viewportPos);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static Vector2Int AsInt(this Vector2 Vector)
        {
            return new Vector2Int((int)Vector.x, (int)Vector.y);
        }
        public static Vector2Int AsIntClamped(this Vector2 Vector)
        {
            return new Vector2Int(Math.Clamp((int)Vector.x, 0, Core.Sphere.Width - 1), Math.Clamp((int)Vector.y, 0, Core.Sphere.Height - 1));
        }
        public static bool IntersectMesh(Ray ray, out Vector2 pVector)
        {
            pVector = Vector2Int.zero;
            if (Physics.Raycast(ray, out var hitinfo, Mathf.Infinity))
            {
                pVector = To2D(hitinfo.point.x, hitinfo.point.y, hitinfo.point.z);
                return true;
            }
            return false;
        }
        public static WorldTile GetTile(this BaseEffect Effect)
        {
            return Effect.tile ?? World.world.GetTile((int)Effect.transform.position.x, (int)Effect.transform.position.y);
        }
        public static bool IsBase(this MapLayer layer)
        {
            return Core.Sphere.BaseLayers.Contains(layer);
        }
        public static float TileHeight(this WorldTile Tile)
        {
            return Tile.WorldToSphere().Scale.z * 0.5f;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool EqualsColor(this Color32 mycolor, Color32 color)
        {
            return *(uint*)&mycolor == *(uint*)&color;
        }
        public static Vector2 Direction3D(Vector2 v1, Vector2 v2)
        {
            return MathStuff.Direction3D(v1.x, v2.x, v1.y, v2.y);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 To3D(float x, float y, float z)
        {
            return Core.Sphere.SpherePos(x, y, z);
        }
        public static Vector3 To3D(this Vector2 v, float Height = 0)
        {
            return To3D(v.x, v.y, Height);
        }
        public static Vector3 To3D(this Vector3 v)
        {
            return To3D(v.x, v.y, v.z);
        }
        //not perfect, but good enough
        //im not even sure if this works as intended, visually it looks better but idk
        public static float GetTileHeightSmooth(this Vector2 Pos)
        {
            Vector2Int pos = Pos.AsInt();
            WorldTile Tile = World.world.GetTile(pos.x, pos.y);
            if (Tile == null)
            {
                return -Core.Sphere.Radius;
            }
            Vector2 SubPos = Pos - pos;
            Vector2Int NextPos = new Vector2Int(Round(SubPos.x), Round(SubPos.y));
            WorldTile ToTile = World.world.GetTile(pos.x + NextPos.x, pos.y + NextPos.y);
            if (ToTile == null)
            {
                return Tile.TileHeight();
            }
            Vector2Int posrounded = new Vector2Int(Mathf.RoundToInt(SubPos.x), Mathf.RoundToInt(SubPos.y));
            return Mathf.LerpUnclamped(Tile.TileHeight(), ToTile.TileHeight(), HalfRoot - Vector2.Distance(SubPos, posrounded));
        }
        public static Vector3 To3DTileHeight(this Vector2 v, float ExtraHeight = 0)
        {
            return To3D(v, GetTileHeightSmooth(v)+ExtraHeight);
        }
        public static Vector3 To3DTileHeight(this Vector3 v, bool UseHeight)
        {
            return To3D(v, GetTileHeightSmooth(v) + (UseHeight ? v.z : 0));
        }
        public static bool Is3D(this Vector3 v)
        {
            return v.z >= ZDisplacement;
        }
        public static Vector2 To2D(this Vector3 v)
        {
            return To2D(v.x, v.y, v.z);
        }
        public static Vector2 To2D(float x, float y, float z)
        {
            return Core.Sphere.TilePos(x, y, z);
        }
        public static Vector3 To2DWithHeight(this Vector3 v)
        {
            return To2DWithHeight(v.x, v.y, v.z);
        }
        public static Vector3 To2DWithHeight(float x, float y, float z)
        {
            return Core.Sphere.TilePosWithHeight(x, y, z);
        }
        public static Vector3 Get3DPos(this Building Building)
        {
            return To3DTileHeight(Building.cur_transform_position, 0.1f);
        }
        public static Vector3 Get3DRot(this Building Building)
        {
            Quaternion Rot = Building.current_rotation.AsQuaternion();
            GetCameraAngle(out Quaternion rot, Building.cur_transform_position, Building.IsUpright());
            return (rot * Rot).eulerAngles;
        }
        public static Vector3 Get3DRot(this Actor tActor)
        {
            if (!tActor.IsUpright())
            {
                return GetRotation(tActor.cur_transform_position.AsIntClamped()).eulerAngles;
            }
            Quaternion Rot = tActor.updateRotation().AsQuaternion();
            GetCameraAngle(out Quaternion rot, tActor.cur_transform_position);
            return (rot * Rot).eulerAngles;
        }
        public static float TrueHeight(int HeightID, int BaseHeight = -1) => HeightID switch
        {
            #region ocean
            0 => 0.01f,//nothing
            1 => 0.1f, //pit deep
            2 => 0.5f, //pit close
            3 => 0.8f, //pit shallow
            4 => 1f, //deep
            5 => 1.5f, //close
            6 => 1.8f, //shallow
            17 => 2f, //ice
            18 => 2.2f, //sand
            19 => 2.3f, //snowy sand
            #endregion
            #region biomes
            20 => 2.5f, //soil low //high is 1.5 higher then low
            21 => 4f, //soil high
            22 => 2.55f, //wasteland
            23 => 4.05f, //wasteland high
            28 => 2.6f,//swamp
            31 => 4.1f, //swamp high
            29 => 2.65f, //desert
            74 => 4.15f,
            34 => 2.7f, //biomass
            35 => 4.2f,
            36 => 2.75f, //pumpkin
            37 => 4.25f,
            38 => 2.8f, //cybertile
            39 => 4.3f,
            40 => 2.85f, //grass
            41 => 4.35f,
            42 => 2.9f, //coruption
            43 => 4.4f,
            44 => 2.95f, //enchanted
            45 => 4.45f,
            46 => 3f,//mushroom
            47 => 4.5f,
            48 => 3.05f,//birch
            49 => 4.55f,
            50 => 3.1f, //maple
            51 => 4.6f,
            52 => 3.15f, //rocks
            53 => 4.65f,
            54 => 3.2f, //garlic
            55 => 4.7f,
            56 => 3.25f, //flowers
            57 => 4.75f,
            58 => 3.3f, //celestial
            59 => 4.8f,
            60 => 3.35f, //singularity
            61 => 4.85f,
            62 => 3.4f, //clover
            63 => 4.9f,
            64 => 3.45f, //paradox
            65 => 4.95f,
            66 => 3.5f, //solitude
            67 => 5f,
            68 => 3.55f, //savannah
            69 => 5.05f,
            70 => 3.6f, //jungle
            71 => 5.1f,
            72 => 3.65f, //infernal
            73 => 5.15f,
            75 => 3.7f, //crystal
            76 => 5.2f,
            77 => 3.75f, //candy
            78 => 5.25f,
            79 => 3.8f, //lemon
            80 => 5.5f,
            83 => 3.85f,//permafrost
            84 => 5.55f,
            85 => 3.9f, //tumor
            86 => 5.6f,
            #endregion
            #region peaks
            92 => Randy.randomFloat(8.5f, 9f), //hills
            93 => TrueHeight(BaseHeight) + 0.1f,
            94 => Randy.randomFloat(12, 17), //mountains
            95 => TrueHeight(BaseHeight) + 0.1f,
            96 => 19f, //summit
            97 => 19.2f,
            #endregion
            #region special
            30 => TrueHeight(BaseHeight) - 0.25f, //road
            32 => 2.8f, //farm
            33 => 5.8f, //fuse
            87 => 3f,//landmine
            88 => 2,//waterbomb
            89 => 6f,//tnt timed
            90 => 5.8f,//tnt
            91 => 5.8f,//fireworks
            98 => Randy.randomFloat(3, 9),//grey goo
            #endregion
            #region lava
            24 => 3,
            25 => 4.5f,
            26 => 6,
            27 => 7.5f,
            #endregion
            #region walls
            10 => 9.5f,//evil
            11 => 9,//order
            12 => 10,//ancient
            13 => 8, //wild
            14 => 7.5f, //green
            15 => 8.5f, //iron
            16 => 10.5f, //light
            #endregion
            _ => 3f
        };
        public static int AddLooped(int x1, int X2)
        {
            if (!Core.IsWorld3D)
            {
                return x1 + X2;
            }
            return (int)Wrap(x1, X2, Core.Sphere.Width);
        }
        public static void To3DBounds(ref int pX, ref int pY)
        {
            pX = (int)Wrap(pX, 0, MapBox.width);
        }
        public static int GetHeight(this WorldTile pTile)
        {
            return pTile.DisplayedType().render_z;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int AsInt(this Vector3 Vector)
        {
            return new Vector2Int((int)Vector.x, (int)Vector.y);
        }
        #region Rotations
        public static Quaternion AsQuaternion(this Vector3 Angle)
        {
            return Quaternion.Euler(Angle);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion GetRotation(Vector2Int Pos)
        {
            return Core.Sphere.GetTile(Pos.x, Pos.y).Rotation;
        }
        public static Vector2Int AsIntClamped(this Vector3 Vector)
        {
            return new Vector2Int(Math.Clamp((int)Vector.x, 0, Core.Sphere.Width-1), Math.Clamp((int)Vector.y, 0, Core.Sphere.Height-1));
        }
        public static Quaternion GetUprightRotation(Vector2Int Pos)
        {
            return GetRotation(Pos) * ToUpright;
        }
        public static Quaternion RotateToCamera(Vector2 Pos)
        {
            Vector2 direction = Direction3D(CameraManager.Position, Pos);
            float angle = Angle(direction.y, direction.x);
            return Quaternion.AngleAxis(angle, Vector3.up);
        }
        public static Quaternion RotateToCamera(ref Vector3 Pos)
        {
            return RotateToCamera(Pos.To2D());
        }
        public static Quaternion RotateToCameraAtTile(Vector2Int Pos)
        {
            return GetUprightRotation(Pos) * RotateToCamera(Pos);
        }
        public static bool GetCameraAngle(out Quaternion quaternion, Vector3 position, bool Upright = true)
        {
            if (Core.savedSettings.RotateStuffToCamera && Upright)
            {
                quaternion = RotateToCameraAtTile(position.AsIntClamped());
                return true;
            }
            quaternion = Upright ? GetUprightRotation(position.AsIntClamped()) : GetRotation(position.AsIntClamped());
            return false;
        }
        #endregion
        public static Color32 Normalised(this Color32 color)
        {
            if (color.a == 255) return color;
            if (color.a == 0) return new Color32(0, 0, 0, 0);

            byte r = (byte)(color.r * color.a / 255);
            byte g = (byte)(color.g * color.a / 255);
            byte b = (byte)(color.b * color.a / 255);

            return new Color32(r, g, b, color.a);
        }
        public static int Index(this SphereTile Tile)
        {
            return Tile.SphereToWorld().data.tile_id;
        }
        public static WorldTile SphereToWorld(this SphereTile Tile)
        {
            return World.world.GetTileSimple(Tile.X, Tile.Y);
        }
        public static SphereTile WorldToSphere(this WorldTile Tile)
        {
            return Core.Sphere.GetTile(Tile.x, Tile.y);
        }
        public static TileTypeBase DisplayedType(this WorldTile pTile)
        {
            TileTypeBase Type = pTile.main_type;
            if (pTile.Type != null)
            {
                Type = pTile.Type;
            }
            return Type;
        }
        public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDistanceDelta)
        {
            if (!Core.IsWorld3D || !Core.Sphere.IsWrapped)
            {
                return Vector2.MoveTowards(current, target, maxDistanceDelta);
            }
            return WrappedMoveTowards(current, target, maxDistanceDelta, Core.Sphere.Width);
        }
        public static Vector3 MoveTowardsV3(Vector3 current, Vector3 target, float maxDistanceDelta)
        {
            if (!Core.IsWorld3D || !Core.Sphere.IsWrapped)
            {
                return Vector3.MoveTowards(current, target, maxDistanceDelta);
            }
            return WrappedMoveTowards(current, target, maxDistanceDelta, Core.Sphere.Width);
        }
        //behold,the class i had to use ai for
        public static class MathStuff
        {
            public static float Flip(float X, float Max)
            {
                float Half = Max / 2;
                if(X > Half)
                {
                   return X + ((Max-X)*2);
                }
                return X - (X*2);
            }
            public static Vector3 WrappedMoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta, float worldWidth)
            {
                float dx = target.x - current.x;
                if (Mathf.Abs(dx) > worldWidth / 2f)
                {
                    if (dx > 0)
                        target.x -= worldWidth;
                    else
                        target.x += worldWidth;
                }
                Vector3 newPos = Vector3.MoveTowards(current, target, maxDistanceDelta);
                if (newPos.x < 0)
                    newPos.x += worldWidth;
                else if (newPos.x >= worldWidth)
                    newPos.x -= worldWidth;
                return newPos;
            }
            //dist but with direction
            public static Vector2 Direction3D(float x1, float x2, float y1, float y2)
            {
                float x = Core.Sphere.IsWrapped ? WrappedDist(x2, x1, Core.Sphere.Width) : x2 - x1;
                float y = y1 - y2;
                return new Vector2(x, y);
            }
            public static float Dist(float x1, float x2, float y1, float y2)
            {
                return Mathf.Sqrt(SquaredDist(x1, x2, y1, y2));
            }
            public static Vector2 Lerp3D(Vector2 a, Vector2 b, float t)
            {
                t = Mathf.Clamp01(t);
                Vector2 result = new Vector2(a.x + WrappedDist(b.x, a.x) * t, a.y + (b.y - a.y) * t);
                return result;
            }
            public static float SquaredDist(float x1, float x2, float y1, float y2)
            {
                if (!Core.IsWorld3D)
                {
                    return (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);
                }
                return SquaredDist3D(x1, x2, y1, y2);
            }
            public static float WrappedDist(float a, float b)
            {
                if (!Core.Sphere.IsWrapped)
                {
                    return a - b;
                }
                return WrappedDist(a, b, Core.Sphere.Width);
            }
            public static float Angle(float y, float x)
            {
                return Mathf.Atan2(y, x) * Mathf.Rad2Deg;
            }
            public static float SquaredDist3D(float x1, float x2, float y1, float y2)
            {
                float x = WrappedDist(x1, x2);
                float y = y1 - y2;
                return (x * x) + (y * y);
            }
            //only between 0 and 1
            public static int Round(float num)
            {
                if (num < 0.5f)
                {
                    return -1;
                }
                return 1;
            }
            public static float Wrap(float Pos, float Change, float Max)
            {
                Pos += Change;
                if (!Core.Sphere.IsWrapped)
                {
                    return Pos;
                }
                if (Pos < 0)
                {
                    return Max + Pos;
                }
                return Pos % Max;
            }
            public static Vector2 PointOnCircle(float X, float Radius, float Height = 0)
            {
                float phi = X / Radius;
                float x = (Radius + Height) * Mathf.Cos(phi);
                float y = (Radius + Height) * Mathf.Sin(phi);
                return new Vector2(x, y);
            }
            public static float WrappedDist(float a, float b, float L)
            {
                float delta = a - b;
                if (delta > L / 2f)
                    delta -= L;
                else if (delta < -L / 2f)
                    delta += L;
                return delta;
            }
        }
    }
}
