using UnityEngine;

/// <summary>
/// 地块分类标记：重建地图时挂到每个地块上，
/// 便于在场景里查看/统计每块地属于哪一类。
/// </summary>
public sealed class MapTileVisual : MonoBehaviour
{
    public MapTileCategory category;
    public Vector2Int gridCoord;
}
