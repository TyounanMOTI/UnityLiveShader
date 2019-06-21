using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace UnityLiveShader
{
    public class DrawSimpleTriangle : MonoBehaviour
    {
        IntPtr drawCallback;
        readonly float[] matrixArray = new float[16];
        Camera mainCamera;

        void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;

            drawCallback = Library.GetDrawCallback();

            var command = new CommandBuffer();
            command.IssuePluginEvent(drawCallback, 0);
            mainCamera.AddCommandBuffer(CameraEvent.AfterSkybox, command);
        }

        public void SetShaderCode(string code)
        {
            Library.SetShaderCode(code);
        }

        void Update()
        {
            var modelMatrix = transform.localToWorldMatrix;
            var viewMatrix = mainCamera.worldToCameraMatrix;
            var projectionMatrix = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, true);
            var mvpMatrix = projectionMatrix * (viewMatrix * modelMatrix);
            Library.SetModelViewProjectionMatrix(MatrixToArray(mvpMatrix.transpose));
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
