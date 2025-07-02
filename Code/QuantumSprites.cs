using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace WorldSphereMod.QuantumSprites
{
    public static class QuantumSpriteManager
    {

    }
    public class QuantumSpritePatches
    {
       // [HarmonyPatch(typeof(GroupSpriteObject), nameof(GroupSpriteObject.setRotation))]
       // [HarmonyPrefix]
        static bool SetRotation3D(GroupSpriteObject __instance, ref Vector3 pVec)
        {
            if (!Core.IsWorld3D)
            {
                return true;
            }
            if (__instance._last_angles_v3.y != pVec.y || __instance._last_angles_v3.z != pVec.z)
            {
                __instance._last_angles_v3 = pVec;
            }
            __instance.m_transform.rotation = Tools.RotateToCameraAtTile(__instance.m_transform.position) * Quaternion.Euler(__instance._last_angles_v3);
            return false;
        }
    }
}
