// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.CameraToWorld
{
    [MetaCodeSample("PassthroughCameraApiSamples-CameraToWorld")]
    public class CameraToWorldCameraCanvas : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private Text m_debugText;
        [SerializeField] private RawImage m_image;
        private Texture2D m_cameraSnapshot;

        public void MakeCameraSnapshot()
        {
            if (!m_cameraAccess.IsPlaying)
            {
                Debug.LogError("!m_cameraAccess.IsPlaying");
                return;
            }

            if (m_cameraSnapshot == null)
            {
                var size = m_cameraAccess.CurrentResolution;
                m_cameraSnapshot = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
            }

            var pixels = m_cameraAccess.GetColors();
            m_cameraSnapshot.LoadRawTextureData(pixels);
            m_cameraSnapshot.Apply();

            StopCoroutine(ResumeStreamingFromCameraCor());
            m_image.texture = m_cameraSnapshot;
        }

        public void ResumeStreamingFromCamera()
        {
            StartCoroutine(ResumeStreamingFromCameraCor());
        }

        private IEnumerator ResumeStreamingFromCameraCor()
        {
            while (!m_cameraAccess.IsPlaying)
            {
                yield return null;
            }
            m_image.texture = m_cameraAccess.GetTexture();
        }

        private IEnumerator Start()
        {
            m_debugText.text = "No permission granted.";
            while (!OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.PassthroughCameraAccess))
            {
                yield return null;
            }
            m_debugText.text = "Permission granted.";

            while (!m_cameraAccess.IsPlaying)
            {
                yield return null;
            }
            ResumeStreamingFromCamera();
        }
    }
}
