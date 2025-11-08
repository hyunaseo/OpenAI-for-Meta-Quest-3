using UnityEngine;

public interface IImageProvider
{
    /// <summary>
    /// Retrieves the current image in JPEG format.
    /// </summary>
    /// <param name="maxSize">Maximum dimension (in pixels) of the longer side.</param>
    /// <param name="quality">JPEG quality (0–100).</param>
    /// <param name="jpg">Output JPEG byte array.</param>
    /// <param name="width">Output image width.</param>
    /// <param name="height">Output image height.</param>
    /// <returns>True if the image was successfully retrieved; otherwise, false.</returns>
    bool GetImage(int maxSize, int quality, out byte[] jpg, out int width, out int height);
}
