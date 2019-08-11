using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace UnityLiveShader
{
    public class DrawQuad : MonoBehaviour
    {
        [StructLayout(LayoutKind.Sequential)]
        class Constants
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public float[] mvpMatrix;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] cameraPosition;

            public Constants()
            {
                mvpMatrix = new float[16];
                cameraPosition = new float[3];
            }
        }

        public Mesh mesh;

        IntPtr drawCallback;
        Camera mainCamera;
        CommandBuffer leftEyeCommand;
        CommandBuffer rightEyeCommand;
        IntPtr leftConstantsPtr;
        IntPtr rightConstantsPtr;
        Constants leftConstants = new Constants();
        Constants rightConstants = new Constants();

        void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;

            drawCallback = Library.GetDrawCallback();
            leftEyeCommand = new CommandBuffer();
            rightEyeCommand = new CommandBuffer();

            leftConstantsPtr = Marshal.AllocHGlobal(sizeof(float) * 16);
            rightConstantsPtr = Marshal.AllocHGlobal(sizeof(float) * 16);

            leftEyeCommand.IssuePluginEventAndData(drawCallback, 0, leftConstantsPtr);
            rightEyeCommand.IssuePluginEventAndData(drawCallback, 0, rightConstantsPtr);

            Library.SetMesh(mesh);
        }

        void OnApplicationQuit()
        {
            Marshal.FreeHGlobal(leftConstantsPtr);
            Marshal.FreeHGlobal(rightConstantsPtr);
        }

        public void SetShaderCode(string code)
        {
            Library.SetShaderCode(code);
        }

        void Update()
        {
            Library.SetTime(Time.time);
            Library.SetResolution(mainCamera.scaledPixelWidth, mainCamera.scaledPixelHeight);
        }

        void OnRenderObject()
        {
            var eye = (mainCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left) ? Camera.StereoscopicEye.Left : Camera.StereoscopicEye.Right;
            var modelMatrix = transform.localToWorldMatrix;
            var viewMatrix = mainCamera.GetStereoViewMatrix(eye);
            var projectionMatrix = GL.GetGPUProjectionMatrix(mainCamera.GetStereoProjectionMatrix(eye), true);
            var mvpMatrix = projectionMatrix * (viewMatrix * modelMatrix);
            var cameraTranslation = viewMatrix.GetRow(3);
            if (eye == Camera.StereoscopicEye.Left)
            {
                for (var i = 0; i < 3; i++)
                {
                    leftConstants.cameraPosition[i] = cameraTranslation[i];
                }
                CopyMatrixToArray(mvpMatrix.transpose, leftConstants.mvpMatrix);
                Marshal.StructureToPtr(leftConstants, leftConstantsPtr, false);
                Graphics.ExecuteCommandBuffer(leftEyeCommand);
            }
            else
            {
                for (var i = 0; i < 3; i++)
                {
                    rightConstants.cameraPosition[i] = cameraTranslation[i];
                }
                CopyMatrixToArray(mvpMatrix.transpose, rightConstants.mvpMatrix);
                Marshal.StructureToPtr(rightConstants, rightConstantsPtr, false);
                Graphics.ExecuteCommandBuffer(rightEyeCommand);
            }
        }

        void CopyMatrixToArray(Matrix4x4 matrix, float[] array)
        {
            for (var i = 0; i < 16; i++)
            {
                array[i] = matrix[i];
            }
        }
    }
}
