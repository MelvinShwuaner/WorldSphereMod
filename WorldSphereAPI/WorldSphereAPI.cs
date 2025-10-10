using System;
using System.Reflection;
delegate bool IsWorld3D();
delegate void MakePerp(string ID);
delegate void EditEffect(string ID, bool IsUpright, bool SeperateSprite, float ExtraHeight, bool OnGround);
/// <summary>
/// WorldSphereMod API Calller
/// </summary>
public class WorldSphereAPI
{
    IsWorld3D is3D;
    MakePerp actor;
    MakePerp building;
    MakePerp proj;
    EditEffect editEffect;
    internal WorldSphereAPI() { }
    /// <summary>
    /// returns true if the world Is 3D
    /// </summary>
    public bool IsWorld3D { get { return is3D(); } }
    internal WorldSphereAPI(Type WorldSpherePort)
    {
        is3D = (IsWorld3D)Delegate.CreateDelegate(typeof(IsWorld3D), WorldSpherePort.GetMethod("IsWorld3D", BindingFlags.Static | BindingFlags.Public));
        actor = (MakePerp)Delegate.CreateDelegate(typeof(MakePerp), WorldSpherePort.GetMethod("MakeActorPerp", BindingFlags.Static | BindingFlags.Public));
        building = (MakePerp)Delegate.CreateDelegate(typeof(MakePerp), WorldSpherePort.GetMethod("MakeBuildingPerp", BindingFlags.Static | BindingFlags.Public));
        proj = (MakePerp)Delegate.CreateDelegate(typeof(MakePerp), WorldSpherePort.GetMethod("MakeProjectilePerp", BindingFlags.Static | BindingFlags.Public));
        editEffect = (EditEffect)Delegate.CreateDelegate(typeof(EditEffect), WorldSpherePort.GetMethod("EditEffect", BindingFlags.Static | BindingFlags.Public));
    }
    /// <summary>
    /// Makes a actor with asset <paramref name="ID"/> non upright, it will face towards the ground and not rotate to the camera
    /// </summary>
    /// <param name="ID"></param>
    public void MakeActorNonUpright(string ID)
    {
        actor(ID);
    }
    /// <summary>
    /// Makes a building with asset <paramref name="ID"/> non upright, it will face towards the ground and not rotate to the camera
    /// </summary>
    /// <param name="ID"></param>
    public void MakeBuildingNonUpright(string ID)
    {
        building(ID);
    }
    /// <summary>
    /// Makes a projectile with asset <paramref name="ID"/> non upright, it will face towards the ground and not rotate to the camera
    /// </summary>
    /// <param name="ID"></param>
    public void MakeProjectileNonUpright(string ID)
    {
        proj(ID);
    }
    /// <summary>
    /// edits the data of an effect that worldspheremod uses
    /// </summary>
    /// <param name="ID">the asset ID of the effect</param>
    /// <param name="isUpright">if upright, the effect will be rotated upright and can rotate towards the camera, like nukes for example</param>
    /// <param name="SeperateSprite">if true, worldspheremod will make the spriterenderer of your effect completly seperate, any changes to your effect wll translate over to the spriterenderer, and the spriterenderer will be in 3D space, and have rotations</param>
    /// <param name="ExtraHeight">how high off the ground the effect will be</param>
    /// <param name="OnGround">if true, the base height of the effect is the height of the tile it is on, otherwise the base height is 0</param>
    public void EditEffect(string ID, bool isUpright, bool SeperateSprite = false, float ExtraHeight = 0, bool OnGround = true)
    {
        editEffect(ID, isUpright, SeperateSprite, ExtraHeight, OnGround);
    }
    /// <summary>
    /// Connects to WorldSphereMod, if it is in the Game
    /// </summary>
    /// <remarks>only use this in post init</remarks>
    /// <returns>true if worldspheremod is detected</returns>
    public static bool Connect(out WorldSphereAPI API)
    {
        API = null;
        Type WorldSpherePort = Type.GetType("WorldSphereMod.API.WorldSphereModAPI, THE_3D_WORLDBOX_MOD, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
        if (WorldSpherePort != null)
        {
            API = new WorldSphereAPI(WorldSpherePort);
            return true;
        }
        return false;
    }
}
