using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using static WorldSphereMod.Effects.EffectManager;
using static WorldSphereMod.Constants;
namespace WorldSphereMod.Effects
{
    public struct EffectData
    {
       public bool IsUpright;
       public bool SeperateSprite;
       public float ExtraHeight;
        public bool OnGround;
       public EffectData(bool isUpright, bool SeperateSprite = false, float ExtraHeight = 0, bool OnGround = true)
       {
            IsUpright = isUpright;
            this.ExtraHeight = ExtraHeight;
            this.SeperateSprite = SeperateSprite;
            this.OnGround = OnGround;
       }
    }
    public static class EffectManager
    {
        public static void Destroy(UnityEngine.Object obj)
        {
            if(obj == null)
            {
                return;
            }
            UnityEngine.Object.Destroy(obj);
        }
        //this is prob going to create a hundred errors in the future
        public static void SeperateSprite(BaseEffect Effect)
        {
            GameObject sprite = GetSprite(Effect.controller.prefab);
            Destroy(Effect.sprite_renderer);
            sprite.transform.position = Effect.transform.position;
            sprite.gameObject.SetActive(true);
            Effect.sprite_renderer = sprite.GetComponent<SpriteRenderer>();
            if (Effect.sprite_animation != null)
            {
                Effect.sprite_animation.spriteRenderer = Effect.sprite_renderer;
            }
        }
        public static GameObject GetSprite(Transform OriginalPrefab)
        {
            if(SpritePrefabs.TryGetValue(OriginalPrefab, out GameObject prefab)){
                return UnityEngine.Object.Instantiate(prefab);
            }
            GameObject sprite = UnityEngine.Object.Instantiate(OriginalPrefab.gameObject);
            Destroy(sprite.GetComponent<BaseEffect>());
            Destroy(sprite.GetComponent<SpriteShadow>());
            Destroy(sprite.GetComponent<SpriteAnimation>());
            SpritePrefabs.Add(OriginalPrefab, sprite);
            return UnityEngine.Object.Instantiate(sprite);
        }
        internal static ConcurrentDictionary<Transform, GameObject> SpritePrefabs = new ConcurrentDictionary<Transform, GameObject>();
        public static void SetEffect3D(BaseEffect Effect, EffectData Data)
        {
            Transform transform = Data.SeperateSprite ? Effect.sprite_renderer.transform : Effect.transform;
            Vector3 Pos = Effect.transform.position;
            bool Is3D = Pos.Is3D();
            if (Is3D)
            {
                Pos = Pos.To2D();
            }
            if (Data.IsUpright)
            {
                RotateToPlayer(transform, Pos);
            }
            else
            {
                transform.rotation = Tools.GetRotation(Pos.AsIntClamped());
            }
            if (!Is3D)
            {
                if (Data.SeperateSprite)
                {
                    UpdateSeperatedSprite(Effect);
                }
                else
                {
                    Effect.transform.position = ((Vector2)Effect.transform.position).To3D(Data.ExtraHeight + (Data.OnGround ? Tools.GetTileHeightSmooth(Effect.transform.position) : 0));
                }
            }
        }
        public static void RotateToPlayer(Transform transform, Vector3 Position)
        {
            Tools.GetCameraAngle(out Quaternion Rot, Position);
            transform.rotation = Rot;
        }
        public static void UpdateSeperatedSprite(BaseEffect Effect, bool OnGround = true, float Height = 0)
        {
            Effect.sprite_renderer.transform.position = ((Vector2)Effect.transform.position).To3D(Height + (OnGround ? Tools.GetTileHeightSmooth(Effect.transform.position) : 0));
            Effect.sprite_renderer.transform.localScale = Effect.transform.localScale;
        }
        public static void UpdateEffect(BaseEffect Effect)
        {
            EffectData Data = GetData(Effect);
            Transform transform = Data.SeperateSprite ? Effect.sprite_renderer.transform : Effect.transform;
            Vector3 Pos = Effect.transform.position;
            if (!Data.SeperateSprite)
            {
                Pos = Pos.To2D();
            }
            if (Data.IsUpright)
            {
                RotateToPlayer(transform, Pos);
            }
            else
            {
                try
                {
                    transform.rotation = Tools.GetRotation(Pos.AsIntClamped());
                }
                catch
                {
                    Debug.Log(Pos.AsIntClamped());
                }
            }
            if (Data.SeperateSprite)
            {
                UpdateSeperatedSprite(Effect, Data.OnGround, Data.ExtraHeight);
            }
        }
        public static void UpdateShadow(SpriteShadow Shadow)
        {
            Vector3 Pos = Shadow.sprRndCaster.transform.position;
            if (Pos.Is3D())
            {
                Pos = Pos.To2DWithHeight();
            }
            Shadow.transShadow.position = Tools.To3DTileHeight(new Vector2(Pos.x + Shadow.offset.x, Pos.y), Pos.z+Shadow.offset.y);
            Shadow.transShadow.rotation = Shadow.sprRndCaster.transform.rotation;
            Color tColor = Shadow.shadowColor;
            tColor.a = Shadow.sprRndCaster.color.a * 0.5f;
            Shadow.sprRndShadow.color = tColor;
            Shadow.sprRndShadow.sprite = Shadow.sprRndCaster.sprite;
            Shadow.sprRndShadow.flipX = Shadow.sprRndCaster.flipX;
        }
        public static void DestroyEffect(BaseEffect Effect)
        {
            if(Effect.sprite_renderer != null)
            {
                Destroy(Effect.sprite_renderer.gameObject);
            }
            if(Effect != null)
            {
                Destroy(Effect.gameObject);
            }
        }
        public static BaseEffect spawnAt3D(string pID, Vector3 pPos, float pScale)
        {
            BaseEffect tEffect = EffectsLibrary.spawn(pID);
            if (tEffect == null)
            {
                return null;
            }
            tEffect.Prepare3D(pPos, pScale);
            return tEffect;
        }
        public static void Prepare3D(this BaseEffect effect, Vector3 pVector, float pScale = 1f)
        {
            effect.state = 1;
            effect.transform.rotation = Quaternion.identity;
            effect.transform.localPosition = pVector;
            effect.setScale(pScale);
            effect.setAlpha(1f);
            effect.resetAnim();
        }
        public static EffectData GetData(BaseEffect Effect)
        {
            if (EffectDatas.TryGetValue(Effect.controller.asset.id, out var data))
            {
                return data;
            }
            return new EffectData(true, ShouldSeperateSprite(Effect));
        }
        //we should seperate the sprite if this a custom effect with custom code, otherwise no need.
        public static bool ShouldSeperateSprite(BaseEffect Effect)
        {
            return Effect.GetType().IsSubclassOf(typeof(BaseEffect)) && Effect.controller.prefab.HasComponent<SpriteRenderer>();
        }
    }
    class EffectPatches
    {
        [HarmonyPatch(typeof(BaseEffectController), nameof(BaseEffectController.GetObject))]
        [HarmonyPostfix]
        public static void SeperateSprite(BaseEffect __result)
        {
            if (!Core.IsWorld3D || __result == null)
            {
                return;
            }
            EffectData data = GetData(__result);
            if (data.SeperateSprite)
            {
                if (__result.sprite_renderer.gameObject == __result.gameObject)
                {
                    EffectManager.SeperateSprite(__result);
                }
                else
                {
                    __result.sprite_renderer.gameObject.SetActive(true);
                }
            }
        }
        //for the person whose reading this, did you know that the jews caused 911 and control america!
        public static void BasePatch(BaseEffect __instance)
        {
            if (!Core.IsWorld3D)
            {
                return;
            }
            EffectData data = GetData(__instance);
            SetEffect3D(__instance, data);
        }
        [HarmonyPatch(typeof(Meteorite), nameof(Meteorite.spawnOn))]
        [HarmonyPostfix]
        public static void MeteoritePatch(Meteorite __instance)
        {
            BasePatch(__instance);
        }
        [HarmonyPatch(typeof(ExplosionFlash), nameof(ExplosionFlash.start))]
        [HarmonyPostfix]
        public static void ExplosionPatch(ExplosionFlash __instance)
        {
            BasePatch(__instance);
        }
        [HarmonyPatch(typeof(EffectsLibrary), nameof(EffectsLibrary.spawnAt), new Type[] {typeof(string), typeof(Vector3), typeof(float) })]
        [HarmonyPrefix]
        public static bool Spawn3D(string pID, Vector3 pPos, float pScale, ref BaseEffect __result)
        {
            if (!Core.IsWorld3D)
            {
                return true;
            }
            if (pPos.Is3D())
            {
                __result = spawnAt3D(pID, pPos, pScale);
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(BaseEffect), nameof(BaseEffect.update))]
        [HarmonyPostfix]
        public static void UpdateEffect(BaseEffect __instance)
        {
            if (Core.IsWorld3D)
            {
                EffectManager.UpdateEffect(__instance);
            }
        }
        [HarmonyPatch(typeof(Cloud), nameof(Cloud.update))]
        [HarmonyPostfix]
        public static void UpdateCloud(Cloud __instance)
        {
            if (Core.IsWorld3D)
            {
                EffectManager.UpdateEffect(__instance);
            }
        }
        [HarmonyPatch(typeof(BaseEffect), nameof(BaseEffect.deactivate))]
        [HarmonyPrefix]
        public static void DeactivateSprite(BaseEffect __instance)
        {
            if (!Core.IsWorld3D)
            {
                return;
            }
            if(!GetData(__instance).SeperateSprite)
            {
                return;
            }
            __instance.sprite_renderer.gameObject.SetActive(false);
        }
        [HarmonyPatch(typeof(SpriteShadow), nameof(SpriteShadow.LateUpdate))]
        [HarmonyPrefix]
        public static bool Shadow3D(SpriteShadow __instance)
        {
            static void updateshadow(SpriteShadow shadow)
            {
                if (shadow.sprRndCaster == null)
                {
                    BaseEffect effect = shadow.GetComponent<BaseEffect>();
                    shadow.sprRndCaster = effect.sprite_renderer;
                }
                UpdateShadow(shadow);
            }
            if (Core.IsWorld3D)
            {
                updateshadow(__instance);
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(BaseEffectController), nameof(BaseEffectController.clear))]
        [HarmonyPrefix]
        public static bool Clear3D(BaseEffectController __instance)
        {
            if (Core.IsWorld3D)
            {
                List<BaseEffect> tList = __instance._list;
                for (int i = 0; i < tList.Count; i++)
                {
                    DestroyEffect(tList[i]);
                }
                tList.Clear();
                __instance.activeIndex = 0;
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(SpriteAnimation), nameof(SpriteAnimation.update))]
        [HarmonyPrefix]
        public static void FixSpriteAnimation(SpriteAnimation __instance)
        {
            if (__instance.spriteRenderer == null)
            {
                __instance.spriteRenderer = __instance.GetComponent<BaseEffect>()?.sprite_renderer;
            }
        }
        [HarmonyPatch(typeof(StatusParticle), nameof(StatusParticle.spawnParticle))]
        [HarmonyPrefix]
        public static bool FixParticle(StatusParticle __instance, Vector3 pVector, Color pColor, float pScale)
        {
            __instance.prepare(pVector, pScale);
            __instance.sprite_renderer.color = pColor;
            return false;
        }
    }
}