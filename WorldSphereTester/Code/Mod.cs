using HarmonyLib;
using NeoModLoader;
using NeoModLoader.api;
using UnityEngine;

public class Mod : MonoBehaviour, IMod, IStagedLoad
{
    public ModDeclare GetDeclaration()
    {
        return declare;
    }
    public GameObject GetGameObject()
    {
        return Object;
    }
    public void OnLoad(ModDeclare pModDecl, GameObject pGameObject)
    {
        declare = pModDecl;
        Object = pGameObject;
    }
    public string GetUrl()
    {
        return "https://github.com/MelvinShwuaner?tab=repositories";
    }

    public void Init()
    {
        Debug.Log("Testing worldsphere API!");
    }

    public void PostInit()
    {
        if(!WorldSphereAPI.Connect(out WorldSphereMod))
        {
            Debug.Log("worldspheremod not detected!!!!!!");
            return;
        }
        Debug.Log("Detected WorldSphereMod!");
        Test();
    }
    public void Test()
    {
        WorldSphereMod.MakeActorNonUpright("human");
        WorldSphereMod.MakeBuildingNonUpright("jungle_tree");
        WorldSphereMod.EditEffect("fx_lightning_big", false, true, 20);
        WorldSphereMod.MakeProjectileNonUpright("red_orb");
        Harmony.CreateAndPatchAll(typeof(Mod), "worldsphere tester");
    }
    [HarmonyPatch(typeof(MapBox), "Update")]
    [HarmonyPostfix]
    public static void Postfix()
    {
        Debug.Log(WorldSphereMod.IsWorld3D);
    }
    static WorldSphereAPI WorldSphereMod;
    public static GameObject Object;
    ModDeclare declare;
}
