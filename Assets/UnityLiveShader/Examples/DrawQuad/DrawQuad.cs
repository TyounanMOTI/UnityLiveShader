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
        public Mesh mesh;

        IntPtr drawCallback;
        readonly float[] matrixArray = new float[16];
        Camera mainCamera;
        CommandBuffer leftEyeCommand;
        CommandBuffer rightEyeCommand;
        IntPtr leftMvpMatrix;
        IntPtr rightMvpMatrix;

        void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;

            drawCallback = Library.GetDrawCallback();
            leftEyeCommand = new CommandBuffer();
            rightEyeCommand = new CommandBuffer();

            leftMvpMatrix = Marshal.AllocHGlobal(sizeof(float) * 16);
            rightMvpMatrix = Marshal.AllocHGlobal(sizeof(float) * 16);

            leftEyeCommand.IssuePluginEventAndData(drawCallback, 0, leftMvpMatrix);
            rightEyeCommand.IssuePluginEventAndData(drawCallback, 0, rightMvpMatrix);

            Library.SetMesh(mesh);
        }

        void OnApplicationQuit()
        {
            Marshal.FreeHGlobal(leftMvpMatrix);
            Marshal.FreeHGlobal(rightMvpMatrix);
        }

        public void SetShaderCode(string code)
        {
            Library.SetShaderCode(code);
        }

        void Update()
        {
            Library.SetTime(Time.time);
        }

        void OnRenderObject()
        {
            var eye = (mainCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left) ? Camera.StereoscopicEye.Left : Camera.StereoscopicEye.Right;
            var modelMatrix = transform.localToWorldMatrix;
            var viewMatrix = mainCamera.GetStereoViewMatrix(eye);
            var projectionMatrix = GL.GetGPUProjectionMatrix(mainCamera.GetStereoProjectionMatrix(eye), true);
            var mvpMatrix = projectionMatrix * (viewMatrix * modelMatrix);
            var mvpMatrixArray = MatrixToArray(mvpMatrix.transpose);
            if (eye == Camera.StereoscopicEye.Left)
            {
                Marshal.Copy(mvpMatrixArray, 0, leftMvpMatrix, 16);
                Graphics.ExecuteCommandBuffer(leftEyeCommand);
            }
            else
            {
                Marshal.Copy(mvpMatrixArray, 0, rightMvpMatrix, 16);
                Graphics.ExecuteCommandBuffer(rightEyeCommand);
            }
        }

        float[] MatrixToArray(Matrix4x4 matrix)
        {
            for (var i = 0; i < 16; i++)
            {
                matrixArray[i] = matrix[i];
            }

            return matrixArray;
        }
    }
}
