using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using WorldSphereMod.NewCamera;
using WorldSphereMod.QuantumSprites;
using static HarmonyLib.AccessTools;
using static WorldSphereMod.Constants;
namespace WorldSphereMod
{
    //i fucking hate my life
    public static class DimensionConverter
    {
        public delegate void ToQuantumDelegate(Transform transform, Vector3 position);
        static Harmony Patcher => Core.Patcher;
        static HarmonyMethod PositionTranspiler;
        static HarmonyMethod QuantumTranspiler;
        public static void Prepare()
        {
            ToTranspile = new List<int>();
            PositionTranspiler = new HarmonyMethod(Method(typeof(TranspilerPosition), nameof(TranspilerPosition.Transpiler)));
            QuantumTranspiler = new HarmonyMethod(Method(typeof(TranspilerQuantum), nameof(TranspilerQuantum.Transpiler)));
        }
        static void CheckToTranspile(int[] totranspile)
        {
            ToTranspile.Clear();
            ToTranspile.AddRange(totranspile);
        }
        public static void ConvertPositions(MethodInfo Method, params int[] totranspile)
        {
            CheckToTranspile(totranspile);
            Patcher.Transpile(Method, PositionTranspiler);
        }
        public static void ConvertQuantum(MethodInfo Method, ToQuantumDelegate To3D, params int[] totranspile)
        {
            CheckToTranspile(totranspile);
            TranspilerQuantum.ToQuantum = To3D.Method;
            Patcher.Transpile(Method, QuantumTranspiler);
        }
        #region basic
        public static Vector3 ConvertTo2D(this Vector3 v)
        {
            if (!Core.IsWorld3D || !v.Is3D())
            {
                return v;
            }
            return Tools.To2D(v);
        }
        public static Vector3 ConvertTo3D(this Vector3 v)
        {
            if (!Core.IsWorld3D || v.Is3D())
            {
                return v;
            }
            return Tools.To3DTileHeight(v);
        }
        public static Vector3 ConvertTo3DWithHeight(this Vector3 v)
        {
            if (v.Is3D())
            {
                return v;
            }
            return Tools.To3D(v);
        }
        #endregion
        #region quantum
        public static void ToQuantum(this Transform transform, Vector3 v)
        {
            if (!Core.IsWorld3D)
            {
                transform.position = v;
                return;
            }
            v = v.ConvertTo3D();
            transform.position = v;
            transform.rotation = Tools.GetUprightRotation(v);
            QuantumSprites.Manager.RotateToCamera(transform);
        }
        public static void YToZ(this Transform transform, Vector3 v)
        {
            if (!Core.IsWorld3D)
            {
                transform.position = v;
                return;
            }
            v = Tools.To3D(v.x, v.y - v.z, v.z);
            transform.position = v;
            transform.rotation = Tools.GetUprightRotation(v);
            QuantumSprites.Manager.RotateToCamera(transform);
        }
        public static void ToQuantumWithHeight(this Transform transform, Vector3 v)
        {
            if (!Core.IsWorld3D)
            {
                transform.position = v;
                return;
            }
            if (!v.Is3D())
            {
                v = v.To3DTileHeight(true);
            }
            transform.position = v;
            transform.rotation = Tools.GetUprightRotation(v);
            QuantumSprites.Manager.RotateToCamera(transform);
        }
        public static void ToQuantumNonUpright(this Transform transform, Vector3 v)
        {
            if (!Core.IsWorld3D)
            {
                transform.position = v;
                return;
            }
            v = v.ConvertTo3D();
            transform.position = v;
            transform.rotation = Tools.GetRotation(v);
        }
        #endregion
        #region special
        public static void ToSpecial(this Transform transform, Vector3 v)
        {
            if (!Core.IsWorld3D)
            {
                transform.position = v;
                return;
            }
            transform.position = v.ConvertTo3DWithHeight();
            transform.rotation = Tools.GetRotation(v.AsIntClamped());
        }
        //osama bin laden once said "this type of shit, is why i bomb people"
        public static void ToSpecialNonUprightWithHeight(this Transform transform, Vector3 v)
        {
            if (!Core.IsWorld3D)
            {
                transform.position = v;
                return;
            }
            v = Tools.To3DTileHeight(v, SpecialHeight);
            transform.position = v;
            transform.rotation = Tools.GetRotation(v);
        }
        public static void ToSpecialUpright(this Transform transform, Vector3 v)
        {
            if (!Core.IsWorld3D)
            {
                transform.position = v;
                return;
            }
            v = Tools.To3D(v);
            transform.position = v;
            transform.rotation = Tools.GetUprightRotation(v);
            QuantumSprites.Manager.RotateToCamera(transform);
        }
        #endregion
        public static void Rotation3D(this Transform transform, Vector3 v)
        {
            transform.eulerAngles = v;
            if (!Core.IsWorld3D)
            {
                return;
            }
            transform.rotation *= Tools.GetUprightRotation(transform.position);
            if (Core.savedSettings.RotateStuffToCamera)
            {
                if (Core.savedSettings.RotateStuffToCameraAdvanced)
                {
                    transform.rotation *= Tools.RotateToCameraAtTile(transform.position);
                }
                else
                {
                    transform.forward = CameraManager.MainCamera.transform.forward;
                }
            }

        }
        static List<int> ToTranspile;
        class TranspilerPosition
        {
            static MethodInfo GetLocalPosition => Method(typeof(Transform), "get_localPosition");
            static MethodInfo SetLocalPosition => Method(typeof(Transform), "set_localPosition");
            static MethodInfo GetPosition => Method(typeof(Transform), "get_position");
            static MethodInfo SetPosition => Method(typeof(Transform), "set_position");
            static MethodInfo To2D => Method(typeof(DimensionConverter), nameof(ConvertTo2D));
            static MethodInfo To3D => Method(typeof(DimensionConverter), nameof(ConvertTo3D));
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
            public static MethodInfo ToQuantum;
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
                    Matcher.RemoveInstruction();
                    Matcher.Insert(new CodeInstruction(OpCodes.Callvirt, ToQuantum));
                }
                return Matcher.Instructions();
            }
        }
    }
}