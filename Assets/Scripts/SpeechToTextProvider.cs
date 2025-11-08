using UnityEngine;
using System;

using Oculus.Voice;
using Meta.WitAi.TTS.Utilities;
using Meta.WitAi.TTS.Data;

public class SpeechToTextProvider : MonoBehaviour, ITextProvider
{
    [Header("Wit Configuration")]
    [SerializeField] private AppVoiceExperience appVoiceExperience;

    /// <summary>Latest partial transcript (non-empty only while speaking).</summary>
    public string LatestPartial { get; private set; } = string.Empty;

    /// <summary>Latest final transcript.</summary>
    public string LatestFinal { get; private set; } = string.Empty;


    /// <summary>Raised when a new FINAL transcript is produced.</summary>
    public event Action<string> OnFinalTranscription;

    /// <summary>Raised whenever a new PARTIAL transcript arrives.</summary>
    public event Action<string> OnPartialTranscription;

    /// <summary>True if the underlying AppVoiceExperience is currently listening.</summary>
    public bool IsListening => appVoiceExperience != null && appVoiceExperience.Active;

    private bool _subscribed;

    private void Reset()
    {
        if (!appVoiceExperience) appVoiceExperience = FindAnyObjectByType<AppVoiceExperience>();
    }

    private void Awake()
    {
        if (appVoiceExperience == null)
        {
            Debug.LogError("[SpeechToTextProvider] AppVoiceExperience is not assigned!");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        EnsureSubscriptions(true);
    }

    private void OnDisable()
    {
        EnsureSubscriptions(false);
    }

    /// <summary>
    /// Returns the latest FINAL transcript. Does NOT clear internal state.
    /// </summary>
    public string GetText()
    {
        return LatestFinal ?? string.Empty;
    }

    /// <summary>Optionally clear the stored FINAL transcript.</summary>
    public void ClearFinal() => LatestFinal = string.Empty;


    /// <summary>Begin microphone capture for STT (call from other components when ready).</summary>
    public void StartListening()
    {
        if (appVoiceExperience == null) return;
        if (!appVoiceExperience.Active) appVoiceExperience.Activate();
    }

    /// <summary>Stop microphone capture for STT.</summary>
    public void StopListening()
    {
        if (appVoiceExperience == null) return;
        if (appVoiceExperience.Active) appVoiceExperience.Deactivate();
    }

    private void EnsureSubscriptions(bool enable)
    {
        if (appVoiceExperience == null) return;

        var voiceEvents = appVoiceExperience.VoiceEvents;

        if (enable && !_subscribed)
        {
            voiceEvents.OnPartialTranscription.AddListener(HandlePartial);
            voiceEvents.OnFullTranscription.AddListener(HandleFull);
            voiceEvents.OnRequestCompleted.AddListener(HandleRequestCompleted);

            voiceEvents.OnError.AddListener(HandleError);
            voiceEvents.OnAborted.AddListener(HandleAborted);
            voiceEvents.OnStoppedListening.AddListener(HandleStoppedListening);
            voiceEvents.OnStoppedListeningDueToInactivity.AddListener(HandleStoppedListening);
            voiceEvents.OnStoppedListeningDueToDeactivation.AddListener(HandleStoppedListening);

            _subscribed = true;
        }
        else if (!enable && _subscribed)
        {
            voiceEvents.OnPartialTranscription.RemoveListener(HandlePartial);
            voiceEvents.OnFullTranscription.RemoveListener(HandleFull);
            voiceEvents.OnRequestCompleted.RemoveListener(HandleRequestCompleted);

            voiceEvents.OnError.RemoveListener(HandleError);
            voiceEvents.OnAborted.RemoveListener(HandleAborted);
            voiceEvents.OnStoppedListening.RemoveListener(HandleStoppedListening);
            voiceEvents.OnStoppedListeningDueToInactivity.RemoveListener(HandleStoppedListening);
            voiceEvents.OnStoppedListeningDueToDeactivation.RemoveListener(HandleStoppedListening);

            _subscribed = false;
        }
    }

    private void HandlePartial(string text)
    {
        LatestPartial = text ?? string.Empty;
        OnPartialTranscription?.Invoke(LatestPartial);
    }

    private void HandleFull(string text)
    {
        LatestPartial = string.Empty;
        LatestFinal = text ?? string.Empty;
        OnFinalTranscription?.Invoke(LatestFinal);
    }

    private void HandleRequestCompleted()
    {
        LatestPartial = string.Empty;
    }

    private void HandleStoppedListening()
    {
        LatestPartial = string.Empty;
    }

    private void HandleAborted()
    {
        LatestPartial = string.Empty;
    }
    
    private void HandleError(string error, string message)
    {
        Debug.LogWarning($"[SpeechToTextProvider] Error: {error} - {message}");
        LatestPartial = string.Empty;
    }
}