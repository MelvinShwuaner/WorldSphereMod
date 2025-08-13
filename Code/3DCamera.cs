using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
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
        public static Vector2 Position => Manager.transform.position;
        public static Transform transform => MainCamera.transform;
        public static void MakeCamera3D()
        {
            OriginalCamera.enabled = false;
            MainCamera.enabled = true;
            Manager.mainCamera = MainCamera;
        }
        public static void MakeCamera2D()
        {
            OriginalCamera.enabled = true;
            MainCamera.enabled = false;
            Manager.mainCamera = OriginalCamera;
        }
        //i want to rename this function to prepare but for some reason that breaks something. this is so fucking random i have no fucking idea how thats even possible
        public static void Begin()
        {
            MainCamera = new GameObject("WorldSphere Camera").AddComponent<Camera>();
            MainCamera.gameObject.tag = "MainCamera";
            MainCamera.transparencySortMode = TransparencySortMode.Default;
            Manager = MoveCamera.instance;
            OriginalCamera = Manager.mainCamera;
        }
        public static MoveCamera Manager;
        public static Camera MainCamera;
        public static Camera OriginalCamera;
        public static float Height;
        public static float MaxHeight => Manager.orthographicSizeMax;
        static void Postfix()
        {
            if (!Core.IsWorld3D)
            {
                return;
            }
            MainCamera.transform.position = Core.Sphere.SpherePos(Position.x, Position.y, Height);
            float MinZoom = CameraTile.TileHeight()+1;
            if (ControllableUnit._unit_main != null || Manager._target_zoom < MinZoom)
            {
                Manager._target_zoom = MinZoom;
            }
            Bench.bench("Draw Sphere", "game_total"); //im not even sure if the lag is actually tracked
            Core.Sphere.DrawTiles((int)Position.x);
            Bench.benchEnd("Draw Sphere", "game_total");
        }
        public static WorldTile CameraTile => World.world.GetTile((int)Position.x, (int)Position.y);
    }
    public class MovementEnhancement
    {
        public static Vector3 GetMovementVector(float Speed, bool Vertical)
        {
            Vector3 vector;
            if (Core.savedSettings.InvertedCameraMovement ? !Vertical : Vertical)
            {
                vector = -transform.forward * Speed;
                vector.x *= RotateCamera.InvertMult;
            }
            else
            {
                vector = transform.right * Speed;
                vector.x *= RotateCamera.InvertMult;
            }
            return new Vector2(vector.z, vector.x);
        }
        public static void Move(HotkeyAsset pAsset)
        {
            string id = pAsset.id;
            float tMove = MoveCamera.getMoveDistance(pAsset.id.StartsWith("fast_")) * 5 / Manager._target_zoom;
            float Change = id.Contains("up") || id.Contains("right") ? tMove : -tMove;
            bool Vertical = id.Contains("down") || id.Contains("up");
            Manager._key_move_velocity += GetMovementVector(Change, Vertical);
        }
        [HarmonyPatch(typeof(MoveCamera), nameof(MoveCamera.move))]
        [HarmonyPrefix]
        public static bool movecamera(HotkeyAsset pAsset)
        {
            if (Core.IsWorld3D)
            {
                Move(pAsset);
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(ControllableUnit), nameof(ControllableUnit.updateMovementVectorKeyboard))]
        [HarmonyPostfix]
        public static void movepossesed()
        {
            if (ControllableUnit._movement_vector != Vector2.zero)
            {
                UpdatePossesed();
            }
        }
        public static void UpdatePossesed()
        {
            if(ControllableUnit._movement_vector.y != 0)
            {
                ControllableUnit._movement_vector = GetMovementVector(ControllableUnit._movement_vector.y, true);
            }
            else
            {
                ControllableUnit._movement_vector = GetMovementVector(ControllableUnit._movement_vector.x, false);
            }
        }
        [HarmonyPatch(typeof(ControllableUnit), nameof(ControllableUnit.updateMovementVectorJoystick))]
        [HarmonyPostfix]
        public static void movepossesedjoystick()
        {
            if (ControllableUnit.isMovementActionActive())
            {
                UpdatePossesed();
            }
        }
    }
    [HarmonyPatch(typeof(MoveCamera), nameof(MoveCamera.updateMouseCameraDrag))]
    public class RotateCamera
    {
        public static Vector3 Rotation = new Vector3(0, 90);
        static bool Prefix()
        {
            if (Core.IsWorld3D)
            {
                UpdatePanning(Manager);
                return false;
            }
            return true;
        }
        public static float InvertMult
        {
            get { return Rotation.x < 90 || Rotation.x > 270 ? 1 : -1; }
        }
        static void UpdateRotation(Vector2 Change)
        {
            Rotation.x = Tools.MathStuff.Wrap(Rotation.x, -Change.y, 360);
            Rotation.y = Rotation.y + (Change.x * InvertMult);
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
        //who knew that finding the position your mouse touches on a 3d tube is simpler the finding it on a 2d square
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
            pos.x = Mathf.Clamp(Tools.MathStuff.Wrap(Manager.transform.position.x, 0, Core.Sphere.Width), 0, Core.Sphere.Width);
            pos.y = Mathf.Clamp(Manager.transform.position.y, 0, MapBox.height-0.5f);
            pos.z = -0.5f;
            Manager.transform.position = pos;
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
                Manager.orthographicSizeMax = AsMaxHeight(width);
            }
            else
            {
                Manager.orthographicSizeMax = AsMaxHeight(height);
            }
            if (tInitialZoom > Manager.orthographicSizeMax)
            {
                tInitialZoom = (int)Manager.orthographicSizeMax;
            }
            Manager._target_zoom = tInitialZoom;
            Manager.mainCamera.orthographicSize = Mathf.Clamp(Manager._target_zoom, 2f, Manager.orthographicSizeMax);
            World.world.setZoomOrthographic(Manager.mainCamera.orthographicSize);
            Manager.mainCamera.farClipPlane = 20000;
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
    static class MinZoomTranspiler
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher Matcher = new CodeMatcher(instructions);
            while (Matcher.FindNext(new CodeInstruction(OpCodes.Ldc_R4, 10f)))
            {
                Matcher.RemoveInstruction();
                Matcher.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MinZoomTranspiler), nameof(GetMinZoom))));
            }
            return Matcher.Instructions();
        }
        public static float GetMinZoom()
        {
            if (Core.IsWorld3D)
            {
                return CameraTile.TileHeight() + 1;
            }
            return 10;
        }
    }
}