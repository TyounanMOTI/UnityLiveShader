using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

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

        public static void SetMesh(Mesh mesh)
        {
            NativePlugin.SetVertexBuffer(mesh.GetNativeVertexBufferPtr(0), mesh.vertexCount);
            NativePlugin.SetIndexBuffer(mesh.GetNativeIndexBufferPtr(), mesh.GetIndexCount(0), (mesh.indexFormat == IndexFormat.UInt16) ? 0 : 1);
        }

        public static void SetResolution(float width, float height)
        {
            NativePlugin.SetResolution(width, height);
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
        public static extern void SetVertexBuffer(IntPtr buffer, int vertexCount);

        [DllImport(dllName)]
        public static extern void SetIndexBuffer(IntPtr buffer, uint indexCount, int indexFormat);

        [DllImport(dllName)]
        public static extern void SetTime(float time);

        [DllImport(dllName)]
        public static extern void SetResolution(float width, float height);
    }
}
