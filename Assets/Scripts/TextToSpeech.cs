using System;
using UnityEngine;
using Meta.WitAi.TTS.Utilities; // TTSSpeaker

[DisallowMultipleComponent]
public class TextToSpeech : MonoBehaviour
{
    [SerializeField] private TTSSpeaker speaker;
    public bool IsSpeaking => speaker != null && speaker.IsSpeaking;
    public event Action OnSpeechStart;
    public event Action OnSpeechComplete;

    private void Awake()
    {
        if (!speaker) speaker = GetComponent<TTSSpeaker>();

        speaker.Events.OnTextPlaybackStart.AddListener(HandleTextPlaybackStart);
        speaker.Events.OnTextPlaybackFinished.AddListener(HandleTextPlaybackFinished);
    }

    public void Speak(string text)
    {
        if (speaker) speaker.Speak(text);
    }

    public void SpeakNow(string text)
    {
        if (speaker)
        {
            speaker.Stop();
            speaker.Speak(text);
        }
    }

    public void SpeakQueued(string text)
    {
        if (speaker) speaker.SpeakQueued(text);
    }

    public void StopSpeaking()
    {
        if (speaker && (speaker.IsSpeaking || speaker.IsLoading))
            speaker.Stop();
    }

    private void HandleTextPlaybackStart(string text)
    {
        OnSpeechStart?.Invoke();
    }

    private void HandleTextPlaybackFinished(string text)
    {
        OnSpeechComplete?.Invoke();
    }
}
