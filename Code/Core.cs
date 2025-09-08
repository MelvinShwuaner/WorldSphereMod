using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
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
using WorldSphereMod.TileMapToSphere;
using WorldSphereMod.UI;
using WorldSphereMod.QuantumSprites;
using SleekRender;
using ai.behaviours;
namespace WorldSphereMod
{
    public static class Core
    {
        public static SavedSettings savedSettings = new SavedSettings();
        public static string SettingsVersion = "1.2";

        public static Harmony Patcher;
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
        public static void Init()
        {
            LoadSettings();
            WorldSphereTab.Begin();
            DimensionConverter.Prepare();
            Patch();
            CameraManager.Begin();
            DoSomeOtherStuff();
        }
        static void DoSomeOtherStuff()
        {
            AssetManager.options_library.get("vignette").action = delegate (OptionAsset pAsset)
            {
                CameraManager.OriginalCamera.GetComponent<SleekRenderPostProcess>().settings.vignetteEnabled = AssetManager.options_library.getSavedBool(pAsset.id);
            };
            AssetManager.options_library.get("bloom").action = delegate (OptionAsset pAsset)
            {
                CameraManager.OriginalCamera.GetComponent<SleekRenderPostProcess>().settings.bloomEnabled = AssetManager.options_library.getSavedBool(pAsset.id);
            };
        }
        // load the textures after mods are loaded incase some mods add new world tiles
        public static void PostInit()
        {
            Sphere.Prepare();
        }
        const string HarmonyID = "WorldSphereMod";
        //this mod makes the game 3D, of course im patching alot (rip compatibility)
        //literally the core function of the mod
        static void Patch()
        {

            Patcher = new Harmony(HarmonyID);
            Patcher.PatchAll();

            Patcher.PatchAll(typeof(SphereControl));
            Patcher.PatchAll(typeof(Dist3D));
            Patcher.PatchAll(typeof(EffectPatches));
            Patcher.PatchAll(typeof(MovementEnhancement));
            Patcher.PatchAll(typeof(Drop3D));
            Patcher.PatchAll(typeof(FixCrabzilla));
            Patcher.PatchAll(typeof(AddLayers));
            Patcher.PatchAll(typeof(QuantumSpritePatches));
            Patcher.PatchAll(typeof(WorldLoop));
            Patcher.PatchAll(typeof(SourcePatches));

            MethodInfo WorldLoopPatch = Method(typeof(WorldLoop), nameof(WorldLoop.Tiles));
            Patcher.Patch(Method(typeof(GeneratorTool), nameof(GeneratorTool.getTile)), new HarmonyMethod(WorldLoopPatch));
            Patcher.Patch(Method(typeof(MapBox), nameof(MapBox.GetTile)), new HarmonyMethod(WorldLoopPatch));

            MethodInfo Lerp3DPatch = Method(typeof(Lerp3D), nameof(Lerp3D.Transpiler));
            Patcher.Patch(Method(typeof(PlayerControl), nameof(PlayerControl.clickedStart)), null, null, new HarmonyMethod(Lerp3DPatch));

            HarmonyMethod brushTranspiler = new HarmonyMethod(Method(typeof(BrushTranspiler), nameof(BrushTranspiler.Transpiler)));
            Patcher.Transpile(Method(typeof(MapAction), nameof(MapAction.applyTileDamage)), brushTranspiler);
            Patcher.Transpile(Method(typeof(MapBox), nameof(MapBox.loopWithBrush), new Type[] { typeof(WorldTile), typeof(BrushData), typeof(PowerActionWithID), typeof(string) }), brushTranspiler);
            Patcher.Transpile(Method(typeof(MapBox), nameof(MapBox.loopWithBrush), new Type[] { typeof(WorldTile), typeof(BrushData), typeof(PowerAction), typeof(GodPower) }), brushTranspiler);
            Patcher.Transpile(Method(typeof(BehWormDigEat), nameof(BehWormDigEat.loopWithBrush)), brushTranspiler);
            Patcher.Transpile(Method(typeof(MapBox), nameof(MapBox.loopWithBrushPowerForDropsRandom)), brushTranspiler);

            MethodInfo EffectPatch = Method(typeof(EffectPatches), nameof(EffectPatches.BasePatch));
            Patcher.Patch(Method(typeof(BaseEffect), nameof(BaseEffect.prepare), new Type[] { }), null, new HarmonyMethod(EffectPatch));
            Patcher.Patch(Method(typeof(BaseEffect), nameof(BaseEffect.prepare), new Type[] {typeof(WorldTile), typeof(float) }), null, new HarmonyMethod(EffectPatch));
            Patcher.Patch(Method(typeof(BaseEffect), nameof(BaseEffect.prepare), new Type[] {typeof(Vector2), typeof(float) }), null, new HarmonyMethod(EffectPatch));
            //may allah forgive me
            HarmonyMethod MapLayerTranspiler = new HarmonyMethod(Method(typeof(AddLayers), nameof(AddLayers.MapLayerTranspiler)));
            Patcher.Transpile(Method(typeof(DebugLayer), nameof(DebugLayer.drawBuildings)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(BurnedTilesLayer), nameof(BurnedTilesLayer.UpdateDirty)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(ConwayLife), nameof(ConwayLife.UpdateVisual)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(DebugLayer), nameof(DebugLayer.clear)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(DebugLayer), nameof(DebugLayer.drawCitizenJobs)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(DebugLayer), nameof(DebugLayer.drawConstructionTiles)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(DebugLayer), nameof(DebugLayer.drawProfession)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(DebugLayer), nameof(DebugLayer.drawTargetedBy)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(DebugLayer), nameof(DebugLayer.drawUnitKingdoms)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(DebugLayer), nameof(DebugLayer.drawUnitsInside)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(DebugLayer), nameof(DebugLayer.drawUnitTiles)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(DebugLayer), nameof(DebugLayer.fill), new Type[] {typeof(List<WorldTile>), typeof(Color), typeof(bool)}), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(DebugLayer), nameof(DebugLayer.fill), new Type[] { typeof(WorldTile[]), typeof(Color), typeof(bool) }), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(DebugLayerCursor), nameof(DebugLayerCursor.fill), new Type[] { typeof(List<WorldTile>), typeof(Color), typeof(bool) }), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(DebugLayerCursor), nameof(DebugLayerCursor.fill), new Type[] { typeof(WorldTile[]), typeof(Color), typeof(bool) }), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(DebugLayerCursor), nameof(DebugLayerCursor.drawIsland)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(ExplosionsEffects), nameof(ExplosionsEffects.UpdateDirty)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(FireLayer), nameof(FireLayer.UpdateDirty)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(LavaLayer), nameof(LavaLayer.drawLavaPixel)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(LavaLayer), nameof(LavaLayer.updateLava)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(PathFindingVisualiser), nameof(PathFindingVisualiser.showPath)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(PixelFlashEffects), nameof(PixelFlashEffects.UpdateDirty)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(UnitLayer), nameof(UnitLayer.UpdateDirty)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(WorldLayerEdges), nameof(WorldLayerEdges.redraw)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(WorldLayerEdges), nameof(WorldLayerEdges.redrawTile)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(ZoneCalculator), nameof(ZoneCalculator.applyMetaColorsToZone)), MapLayerTranspiler);
            Patcher.Transpile(Method(typeof(ZoneCalculator), nameof(ZoneCalculator.colorZone)), MapLayerTranspiler);

            Patcher.Transpile(Method(typeof(Actor), nameof(Actor.updateMovement)), Move3D.Transpiler);
            Patcher.Transpile(Method(typeof(Actor), nameof(Actor.tryToAttack)), Move3D.Transpiler);
            Patcher.Transpile(Method(typeof(MapBox), nameof(MapBox.checkAttackFor)), Move3D.Transpiler);
            Patcher.Transpile(Method(typeof(Actor), nameof(Actor.updatePossessedMovementTowards)), Move3D.Transpiler);
            Patcher.Transpile(Method(typeof(CombatActionLibrary), nameof(CombatActionLibrary.getAttackTargetPosition)), Move3D.Transpiler);
            Patcher.Transpile(Method(typeof(MusicBoxContainerTiles), nameof(MusicBoxContainerTiles.calculatePan)), Move3D.Transpiler);

            Patcher.Transpile(Method(typeof(MoveCamera), nameof(MoveCamera.zoomToBounds)), MinZoomTranspiler.Transpiler);
            Patcher.Transpile(Method(typeof(MoveCamera), nameof(MoveCamera.updateMobileCamera)), MinZoomTranspiler.Transpiler);

            Patcher.Transpile(Method(typeof(HeatRayEffect), nameof(HeatRayEffect.update)), DisableSettingPositions.Transpiler);

            //this is where the fun begins 
            DimensionConverter.ConvertPositions(Method(typeof(Boulder), nameof(Boulder.updateCurrentPosition)), 1);
            DimensionConverter.ConvertPositions(Method(typeof(Boulder), nameof(Boulder.actionLanded)));
            DimensionConverter.ConvertQuantum(Method(typeof(Santa), nameof(Santa.updatePosition)), DimensionConverter.YToZ);
            DimensionConverter.ConvertQuantum(Method(typeof(HeatRayEffect), nameof(HeatRayEffect.play)), DimensionConverter.ToQuantum);

            DimensionConverter.ConvertQuantum(Method(typeof(QuantumSpriteLibrary), nameof(QuantumSpriteLibrary.drawShadowsBuildings)), DimensionConverter.ToQuantumNonUpright);
            DimensionConverter.ConvertQuantum(Method(typeof(QuantumSpriteLibrary), nameof(QuantumSpriteLibrary.drawFires)), DimensionConverter.ToFire);
            DimensionConverter.ConvertQuantum(Method(typeof(QuantumSpriteLibrary), nameof(QuantumSpriteLibrary.drawShadowsUnit)), DimensionConverter.ToQuantumNonUpright);
            DimensionConverter.ConvertPositions(Method(typeof(QuantumSpriteLibrary), nameof(QuantumSpriteLibrary.drawUnitAttackRange)));
            DimensionConverter.ConvertPositions(Method(typeof(QuantumSpriteLibrary), nameof(QuantumSpriteLibrary.drawUnitSize)));
            DimensionConverter.ConvertPositions(Method(typeof(QuantumSpriteLibrary), nameof(QuantumSpriteLibrary.drawUnitsAvatars)));
            DimensionConverter.ConvertPositions(Method(typeof(QuantumSpriteLibrary), nameof(QuantumSpriteLibrary.drawLightAreas)));

            DimensionConverter.ConvertPositions(Method(typeof(GroupSpriteObject), nameof(GroupSpriteObject.setPosOnly), new Type[] {typeof(Vector2)}));
            DimensionConverter.ConvertPositions(Method(typeof(GroupSpriteObject), nameof(GroupSpriteObject.setPosOnly), new Type[] { typeof(Vector2).MakeByRefType() }));
            DimensionConverter.ConvertPositions(Method(typeof(GroupSpriteObject), nameof(GroupSpriteObject.setPosOnly), new Type[] { typeof(Vector3).MakeByRefType() }));
            DimensionConverter.ConvertPositions(Method(typeof(GroupSpriteObject), nameof(GroupSpriteObject.set), new Type[] { typeof(Vector2).MakeByRefType(), typeof(Vector3).MakeByRefType() }));
            DimensionConverter.ConvertQuantum(Method(typeof(GroupSpriteObject), nameof(GroupSpriteObject.set), new Type[] { typeof(Vector2).MakeByRefType(), typeof(float) }), DimensionConverter.ToQuantum);
            DimensionConverter.ConvertQuantum(Method(typeof(GroupSpriteObject), nameof(GroupSpriteObject.set), new Type[] { typeof(Vector3).MakeByRefType(), typeof(float) }), DimensionConverter.ToQuantumWithHeight);
            DimensionConverter.ConvertPositions(Method(typeof(GroupSpriteObject), nameof(GroupSpriteObject.set), new Type[] { typeof(Vector3).MakeByRefType(), typeof(Vector2).MakeByRefType() }));
            DimensionConverter.ConvertPositions(Method(typeof(GroupSpriteObject), nameof(GroupSpriteObject.set), new Type[] { typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType() }));
        } 
        public static void Become3D()
        {
            Sphere.Begin();
            CameraManager.MakeCamera3D();
            Do3DStuff();
        }
        static void Do3DStuff()
        {
            World.world.heat_ray_fx.ray.transform.localPosition = Vector3.zero;
            World.world.heat_ray_fx.ray.transform.eulerAngles = new Vector3(180, 0, 0);
        }
        public static void Become2D()
        {
            Sphere.Finish();
            CameraManager.MakeCamera2D();
            do2DStuff();
        }
        static void do2DStuff()
        {
            World.world.heat_ray_fx.ray.transform.localPosition = new Vector3(0, 2000);
            World.world.heat_ray_fx.ray.transform.eulerAngles = Vector3.zero;
        }
        
        public static PixelFlashEffects FlashLayer => World.world.flash_effects;
        public static bool Generated = false;
        public static bool GeneratingSphere => savedSettings.Is3D && !Generated;
        public static bool IsWorld3D => Sphere.Exists;
        // the layer between the Mod and the compound sphere
        public static class Sphere
        {
            struct Shape
            {
                public Shape(To2D to2d, To2DFast to2dfast, GetSphereTilePosition to3d, GetSphereTileRotation rot, Initiation init, GetCameraRange GetCameraRange, bool IsWrapped)
                {
                    this.To2D = to2d;
                    this.To2DFast = to2dfast;
                    this.To3D = to3d;
                    this.tileRotation = rot;
                    this.Inititation = init;
                    this.GetCameraRange = GetCameraRange;
                    this.IsWrapped = IsWrapped;
                }
                public bool IsWrapped;
                public To2D To2D;
                public To2DFast To2DFast;
                public GetSphereTilePosition To3D;
                public GetSphereTileRotation tileRotation;
                public Initiation Inititation;
                public GetCameraRange GetCameraRange;
            }
            public static bool IsWrapped => CurrentShape.IsWrapped;
            public static float Radius => Manager.Radius;
            public static int Width => Manager.Rows;
            public static int Height => Manager.Cols;
            public static Transform CenterCapsule => Manager.transform.GetChild(0);
            public static bool Exists => Manager != null;
            public static float HeightMult = 0;
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
            public delegate Vector3 To2D(SphereManager manager, float x, float y, float z);
            public delegate Vector2 To2DFast(SphereManager manager, float x, float y, float z);
            static Shape CurrentShape;
            public static void GetCamerRange(out int Min, out int Max)
            {
                CurrentShape.GetCameraRange(Sphere.Manager, out Min, out Max);
            }
            static List<Shape> Shapes = new List<Shape>()
            {
                new Shape(CylindricalToCartesian, CylindricalToCartesianFast, CartesianToCylindrical, CylindricalRotation, CylindricalInitiation, RenderRange, true), //cylinder
                new Shape(FlatToCartesian, FlatToCartesianFast, CartesianToFlat, FlatRotation, FlatInitiation, RenderRangeFlat, false)//flat
            };
            public static void Begin()
            {
                CurrentShape = Shapes[savedSettings.CurrentShape];
                HeightMult = savedSettings.TileHeight;
                CreateSettings();
                int width = MapBox.width;
                int height = MapBox.height;
                Manager = SphereManager.Creator.CreateSphereManager(width, height, SphereManagerConfig);
            }
            public static Color32 GetColor(int index)
            {
                Color32 dst = World.world.world_layer.pixels[index];

                int r = dst.r * dst.a;
                int g = dst.g * dst.a;
                int b = dst.b * dst.a;
                int a = dst.a;

                foreach (MapLayer layer in BaseLayers)
                {
                    Color32 src = layer.pixels[index];
                    if (src.a == 0) continue;

                    int invSrcA = 255 - src.a;

                    r = (src.r * src.a + r * invSrcA) / 255;
                    g = (src.g * src.a + g * invSrcA) / 255;
                    b = (src.b * src.a + b * invSrcA) / 255;
                    a = (src.a + a * invSrcA / 255);
                }

                return new Color32((byte)r, (byte)g, (byte)b, (byte)Mathf.Clamp(a, 0, 255));
            }
            public static Color GetAddedColor(int Index)
            {
                return FlashLayer.pixels[Index].Normalised();
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
                if (Manager == null || Manager.gameObject == null)
                {
                    return;
                }
                Manager.Destroy();
            }
            public static Vector3 TilePosWithHeight(float X, float Y, float Z)
            {
                return CurrentShape.To2D(Manager, X, Y, Z);
            }
            public static Vector2 TilePos(float X, float Y, float Z)
            {
                return CurrentShape.To2DFast(Manager, X, Y, Z);
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
            public static Vector3 SpherePos(float X, float Y, float Height = 0)
            {
                return Manager.SphereTilePosition(X, Y, Height);
            }
            public static void Prepare()
            {
                LoadAssets();
                CreateTextures();
                BaseLayers = new List<MapLayer>(World.world._map_layers);
                BaseLayers.Remove(FlashLayer);
                CreateCachedColors();
            }
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
                LibraryMaterials.instance._night_affected_colors.Add(CompoundSphereMaterial);
            }
            public static SphereTile GetTile(int X, int Y)
            {
                return Manager[X, Y];
            }
            static void CreateSettings()
            {
                SphereManagerConfig = new SphereManagerSettings(
                    CurrentShape.Inititation,
                    CurrentShape.To3D,
                    CurrentShape.tileRotation,
                    SphereTileScale,
                    SphereTileColor,
                    SphereTileTexture,
                    getdisplaymode,
                    Textures,
                    CompoundSphereMesh,
                    CompoundSphereMaterial,
                    CurrentShape.GetCameraRange,
                    new List<IBufferData>() { new CustomBufferData<Vector3>("AddedColors", 12, SphereTileAddedColor) }
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
                        //this shit took me hours to solve
                        return sprite.PixelsFromSpriteAtlas();
                    }
                    return sprite.texture.GetPixels32();
                }
            }
        }
    }
}