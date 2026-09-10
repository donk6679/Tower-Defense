/// <summary>
/// 地图地块相对道路的位置分类。
/// “左/右/上/下”描述地块相对道路的位置，
/// 例如 LeftOfRoad 表示道路在地块右侧。
/// 拐角同时包含内侧拐角与外侧拐角。
/// </summary>
public enum MapTileCategory
{
    None = 0,                        // 不挨着道路
    LeftOfRoad,                      // 在道路左边（道路在地块右侧）
    RightOfRoad,                     // 在道路右边（道路在地块左侧）
    AboveRoad,                       // 在道路上边（道路在地块下方）
    BelowRoad,                       // 在道路下边（道路在地块上方）
    InnerCornerTopLeft,              // 内侧拐角：地块位于拐角左上角
    InnerCornerTopRight,             // 内侧拐角：地块位于拐角右上角
    InnerCornerBottomLeft,           // 内侧拐角：地块位于拐角左下角
    InnerCornerBottomRight,          // 内侧拐角：地块位于拐角右下角
    OuterCornerTopLeft,              // 外侧拐角：地块位于拐角左上角
    OuterCornerTopRight,             // 外侧拐角：地块位于拐角右上角
    OuterCornerBottomLeft,           // 外侧拐角：地块位于拐角左下角
    OuterCornerBottomRight,          // 外侧拐角：地块位于拐角右下角
    Road,                            // 道路本身
}
