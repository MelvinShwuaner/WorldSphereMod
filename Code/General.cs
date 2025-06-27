using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
namespace WorldSphereMod.General
{
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
    public class GetTile3D
    {
        public static void Prefix(ref int pX, ref int pY)
        {
            if (!Core.IsWorld3D)
            {
                return;
            }
            Tools.To3DBounds(ref pX, ref pY);
        }
    }
    public class LoopWithBrush
    {
        [HarmonyPatch(typeof(MapBox), nameof(MapBox.loopWithBrush), new Type[] { typeof(WorldTile), typeof(BrushData), typeof(PowerActionWithID), typeof(string) })]
        [HarmonyPrefix]
        static bool ID(WorldTile pCenterTile, BrushData pBrush, PowerActionWithID pAction, string pPowerID)
        {
            if (Core.IsWorld3D)
            {
                loopWithBrush(pCenterTile, pBrush, pAction, pPowerID);
                return false;
            }
            return true;
        }
        static void loopWithBrush(WorldTile pCenterTile, BrushData pBrush, PowerActionWithID pAction, string pPowerID)
        {
            BrushPixelData[] tPos = pBrush.pos;
            int tLen = tPos.Length;
            for (int i = 0; i < tLen; i++)
            {
                BrushPixelData tPixelData = tPos[i];
                int tX = pCenterTile.x + tPixelData.x;
                int tY = pCenterTile.y + tPixelData.y;
                Tools.To3DBounds(ref tX, ref  tY);
                if (tX >= 0 && tX < MapBox.width && tY >= 0 && tY < MapBox.height)
                {
                    WorldTile tTile = MapBox.instance.GetTileSimple(tX, tY);
                    pAction(tTile, pPowerID);
                }
            }
        }
        [HarmonyPatch(typeof(MapBox), nameof(MapBox.loopWithBrush), new Type[] { typeof(WorldTile), typeof(BrushData), typeof(PowerAction), typeof(GodPower) })]
        [HarmonyPrefix]
        static bool Power(WorldTile pCenterTile, BrushData pBrush, PowerAction pAction, GodPower pPower)
        {
            if (Core.IsWorld3D)
            {
                loopWithBrush(pCenterTile, pBrush, pAction, pPower);
                return false;
            }
            return true;
        }
        static void loopWithBrush(WorldTile pCenterTile, BrushData pBrush, PowerAction pAction, GodPower power)
        {
            BrushPixelData[] tPos = pBrush.pos;
            int tLen = tPos.Length;
            for (int i = 0; i < tLen; i++)
            {
                BrushPixelData tPixelData = tPos[i];
                int tX = pCenterTile.x + tPixelData.x;
                int tY = pCenterTile.y + tPixelData.y;
                Tools.To3DBounds(ref tX, ref tY);
                if (tX >= 0 && tX < MapBox.width && tY >= 0 && tY < MapBox.height)
                {
                    WorldTile tTile = MapBox.instance.GetTileSimple(tX, tY);
                    pAction(tTile, power);
                }
            }
        }
        [HarmonyPatch(typeof(MapBox), nameof(MapBox.loopWithBrushPowerForDropsRandom))]
        [HarmonyPrefix]
        static bool Random(WorldTile pCenterTile, BrushData pBrush, PowerAction pAction, GodPower pPower)
        {
            if (Core.IsWorld3D)
            {
                loopWithBrushPowerForDropsRandom(pCenterTile, pBrush, pAction, pPower);
                return false;
            }
            return true;
        }
        static void loopWithBrushPowerForDropsRandom(WorldTile pCenterTile, BrushData pBrush, PowerAction pAction, GodPower pPower)
        {
            BrushPixelData[] tPos = pBrush.pos;
            int tLen = tPos.Length;
            using (ListPool<WorldTile> tListPool = new ListPool<WorldTile>())
            {
                for (int i = 0; i < tLen; i++)
                {
                    BrushPixelData tPixelData = tPos[i];
                    int tX = pCenterTile.x + tPixelData.x;
                    int tY = pCenterTile.y + tPixelData.y;
                    Tools.To3DBounds(ref tX, ref tY);
                    if (tX >= 0 && tX < MapBox.width && tY >= 0 && tY < MapBox.height)
                    {
                        WorldTile tTile = MapBox.instance.GetTileSimple(tX, tY);
                        tListPool.Add(tTile);
                    }
                }
                int tTotalDrops = pBrush.drops;
                tListPool.Shuffle<WorldTile>();
                for (int j = 0; j < tTotalDrops; j++)
                {
                    if (tListPool.Count == 0)
                    {
                        break;
                    }
                    WorldTile tTile2 = tListPool.Pop<WorldTile>();
                    pAction(tTile2, pPower);
                }
            }
        }
    }
    public class Dist3D
    {
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.Dist), new Type[] {typeof(float), typeof(float), typeof(float), typeof(float) })]
        [HarmonyPrefix]
        static bool distfloat(ref float __result, float x1, float y1, float x2, float y2)
        {
            __result = Tools.Dist(x1, x2, y1, y2);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.Dist), new Type[] { typeof(int), typeof(int), typeof(int), typeof(int) })]
        [HarmonyPrefix]
        static bool distint(ref float __result, int x1, int y1, int x2, int y2)
        {
            __result = Tools.Dist(x1, x2, y1, y2);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.SquaredDist), new Type[] { typeof(float), typeof(float), typeof(float), typeof(float) })]
        [HarmonyPrefix]
        static bool sqrdistfloat(ref float __result, float x1, float y1, float x2, float y2)
        {
            __result = Tools.SquaredDist(x1, x2, y1, y2);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.SquaredDist), new Type[] { typeof(int), typeof(int), typeof(int), typeof(int) })]
        [HarmonyPrefix]
        static bool sqrdistint(ref int __result, int x1, int y1, int x2, int y2)
        {
            __result = (int)Tools.SquaredDist(x1, x2, y1, y2);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.DistVec3))]
        [HarmonyPrefix]
        static bool distvec3(ref float __result, Vector3 pT1, Vector3 pT2)
        {
            __result = Tools.Dist(pT1.x, pT2.x, pT1.y, pT2.y);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.DistVec2))]
        [HarmonyPrefix]
        static bool distvec2(ref float __result, Vector2Int pT1, Vector2Int pT2)
        {
            __result = Tools.Dist(pT1.x, pT2.x, pT1.y, pT2.y);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.DistVec2Float))]
        [HarmonyPrefix]
        static bool distvec2float(ref float __result, Vector2 pT1, Vector2 pT2)
        {
            __result = Tools.Dist(pT1.x, pT2.x, pT1.y, pT2.y);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.SquaredDistVec3))]
        [HarmonyPrefix]
        static bool sqrdistvec3(ref float __result, Vector3 pT1, Vector3 pT2)
        {
            __result = Tools.SquaredDist(pT1.x, pT2.x, pT1.y, pT2.y);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.SquaredDistVec2))]
        [HarmonyPrefix]
        static bool sqrdistvec2(ref int __result, Vector2Int pT1, Vector2Int pT2)
        {
            __result = (int)Tools.SquaredDist(pT1.x, pT2.x, pT1.y, pT2.y);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.SquaredDistVec2Float))]
        [HarmonyPrefix]
        static bool sqrdistvec2float(ref float __result, Vector2 pT1, Vector2 pT2)
        {
            __result = Tools.SquaredDist(pT1.x, pT2.x, pT1.y, pT2.y);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.DistTile))]
        [HarmonyPrefix]
        static bool disttile(ref float __result, WorldTile pT1, WorldTile pT2)
        {
            __result = Tools.Dist(pT1.x, pT2.x, pT1.y, pT2.y);
            return false;
        }
        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.SquaredDistTile))]
        [HarmonyPrefix]
        static bool sqrdisttile(ref int __result, WorldTile pT1, WorldTile pT2)
        {
            __result = (int)Tools.SquaredDist(pT1.x, pT2.x, pT1.y, pT2.y);
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
            Matcher.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Tools), nameof(Tools.Lerp3D))));
            return Matcher.Instructions();
        }
    }
    public class WorldSphereTranspiler
    {
        static MethodInfo GetLocalPosition => AccessTools.Method(typeof(Transform), "get_localPosition");
        static MethodInfo SetLocalPosition => AccessTools.Method(typeof(Transform), "set_localPosition");
        static MethodInfo GetPosition => AccessTools.Method(typeof(Transform), "get_position");
        static MethodInfo SetPosition => AccessTools.Method(typeof(Transform), "set_position");
        static MethodInfo To2D => AccessTools.Method(typeof(Tools), nameof(Tools.To2D), new Type[] { typeof(Vector3) });
        static MethodInfo To3D => AccessTools.Method(typeof(Tools), nameof(Tools.To3DTileHeight));
        static Func<CodeInstruction, bool> IsGet => (CodeInstruction instruction) => instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo method && (method == GetPosition || method == GetLocalPosition);
        static Func<CodeInstruction, bool> IsSet => (CodeInstruction instruction) => instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo method && (method == SetPosition || method == SetLocalPosition);
        //this transpiler converts a transforms position to 2d space whenever read, and to 3D space when written
        //this is the most important transpiler in the mod
        public static IEnumerable<CodeInstruction> SphereWorldTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeMatcher Matcher = new CodeMatcher(instructions, generator);
            while (Matcher.FindNext(new CodeMatch((CodeInstruction instruction) => IsGet(instruction) || IsSet(instruction))))
            {
                if (IsGet(Matcher.Instruction))
                {
                    Matcher.Advance(1);
                    Matcher.Insert(new CodeInstruction(OpCodes.Call, To2D));
                }
                else
                {
                    Matcher.Insert(new CodeInstruction(OpCodes.Call, To3D));
                    Matcher.Advance(1);
                }
            }
            return Matcher.Instructions();
        }
    }
}
