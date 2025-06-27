using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using WorldSphereMod.NewCamera;
using static UnityEngine.Random;
using static WorldSphereMod.Effects.EffectManager;
namespace WorldSphereMod.Effects
{
    public struct EffectData
    {
       public bool IsUpright;
       public EffectData(bool isUpright)
       {
            IsUpright = isUpright;
       }
    }
    public static class EffectManager
    {
        static void Add(this ConcurrentDictionary<string, EffectData> dict, string value, EffectData data)
        {
            dict.TryAdd(value, data);
        }
        internal static ConcurrentDictionary<string, EffectData> EffectDatas = new ConcurrentDictionary<string, EffectData>()
        {
            {"fx_meteorite", new EffectData(false) },
            {"fx_fire_smoke", new EffectData(false) },
            {"fx_antimatter_effect", new EffectData(false) },
        };
       public static readonly EffectData DefaultData = new EffectData(true);
       public static void SetEffect3D(BaseEffect Effect, bool Upright)
       {
            if (!Effect.transform.position.Is3D())
            {
                Effect.transform.position = Tools.To3D(Effect.transform.position, Effect.GetTile()?.TileHeight() ?? 0);
            }
            if (Upright)
            {
                Effect.transform.rotation = Tools.GetUprightRotation(Effect.transform.position.x, Effect.transform.position.y) * Tools.RotateToCamera(Effect.transform.position.x, Effect.transform.position.y, Effect.transform.position.z);
            }
            else
            {
                Effect.transform.rotation = Tools.GetRotation(Effect.transform.position.x, Effect.transform.position.y);
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
        public static EffectData GetData(string ID)
        {
            if(EffectDatas.TryGetValue(ID, out var data))
            {
                return data;
            }
            return DefaultData;
        }
    }
    class EffectPatches
    {
        public static void BasePatch(BaseEffect __instance)
        {
            if (!Core.IsWorld3D)
            {
                return;
            }
            string pID = __instance.controller.asset.id;
            EffectData data = GetData(pID);
            SetEffect3D(__instance, data.IsUpright);
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
    }
}
