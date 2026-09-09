using UnityEngine;

/// <summary>
/// 程序化特效贴图工厂：运行时生成柔光圆点与空心圆环，
/// 用 SpriteRenderer 的 color 着色，因此无需外部美术资源。
/// </summary>
public static class VFXFactory
{
    private const int TextureSize = 64;

    private static Sprite glowSprite;
    private static Sprite ringSprite;
    private static Sprite rangeSprite;

    public static Sprite GlowSprite
    {
        get
        {
            if (glowSprite == null)
                glowSprite = CreateSprite("VFX_Glow", BuildGlowTexture);
            return glowSprite;
        }
    }

    public static Sprite RingSprite
    {
        get
        {
            if (ringSprite == null)
                ringSprite = CreateSprite("VFX_Ring", BuildRingTexture);
            return ringSprite;
        }
    }

    public static Sprite RangeSprite
    {
        get
        {
            if (rangeSprite == null)
                rangeSprite = CreateSprite("VFX_Range", BuildRangeTexture);
            return rangeSprite;
        }
    }

    private static Sprite CreateSprite(string textureName, System.Func<Texture2D> build)
    {
        Texture2D texture = build();
        texture.name = textureName;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            texture.width);
    }

    private static Texture2D BuildGlowTexture()
    {
        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[TextureSize * TextureSize];

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float normalizedX = ((x + 0.5f) / TextureSize - 0.5f) * 2f;
                float normalizedY = ((y + 0.5f) / TextureSize - 0.5f) * 2f;
                float radius = Mathf.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);

                // 中心最亮、向外平滑衰减的柔光
                float alpha = Mathf.Clamp01(1f - radius);
                alpha = alpha * alpha;
                pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D BuildRingTexture()
    {
        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[TextureSize * TextureSize];

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float normalizedX = ((x + 0.5f) / TextureSize - 0.5f) * 2f;
                float normalizedY = ((y + 0.5f) / TextureSize - 0.5f) * 2f;
                float radius = Mathf.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);

                // 约 0.35 半径处的细圆环，带一点过渡抗锯齿
                float distanceFromRing = Mathf.Abs(radius - 0.36f);
                float alpha = Mathf.Clamp01(1f - distanceFromRing / 0.07f);
                pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D BuildRangeTexture()
    {
        // 半透明填充圆盘 + 外缘亮线，供射程提示使用。
        // 提示对象 localScale = 2 * range，圆的半径约等于 range。
        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[TextureSize * TextureSize];

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float normalizedX = ((x + 0.5f) / TextureSize - 0.5f) * 2f;
                float normalizedY = ((y + 0.5f) / TextureSize - 0.5f) * 2f;
                float radius = Mathf.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);

                float fillAlpha = 0.22f * Mathf.Clamp01(1f - radius * radius);
                float ringAlpha = Mathf.Clamp01(1f - Mathf.Abs(radius - 0.96f) / 0.03f) * 0.9f;

                pixels[y * TextureSize + x] =
                    new Color(1f, 1f, 1f, Mathf.Max(fillAlpha, ringAlpha));
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
