using UnityEngine;
using Unity.Collections;
using Meta.XR;


public class PassThroughProvider : MonoBehaviour
{
    [Header ("Passthrough Camera")]
    public PassthroughCameraAccess cameraAccess;

    private Texture2D image;

    public bool TryCapturePassThrough(out byte[] jpgBytes, out int w, out int h)
    {
        jpgBytes = null;
        w = 0;
        h = 0;

        if (cameraAccess == null || cameraAccess.GetTexture() == null || !cameraAccess.IsPlaying)
        {
            Debug.LogWarning("[ImageProvider] PassthroughCameraAccess is not assigned or not playing.");
            return false;
        }

        int srcW = cameraAccess.CurrentResolution.x;
        int srcH = cameraAccess.CurrentResolution.y;

        if (srcW <= 16 || srcH <= 16) return false;

        int width = Mathf.Max(64, srcW / 2);
        int height = Mathf.Max(64, srcH / 2);

        if (image == null || image.width != width || image.height != height)
        {
            image = new Texture2D(width, height, TextureFormat.RGB24, false);
        }

        var rt = new RenderTexture(width, height, 0);
        Graphics.Blit(cameraAccess.GetTexture(), rt);

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        RenderTexture.active = prev;
        rt.Release();

#if UNITY_2020_1_OR_NEWER
        jpgBytes = image.EncodeToJPG(75);
#else
        jpgBytes = image.EncodeToJPG();
#endif
        w = width; h = height;
        return jpgBytes != null && jpgBytes.Length > 0;
    }
}
