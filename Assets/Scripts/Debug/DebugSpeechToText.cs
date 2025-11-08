using UnityEngine;
using System;

[DisallowMultipleComponent]
public class DebugSpeechToText : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpeechToTextProvider stt;

    [Header("Behavior")]
    [Tooltip("Keep listening continuously.")]
    [SerializeField] private bool loopListening = true;

    [Tooltip("Delay before restarting listening (seconds).")]
    [SerializeField] private float restartDelay = 0.15f;

    private bool _subscribed;

    private void Reset()
    {
        if (!stt) stt = FindAnyObjectByType<SpeechToTextProvider>();
    }

    private void Awake()
    {
        if (!stt)
        {
            Debug.LogError("[DebugSpeechToText] SpeechToTextProvider not assigned!");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        EnsureSubscriptions(true);
        SafeStartListening();
    }

    private void OnDisable()
    {
        EnsureSubscriptions(false);
    }

    private void EnsureSubscriptions(bool enable)
    {
        if (stt == null) return;

        if (enable && !_subscribed)
        {
            stt.OnPartialTranscription += HandlePartial;
            stt.OnFinalTranscription   += HandleFinal;
            _subscribed = true;
        }
        else if (!enable && _subscribed)
        {
            stt.OnPartialTranscription -= HandlePartial;
            stt.OnFinalTranscription   -= HandleFinal;
            _subscribed = false;
        }
    }

    private void HandlePartial(string text)
    {
        Debug.Log($"[STT][Partial][{Time.time:F2}s] {text}");
    }

    private void HandleFinal(string text)
    {
        Debug.Log($"[STT][Final  ][{Time.time:F2}s] {text}");

        if (loopListening && isActiveAndEnabled)
            Invoke(nameof(SafeStartListening), restartDelay);
    }

    private void SafeStartListening()
    {
        if (stt == null) return;
        if (!stt.IsListening)
        {
            Debug.Log("[DebugSpeechToText] StartListening()");
            stt.StartListening();
        }
    }
}
