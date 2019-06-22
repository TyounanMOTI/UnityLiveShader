﻿using System;
using System.Runtime.InteropServices;

namespace UnityLiveShader
{
    public static class Library
    {
        public static IntPtr GetDrawCallback()
        {
            return NativePlugin.GetDrawCallback();
        }

        public static bool SetShaderCode(string code)
        {
            return NativePlugin.SetShaderCode(code) == 0;
        }

        public static void SetTime(float time)
        {
            NativePlugin.SetTime(time);
        }
    }

    static class NativePlugin
    {
        const string dllName = "UnityLiveShader";

        [DllImport(dllName)]
        public static extern IntPtr GetDrawCallback();

        [DllImport(dllName)]
        public static extern int SetShaderCode(string code);

        [DllImport(dllName)]
        public static extern void SetTime(float time);
    }
}
