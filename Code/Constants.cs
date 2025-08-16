using System.Collections.Concurrent;
using UnityEngine;
using WorldSphereMod.Effects;

namespace WorldSphereMod
{
    public static class Constants
    {
        public const int ZDisplacement = 100;


        //square root of 1/2
        public const float HalfRoot = 0.70710678118f;
        //idk
        public const float TileHeightDiffSpeed = 0.8f;

        public static readonly Quaternion ConstRot = Quaternion.Euler(0, 90, 180);
        public static readonly Quaternion ToUpright = Quaternion.Euler(90, 0, 0);
        public static readonly Quaternion FromUpright = Quaternion.Euler(-90, 0, 0);
        public static readonly ConcurrentDictionary<string, EffectData> EffectDatas = new ConcurrentDictionary<string, EffectData>()
        {
            {"fx_meteorite", new EffectData(false) },
            {"fx_fire_smoke", new EffectData(false) },
            {"fx_antimatter_effect", new EffectData(false) },
            {"fx_napalm_flash", new EffectData(false) },
            {"fx_boulder", new EffectData(true) },
            {"fx_explosion_wave", new EffectData(false) },
            {"fx_tile_effect", new EffectData(false) },
            {"fx_cloud", new EffectData(false, true, 21, false) }
        };
        public const int SpecialHeight = 4;
        public static float YConst = 1f / (81 / Core.Sphere.HeightMult);
        public static Vector3 HighlightedZoneSize = new Vector3(1, 1 + (10 * YConst), 1);
        public static Vector3 Zero = Vector3.zero;
    }
}
