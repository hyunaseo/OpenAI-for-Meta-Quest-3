// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.ShaderSample
{
    [MetaCodeSample("PassthroughCameraApiSamples-ShaderSample")]
    public class ShaderSampleManager : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private Text m_debugText;
        [SerializeField] private MeshRenderer m_renderer;

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
            // Set PassthroughCameraAccess GPU texture as a Main texture of our material
            m_renderer.material.SetTexture("_MainTex", m_cameraAccess.GetTexture());
        }
    }
}
