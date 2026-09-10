using UnityEngine;

/// <summary>
/// 13 类地块 + 道路的贴图配置。
/// 在 Unity 里选中该资产，可以手动替换任意一张素材；
/// 也可以使用菜单里的 Kenney 自动映射。
/// </summary>
public sealed class MapTileSpriteConfig : ScriptableObject
{
    [Header("Ground Tiles")]
    public Sprite tileNone;
    public Sprite tileLeftOfRoad;
    public Sprite tileRightOfRoad;
    public Sprite tileAboveRoad;
    public Sprite tileBelowRoad;

    [Header("Inner Corners")]
    public Sprite tileInnerCornerTopLeft;
    public Sprite tileInnerCornerTopRight;
    public Sprite tileInnerCornerBottomLeft;
    public Sprite tileInnerCornerBottomRight;

    [Header("Outer Corners")]
    public Sprite tileOuterCornerTopLeft;
    public Sprite tileOuterCornerTopRight;
    public Sprite tileOuterCornerBottomLeft;
    public Sprite tileOuterCornerBottomRight;

    [Header("Road")]
    public Sprite roadSprite;

    [Header("Buildable Overlay")]
    [Tooltip("是否给可建造地块叠加一层半透明高亮，避免换素材后看不出哪里能建塔。")]
    public bool showBuildableOverlay = true;
    public Color buildableOverlayColor = new Color(0.3f, 1f, 0.45f, 0.22f);
}
