using System;
using UnityEngine;

[DisallowMultipleComponent]
public class StreamingDataSelector : MonoBehaviour
{
    [SerializeField] private bool sendText = true;
    [SerializeField] private bool sendImage = false;

    public bool SendText => sendText;
    public bool SendImage => sendImage;
}
