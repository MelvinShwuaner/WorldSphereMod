using CompoundSpheres;
using NeoModLoader.api;
using UnityEngine;
using WorldSphereMod;
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
        if (!SystemInfo.supportsInstancing || !SystemInfo.supportsComputeShaders || !SystemInfo.supportsIndirectArgumentsBuffer)
        {
            throw new IncompatibleHardwareException();
        }
    }
    public string GetUrl()
    {
        return "https://github.com/MelvinShwuaner?tab=repositories";
    }

    public void Init()
    {
        Core.Init();
    }

    public void PostInit()
    {
        Core.PostInit();
    }

    public static GameObject Object;
    ModDeclare declare;
}
