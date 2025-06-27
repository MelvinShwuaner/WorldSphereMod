using CompoundSpheres;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using static UnityEngine.GraphicsBuffer;
using WorldSphereMod.NewCamera;

namespace WorldSphereMod
{
    public static class Tools
    {
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
            for (int i = Y; i < SpriteLocation.y + 8; i++)
            {
                for (int j = X; j < SpriteLocation.x + 8; j++)
                {
                    Colors[((i - Y) * 8) + (j - X)] = Texture[(i * sprite.texture.width) + j];
                }
            }
            return Colors;
        }
        public static void MoveLabels(CodeInstruction PrevInstruction, CodeInstruction NewInstruction)
        {
            PrevInstruction.MoveLabelsTo(NewInstruction);
        }
        public static bool ViewPortToRay(this Camera Camera, Vector2 Pos, out Ray Ray)
        {
            Ray = default;
            Pos.x *= Screen.width;
            Pos.y *= Screen.height;
            Vector2 viewportPos = Camera.ScreenToViewportPoint(Pos);
            if (viewportPos.x <= 0f || viewportPos.x >= 1f || viewportPos.y <= 0f || viewportPos.y >= 1f)
            {
                return false;
            }
            try
            {
                Ray = Camera.main.ViewportPointToRay(viewportPos);
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
        public static bool IntersectMesh(Ray ray, out Vector2Int pVector)
        {
            pVector = Vector2Int.zero;
            if (Physics.Raycast(ray, out var hitinfo))
            {
                pVector = To2D(hitinfo.point.x, hitinfo.point.y, hitinfo.point.z).AsInt();
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
        public static float Angle(float X)
        {
            return Mathf.Tan(X / Core.Sphere.Radius);
        }
        public static float InverseAngle(float X)
        {
            return Mathf.Atan(X) * Core.Sphere.Radius;
        }
        const float HeightMult = 1.5f;
        public static float TileHeight(this WorldTile Tile)
        {
            return TrueHeight(Tile.DisplayedType().render_z) * HeightMult;
        }
        public static Vector3 To3D(this WorldTile Tile)
        {
            return To3D(Tile.x, Tile.y, Tile.TileHeight());
        }
        public static bool EqualsColor(this Color32 mycolor, Color32 color)
        {
            return mycolor.a == color.a && mycolor.r == color.r && mycolor.g == color.g && mycolor.b == color.b;
        }
        public static Vector2 Direction3D(Vector2 v1, Vector2 v2)
        {
            return Direction3D(v1.x, v2.x, v1.y, v2.y);
        }
        public static Vector2 Direction3D(float x1, float x2, float y1, float y2)
        {
            float x = InverseAngle(Angle(x1) - Angle(x2));
            float y = y1 - y2;
            return new Vector2(x, y);
        }
        public static float SquaredDist3D(float x1, float x2, float y1, float y2)
        {
            float x = InverseAngle(Angle(x1) - Angle(x2));
            float y = y1 - y2;
            return (x * x) + (y * y);
        }
        public static Vector2 Lerp3D(Vector2 a, Vector2 b, float t)
        {
            a.x = Angle(a.x);
            b.x = Angle(b.x);
            Vector2 result = Vector2.Lerp(a, b, t);
            result.x = InverseAngle(result.x);
            return result;
        }
        public static Vector3 To3D(float x, float y, float z)
        {
            return Core.Sphere.TilePos(x, y, z);
        }
        public static Vector3 To3D(this Vector2 v, float Height = 0)
        {
            return To3D(v.x, v.y, Height);
        }
        public static Vector3 To3D(this Vector3 v)
        {
            return To3D(v.x, v.y, v.z);
        }
        public static Vector3 To3DTileHeight(this Vector3 v)
        {
            Vector2Int pos = ((Vector2)v).AsInt();
            WorldTile Tile = World.world.GetTile(pos.x, pos.y);
            return To3D(v, Tile?.TileHeight() ?? -Core.Sphere.Radius);
        }
        public static bool Is3D(this Vector3 v)
        {
            return v.z > 0;
        }
        public static Vector3 To2D(this Vector3 v)
        {
            return To2D(v.x, v.y, v.z);
        }
        public static Vector2 To2D(float x, float y, float z)
        {
            return Core.Sphere.CylindricalToCarteisanFast(x, y, z);
        }
        public static float TrueHeight(int height) => height switch
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
            20 => 2.5f, //soil low //high is 3 higher then low
            21 => 5.5f, //soil high
            22 => 2.6f, //wasteland
            23 => 5.6f, //wasteland high
            28 => 2.7f,//swamp
            31 => 5.7f, //swamp high
            29 => 2.8f, //desert
            74 => 5.8f, 
            34 => 2.9f, //biomass
            35 => 5.9f,
            36 => 3f, //pumpkin
            37 => 6f,
            38 => 3.1f, //cybertile
            39 => 6.1f,
            40 => 3.2f, //grass
            41 => 6.2f,
            42 => 3.3f, //coruption
            43 => 6.3f,
            44 => 3.4f, //enchanted
            45 => 6.4f,
            46 => 3.5f,//mushroom
            47 => 6.5f,
            48 => 3.6f,//birch
            49 => 6.6f,
            50 => 3.7f, //maple
            51 => 6.7f,
            52 => 3.8f, //rocks
            53 => 6.8f,
            54 => 3.9f, //garlic
            55 => 6.9f,
            56 => 4f, //flowers
            57 => 7f,
            58 => 4.1f, //celestial
            59 => 7.1f,
            60 => 4.2f, //singularity
            61 => 7.2f,
            62 => 4.3f, //clover
            63 => 7.3f,
            64 => 4.4f, //paradox
            65 => 7.4f,
            66 => 4.5f, //solitude
            67 => 7.5f,
            68 => 4.6f, //savannah
            69 => 7.6f,
            70 => 4.7f, //jungle
            71 => 7.7f,
            72 => 4.8f, //infernal
            73 => 7.8f,
            75 => 4.9f, //crystal
            76 => 7.9f,
            77 => 5f, //candy
            78 => 8f,
            79 => 5.1f, //lemon
            80 => 8.1f,
            83 => 5.2f,//permafrost
            84 => 8.2f,
            85 => 5.3f, //tumor
            86 => 8.3f,
            #endregion
            #region peaks
            92 => 10f, //hills
            93 => 10.1f, //snowy hills
            94 => 13f, //mountains
            95 => 13.1f,
            96 => 14f, //summit
            97 => 14.1f,
            #endregion
            #region special
            30 => 3.4f, //road
            32 => 3f, //farm
            33 => 8.5f, //fuse
            87 => 3.3f,//landmine
            88 => 2,//waterbomb
            89 => 9f,//tnt timed
            90 => 8.8f,//tnt
            91 => 8.7f,//fireworks
            98 => Randy.randomFloat(3, 9),//grey goo
            #endregion
            #region lava
            24 => 4,
            25 => 6,
            26 => 8,
            27 => 9,
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
        public static float Dist(float x1, float x2, float y1, float y2)
        {
            return Mathf.Sqrt(SquaredDist(x1, x2, y1, y2));
        }
        public static float SquaredDist(float x1, float x2, float y1, float y2)
        {
            if (!Core.IsWorld3D)
            {
                return (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);
            }
            return SquaredDist3D(x1, x2, y1, y2);
        }

        public static void To3DBounds(ref int pX, ref int pY)
        {
            if (!Core.IsWorld3D)
            {
                return;
            }
            pX = (int)Core.Sphere.InBounds(pX);
        }
        public static int GetHeight(this WorldTile pTile)
        {
            return pTile.DisplayedType().render_z;
        }
        public static Color Blend(this Color backColor, Color color)
        {
            float amount = color.a;
            float r = color.r * amount + backColor.r * (1 - amount);
            float g = color.g * amount + backColor.g * (1 - amount);
            float b = color.b * amount + backColor.b * (1 - amount);
            return new Color(r, g, b);
        }
        static readonly Quaternion ConstRot = Quaternion.Euler(0, 90, 180);
        static readonly Quaternion ConstUprightRot = Quaternion.Euler(-90, 90, 180);
        public static Quaternion GetRotation(float X, float Y)
        {
            return Quaternion.AngleAxis(Angle(Y, X), Vector3.forward) * ConstRot;
        }
        public static Quaternion RotateToCamera(float X, float Y, float Z)
        {
            Vector2 direction = Direction3D(CameraManager.Position, To2D(X, Y, Z));
            float angle = Angle(direction.y, direction.x);
            return Quaternion.AngleAxis(angle, Vector3.up);
        }
        public static Quaternion GetUprightRotation(float X, float Y)
        {
            return Quaternion.AngleAxis(Angle(Y, X), Vector3.forward) * ConstUprightRot;
        }
        public static Color Normalised(this Color Color)
        {
            float amount = Color.a;
            float r = Color.r * amount;
            float g = Color.g * amount;
            float b = Color.b * amount;
            return new Color(r, g, b);
        }
        public static int Index(this SphereTile Tile)
        {
            return Tile.SphereToWorld().data.tile_id;
        }
        public static float Angle(float y, float x)
        {
            return Mathf.Atan2(y, x) * Mathf.Rad2Deg;
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
    }
}
