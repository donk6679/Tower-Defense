using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 单个可建造地块：点击后由 BuildManager 尝试在此建造。
/// 如果点击发生在 UI 上（如塔选择按钮），不会误触发建造。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class BuildSlot : MonoBehaviour
{
    [Header("Map Info")]
    public bool isBuildable = true;
    public Vector2Int gridCoord;

    [Header("State")]
    [SerializeField] private bool occupied;

    private SpriteRenderer spriteRenderer;
    private Color normalColor = Color.white;

    public bool IsOccupied => occupied;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            normalColor = spriteRenderer.color;
    }

    public void SetOccupied(bool value)
    {
        occupied = value;

        if (spriteRenderer == null)
            return;

        spriteRenderer.color = value
            ? new Color(0.55f, 0.55f, 0.55f, 0.75f)
            : normalColor;
    }

    private void OnMouseDown()
    {
        if (!isBuildable)
            return;

        // 点击 UI 时不要往下面的地图建造
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (BuildManager.Instance != null)
            BuildManager.Instance.TryBuild(this);
    }

    private void OnDrawGizmos()
    {
        if (!isBuildable)
            return;

        Gizmos.color = occupied
            ? new Color(1f, 0.35f, 0.2f, 0.75f)
            : new Color(0.45f, 1f, 0.55f, 0.5f);

        Vector3 center = transform.position + Vector3.back * 0.05f;
        Gizmos.DrawWireCube(center, Vector3.one * 0.88f);
    }
}
