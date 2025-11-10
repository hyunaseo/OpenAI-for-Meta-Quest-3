// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace PassthroughCameraSamples.BrightnessEstimation
{
    [MetaCodeSample("PassthroughCameraApiSamples-BrightnessEstimation")]
    public class BrightnessEstimationManager : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private float m_refreshTime = 0.05f;
        [SerializeField][Range(1, 100)] private int m_bufferSize = 10;
        [SerializeField] private UnityEvent<float> m_onBrightnessChange;
        [SerializeField] private UnityEngine.UI.Text m_debugger;

        private float m_refreshCurrentTime = 0.0f;
        private List<float> m_brightnessVals = new();

        private void Update()
        {
            if (m_cameraAccess.IsPlaying)
            {
                if (!IsWaiting())
                {
                    m_debugger.text = GetRoomAmbientLight();
                    m_onBrightnessChange?.Invoke(GetGlobalBrigthnessLevel());
                }
            }
            else
            {
                m_debugger.text = OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.PassthroughCameraAccess) ? "Permission granted." : "No permission granted.";
            }
        }

        /// <summary>
        /// Estimate the Brightness Level using a Texture2D
        /// </summary>
        /// <returns>String data for debugging purposes</returns>
        private string GetRoomAmbientLight()
        {
            m_refreshCurrentTime = m_refreshTime;
            var pixels = m_cameraAccess.GetColors();

            float colorSum = 0;
            for (int x = 0, len = pixels.Length; x < len; x++)
            {
                colorSum += 0.2126f * pixels[x].r + 0.7152f * pixels[x].g + 0.0722f * pixels[x].b;
            }
            var size = m_cameraAccess.CurrentResolution;
            var brightnessVals = Mathf.Floor(colorSum / (size.x * size.y));

            m_brightnessVals.Add(brightnessVals);

            if (m_brightnessVals.Count > m_bufferSize)
            {
                m_brightnessVals.RemoveAt(0);
            }

            return $"Current brightness level: {brightnessVals}\nGlobal value: {GetGlobalBrigthnessLevel()}";
        }

        /// <summary>
        /// Return true if the waiting time is bigger than zero.
        /// </summary>
        /// <returns>True or False</returns>
        private bool IsWaiting()
        {
            m_refreshCurrentTime -= Time.deltaTime;
            return m_refreshCurrentTime > 0.0f;
        }

        /// <summary>
        /// Get the average Brightness level based on the buffer size.
        /// </summary>
        /// <returns>Average brightness level (float)</returns>
        private float GetGlobalBrigthnessLevel()
        {
            if (m_brightnessVals.Count == 0)
            {
                return -1;
            }

            var sum = 0.0f;
            foreach (var b in m_brightnessVals)
            {
                sum += b;
            }
            return sum / m_brightnessVals.Count;
        }
    }
}
