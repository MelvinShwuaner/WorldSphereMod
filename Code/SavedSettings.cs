using System;
namespace WorldSphereMod
{
    [Serializable]
    public class SavedSettings
    {
        public string Version = "1.0.0";
        public bool Is3D = true;
        public bool InvertedCameraMovement = true;
        public bool InvertedWorld = true;
    }
}
