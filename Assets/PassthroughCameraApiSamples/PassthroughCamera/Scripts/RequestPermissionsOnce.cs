// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.SceneManagement;

namespace PassthroughCameraSamples
{
    internal static class RequestPermissionsOnce
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            bool permissionsRequestedOnce = false;
            SceneManager.sceneLoaded += (scene, _) =>
            {
                if (scene.name != "StartScene")
                {
                    if (!permissionsRequestedOnce)
                    {
                        permissionsRequestedOnce = true;
                        OVRPermissionsRequester.Request(new[]
                        {
                            OVRPermissionsRequester.Permission.Scene,
                            OVRPermissionsRequester.Permission.PassthroughCameraAccess
                        });
                    }
                }
            };
        }
    }
}
