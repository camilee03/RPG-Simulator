using System;
using UnityEngine;

public class Converter : ScriptableObject
{
    // Helper: convert Sprite to PNG byte[] (extracts correct sub-rect for sprites in atlases)
    public static byte[] SpriteToPng(Sprite sprite, out int width, out int height)
    {
        width = Mathf.RoundToInt(sprite.rect.width);
        height = Mathf.RoundToInt(sprite.rect.height);

        try
        {
            Texture2D tex = new(width, height, TextureFormat.RGBA32, false);
            Rect texRect = sprite.textureRect;
            // GetPixels requires ints
            int x = Mathf.RoundToInt(texRect.x);
            int y = Mathf.RoundToInt(texRect.y);
            int mipLevel = 0;
            Color32[] pixels = sprite.texture.GetPixels32(mipLevel);
            tex.SetPixels32(pixels, mipLevel);
            tex.Apply();
            return tex.EncodeToPNG();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Whiteboard] SpriteToPng failed: {e}");
            width = height = 0;
            return null;
        }
    }

    // Helper: rebuild Sprite from PNG bytes
    public static Sprite PngBytesToSprite(byte[] data, int width, int height)
    {
        try
        {
            Texture2D tex = new(width, height, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(data))
            {
                Debug.LogWarning("[Whiteboard] Texture.LoadImage failed for background.");
                return null;
            }
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        catch (Exception e)
        {
            Debug.LogError($"[Whiteboard] PngBytesToSprite failed: {e}");
            return null;
        }
    }
}
