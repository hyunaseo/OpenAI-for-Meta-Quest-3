// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace PassthroughCameraSamples.CameraViewer
{
    [MetaCodeSample("PassthroughCameraApiSamples-CameraViewer")]
    public class CameraViewerManager : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private Text m_debugText;
        [SerializeField] private RawImage m_image;

        private IEnumerator Start()
        {
            var supportedResolutions = PassthroughCameraAccess.GetSupportedResolutions(PassthroughCameraAccess.CameraPositionType.Left);
            Assert.IsNotNull(supportedResolutions, nameof(supportedResolutions));
            Debug.Log($"PassthroughCameraAccess.GetSupportedResolutions(): {string.Join(", ", supportedResolutions)}");

            while (!m_cameraAccess.IsPlaying)
            {
                yield return null;
            }
            // Set texture to the RawImage Ui element
            m_image.texture = m_cameraAccess.GetTexture();
        }

        private void Update() => m_debugText.text = OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.PassthroughCameraAccess) ? "Permission granted." : "No permission granted.";
    }
}
