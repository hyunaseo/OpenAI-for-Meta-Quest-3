using UnityEngine;
using System;

public class DebugPassThrough: MonoBehaviour 
{
    public PassThroughProvider passThroughProvider;

    public GameObject debugCube;


    void Update()
    {
        if (passThroughProvider.TryCapturePassThrough(out byte[] jpgBytes, out int w, out int h))
        {
            // set debug cube color red
            debugCube.GetComponent<Renderer>().material.color = Color.red;
            // debug with time stamp
            Debug.Log($"[DebugPassThrough] Captured Passthrough Image at {DateTime.Now.ToString("HH:mm:ss.fff")}");
            Debug.Log($"[DebugPassThrough] Captured Passthrough Image: {jpgBytes.Length} bytes, Width: {w}, Height: {h}");
            Debug.Log(" ");
        }
        else
        {
            // set debug cube color white
            debugCube.GetComponent<Renderer>().material.color = Color.white;
            Debug.LogError("[DebugPassThrough] Failed to capture Passthrough Image");
        }
    }    
}