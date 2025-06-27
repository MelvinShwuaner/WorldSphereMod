using CompoundSpheres;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static WorldSphereMod.TileMapToSphere.TileMapToSphere;
namespace WorldSphereMod.TileMapToSphere
{
    [HarmonyPatch(typeof(ZoneCamera), nameof(ZoneCamera.getZoneWithinCamera))]
    class getzone3D
    {
        static ZoneCamera ZoneCamera => World.world.zone_camera;
        static bool Prefix(int pX, int pY, float pBonusY, ref TileZone __result)
        {
            if (Core.IsWorld3D)
            {
                __result = getZoneWithinCamera(pX, pY, pBonusY);
                return false;
            }
            return true;
        }
        static TileZone Default(float pX, float pY)
        {
            int X = pX == 0 ? 0 : ZoneCamera._zone_manager.zones_total_x - 1;
            int Y = pY == 0 ? 0 : ZoneCamera._zone_manager.zones_total_y - 1;
            return ZoneCamera._zone_manager.getZone(X, Y);
        }
        static TileZone getZoneWithinCamera(float pX, float pY, float pBonusY = 0f)
        {
            bool RayExists = World.world.camera.ViewPortToRay(new Vector3(pX, pY), out Ray Ray);
            if (!RayExists)
            {
                return Default(pX, pY + pBonusY);
            }
            bool Intersected = Tools.IntersectMesh(Ray, out Vector2Int Pos);
            if (!Intersected)
            {
                return Default(pX, pY + pBonusY);
            }
            WorldTile tile = World.world.GetTile(Pos.x, Pos.y);
            if (tile == null)
            {
                return Default(pX, pY + pBonusY);
            }
            return tile.zone;
        }
    }
    [HarmonyPatch(typeof(WorldTilemap), nameof(WorldTilemap.generate))]
    class Generate3D {
        static bool Prefix(int pCount)
        {
            if (Core.GeneratingSphere)
            {
                Regenerate(pCount);
                return false;
            }
            return true;
        }
        static void Regenerate(int pCount)
        {
            ScaleQueue = new TileQueue(pCount);
            ColorQueue = new TileQueue(pCount);
            TextureQueue = new TileQueue(pCount);
        }
    }
    [HarmonyPatch(typeof(WorldTilemap), nameof(WorldTilemap.addToQueueToRedraw))]
    class Queue3D
    {
        static bool Prefix(WorldTile pTile)
        {
            if (Core.GeneratingSphere)
            {
                return false;
            }
            if (Core.IsWorld3D)
            {
                AddToQueue(pTile);
                return false;
            }
            return true;
        }
    }
    public class TileQueue
    {
        public HashSet<WorldTile>[] TilesByZone;
        public HashSet<TileZone> DirtyZones;
        public List<TileZone> ClearList;
        public int Count => DirtyZones.Count;
        public TileQueue(int pCount)
        {
            DirtyZones = new HashSet<TileZone>();
            ClearList = new List<TileZone>();
            TilesByZone = new HashSet<WorldTile>[pCount];
            for (int i = 0; i < pCount; i++)
            {
                TilesByZone[i] = new HashSet<WorldTile>(64);
            }
        }
        public bool HasZone(TileZone Zone)
        {
            return DirtyZones.Contains(Zone);
        }
        public void AddTile(WorldTile pTile)
        {
            TileZone tZone = pTile.zone;
            DirtyZones.Add(tZone);
            TilesByZone[tZone.id].Add(pTile);
        }
        public HashSet<WorldTile> GetTiles(TileZone pZone)
        {
            return TilesByZone[pZone.id];
        }
        public void ClearZone(TileZone pZone)
        {
            ClearList.Add(pZone);
        }
        public void Clear()
        {
            DirtyZones.ExceptWith(ClearList);
            ClearList.Clear();
        }
        public void Dispose()
        {
            DirtyZones.Clear();
            ClearList.Clear();
        }
    }
    [HarmonyPatch(typeof(WorldTilemap), nameof(WorldTilemap.redrawTiles))]
    public static class TileMapToSphere
    {
        public static void AddToQueue(WorldTile pTile)
        {
            ScaleQueue.AddTile(pTile);
            TextureQueue.AddTile(pTile);
        }
        public static void AddToColorQueue(WorldTile pTile)
        {
            ColorQueue.AddTile(pTile);
        }
        public static TileQueue TextureQueue;
        public static TileQueue ColorQueue;
        public static TileQueue ScaleQueue;
        public static bool UpdateTextures => MapBox.isRenderGameplay();
        public static void CheckScale(WorldTile pTile)
        {
            int height = pTile.GetHeight();
            if (pTile.last_rendered_pos_tile.z != height)
            {
                AddTileToScaleQueue(pTile);
                pTile.last_rendered_pos_tile.z = height;
            }
        }
        public static void CheckTexture(WorldTile pTile)
        {
            TileTypeBase tTypeToDraw = pTile.DisplayedType();
            if (pTile.last_rendered_tile_type != tTypeToDraw)
            {
                pTile.last_rendered_tile_type = tTypeToDraw;
                AddTileToTextureQueue(pTile);
            }
        }
        static void Dispose()
        {
            ColorQueue.Dispose();
            TextureQueue.Dispose();
            ScaleQueue.Dispose();
        }
        public static bool Prefix(bool pForceAll)
        {
            if (Core.GeneratingSphere && pForceAll)
            {
                Dispose();
                return false;
            }
            return true;
        }
        static void CheckScales(TileZone pZone)
        {
            if (!ScaleQueue.HasZone(pZone))
            {
                return;
            }
            HashSet<WorldTile> tTiles = ScaleQueue.GetTiles(pZone);
            foreach (WorldTile tTile in tTiles)
            {
                CheckScale(tTile);
            }
            tTiles.Clear();
            ScaleQueue.ClearZone(pZone);
        }
        static void CheckTextures(TileZone pZone)
        {
            if (!TextureQueue.HasZone(pZone))
            {
                return;
            }
            HashSet<WorldTile> tTiles = TextureQueue.GetTiles(pZone);
            foreach (WorldTile tTile in tTiles)
            {
                CheckTexture(tTile);
            }
            tTiles.Clear();
            TextureQueue.ClearZone(pZone);
        }
        static void CheckColors(TileZone pZone)
        {
            if (!ColorQueue.HasZone(pZone))
            {
                return;
            }
            HashSet<WorldTile> tTiles = ColorQueue.GetTiles(pZone);
            foreach (WorldTile tTile in tTiles)
            {
                CheckColor(tTile);
            }
            tTiles.Clear();
            ColorQueue.ClearZone(pZone);
        }
        static void CheckColor(WorldTile tTile)
        {
            AddTileToColorQueue(tTile);
        }
        static void checkZoneToRender(TileZone pZone)
        {
            CheckScales(pZone);
            if (UpdateTextures)
            {
                CheckTextures(pZone);
            }
            else
            {
                CheckColors(pZone);
            }
        }
        public static void Redraw3DTiles()
        {
            if (TextureQueue.Count == 0 && ScaleQueue.Count == 0 && ColorQueue.Count == 0)
            {
                return;
            }
            for (int iZone = 0; iZone < World.world.zone_camera.zones.Count; iZone++)
            {
                TileZone tZone2 = World.world.zone_camera.zones[iZone];
                checkZoneToRender(tZone2);
            }
            Finish();
        }
        static void Finish()
        {
            ScaleQueue.Clear();
            if (UpdateTextures)
            {
                TextureQueue.Clear();
            }
            else
            {
                ColorQueue.Clear();
            }
        }
        public static void AddTileToTextureQueue(WorldTile pTile)
        {
            SphereTile Tile = pTile.WorldToSphere();
            Core.Sphere.UpdateTexture(Tile);
        }
        public static void AddTileToScaleQueue(WorldTile pTile)
        {
            SphereTile Tile = pTile.WorldToSphere();
            Core.Sphere.UpdateScale(Tile);
        }
        public static void AddTileToColorQueue(WorldTile pTile)
        {
            SphereTile Tile = pTile.WorldToSphere();
            Core.Sphere.UpdateBaseLayer(Tile);
        }
    }
    [HarmonyPatch(typeof(WorldTilemap), nameof(WorldTilemap.enableTiles))]
    public class DisableTileMap
    {
        static void Prefix(ref bool pValue)
        {
            if (Core.IsWorld3D)
            {
                pValue = false;
            }
        }
    }
    [HarmonyPatch(typeof(MapBox), nameof(MapBox.renderStuff))]
    public static class RefreshSphere
    {
        static void render3DStuff()
        {
            QuantumSpriteManager.update();
            Bench.bench("redraw_tiles", "game_total", false);
            Redraw3DTiles();
            Bench.benchEnd("redraw_tiles", "game_total", false, 0L, false);
            Bench.bench("update_debug_texts", "game_total", false);
            World.world.updateDebugGroupSystem();
            Bench.benchEnd("update_debug_texts", "game_total", false, 0L, false);
            if (World.world._redraw_timer > 0f)
            {
                World.world._redraw_timer -= Time.deltaTime;
            }
            else
            {
                World.world._redraw_timer = 0.1f;
                Bench.bench("redraw_mini_map", "game_total", false);
                if (World.world.tiles_dirty.Count > 0)
                {
                    World.world.redrawMiniMap(false);
                }
                Bench.benchEnd("redraw_mini_map", "game_total", false, 0L, false);
                Bench.bench("Refresh Sphere", "game_total");
                Core.Sphere.RefreshSphere();
                Bench.benchEnd("Refresh Sphere", "game_total");
            }
        }
        static bool Prefix()
        {
            if (Core.IsWorld3D)
            {
                render3DStuff();
                return false;
            }
            return true;
        }
    }
    public static class AddLayers
    {
        static MethodInfo GetPixel => AccessTools.Method(typeof(Dictionary<MapLayer, PixelArray>), "get_Item");
        static MethodInfo SetPixel => AccessTools.Method(typeof(PixelArray), "set_Item");
        static FieldInfo Pixels => AccessTools.Field(typeof(Core.Sphere), nameof(Core.Sphere.CachedColors));
        static CodeMatch FindPixels => new CodeMatch((CodeInstruction instruction) => instruction.opcode == OpCodes.Ldfld && instruction.operand is FieldInfo field && field.Name == "pixels");
        static CodeMatch FindStelem => new CodeMatch(OpCodes.Stelem);
        //same as the other one, but only for the world layer because it does not manage itself, Mapbox manages it
        [HarmonyPatch(typeof(MapBox), nameof(MapBox.updateDirtyTile))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> WorldTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeMatcher Matcher = new CodeMatcher(instructions, generator);
            while (Matcher.FindNext(FindPixels))
            {
                Matcher.RemoveInstruction();
                Matcher.Insert(new CodeInstruction(OpCodes.Callvirt, GetPixel));
                Matcher.Advance(-2);
                CodeInstruction instruct = Matcher.Instruction;
                Matcher.Insert(new CodeInstruction(OpCodes.Ldsfld, Pixels));
                Tools.MoveLabels(instruct, Matcher.Instruction);
                Matcher.MatchForward(false, FindStelem);
                Matcher.RemoveInstruction();
                Matcher.Insert(new CodeInstruction(OpCodes.Callvirt, SetPixel));
            }
            return Matcher.Instructions();
        }
        //this transpiler replaces all references in a maplayer from its color array to a custom color array that tracks the changes made
        public static IEnumerable<CodeInstruction> MapLayerTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeMatcher Matcher = new CodeMatcher(instructions, generator);
            while (Matcher.FindNext(FindPixels))
            {
                Matcher.RemoveInstruction();
                Matcher.Insert(new CodeInstruction(OpCodes.Callvirt, GetPixel));
                Matcher.Advance(-1);
                CodeInstruction instruct = Matcher.Instruction;
                Matcher.Insert(new CodeInstruction(OpCodes.Ldsfld, Pixels));
                Tools.MoveLabels(instruct, Matcher.Instruction);
                Matcher.MatchForward(false, FindStelem);
                Matcher.RemoveInstruction();
                Matcher.Insert(new CodeInstruction(OpCodes.Callvirt, SetPixel));
            }
            return Matcher.Instructions();
        }
        [HarmonyPatch(typeof(MapLayer), nameof(MapLayer.clear))]
        [HarmonyPrefix]
        static bool ClearPatch(MapLayer __instance)
        {
            if (__instance.pixels != null)
            {
                __instance.pixels_to_update.Clear();
                Color32 color = Color.clear;
                for (int i = 0; i < __instance.pixels.Length; i++)
                {
                    Core.Sphere.CachedColors[__instance][i] = color;
                }
                __instance.updatePixels();
            }
            return false;
        }
        [HarmonyPatch(typeof(MapLayer), nameof(MapLayer.createTextureNew))]
        [HarmonyPrefix]
        static bool TextureNew3D(MapLayer __instance)
        {
            if (!(__instance.texture == null) && MapBox.width == __instance.textureWidth && MapBox.height == __instance.texture.height)
            {
                return true;
            }
            if (__instance.sprRnd.sprite != null && __instance.textureWidth != 0)
            {
                Texture2DStorage.addToStorage(__instance.sprRnd.sprite, __instance.textureWidth, __instance.textureHeight);
            }
            __instance.textureWidth = MapBox.width;
            __instance.textureHeight = MapBox.height;
            __instance.sprRnd.sprite = Texture2DStorage.getSprite(__instance.textureWidth, __instance.textureHeight);
            __instance.texture = __instance.sprRnd.sprite.texture;
            __instance.textureID = __instance.texture.GetHashCode();
            int num = __instance.texture.height * __instance.texture.width;
            Color32 color = Color.clear;
            __instance.pixels = new Color32[num];
            for (int i = 0; i < num; i++)
            {
                Core.Sphere.CachedColors[__instance][i] = color;
            }
            __instance.updatePixels();
            return false;
        }
        [HarmonyPatch(typeof(MapLayer), nameof(MapLayer.updatePixels))]
        [HarmonyPrefix]
        static bool DontUpdate()
        {
            return !Core.IsWorld3D;
        }
    }
    public class PixelArray
    {
        public static void AddLayer(MapLayer Layer, int I)
        {
            if (!Core.IsWorld3D)
            {
                return;
            }
            if (Layer.IsBase())
            {
                AddToColorQueue(World.world.tiles_list[I]);
                return;
            }
            Core.Sphere.UpdateLayer(World.world.tiles_list[I].WorldToSphere());
        }
        MapLayer Layer;
        public PixelArray(MapLayer Layer)
        {
            this.Layer = Layer;
        }
        void Set(int I, Color32 Color)
        {
            if (!Layer.pixels[I].EqualsColor(Color))
            {
                Layer.pixels[I] = Color;
                AddLayer(Layer, I);
            }
        }
        public Color32 this[int x]
        {
            get { return Layer.pixels[x]; }
            set
            {
                Set(x, value);
            }
        }
    }
}