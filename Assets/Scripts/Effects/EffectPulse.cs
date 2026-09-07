using UnityEngine;

/// <summary>
/// 通用瞬态特效：从起始尺寸缩放到结束尺寸，同时淡出，可选向外飞散。
/// 用于命中闪光、扩散圆环、拖尾光点等。
/// </summary>
public sealed class EffectPulse : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Vector3 startScale;
    private Vector3 endScale;
    private Vector2 velocity;
    private float duration = 0.2f;
    private float lifeTime;
    private bool launched;

    public static EffectPulse Create(
        Sprite sprite,
        Color color,
        Vector3 position,
        float startScale,
        float endScale,
        float durationSeconds,
        Vector2 velocity = default,
        int sortingOrder = 20)
    {
        GameObject effectObject = new GameObject("EffectPulse");
        effectObject.transform.position = position;
        effectObject.transform.rotation = Quaternion.identity;

        SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        EffectPulse pulse = effectObject.AddComponent<EffectPulse>();
        pulse.Initialize(startScale, endScale, durationSeconds, velocity);
        return pulse;
    }

    private void Initialize(
        float scaleFrom,
        float scaleTo,
        float durationSeconds,
        Vector2 moveSpeed)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startScale = Vector3.one * Mathf.Max(0.01f, scaleFrom);
        endScale = Vector3.one * Mathf.Max(0f, scaleTo);
        duration = Mathf.Max(0.01f, durationSeconds);
        velocity = moveSpeed;
        transform.localScale = startScale;
        launched = true;
    }

    private void Update()
    {
        if (!launched)
            return;

        lifeTime += Time.deltaTime;
        float progress = Mathf.Clamp01(lifeTime / duration);

        transform.localScale = Vector3.Lerp(startScale, endScale, progress);

        if (velocity.sqrMagnitude > 0.0001f)
            transform.position += (Vector3)(velocity * Time.deltaTime);

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(1f, 0f, progress);
            spriteRenderer.color = color;
        }

        if (progress >= 1f)
            Destroy(gameObject);
    }
}
