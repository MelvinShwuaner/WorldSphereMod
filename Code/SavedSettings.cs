using System;
namespace WorldSphereMod
{
    [Serializable]
    public class SavedSettings
    {
        public string Version = "1.0.2";
        public bool Is3D = true;
        public bool InvertedCameraMovement = true;
        public bool RotateStuffToCamera = true;
        public bool RotateStuffToCameraAdvanced = true;
        public float RenderRange = 2;
        public int CurrentShape = 1;
    }
}
