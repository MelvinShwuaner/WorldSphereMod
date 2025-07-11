using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading.Tasks;
using UnityEngine;
using WorldSphereMod.NewCamera;
using static WorldSphereMod.QuantumSprites.QuantumSpriteManager;
namespace WorldSphereMod.QuantumSprites
{
    public static class QuantumSpriteManager
    {
        //warning, if rotation not set beforehand, the sprite will go woosh woosh
        public static void RotateToCamera(Transform transform)
        {
            if (Core.savedSettings.RotateStuffToCamera)
            {
                if (!Core.savedSettings.RotateStuffToCameraAdvanced)
                {
                    transform.rotation *= Quaternion.LookRotation(CameraManager.Camera.transform.forward);
                }
                else
                {
                    Vector3 pos = transform.position;
                    transform.rotation *= Tools.RotateToCamera(ref pos);
                }
            }
        }
        public static void set(this GroupSpriteObject Object, ref Vector3 pPosition, float pScale, QuantumSpriteAsset pAsset)
        {
            if (Object._last_pos_v3.x != pPosition.x || Object._last_pos_v3.y != pPosition.y || Object._last_pos_v3.z != pPosition.z)
            {
                Object._last_pos_v2 = pPosition;
                Object._last_pos_v3 = pPosition;
                if (!pAsset.IsQuantumSpriteUpright())
                {
                    Object.m_transform.ToSpecial(Object._last_pos_v3);
                }
                else
                {
                    Object.m_transform.ToSpecialUpright(Object._last_pos_v3);
                }
            }
            if (Object._last_scale_v2.x != pScale)
            {
                Object.setScale(pScale);
            }
        }
        public static void drawSocialize3D(QuantumSpriteAsset pAsset)
        {
            if (!PlayerConfig.optionBoolEnabled("talk_bubbles"))
            {
                return;
            }
            float tMax = 1f;
            double tCurTime = World.world.getCurSessionTime();
            Actor[] tArr = World.world.units.visible_units_socialize.array;
            int tLen = World.world.units.visible_units_socialize.count;
            tLen = Math.Min(tLen, 1000);
            for (int i = 0; i < tLen; i++)
            {
                Actor tActor = tArr[i];
                if (!tActor.hasTrait("mute"))
                {
                    CommunicationAsset normal = CommunicationLibrary.normal;
                    float tDiff = (float)(tCurTime - tActor.timestamp_tween_session_social);
                    if (tDiff > tMax)
                    {
                        tDiff = 1f;
                    }
                    Vector3 headOffsetPositionForFunRendering = tActor.getHeadOffsetPositionForFunRendering();
                    float tTween = iTween.easeOutCubic(0f, 1f, tDiff);
                    float tOffsetX = Randy.randomFloat(-0.03f, 0.03f);
                    float tOffsetY = Randy.randomFloat(-0.03f, 0.03f);
                    Vector2 tScale = tActor.current_scale;
                    float tX = headOffsetPositionForFunRendering.x + tOffsetX * tScale.x;
                    float tY = headOffsetPositionForFunRendering.y + tOffsetY * tScale.y;
                    Vector2 tPos = new Vector2(tX, tY);
                    tScale.y *= tTween;
                    QuantumSprite tQBubble = pAsset.group_system.getNext();
                    tQBubble.set(ref tPos, tScale.y);
                    Sprite tSpeechSprite = normal.getSpriteBubble();
                    tQBubble.setSprite(tSpeechSprite);
                    if (normal.show_topic)
                    {
                        Vector3 tPosTopic = tQBubble.m_transform.TransformPoint(0, 10f, 0.1f);
                        QuantumSprite next = pAsset.group_system.getNext();
                        next.set(ref tPosTopic, tScale.y * 0.35f);
                        Sprite tTopicSprite = tActor.getSocializeTopic();
                        next.setSprite(tTopicSprite);
                    }
                }
            }
        }
        public static void setnotupright(this GroupSpriteObject Object, ref Vector2 pPosition, float pScale)
        {
            if (Object._last_pos_v3.x != pPosition.x || Object._last_pos_v3.y != pPosition.y)
            {
                Object._last_pos_v2 = pPosition;
                Object._last_pos_v3 = pPosition;
                Object.m_transform.ToSpecialNonUprightWithHeight(Object._last_pos_v3);
            }
            if (Object._last_scale_v2.x != pScale)
            {
                Object.setScale(pScale);
            }
        }
        public static bool IsQuantumSpriteUpright(this QuantumSpriteAsset pAsset)
        {
            return pAsset.id == "selected_units" || pAsset.id == "draw_building_stockpiles";
        }
        public static void DrawResourceInStockPile3D(QuantumSpriteAsset pAsset, Vector3 pMainPosition, Sprite pSprite, int pIndex, int pRow, int pColumn, ref Color pColor)
        {
            Vector3 tPos = pMainPosition;
            tPos.z -= 1;
            tPos.y += (0.58f * pRow)-5;
            tPos.x -= (0.5f * pColumn)-1;
            if (pColumn % 2 != 0)
            {
                tPos.y += 0.29f;
            }
            tPos.z += 0.5f * pIndex;
            QuantumSprite quantumSprite = QuantumSpriteLibrary.drawQuantumSprite(pAsset, tPos, null, null, null, null, 1f, false, -1f);
            quantumSprite.setSprite(pSprite);
            quantumSprite.setColor(ref pColor);
        }
    }
    public class QuantumSpritePatches
    {
        [HarmonyPatch(typeof(QuantumSpriteLibrary), nameof(QuantumSpriteLibrary.drawResourceIconOnStockpile))]
        [HarmonyPrefix]
        private static bool drawresourceiconpatch(QuantumSpriteAsset pAsset, Vector3 pMainPosition, Sprite pSprite, int pIndex, int pRow, int pColumn, ref Color pColor)
        {
            if (Core.IsWorld3D)
            {
                DrawResourceInStockPile3D(pAsset, pMainPosition, pSprite, pIndex, pRow, pColumn, ref pColor);
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(QuantumSpriteLibrary), nameof(QuantumSpriteLibrary.showLightAt))]
        [HarmonyPrefix]
        private static bool showLightAt(Vector2 pPos, Color pColor, float pScale = 1f)
        {
            QuantumSprite next = QuantumSpriteLibrary.light_areas.group_system.getNext();
            next.setnotupright(ref pPos, pScale);
            next.setColor(ref QuantumSpriteLibrary.light_areas.color);
            return false;
        }
        [HarmonyPatch(typeof(QuantumSpriteLibrary), nameof(QuantumSpriteLibrary.drawSocialize))]
        [HarmonyPrefix]
        private static bool drawresourceiconpatch(QuantumSpriteAsset pAsset)
        {
            if (Core.IsWorld3D)
            {
                drawSocialize3D(pAsset);
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(GroupSpriteObject), nameof(GroupSpriteObject.setScale), new Type[] {typeof(float)})]
        [HarmonyPrefix]
        static bool setScale(GroupSpriteObject __instance, float pScale)
        {
            if (__instance._last_scale_v3.y != pScale)
            {
                __instance._last_scale_v2 = new Vector2(pScale, pScale);
                __instance._last_scale_v3 = new Vector3(pScale, pScale, 0.1f);
                __instance.m_transform.localScale = __instance._last_scale_v3;
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(QuantumSpriteLibrary), nameof(QuantumSpriteLibrary.drawQuantumSprite), new Type[] { typeof(QuantumSpriteAsset), typeof(Vector3), typeof(WorldTile), typeof(Kingdom), typeof(City), typeof(BattleContainer), typeof(float), typeof(bool), typeof(float) })]
    public class MainQuantumSpritePatch
    {
        static void Prefix(QuantumSpriteAsset pAsset, ref Vector3 pPos)
        {
            if (!Core.IsWorld3D)
            {
                return;
            }
            if (pAsset.id == "highlight_cursor_zones")
            {
                pPos.z = 10;
            }
            else
            {
                pPos.z += Constants.SpecialHeight + Tools.GetTileHeightSmooth(pPos);
            }
        }
        static void Postfix(QuantumSpriteAsset pAsset, ref QuantumSprite __result)
        {
            if (!Core.IsWorld3D)
            {
                return;
            }
            if (pAsset.id == "highlight_cursor_zones")
            {
                __result.setScale(ref Constants.HighlightedZoneSize);
            }
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeMatcher Matcher = new CodeMatcher(instructions, generator);
            Matcher.MatchForward(false, new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(GroupSpriteObject), nameof(GroupSpriteObject.set), new Type[] { typeof(Vector3).MakeByRefType(), typeof(float) })));
            Matcher.RemoveInstruction();
            Matcher.Insert(new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(QuantumSpriteManager), nameof(QuantumSpriteManager.set))));
            return Matcher.Instructions();
        }
    }
    //better to patch data from the source if rotations are handled differently, or weird shit happens
    public static class SourcePatches
    {
        //i only need to change 2 LINES OF CODE. I WOULD USE A TRANSPILER, BUT THIS FUCKASS FUNCTION USES A DELEGATE, WHICH I CANNOT FUCKING TRANSPILE
        [HarmonyPatch(typeof(ActorManager), nameof(ActorManager.precalculateRenderDataParallel))]
        [HarmonyPrefix]
        public static bool calculateactordata3D(ActorManager __instance)
        {
            if (!Core.IsWorld3D)
            {
                return true;
            }
            int tDebugItemScale = (DebugConfig.isOn(DebugOption.RenderBigItems) ? 10 : 1);
            bool tShouldRenderUnitShadows = World.world.quality_changer.shouldRenderUnitShadows();
            int tTotalVisibleObjects = __instance.visible_units.count;
            Actor[] tArray = __instance.visible_units.array;
            int tDynamicBatchSize = 256;
            int tTotalBatches = ParallelHelper.calcTotalBatches(tTotalVisibleObjects, tDynamicBatchSize);
            Parallel.For(0, tTotalBatches, World.world.parallel_options, delegate (int pBatchIndex)
            {
                int num = ParallelHelper.calculateBatchBeg(pBatchIndex, tDynamicBatchSize);
                int tIndexEnd = ParallelHelper.calculateBatchEnd(num, tDynamicBatchSize, tTotalVisibleObjects);
                for (int tIndex = num; tIndex < tIndexEnd; tIndex++)
                {
                    Actor tActor = tArray[tIndex];
                    Vector3 tActorScale = tActor.current_scale;
                    Vector3 tCurrentActorPos = tActor.Get3DPos();
                    Vector3 tActorRotation = tActor.Get3DRot();
                    bool tHasRenderedItem = tActor.checkHasRenderedItem();
                    bool tHasNormalRender = !tActor.asset.ignore_generic_render;
                    Sprite tItemSpriteFinal;
                    if (tHasRenderedItem)
                    {
                        Sprite tItemSpriteMain = tActor.getRenderedItemSprite();
                        IHandRenderer cachedHandRendererAsset = tActor.getCachedHandRendererAsset();
                        int tColorAssetID = -900000;
                        if (cachedHandRendererAsset.is_colored)
                        {
                            tColorAssetID = tActor.kingdom.kingdomColor.GetHashCode();
                        }
                        tItemSpriteFinal = DynamicSprites.getCachedAtlasItemSprite(DynamicSprites.getItemSpriteID(tItemSpriteMain, tColorAssetID), tItemSpriteMain);
                    }
                    else
                    {
                        tItemSpriteFinal = null;
                    }
                    __instance.render_data.positions[tIndex] = tCurrentActorPos;
                    __instance.render_data.scales[tIndex] = tActorScale;
                    __instance.render_data.rotations[tIndex] = tActorRotation;
                    __instance.render_data.flip_x_states[tIndex] = tActor.flip;
                    __instance.render_data.colors[tIndex] = tActor.color;
                    __instance.render_data.has_normal_render[tIndex] = tHasNormalRender;
                    __instance.render_data.shadows[tIndex] = tActor.show_shadow;
                    __instance.render_data.has_item[tIndex] = tHasRenderedItem;
                    __instance.render_data.item_sprites[tIndex] = tItemSpriteFinal;
                    AnimationFrameData tFrameData = tActor.getAnimationFrameData();
                    if (tShouldRenderUnitShadows && tActor.show_shadow)
                    {
                        __instance.render_data.shadow_sprites[tIndex] = tActor.asset.shadow_sprite;
                        float tAbsAngle = Mathf.Abs(tActorRotation.z);
                        Vector2 tSizeShadow = tActor.asset.shadow_size * tActorScale;
                        int tFlip = (tActor.flip ? 1 : (-1));
                        float tOffsetX = tSizeShadow.x / 2f;
                        float tOffsetY = tSizeShadow.y * 0.6f;
                        Vector2 tShadowPosition = tActor.current_shadow_position;
                        tShadowPosition.x += tOffsetX * (tActorRotation.z * (float)tFlip) / 90f;
                        tShadowPosition.y -= tOffsetY * tAbsAngle / 90f;
                        __instance.render_data.shadow_position[tIndex] = tShadowPosition;
                        if (tFrameData != null && tFrameData.size_unit != default(Vector2))
                        {
                            float tScaleWidthLay = (tFrameData.size_unit * tActorScale).y / tSizeShadow.x * tActorScale.x;
                            float tScaleX = Mathf.Lerp(tActorScale.x, tScaleWidthLay, tAbsAngle / 90f);
                            __instance.render_data.shadow_scales[tIndex] = new Vector2(tScaleX, tActorScale.y);
                        }
                        else
                        {
                            __instance.render_data.shadow_scales[tIndex] = tActorScale;
                        }
                    }
                    if (tHasNormalRender)
                    {
                        if (tActor.canParallelSetColoredSprite())
                        {
                            Sprite tMainSprite = tActor.calculateMainSprite();
                            __instance.render_data.main_sprites[tIndex] = tMainSprite;
                            if (tActor.hasColoredSprite())
                            {
                                if (!tActor.isColoredSpriteNeedsCheck(tMainSprite))
                                {
                                    __instance.render_data.main_sprite_colored[tIndex] = tActor.getLastColoredSprite();
                                }
                                else
                                {
                                    __instance.render_data.main_sprite_colored[tIndex] = null;
                                }
                            }
                            else
                            {
                                __instance.render_data.main_sprite_colored[tIndex] = tMainSprite;
                            }
                        }
                        else
                        {
                            __instance.render_data.main_sprites[tIndex] = null;
                            __instance.render_data.main_sprite_colored[tIndex] = null;
                        }
                        if (tHasRenderedItem)
                        {
                            __instance.render_data.item_scale[tIndex] = tActorScale * (float)tDebugItemScale;
                            float tFrameDataPosX = 0f;
                            float tFrameDataPosY = 0f;
                            if (tFrameData != null)
                            {
                                tFrameDataPosX = tFrameData.pos_item.x;
                                tFrameDataPosY = tFrameData.pos_item.y;
                            }
                            float tX = tCurrentActorPos.x + tFrameDataPosX * tActorScale.x;
                            float tY = tCurrentActorPos.y + tFrameDataPosY * tActorScale.y;
                            float tZ = -0.01f + tFrameDataPosY * tActorScale.y;
                            Vector3 tItemPosition = new Vector3(tX, tY);
                            Vector3 tAngle = tActorRotation;
                            if (tAngle.y != 0f || tAngle.z != 0f)
                            {
                                Vector3 t_pivot = new Vector3(tCurrentActorPos.x, tCurrentActorPos.y, 0f);
                                tItemPosition = Toolbox.RotatePointAroundPivot(ref tItemPosition, ref t_pivot, ref tAngle);
                            }
                            tItemPosition.z = tZ;
                            __instance.render_data.item_pos[tIndex] = tItemPosition;
                        }
                    }
                }
            });
            return false;
        }
        [HarmonyPatch(typeof(BuildingManager), nameof(BuildingManager.precalculateRenderDataParallel))]
        [HarmonyPrefix]
        public static bool calculatebuildindata3D(BuildingManager __instance)
        {
            if (!Core.IsWorld3D)
            {
                return true;
            }
            Building[] tArrayVisibleBuildings = __instance._array_visible_buildings;
            bool tNeedShadows = World.world.quality_changer.shouldRenderBuildingShadows();
            int tTotalVisibleObjects = __instance._visible_buildings_count;
            Vector3[] tRenderScales = __instance.render_data.scales;
            Vector3[] tRenderPositions = __instance.render_data.positions;
            Vector3[] tRenderRotations = __instance.render_data.rotations;
            Material[] tRenderMaterials = __instance.render_data.materials;
            bool[] tRenderFlipXStates = __instance.render_data.flip_x_states;
            Color[] tRenderColors = __instance.render_data.colors;
            Sprite[] tRenderMainSprites = __instance.render_data.main_sprites;
            Sprite[] tRenderColoredSprites = __instance.render_data.colored_sprites;
            bool[] tRenderShadows = __instance.render_data.shadows;
            Sprite[] tRenderShadowSprites = __instance.render_data.shadow_sprites;
            int tDynamicBatchSize = 256;
            int tTotalBatches = ParallelHelper.calcTotalBatches(tTotalVisibleObjects, tDynamicBatchSize);
            bool tNeedNormalCheck = false;
            Parallel.For(0, tTotalBatches, World.world.parallel_options, delegate (int pBatchIndex)
            {
                int num = ParallelHelper.calculateBatchBeg(pBatchIndex, tDynamicBatchSize);
                int tIndexEnd = ParallelHelper.calculateBatchEnd(num, tDynamicBatchSize, tTotalVisibleObjects);
                for (int tIndex = num; tIndex < tIndexEnd; tIndex++)
                {
                    Building tBuilding = tArrayVisibleBuildings[tIndex];
                    BuildingAsset tAsset = tBuilding.asset;
                    tRenderScales[tIndex] = tBuilding.getCurrentScale();
                    tRenderPositions[tIndex] = tBuilding.Get3DPos();
                    tRenderRotations[tIndex] = tBuilding.Get3DRot();
                    tRenderMaterials[tIndex] = tBuilding.material;
                    tRenderFlipXStates[tIndex] = tBuilding.flip_x;
                    tRenderColors[tIndex] = tBuilding.kingdom.asset.color_building;
                    Sprite tMainSprite = tBuilding.calculateMainSprite();
                    tRenderMainSprites[tIndex] = tMainSprite;
                    if (tBuilding.isColoredSpriteNeedsCheck(tMainSprite))
                    {
                        tRenderColoredSprites[tIndex] = null;
                        tNeedNormalCheck = true;
                    }
                    else
                    {
                        tRenderColoredSprites[tIndex] = tBuilding.getLastColoredSprite();
                    }
                    if (tNeedShadows)
                    {
                        tRenderShadows[tIndex] = tAsset.shadow && !tBuilding.chopped;
                        tRenderShadowSprites[tIndex] = DynamicSprites.getShadowBuilding(tBuilding.asset, tRenderMainSprites[tIndex]);
                    }
                    if (tAsset.is_stockpile)
                    {
                        tNeedNormalCheck = true;
                    }
                    if (tAsset.sparkle_effect)
                    {
                        tNeedNormalCheck = true;
                    }
                }
            });
            __instance._need_normal_check = tNeedNormalCheck;
            return false;
        }
    }
}
