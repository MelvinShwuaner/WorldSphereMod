using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static HarmonyLib.AccessTools;
namespace WorldSphereMod
{
    //i fucking hate my life
    public static class DimensionConversions
    {
        static Harmony Harmony => Core.Patcher;
        static HarmonyMethod PositionTranspiler;
        static HarmonyMethod RotationTranspiler;
        static HarmonyMethod QuantumTranspiler;
        public static void Prepare()
        {
            ToTranspile = new List<int>();
            PositionTranspiler = new HarmonyMethod(Method(typeof(TranspilerPosition), nameof(TranspilerPosition.Transpiler)));
            RotationTranspiler = new HarmonyMethod(Method(typeof(TranspilerRotation), nameof(TranspilerRotation.Transpiler)));
            QuantumTranspiler = new HarmonyMethod(Method(typeof(TranspilerQuantum), nameof(TranspilerQuantum.Transpiler)));
        }
        public static void ConvertPositions(MethodInfo Method, params int[] totranspile)
        {
            ToTranspile.Clear();
            ToTranspile.AddRange(totranspile);
            Harmony.Transpile(Method, PositionTranspiler);
        }
        public static void ConvertRotations(MethodInfo Method, params int[] totranspile)
        {
            ToTranspile.Clear();
            ToTranspile.AddRange(totranspile);
            Harmony.Transpile(Method, RotationTranspiler);
        }
        public static void ConvertBoth(MethodInfo Method, params int[] totranspile)
        {
            ToTranspile.Clear();
            ToTranspile.AddRange(totranspile);
            Harmony.Transpile(Method, QuantumTranspiler);
        }
        public static Vector3 ConvertTo2D(this Vector3 v)
        {
            if (!Core.IsWorld3D)
            {
                return v;
            }
            return Tools.To2D(v);
        }
        public static Vector3 ConvertTo3D(this Vector3 v)
        {
            if (!Core.IsWorld3D)
            {
                return v;
            }
            return Tools.To3DTileHeight(v);
        }
        public static void ToQuantum(this Transform transform, Vector3 v)
        {
            if (!Core.IsWorld3D)
            {
                transform.position = v;
                return;
            }
            v = v.ConvertTo3D();
            transform.position = v;
            transform.rotation = Tools.GetUprightRotation(v.x, v.y);
        }
        public static void ToQuantumLocal(this Transform transform, Vector3 v)
        {
            if (!Core.IsWorld3D)
            {
                transform.localPosition = v;
                return;
            }
            v = v.ConvertTo3D();
            transform.localPosition = v;
            transform.rotation = Tools.GetUprightRotation(v.x, v.y);
        }
        public static void Rotation3D(this Transform transform, Vector3 v)
        {
            transform.eulerAngles = v;
            if (!Core.IsWorld3D)
            {
                return;
            }
            transform.rotation *= Tools.RotateToCameraAtTile(transform.position);
        }
        static List<int> ToTranspile;
        class TranspilerPosition
        {
            static MethodInfo GetLocalPosition => Method(typeof(Transform), "get_localPosition");
            static MethodInfo SetLocalPosition => Method(typeof(Transform), "set_localPosition");
            static MethodInfo GetPosition => Method(typeof(Transform), "get_position");
            static MethodInfo SetPosition => Method(typeof(Transform), "set_position");
            static MethodInfo To2D => Method(typeof(DimensionConversions), nameof(ConvertTo2D));
            static MethodInfo To3D => Method(typeof(DimensionConversions), nameof(ConvertTo3D));
            static Func<CodeInstruction, bool> IsGet => (CodeInstruction instruction) => instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo method && (method == GetPosition || method == GetLocalPosition);
            static Func<CodeInstruction, bool> IsSet => (CodeInstruction instruction) => instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo method && (method == SetPosition || method == SetLocalPosition);
            //this transpiler converts a transforms position to 2d space whenever read, and to 3D space when written
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                CodeMatcher Matcher = new CodeMatcher(instructions, generator);
                int Current = 0;
                while (Matcher.FindNext(new CodeMatch((CodeInstruction instruction) => IsGet(instruction) || IsSet(instruction))))
                {
                    if (!ToTranspile.Contains(Current++) && ToTranspile.Count != 0)
                    {
                        continue;
                    }
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
        class TranspilerQuantum
        {
            static MethodInfo SetLocalPosition => Method(typeof(Transform), "set_localPosition");
            static MethodInfo SetPosition => Method(typeof(Transform), "set_position");
            static MethodInfo ToQuantum => Method(typeof(DimensionConversions), nameof(DimensionConversions.ToQuantum));
            static MethodInfo ToQuantumLocal => Method(typeof(DimensionConversions), nameof(DimensionConversions.ToQuantumLocal));
            static Func<CodeInstruction, bool> IsSet => (CodeInstruction instruction) => instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo method && (method == SetPosition || method == SetLocalPosition);
            //this transpiler converts a transforms position to 2d space whenever read, and to 3D space when written
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                CodeMatcher Matcher = new CodeMatcher(instructions, generator);
                int Current = 0;
                while (Matcher.FindNext(new CodeMatch((CodeInstruction instruction) => IsSet(instruction))))
                {
                    if (!ToTranspile.Contains(Current++) && ToTranspile.Count != 0)
                    {
                        continue;
                    }
                    if ((MethodInfo)Matcher.Operand == SetPosition)
                    {
                        Matcher.RemoveInstruction();
                        Matcher.Insert(new CodeInstruction(OpCodes.Callvirt, ToQuantum));
                    }
                    else
                    {
                        Matcher.RemoveInstruction();
                        Matcher.Insert(new CodeInstruction(OpCodes.Callvirt, ToQuantumLocal));
                    }
                }
                return Matcher.Instructions();
            }
        }
        class TranspilerRotation
        {
            static MethodInfo SetRotation => Method(typeof(Transform), "set_eulerAngles");
            static MethodInfo To3D => Method(typeof(DimensionConversions), nameof(Rotation3D));
            static Func<CodeInstruction, bool> IsSet => (CodeInstruction instruction) => instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo method && (method == SetRotation);
            //this transpiler converts a transforms position to 2d space whenever read, and to 3D space when written
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                CodeMatcher Matcher = new CodeMatcher(instructions, generator);
                int Current = 0;
                while (Matcher.FindNext(new CodeMatch((CodeInstruction instruction) => IsSet(instruction))))
                {
                    if (!ToTranspile.Contains(Current++) && ToTranspile.Count != 0)
                    {
                        continue;
                    }
                    Matcher.RemoveInstruction();
                    Matcher.Insert(new CodeInstruction(OpCodes.Call, To3D));
                }
                return Matcher.Instructions();
            }
        }
    }
}
