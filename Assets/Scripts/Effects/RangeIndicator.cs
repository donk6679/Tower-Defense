using UnityEngine;

/// <summary>
/// 射程提示圆圈：根据炮塔射程设置 localScale（世界半径 = range），
/// 颜色可调。由 BuildManager 动态创建并挂在炮塔/建造地块下。
/// </summary>
public sealed class RangeIndicator : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Show(float range, Color color)
    {
        gameObject.SetActive(true);

        // 父级（炮塔）可能被整体放大，这里抵消父级缩放，
        // 保证射程圈的世界半径始终等于 range。
        float parentScale = 1f;
        if (transform.parent != null)
            parentScale = Mathf.Max(0.0001f, Mathf.Abs(transform.parent.lossyScale.x));

        transform.localScale = Vector3.one * Mathf.Max(0.1f, range * 2f / parentScale);

        if (spriteRenderer != null)
            spriteRenderer.color = color;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
