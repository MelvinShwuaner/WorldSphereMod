using NeoModLoader.General;
using System;
using System.Reflection;
using UnityEngine;
using WorldSphereMod.Effects;

namespace WorldSphereMod.API
{
    public static class WorldSphereModAPI
    {
        public static bool IsWorld3D()
        {
            return Core.IsWorld3D;
        }
        public static void MakeActorPerp(string ID)
        {
            Constants.PerpActors.Add(ID, true);
        }
        public static void MakeBuildingPerp(string ID)
        {
            Constants.PerpBuildings.Add(ID, true);
        }
        public static void MakeProjectilePerp(string ID)
        {
            Constants.PerpProjectiles.Add(ID, true);
        }
        public static void EditEffect(string ID, bool isUpright, bool SeperateSprite, float ExtraHeight, bool OnGround)
        {
            Constants.EffectDatas.Add(ID, new EffectData(isUpright, SeperateSprite, ExtraHeight, OnGround));
        }
        public static object GetSetting(string Name, Type Type)
        {
            try
            {
                FieldInfo field = typeof(SavedSettings).GetField(Name);
                return field.GetValue(Core.savedSettings);
            }
            catch (Exception ex)
            {
                Debug.Log($"Setting of Name {Name} and Type {Type} Not Found!");
                return null;
            }
        }
    }
}
