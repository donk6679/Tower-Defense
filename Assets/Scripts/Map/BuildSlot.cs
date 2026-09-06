using UnityEngine;

/// <summary>
/// 单个可建造地块：记录占用状态，供后续 BuildManager 使用。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class BuildSlot : MonoBehaviour
{
    [Header("Map Info")]
    public bool isBuildable = true;
    public Vector2Int gridCoord;

    [Header("State")]
    [SerializeField] private bool occupied;

    public bool IsOccupied => occupied;

    public void SetOccupied(bool value)
    {
        occupied = value;
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
