using HarmonyLib;
using UnityEngine;
using static WorldSphereMod.NewCamera.CameraManager;
namespace WorldSphereMod.NewCamera
{
    //yes this feels like cheating
    [HarmonyPatch(typeof(Camera), "get_orthographicSize")]
    public class GetSize
    {
        public static bool Prefix(ref float __result)
        {
            if (Core.IsWorld3D)
            {
                __result = Height;
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Camera), "set_orthographicSize")]
    public class SetSize
    {
        public static bool Prefix(float value)
        {
            if (Core.IsWorld3D)
            {
                Height = value;
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(MoveCamera), "update")]
    //this manages the camera
    public static class CameraManager
    {
        public static Vector2 Position => cam.transform.position;
        public static Transform transform => Camera.transform;
        public static void MakeCamera3D()
        {
            OriginalCamera.enabled = false;
            Camera.enabled = true;
            Camera.transform.LookAt(Core.Sphere.CenterCapsule);
            cam.mainCamera = Camera;
        }
        public static void MakeCamera2D()
        {
            OriginalCamera.enabled = true;
            Camera.enabled = false;
            cam.mainCamera = OriginalCamera;
        }
        //i want to rename this function to prepare but for some reason that breaks something. this is so fucking random i have no fucking idea how thats even possible
        public static void Begin()
        {
            Camera = new GameObject("WorldSphere Camera").AddComponent<Camera>();
            Camera.gameObject.tag = "MainCamera";
            cam = MoveCamera.instance;
            OriginalCamera = cam.mainCamera;
        }
        public static MoveCamera cam;
        public static Camera Camera;
        public static Camera OriginalCamera;
        public static float Height;
        static void Postfix()
        {
            if (!Core.IsWorld3D)
            {
                return;
            }
            Camera.transform.position = Core.Sphere.SpherePos(Position.x, Position.y, Height);
            Bench.bench("Draw Sphere", "game_total"); //im not even sure if the lag is actually tracked
            Core.Sphere.DrawTiles((int)Position.x);
            Bench.benchEnd("Draw Sphere", "game_total");
        }
    }
    [HarmonyPatch(typeof(MoveCamera), nameof(MoveCamera.move))]
    //would use a transpiler, but the c# compiler is a fucking bitch
    public class MovementEnhancement
    {
        public static void Move(HotkeyAsset pAsset)
        {
            string id = pAsset.id;
            float tMove = MoveCamera.getMoveDistance(pAsset.id.StartsWith("fast_")) * Mult;
            float Change = id.Contains("up") || id.Contains("right") ? tMove : -tMove;
            bool Vertical = id.Contains("down") || id.Contains("up");
            if (Core.savedSettings.InvertedCameraMovement ? !Vertical : Vertical)
            {
                cam._key_move_velocity.y += Change;
            }
            else
            {
                cam._key_move_velocity.x += Change;
            }
        }
        public static bool Prefix(HotkeyAsset pAsset)
        {
            if (Core.IsWorld3D)
            {
                Move(pAsset);
                return false;
            }
            return true;
        }
        static float Mult
        {
            get
            {
                return RotateCamera.Multiplier(RotateCamera.Rotation.y) * 5 / cam._target_zoom;
            }
        }
    }
    [HarmonyPatch(typeof(MoveCamera), nameof(MoveCamera.updateMouseCameraDrag))]
    public class RotateCamera
    {
        public static Vector2 Rotation = Vector2.zero;
        static bool Prefix()
        {
            if (Core.IsWorld3D)
            {
                UpdatePanning(cam);
                return false;
            }
            return true;
        }
        public static int Multiplier(float Rot, float Max = 180, float Min = 0)
        {
            if (Rot < Max && Rot > Min)
            {
                return -1;
            }
            return 1;
        }
        static void UpdateRotation(Vector2 Change)
        {
            Rotation.x = Mathf.Clamp(Rotation.x - Change.y, -90, 90);
            Rotation.y = Tools.MathStuff.Clamp(Rotation.y, Change.x, 360);
            transform.rotation = Quaternion.Euler(Rotation);
        }
        //i dont know how this fucking works and im too scared to touch it
        static void UpdatePanning(MoveCamera Cam)
        {
            MoveCamera.camera_drag_run = false;
            bool tInputDetectedDown = false;
            bool tInputDetected = false;
            if (InputHelpers.mouseSupported)
            {
                tInputDetectedDown = Cam.checkMouseInputDown();
                tInputDetected = Cam.checkMouseInput();
            }
            if (!tInputDetected)
            {
                Cam.clearTouches();
                return;
            }
            if (tInputDetectedDown && World.world.isOverUI())
            {
                Cam.clearTouches();
                return;
            }
            Vector3 Position = Input.mousePosition;
            if (tInputDetectedDown && Cam._origin.x == -1f && Cam._origin.z == -1f)
            {
                Cam._origin = Position;
            }
            if (Cam._origin.x == -1f && Cam._origin.y == -1f && Cam._origin.z == -1f)
            {
                return;
            }
            if (tInputDetected)
            {
                MoveCamera.camera_drag_run = true;
                Vector3 MousePos = Position;
                if (Toolbox.DistVec3(Vector3.zero, Position) > 0.1f)
                {
                    MoveCamera.camera_drag_activated = true;
                }
                Vector3 Change = MousePos - Cam._origin;
                Cam._origin = MousePos;
                if (InputHelpers.touchSupported)
                {
                    MoveCamera._touch_dist = Toolbox.DistVec3(Cam._first_touch, Cam.TouchPos(true));
                    if (World.world.player_control.touch_ticks_skip > 5)
                    {
                        if (MoveCamera._touch_dist >= 20f || (float)World.world.player_control.touch_ticks_skip > 0.3f)
                        {
                            World.world.player_control.already_used_zoom = true;
                            World.world.player_control.already_used_power = false;
                        }
                    }
                    else if (InputHelpers.touchCount == 1)
                    {
                        return;
                    }
                }
                if (!InputHelpers.mouseSupported)
                {
                    return;
                }
                UpdateRotation(Change / 2);
            }
        }
    }
    [HarmonyPatch(typeof(PixelDetector), nameof(PixelDetector.IntersectsSprite))]
    public class GetSphereTileUnderMouse
    {
        //who knew that finding the position your mouse touches on a fucking 3d tube is simpler the finding it on a fucking 2d square
        static bool Prefix(Ray ray, ref Vector2Int pVector, ref bool __result)
        {
            if (Core.IsWorld3D)
            {
                __result = Tools.IntersectMesh(ray, out Vector2 Vector);
                pVector = Vector.AsInt();
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(MoveCamera), nameof(MoveCamera.cameraToBounds))]
    public class Bounds3D
    {
        static void ToBounds3D()
        {
            Vector3 pos = default;
            pos.x = Core.Sphere.InBounds(cam.transform.position.x);
            pos.y = Mathf.Clamp(cam.transform.position.y, 0, MapBox.height-0.5f);
            pos.z = -0.5f;
            cam.transform.position = pos;
            World.world.nameplate_manager.update();
        }
        static bool Prefix()
        {
            if (Core.IsWorld3D)
            {
                ToBounds3D();
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(MoveCamera), nameof(MoveCamera.resetZoom))]
    public class Size3D
    {
        static float AsMaxHeight(float Num)
        {
            return Num/2;
        }
        static void ResetZoom()
        {
            int tInitialZoom;
            if (Screen.width < Screen.height)
            {
                tInitialZoom = Screen.width / 4;
            }
            else
            {
                tInitialZoom = Screen.height / 4;
            }
            int width = MapBox.width / 2;
            int height = MapBox.height;
            if (width > height)
            {
                cam.orthographicSizeMax = AsMaxHeight(width);
            }
            else
            {
                cam.orthographicSizeMax = AsMaxHeight(height);
            }
            if (tInitialZoom > cam.orthographicSizeMax)
            {
                tInitialZoom = (int)cam.orthographicSizeMax;
            }
            cam._target_zoom = tInitialZoom;
            cam.mainCamera.orthographicSize = Mathf.Clamp(cam._target_zoom, 10f, cam.orthographicSizeMax);
            World.world.setZoomOrthographic(cam.mainCamera.orthographicSize);
            cam.mainCamera.farClipPlane = 10000;
        }
        static bool Prefix()
        {
            if (Core.savedSettings.Is3D)
            {
                ResetZoom();
                return false;
            }
            return true;
        }
    }
}