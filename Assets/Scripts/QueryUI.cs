using System.Collections;
using TMPro;
using UnityEngine;

public class QueryUI : MonoBehaviour
{
    [SerializeField] private QueryManager queryManager;
    [SerializeField] private TMP_Text userText;
    [SerializeField] private TMP_Text agentText;

    private string processingLabel = "Processing";
    private float dotInterval = 0.35f;

    private Coroutine _processingCoroutine;
    private bool _isProcessing;

    private void Reset()
    {
        if (!queryManager) queryManager = FindAnyObjectByType<QueryManager>();
    }

    private void Awake()
    {
        if (!queryManager) Debug.LogWarning("[QueryUI] QueryManager not assigned.");
        if (!userText) Debug.LogWarning("[QueryUI] User Text not assigned.");
        if (!agentText) Debug.LogWarning("[QueryUI] Agent Text not assigned.");
    }

    private void OnEnable()
    {
        queryManager.OnListeningPartial += HandleListeningPartial;
        queryManager.OnListeningFinal += HandleListeningFinal;

        queryManager.OnProcessingStart += HandleProcessingStart;
        queryManager.OnReply += HandleReply;

        queryManager.OnTtsStart += HandleTtsStart;
        queryManager.OnTtsComplete += HandleTtsComplete;
    }

    private void OnDisable()
    {
        queryManager.OnListeningPartial -= HandleListeningPartial;
        queryManager.OnListeningFinal -= HandleListeningFinal;

        queryManager.OnProcessingStart -= HandleProcessingStart;
        queryManager.OnReply -= HandleReply;

        queryManager.OnTtsStart -= HandleTtsStart;
        queryManager.OnTtsComplete -= HandleTtsComplete;

        StopProcessing();
    }

    private void HandleListeningPartial(string text)
    {
        userText.text = text ?? string.Empty;
        if (!_isProcessing && agentText) agentText.text = string.Empty;
        if (_isProcessing) StopProcessing();
    }

    private void HandleListeningFinal(string text)
    {
        userText.text = text ?? string.Empty;
    }

    private void HandleProcessingStart()
    {
        StartProcessing();
    }

    private void HandleReply(string text)
    {
        StopProcessing();
        agentText.text = text ?? string.Empty;
    }

    private void HandleTtsStart(string text)
    {
        // Optionally handle TTS start (e.g., change UI state)
    }

    private void HandleTtsComplete()
    {
        // Optionally handle TTS completion (e.g., reset UI state)
    }

    private void StartProcessing()
    {
        if (_isProcessing) return;
        _isProcessing = true;

        if (_processingCoroutine != null)
        {
            StopCoroutine(_processingCoroutine);
        }
        _processingCoroutine = StartCoroutine(ProcessingDots());
    }

    private void StopProcessing()
    {
        _isProcessing = false;
        if (_processingCoroutine != null)
        {
            StopCoroutine(_processingCoroutine);
            _processingCoroutine = null;
        }
    }
    
    private IEnumerator ProcessingDots()
    {
        int dotCount = 0;
        while (_isProcessing)
        {
            dotCount = (dotCount % 3) + 1;
            agentText.text = processingLabel + new string('.', dotCount);
            yield return new WaitForSeconds(dotInterval);
        }
    }
}