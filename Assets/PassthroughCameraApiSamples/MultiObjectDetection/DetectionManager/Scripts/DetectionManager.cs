// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class DetectionManager : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;

        [Header("Controls configuration")]
        [SerializeField] private OVRInput.RawButton m_actionButton = OVRInput.RawButton.A;

        [Header("Ui references")]
        [SerializeField] private DetectionUiMenuManager m_uiMenuManager;

        [Header("Placement configureation")]
        [SerializeField] private GameObject m_spwanMarker;
        [SerializeField] private EnvironmentRayCastSampleManager m_environmentRaycast;
        [SerializeField] private float m_spawnDistance = 0.25f;
        [SerializeField] private AudioSource m_placeSound;

        [Header("Sentis inference ref")]
        [SerializeField] private SentisInferenceRunManager m_runInference;
        [SerializeField] private SentisInferenceUiManager m_uiInference;
        [Space(10)]
        public UnityEvent<int> OnObjectsIdentified;

        private bool m_isPaused = true;
        private List<GameObject> m_spwanedEntities = new();
        private bool m_isStarted = false;
        private bool m_isSentisReady = false;
        private float m_delayPauseBackTime = 0;

        #region Unity Functions
        private void Awake() => OVRManager.display.RecenteredPose += CleanMarkersCallBack;

        private void OnDestroy() => OVRManager.display.RecenteredPose -= CleanMarkersCallBack;

        private IEnumerator Start()
        {
            // Wait until Sentis model is loaded
            var sentisInference = FindAnyObjectByType<SentisInferenceRunManager>();
            while (!sentisInference.IsModelLoaded)
            {
                yield return null;
            }
            m_isSentisReady = true;
        }

        private void Update()
        {
            if (!m_isStarted)
            {
                // Manage the Initial Ui Menu
                if (m_cameraAccess.IsPlaying && m_isSentisReady)
                {
                    m_isStarted = true;
                }
            }
            else
            {
                // Press A button to spawn 3d markers
                if (OVRInput.GetUp(m_actionButton) && m_delayPauseBackTime <= 0)
                {
                    SpwanCurrentDetectedObjects();
                }
                // Cooldown for the A button after return from the pause menu
                m_delayPauseBackTime -= Time.deltaTime;
                if (m_delayPauseBackTime <= 0)
                {
                    m_delayPauseBackTime = 0;
                }
            }

            // Don't start Sentis inference if the app is paused or we don't have a camera image yet
            if (m_isPaused || !m_cameraAccess.IsPlaying)
            {
                if (m_isPaused)
                {
                    // Set the delay time for the A button to return from the pause menu
                    m_delayPauseBackTime = 0.1f;
                }
                return;
            }

            // Run a new inference when the current inference finishes
            if (!m_runInference.IsRunning())
            {
                m_runInference.RunInference(m_cameraAccess);
            }
        }
        #endregion

        #region Marker Functions
        /// <summary>
        /// Clean 3d markers when the tracking space is re-centered.
        /// </summary>
        private void CleanMarkersCallBack()
        {
            foreach (var e in m_spwanedEntities)
            {
                Destroy(e, 0.1f);
            }
            m_spwanedEntities.Clear();
            OnObjectsIdentified?.Invoke(-1);
        }
        /// <summary>
        /// Spwan 3d markers for the detected objects
        /// </summary>
        private void SpwanCurrentDetectedObjects()
        {
            var count = 0;
            foreach (var box in m_uiInference.BoxDrawn)
            {
                if (PlaceMarkerUsingEnvironmentRaycast(box.WorldPos, box.ClassName))
                {
                    count++;
                }
            }
            if (count > 0)
            {
                // Play sound if a new marker is placed.
                m_placeSound.Play();
            }
            OnObjectsIdentified?.Invoke(count);
        }

        /// <summary>
        /// Place a marker using the environment raycast
        /// </summary>
        private bool PlaceMarkerUsingEnvironmentRaycast(Vector3? position, string className)
        {
            // Check if the position is valid
            if (!position.HasValue)
            {
                return false;
            }

            // Check if you spanwed the same object before
            var existMarker = false;
            foreach (var e in m_spwanedEntities)
            {
                var markerClass = e.GetComponent<DetectionSpawnMarkerAnim>();
                if (markerClass)
                {
                    var dist = Vector3.Distance(e.transform.position, position.Value);
                    if (dist < m_spawnDistance && markerClass.GetYoloClassName() == className)
                    {
                        existMarker = true;
                        break;
                    }
                }
            }

            if (!existMarker)
            {
                // spawn a visual marker
                var eMarker = Instantiate(m_spwanMarker);
                m_spwanedEntities.Add(eMarker);

                // Update marker transform with the real world transform
                eMarker.transform.SetPositionAndRotation(position.Value, Quaternion.identity);
                eMarker.GetComponent<DetectionSpawnMarkerAnim>().SetYoloClassName(className);
            }

            return !existMarker;
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// Pause the detection logic when the pause menu is active
        /// </summary>
        public void OnPause(bool pause)
        {
            m_isPaused = pause;
        }
        #endregion
    }
}
