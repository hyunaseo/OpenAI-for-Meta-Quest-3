// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;

namespace PassthroughCameraSamples
{
    [MetaCodeSample("PassthroughCameraApiSamples-PassthroughCamera")]
    public static class PassthroughCameraUtils
    {
        /// <summary>Returns a world-space ray going from camera through a viewport point.</summary>
        /// <param name="viewportPoint">Viewport-space is normalized and relative to the camera. The bottom-left of the camera is (0,0); the top-right is (1,1).</param>
        /// <param name="cameraPose">Optional camera pose that should be used for calculation. For example, you can cache <see cref="PassthroughCameraAccess.GetCameraPose"/>, do a long-running image processing, then use the cached camera pose with this method.</param>
        /// <returns>World-space ray.</returns>
        public static Ray ViewportPointToRay(this PassthroughCameraAccess cameraAccess, Vector2 viewportPoint, Pose? cameraPose = null)
        {
            if (!cameraAccess.ValidateIsPlaying())
            {
                return default;
            }
            var camPose = cameraPose ?? cameraAccess.GetCameraPose();
            var direction = camPose.rotation * cameraAccess.ViewportPointToLocalRay(viewportPoint).direction;
            return new Ray(camPose.position, direction);
        }

        private static Ray ViewportPointToLocalRay(this PassthroughCameraAccess cameraAccess, Vector2 viewportPoint)
        {
            var intrinsics = cameraAccess.Intrinsics;
            var sensorResolution = intrinsics.SensorResolution;
            var principalPoint = intrinsics.PrincipalPoint;
            var focalLength = intrinsics.FocalLength;
            var directionInCamera = new Vector3
            {
                x = (viewportPoint.x * sensorResolution.x - principalPoint.x) / focalLength.x,
                y = (viewportPoint.y * sensorResolution.y - principalPoint.y) / focalLength.y,
                z = 1
            };

            return new Ray(Vector3.zero, directionInCamera);
        }

        private static bool ValidateIsPlaying(this PassthroughCameraAccess cameraAccess)
        {
            if (cameraAccess.IsPlaying)
            {
                return true;
            }
            Debug.LogError(nameof(PassthroughCameraAccess) + " is not playing. Please check the '" + nameof(PassthroughCameraAccess.IsPlaying) + "' before calling this API.", cameraAccess);
            return false;
        }
    }
}
