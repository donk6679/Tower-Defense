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
        transform.localScale = Vector3.one * Mathf.Max(0.1f, range * 2f);

        if (spriteRenderer != null)
            spriteRenderer.color = color;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
