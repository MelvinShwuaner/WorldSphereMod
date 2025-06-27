using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using NeoModLoader.api;
using CompoundSpheres;
using NeoModLoader.utils;
using NeoModLoader.constants;
using System.IO;
using Newtonsoft.Json;
using static WorldSphereMod.CompoundSphereScripts;
using static HarmonyLib.AccessTools;
using WorldSphereMod.NewCamera;
using UnityEngine.Tilemaps;
using WorldSphereMod.General;
using System.Reflection;
using WorldSphereMod.Effects;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using WorldSphereMod.TileMapToSphere;
namespace WorldSphereMod
{
    class Core : MonoBehaviour, IMod, IStagedLoad
    {
        #region NML shit
        public ModDeclare GetDeclaration()
        {
            return declare;
        }
        public GameObject GetGameObject()
        {
            return gameobject;
        }
        public void OnLoad(ModDeclare pModDecl, GameObject pGameObject)
        {
            declare = pModDecl;
            gameobject = pGameObject;
            if (!SystemInfo.supportsInstancing || !SystemInfo.supportsComputeShaders || !SystemInfo.supportsIndirectArgumentsBuffer)
            {
                throw new IncompatibleHardwareException();
            }
        }
        public string GetUrl()
        {
            return "https://github.com/MelvinShwuaner?tab=repositories";
        }
        GameObject gameobject;
        ModDeclare declare;
        #endregion
        public static SavedSettings savedSettings = new SavedSettings();
        public static string SettingsVersion = "1.0.0";

        Harmony harmony;
        public static void SaveSettings()
        {
            string json = JsonConvert.SerializeObject(savedSettings, Formatting.Indented);
            File.WriteAllText($"{Paths.ModsConfigPath}/WorldSphereMod.json", json);
        }
        public static bool LoadSettings()
        {
            SavedSettings? loadedData;
            try
            {
                loadedData = JsonConvert.DeserializeObject<SavedSettings>(File.ReadAllText($"{Paths.ModsConfigPath}/WorldSphereMod.json"));
                if (loadedData == null || loadedData.Version != SettingsVersion)
                {
                    throw new FileLoadException();
                }
            }
            catch
            {
                SaveSettings();
                return false;
            }
            savedSettings = loadedData;
            return true;
        }
        // go go gadget un-box my worldbox
        public void Init()
        {
            LoadSettings();
            Patch();
            WorldSphereTab.Init();
            CameraManager.Begin();
        }
        // load the textures after mods are loaded incase some mods add new world tiles
        public void PostInit()
        {
            Sphere.Prepare();
        }
        const string HarmonyID = "WorldSphereMod";
        //this made makes the game 3D, of course im patching alot (rip compatibility)
        void Patch()
        {
            Harmony.CreateAndPatchAll(typeof(LoopWithBrush), HarmonyID);
            Harmony.CreateAndPatchAll(typeof(SphereControl), HarmonyID);
            Harmony.CreateAndPatchAll(typeof(Dist3D), HarmonyID);
            Harmony.CreateAndPatchAll(typeof(EffectPatches), HarmonyID);
            Harmony.CreateAndPatchAll(typeof(AddLayers), HarmonyID);
            harmony = new Harmony(HarmonyID);
            harmony.PatchAll();

            MethodInfo WorldLoopPatch = Method(typeof(GetTile3D), nameof(GetTile3D.Prefix));
            harmony.Patch(Method(typeof(GeneratorTool), nameof(GeneratorTool.getTile)), new HarmonyMethod(WorldLoopPatch));
            harmony.Patch(Method(typeof(MapBox), nameof(MapBox.GetTile)), new HarmonyMethod(WorldLoopPatch));

            MethodInfo Lerp3DPatch = Method(typeof(Lerp3D), nameof(Lerp3D.Transpiler));
            harmony.Patch(Method(typeof(PlayerControl), nameof(PlayerControl.clickedStart)), null, null, new HarmonyMethod(Lerp3DPatch));

            MethodInfo EffectPatch = Method(typeof(EffectPatches), nameof(EffectPatches.BasePatch));
            harmony.Patch(Method(typeof(BaseEffect), nameof(BaseEffect.prepare), new Type[] { }), null, new HarmonyMethod(EffectPatch));
            harmony.Patch(Method(typeof(BaseEffect), nameof(BaseEffect.prepare), new Type[] {typeof(WorldTile), typeof(float) }), null, new HarmonyMethod(EffectPatch));
            harmony.Patch(Method(typeof(BaseEffect), nameof(BaseEffect.prepare), new Type[] {typeof(Vector2), typeof(float) }), null, new HarmonyMethod(EffectPatch));
            //may allah forgive me
            HarmonyMethod MapLayerTranspiler = new HarmonyMethod(Method(typeof(AddLayers), nameof(AddLayers.MapLayerTranspiler)));
            harmony.Patch(Method(typeof(DebugLayer), nameof(DebugLayer.drawBuildings)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(BurnedTilesLayer), nameof(BurnedTilesLayer.UpdateDirty)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(ConwayLife), nameof(ConwayLife.UpdateVisual)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(DebugLayer), nameof(DebugLayer.clear)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(DebugLayer), nameof(DebugLayer.drawCitizenJobs)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(DebugLayer), nameof(DebugLayer.drawConstructionTiles)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(DebugLayer), nameof(DebugLayer.drawProfession)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(DebugLayer), nameof(DebugLayer.drawTargetedBy)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(DebugLayer), nameof(DebugLayer.drawUnitKingdoms)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(DebugLayer), nameof(DebugLayer.drawUnitsInside)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(DebugLayer), nameof(DebugLayer.drawUnitTiles)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(DebugLayer), nameof(DebugLayer.fill), new Type[] {typeof(List<WorldTile>), typeof(Color), typeof(bool)}), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(DebugLayer), nameof(DebugLayer.fill), new Type[] { typeof(WorldTile[]), typeof(Color), typeof(bool) }), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(DebugLayerCursor), nameof(DebugLayerCursor.fill), new Type[] { typeof(List<WorldTile>), typeof(Color), typeof(bool) }), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(DebugLayerCursor), nameof(DebugLayerCursor.fill), new Type[] { typeof(WorldTile[]), typeof(Color), typeof(bool) }), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(DebugLayerCursor), nameof(DebugLayerCursor.drawIsland)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(ExplosionsEffects), nameof(ExplosionsEffects.UpdateDirty)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(FireLayer), nameof(FireLayer.UpdateDirty)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(LavaLayer), nameof(LavaLayer.drawLavaPixel)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(LavaLayer), nameof(LavaLayer.updateLava)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(PathFindingVisualiser), nameof(PathFindingVisualiser.showPath)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(PixelFlashEffects), nameof(PixelFlashEffects.UpdateDirty)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(UnitLayer), nameof(UnitLayer.UpdateDirty)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(WorldLayerEdges), nameof(WorldLayerEdges.redraw)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(WorldLayerEdges), nameof(WorldLayerEdges.redrawTile)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(ZoneCalculator), nameof(ZoneCalculator.applyMetaColorsToZone)), null, null, MapLayerTranspiler);
            harmony.Patch(Method(typeof(ZoneCalculator), nameof(ZoneCalculator.colorZone)), null, null, MapLayerTranspiler);
            //this is where the fun begins
            HarmonyMethod SphereWorldTranspiler = new HarmonyMethod(Method(typeof(WorldSphereTranspiler), nameof(WorldSphereTranspiler.SphereWorldTranspiler)));
            harmony.Patch(Method(typeof(BoulderCharge), nameof(BoulderCharge.update)), null, null, SphereWorldTranspiler);
            harmony.Patch(Method(typeof(Boulder), nameof(Boulder.updateCurrentPosition)), null, null, SphereWorldTranspiler);
            harmony.Patch(Method(typeof(Boulder), nameof(Boulder.actionLanded)), null, null, SphereWorldTranspiler);
        } 
        public static void Become3D()
        {
            CreateSphere();
            CameraManager.MakeCamera3D();
        }
        public static void Become2D()
        {
            DestroySphere();
            CameraManager.MakeCamera2D();
        }
        public static void CreateSphere()
        {
            Sphere.Begin();
        }
        public static void DestroySphere()
        {
            Sphere.Finish();
        }
        public static bool Generated = false;
        public static bool GeneratingSphere => savedSettings.Is3D && !Generated;
        public static bool IsWorld3D => Sphere.Exists;
        // the layer between the Mod and the compound sphere
        public static class Sphere
        {
            public static float Radius => Manager.Radius;
            public static Transform CenterCapsule => Manager.transform.GetChild(0);
            public static bool Exists => Manager != null;
            #region Fancy stuff
            static SphereManager Manager;
            static Mesh CompoundSphereMesh;
            static Material CompoundSphereMaterial;
            static Texture2DArray Textures;
            static SphereManagerSettings SphereManagerConfig;
            static Dictionary<Tile, int> TileIDS;
            #endregion
            public static List<MapLayer> BaseLayers;
            public static Dictionary<MapLayer, PixelArray> CachedColors;
            public static PixelFlashEffects FlashLayer => World.world.flash_effects;
            public static void Begin()
            {
                int width = MapBox.width;
                int height = MapBox.height;
                Manager = SphereManager.Creator.CreateSphereManager(width, height, SphereManagerConfig);
            }
            public static Color GetColor(int Index)
            {
                Color color = World.world.world_layer.pixels[Index];
                foreach(MapLayer layer in BaseLayers)
                {
                    color = color.Blend(layer.pixels[Index]);
                }
                return color;
            }
            public static Color GetAddedColor(int Index)
            {
                return ((Color)FlashLayer.pixels[Index]).Normalised();
            }
            public static float InBounds(float X)
            {
                return Manager.Clamp(X, 0);
            }
            public static void UpdateScale(SphereTile Tile)
            {
                Manager.UpdateScale(Tile.X, Tile.Y);
            }
            public static void UpdateTexture(SphereTile Tile)
            {
                Manager.UpdateTexture(Tile.X, Tile.Y);
            }
            public static void RefreshSphere()
            {
                Manager.RefreshScales();
                Manager.RefreshTextures();
                Manager.RefreshCustom("AddedColors");
                Manager.RefreshColors();
            }
            public static void UpdateLayer(SphereTile Tile)
            {
                Manager.UpdateCustom("AddedColors", Tile.X, Tile.Y);
            }
            public static void UpdateBaseLayer(SphereTile Tile)
            {
                Manager.UpdateColor(Tile.X, Tile.Y);
            }
            public static void Finish()
            {
                if(Manager == null || Manager.gameObject == null)
                {
                    return;
                }
                Manager.Destroy();
            }
            public static Vector3 CylindricalToCarteisan(float X, float Y, float Z)
            {
                return CylindricalToCartesian(Manager, X, Y, Z);
            }
            public static Vector2 CylindricalToCarteisanFast(float X, float Y, float Z)
            {
                return CylindricalToCartesianFast(Manager, X, Y, Z);
            }
            public static void DrawTiles(int CameraX)
            {
                Manager.DrawTiles(CameraX);
            }
            static void CreateCachedColors()
            {
                CachedColors = new Dictionary<MapLayer, PixelArray>();
                foreach (var layer in World.world._map_layers)
                {
                    CachedColors.Add(layer, new PixelArray(layer));
                }
            }
            public static Vector3 TilePos(float X, float Y, float Height = 0)
            {
                return Manager.SphereTilePosition(X, Y, Height);
            }
            public static void Prepare()
            {
                LoadAssets();
                CreateTextures();
                CreateSettings();
                BaseLayers = new List<MapLayer>(World.world._map_layers);
                BaseLayers.Remove(FlashLayer);
                CreateCachedColors();
            }
            static Func<Color32, Color32, bool> aredifferent = (Color32 first, Color32 secound) => { return !Tools.EqualsColor(first, secound); };
            public static int WorldTileTexture(WorldTile Tile)
            {
                Tile Graphic = World.world.tilemap.getVariation(Tile);
                if(Graphic == null)
                {
                    return 0;
                }
                if (TileIDS.TryGetValue(Graphic, out int ID)) {
                    return ID;
                }
                return 0;
            }
            static void LoadAssets()
            {
                WrappedAssetBundle ab = AssetBundleUtils.GetAssetBundle("worldsphere");
                CompoundSphereMesh = ab.GetObject<Mesh>("assets/worldspheremod/compoundspheremesh.asset");
                CompoundSphereMaterial = ab.GetObject<Material>("assets/worldspheremod/compoundspherematerial.mat");
            }
            public static SphereTile GetTile(int X, int Y)
            {
                return Manager[X, Y];
            }
            static void CreateSettings()
            {
                SphereManagerConfig = new SphereManagerSettings(
                    Initiation,
                    CartesianToCylindrical,
                    CylindricalRotation,
                    SphereTileScale,
                    SphereTileColor,
                    SphereTileTexture,
                    getdisplaymode,
                    Textures,
                    CompoundSphereMesh,
                    CompoundSphereMaterial,
                    RenderRange,
                    new List<IBufferData>() { new CustomBufferData<Vector3>("AddedColors", 12, SphereTileAddedColor) },
                    3
               );
            }
            static void CreateTextures()
            {
                List<Sprite> Sprites = new List<Sprite>();
                TileIDS = new Dictionary<Tile, int>();
                foreach (TileType type in AssetManager.tiles.list)
                {
                    AddTile(type);
                }
                foreach (TopTileType type in AssetManager.top_tiles.list)
                {
                    AddTile(type);
                }
                Textures = new Texture2DArray(8, 8, Sprites.Count, TextureFormat.RGBA32, true, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                for (int i = 0; i < Sprites.Count; i++)
                {
                    Textures.SetPixels32(GetTruePixels(Sprites[i]), i);
                }
                Textures.Apply();
                void AddTile(TileTypeBase Tile)
                {
                    TileSprites sprites = Tile.sprites;
                    if (sprites == null)
                    {
                        return;
                    }
                    foreach (Tile tile in sprites._tiles)
                    {
                        if (TileIDS.TryAdd(tile, Sprites.Count))
                        {
                            Sprites.Add(tile.sprite);
                        }
                    }
                }
                Color32[] GetTruePixels(Sprite sprite)
                {
                    if (sprite.texture.width != 8 || sprite.texture.height != 8)
                    {
                        //seperate a sprite from its atlas
                        return sprite.PixelsFromSpriteAtlas();
                    }
                    return sprite.texture.GetPixels32();
                }
            }
        }
    }
}