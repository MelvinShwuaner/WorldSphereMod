using EpPathFinding.cs;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
namespace WorldSphereMod.General
{
    //this fuckass mod uses so much fucking transpilers i made my own fucking type for fucking easy use (im not going to fucking use it anyway)
    public delegate IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions);
    public class SphereControl
    {
        [HarmonyPatch(typeof(MapBox), nameof(MapBox.finishMakingWorld))]
        [HarmonyPostfix]
        static void CreateSphere()
        {
            Core.Generated = true;
            if(Core.savedSettings.Is3D)
            {
                SmoothLoader.add(delegate { Core.Become3D(); }, "Becoming 3D!");
            }
        }
        [HarmonyPatch(typeof(MapBox), nameof(MapBox.addClearWorld))]
        [HarmonyPostfix]
        static void DestroySphere()
        {
            Core.Generated = false;
            SmoothLoader.add(delegate { Core.Become2D(); }, "Becoming 2D!");
        }
    }
    [HarmonyPatch(typeof(Actor), nameof(Actor.precalcMovementSpeed))]
    public class actormovement3D
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher Matcher = new CodeMatcher(instructions);
            Matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SimGlobalAsset), nameof(SimGlobalAsset.unit_speed_multiplier))));
            Matcher.Advance(1);
            Matcher.Insert(new CodeInstruction(OpCodes.Mul), new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Tools), nameof(Tools.GetDistSpeedMult))));
            return Matcher.Instructions();
        }
    }
    public class WorldLoop
    {
        public static void Tiles(ref int pX, ref int pY)
        {
            if (!Core.IsWorld3D && !Core.GeneratingSphere)
            {
                return;
            }
            Tools.To3DBounds(ref pX, ref pY);
        }
        [HarmonyPatch(typeof(ZoneCalculator), nameof(ZoneCalculator.getZone))]
        [HarmonyPrefix]
        static void Zones(ref int pX, ref int pY)
        {
            if (!Core.IsWorld3D && !Core.GeneratingSphere)
            {
                return;
            }
            pX = (int)Tools.MathStuff.Wrap(pX, 0, World.world.zone_calculator.zones_total_x);
        }
        [HarmonyPatch(typeof(MapChunkManager), nameof(MapChunkManager.get), new Type[] {typeof(int), typeof(int)})]
        [HarmonyPrefix]
        static void Chunks(ref int pX, ref int pY)
        {
            if (!Core.IsWorld3D && !Core.GeneratingSphere)
            {
                return;
            }
            pX = (int)Tools.MathStuff.Wrap(pX, 0, World.world.map_chunk_manager._get_amount_x);
        }
    }
    public static class BrushTranspiler
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher Matcher = new CodeMatcher(instructions);
            Matcher.MatchForward(false, new CodeMatch(OpCodes.Add));
            Matcher.RemoveInstruction();
            Matcher.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Tools), nameof(Tools.AddLooped))));
            return Matcher.Instructions();
        }
    }
    public class Dist3D
    {
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.Dist), new Type[] {typeof(float), typeof(float), typeof(float), typeof(float) })]
        [HarmonyPrefix]
        static bool distfloat(ref float __result, float x1, float y1, float x2, float y2)
        {
            __result = Tools.MathStuff.Dist(x1, x2, y1, y2);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.Dist), new Type[] { typeof(int), typeof(int), typeof(int), typeof(int) })]
        [HarmonyPrefix]
        static bool distint(ref float __result, int x1, int y1, int x2, int y2)
        {
            __result = Tools.MathStuff.Dist(x1, x2, y1, y2);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.SquaredDist), new Type[] { typeof(float), typeof(float), typeof(float), typeof(float) })]
        [HarmonyPrefix]
        static bool sqrdistfloat(ref float __result, float x1, float y1, float x2, float y2)
        {
            __result = Tools.MathStuff.SquaredDist(x1, x2, y1, y2);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.SquaredDist), new Type[] { typeof(int), typeof(int), typeof(int), typeof(int) })]
        [HarmonyPrefix]
        static bool sqrdistint(ref int __result, int x1, int y1, int x2, int y2)
        {
            __result = (int)Tools.MathStuff.SquaredDist(x1, x2, y1, y2);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.DistVec3))]
        [HarmonyPrefix]
        static bool distvec3(ref float __result, Vector3 pT1, Vector3 pT2)
        {
            __result = Tools.MathStuff.Dist(pT1.x, pT2.x, pT1.y, pT2.y);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.DistVec2))]
        [HarmonyPrefix]
        static bool distvec2(ref float __result, Vector2Int pT1, Vector2Int pT2)
        {
            __result = Tools.MathStuff.Dist(pT1.x, pT2.x, pT1.y, pT2.y);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.DistVec2Float))]
        [HarmonyPrefix]
        static bool distvec2float(ref float __result, Vector2 pT1, Vector2 pT2)
        {
            __result = Tools.MathStuff.Dist(pT1.x, pT2.x, pT1.y, pT2.y);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.SquaredDistVec3))]
        [HarmonyPrefix]
        static bool sqrdistvec3(ref float __result, Vector3 pT1, Vector3 pT2)
        {
            __result = Tools.MathStuff.SquaredDist(pT1.x, pT2.x, pT1.y, pT2.y);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.SquaredDistVec2))]
        [HarmonyPrefix]
        static bool sqrdistvec2(ref int __result, Vector2Int pT1, Vector2Int pT2)
        {
            __result = (int)Tools.MathStuff.SquaredDist(pT1.x, pT2.x, pT1.y, pT2.y);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.SquaredDistVec2Float))]
        [HarmonyPrefix]
        static bool sqrdistvec2float(ref float __result, Vector2 pT1, Vector2 pT2)
        {
            __result = Tools.MathStuff.SquaredDist(pT1.x, pT2.x, pT1.y, pT2.y);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.DistTile))]
        [HarmonyPrefix]
        static bool disttile(ref float __result, WorldTile pT1, WorldTile pT2)
        {
            __result = Tools.MathStuff.Dist(pT1.x, pT2.x, pT1.y, pT2.y);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.SquaredDistTile))]
        [HarmonyPrefix]
        static bool sqrdisttile(ref int __result, WorldTile pT1, WorldTile pT2)
        {
            __result = (int)Tools.MathStuff.SquaredDist(pT1.x, pT2.x, pT1.y, pT2.y);
            return false;
        }
    }
    public class Lerp3D
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher Matcher = new CodeMatcher(instructions);
            Matcher.MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Vector2), nameof(Vector2.Lerp))));
            Matcher.RemoveInstruction();
            Matcher.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Tools.MathStuff), nameof(Tools.MathStuff.Lerp3D))));
            return Matcher.Instructions();
        }
    }
    public class Move3D
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher Matcher = new CodeMatcher(instructions);
            Matcher.MatchForward(false, new CodeMatch((CodeInstruction instruct) => instruct.opcode == OpCodes.Call && instruct.operand is MethodInfo info && info.Name == "MoveTowards"));
            if ((Matcher.Operand as MethodInfo).DeclaringType == typeof(Vector2))
            {
                Matcher.RemoveInstruction();
                Matcher.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Tools), nameof(Tools.MoveTowards))));
            }
            else if((Matcher.Instruction.operand as MethodInfo).DeclaringType == typeof(Vector3))
            {
                Matcher.RemoveInstruction();
                Matcher.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Tools), nameof(Tools.MoveTowardsV3))));
            }
            return Matcher.Instructions();
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.getMousePos))]
    public class MousePos3D
    {
        static void Postfix(ref Vector2 __result)
        {
            if (Core.IsWorld3D)
            {
                Vector2 mousepos = Input.mousePosition;
                if(Camera.main.ScreenPointToRay(mousepos, out Ray ray))
                {
                    if (Tools.IntersectMesh(ray, out Vector2 pos))
                    {
                        __result = pos;
                    }
                }
            }
        }
    }
    public class Drop3D {
        [HarmonyPatch(typeof(Drop), nameof(Drop.updatePosition))]
        [HarmonyPrefix]
        static bool UpdatePos(Drop __instance)
        {
            if (Core.IsWorld3D)
            {
                __instance.transform.position = Tools.To3D(__instance.current_position, __instance._currentHeightZ);
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(Drop), nameof(Drop.updateRotation))]
        [HarmonyPostfix]
        static void UpdateRot(Drop __instance)
        {
            if (Core.IsWorld3D)
            {
                __instance.transform.rotation *= Tools.RotateToCameraAtTile(__instance.current_position.AsIntClamped());
            }
        }
    }
    [HarmonyPatch(typeof(ParticleSystem), nameof(ParticleSystem.Emit), new Type[] {typeof(ParticleSystem.EmitParams), typeof(int) })]
    public class FixParticles
    {
        static void Prefix(ref ParticleSystem.EmitParams emitParams)
        {
            if (Core.IsWorld3D)
            {
                emitParams.position = Tools.To3DTileHeight(emitParams.position);
            }
        }
    }
    [HarmonyPatch(typeof(AStarFinder), nameof(AStarFinder.FindPath))]
    public class FixPathfinding
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher Matcher = new CodeMatcher(instructions);
            //first we find the all Subtract functions used to calculate node distance and replace them with a method that accounts for looping, but dont do this if its for the Y Axis
            int Current = 0;
            while(Matcher.FindNext(new CodeMatch(OpCodes.Sub)))
            {
                if(Current++ % 2 == 1)
                {
                    continue;
                }
                Matcher.RemoveInstruction();
                Matcher.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FixPathfinding), nameof(SubLooped))));
            }
            return Matcher.Instructions();
        }
        public static int SubLooped(int x1, int x2)
        {
            if (Core.IsWorld3D)
            {
                return (int)Tools.MathStuff.WrappedDist(x1, x2);
            }
            return x1 - x2;
        }
    }
    [HarmonyPatch(typeof(Actor), nameof(Actor.addChildren))]
    public static class FixCrabzilla { 
       public static void Postfix(Actor __instance)
        {
            if (!Core.IsWorld3D)
            {
                return;
            }
            if (__instance.asset.avatar_prefab == "p_crabzilla")
            {
                GameObject Crabzilla = __instance.avatar;
                foreach (Transform transform in Crabzilla.transform.GetAllChildren())
                {
                    transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
                }
            }
        }
    }
    public static class DisableSettingPositions
    {
        static MethodInfo setpos = AccessTools.Method(typeof(Transform), "set_position");
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher Matcher = new CodeMatcher(instructions);
            while (Matcher.FindNext(new CodeMatch(OpCodes.Callvirt, setpos)))
            {
                Matcher.RemoveInstruction();
                Matcher.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DisableSettingPositions), nameof(SetPositionIf2D))));
            }
            return Matcher.Instructions();
        }
        public static void SetPositionIf2D(this Transform transform, Vector3 Pos)
        {
            if (!Core.IsWorld3D)
            {
                transform.position = Pos;
            }
        }
    }
    [HarmonyPatch(typeof(BatchActors), nameof(BatchActors.updateVisibility))]
    public static class DontShowPossessedUnit
    {
        static MethodInfo setpos = AccessTools.Method(typeof(Actor), nameof(Actor.isInsideSomething));
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher Matcher = new CodeMatcher(instructions);
            Matcher.MatchForward(false, new CodeMatch(OpCodes.Callvirt, setpos));
            Matcher.RemoveInstruction();
            Matcher.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DontShowPossessedUnit), nameof(IsVisible))));
            return Matcher.Instructions();
        }
        public static bool IsVisible(this Actor actor)
        {
            bool IsMainUnit(Actor actor)
            {
                return Core.IsWorld3D && ControllableUnit._unit_main == actor;
            }
            return actor.isInsideSomething() || IsMainUnit(actor);
        }
    }
}
