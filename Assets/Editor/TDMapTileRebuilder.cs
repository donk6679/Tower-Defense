using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// 把现有 Main 场景的地图地块按“与道路的关系”分成 13 类
    /// （4 个方向边 + 4 个内侧拐角 + 4 个外侧拐角 + 不挨道路），
    /// 并用 MapTileSpriteConfig 里的素材重建地块。
    ///
    /// 不会重建场景，也不会动 GameManager / UI / 波次数据。
    /// 用法：Tools > Tower Defense > Map Tiles > Rebuild With Categories
    /// </summary>
    public static class TDMapTileRebuilder
    {
        private const int Cols = 25;
        private const int Rows = 14;
        private const string ConfigPath = "Assets/MapTileSpriteConfig.asset";
        private const string GroundSpritePath = "Assets/Sprites/Map/ground.asset";
        private const string BuildableSpritePath = "Assets/Sprites/Map/buildable.asset";
        private const string PathSpritePath = "Assets/Sprites/Map/path.asset";

        [MenuItem("Tools/Tower Defense/Map Tiles/Create or Select Sprite Config", priority = 20)]
        public static void CreateOrSelectConfig()
        {
            MapTileSpriteConfig config = GetOrCreateConfig();
            AutoAssignKenneyTiles(config, false);

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);

            EditorUtility.DisplayDialog(
                "Map Tile Config",
                "已创建/选中 MapTileSpriteConfig。\n\n" +
                "空字段已自动填入 Kenney 地砖；\n" +
                "也可以手动替换任意一张。",
                "OK");
        }

        [MenuItem("Tools/Tower Defense/Map Tiles/Auto Assign Kenney Tiles (Overwrite)", priority = 22)]
        public static void AutoAssignOverwrite()
        {
            MapTileSpriteConfig config = GetOrCreateConfig();
            AutoAssignKenneyTiles(config, true);

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);

            EditorUtility.DisplayDialog(
                "Kenney Auto Assign",
                "已按 Kenney 地砖编号覆盖全部字段。",
                "OK");
        }

        [MenuItem("Tools/Tower Defense/Map Tiles/Rebuild With Categories", priority = 21)]
        public static void RebuildMapTiles()
        {
            if (TDMenuGuard.IsInPlayMode())
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            GameObject mapRoot = GameObject.Find("Map");
            if (mapRoot == null)
            {
                ShowError("请先打开 Assets/Scenes/Main.unity 场景");
                return;
            }

            MapTileSpriteConfig config = GetOrCreateConfig();
            AutoAssignKenneyTiles(config, false);

            Sprite groundFallback = AssetDatabase.LoadAssetAtPath<Sprite>(GroundSpritePath);
            Sprite buildableFallback = AssetDatabase.LoadAssetAtPath<Sprite>(BuildableSpritePath);
            Sprite pathFallback = AssetDatabase.LoadAssetAtPath<Sprite>(PathSpritePath);

            if (pathFallback == null)
            {
                ShowError("找不到道路占位图 Assets/Sprites/Map/path.asset");
                return;
            }

            char[,] grid = CreateGrid();
            ClearOldTileRoots(mapRoot.transform);

            Transform groundRoot = CreateChild(mapRoot.transform, "GroundTiles");
            Transform pathRoot = CreateChild(mapRoot.transform, "PathTiles");
            Transform buildRoot = CreateChild(mapRoot.transform, "BuildSlots");

            var counts = new Dictionary<MapTileCategory, int>();

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    char type = grid[row, col];
                    bool isRoad = IsRoad(type);

                    MapTileCategory category = isRoad
                        ? MapTileCategory.Road
                        : Classify(grid, row, col);

                    // 只有平地（tileNone，不挨着道路）可以建造；
                    // 道路边、内侧/外侧拐角都只是地形装饰。
                    bool isBuildable = !isRoad && category == MapTileCategory.None;

                    Sprite configuredSprite = isRoad
                        ? config.roadSprite
                        : GetCategorySprite(config, category);

                    Sprite fallbackSprite = isRoad
                        ? pathFallback
                        : isBuildable ? buildableFallback : groundFallback;

                    Sprite sprite = configuredSprite != null ? configuredSprite : fallbackSprite;
                    Color tint = configuredSprite != null ? Color.white : FallbackTint(category);

                    string prefix = isRoad ? "Path" : isBuildable ? "Build" : "Ground";
                    Transform parent = isRoad ? pathRoot : isBuildable ? buildRoot : groundRoot;

                    GameObject tile = new GameObject(prefix + "_" + col + "_" + row);
                    tile.transform.SetParent(parent, false);
                    tile.transform.localPosition = WorldPosition(col, row);

                    SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.color = tint;
                    renderer.sortingOrder = 0;

                    MapTileVisual visual = tile.AddComponent<MapTileVisual>();
                    visual.category = category;
                    visual.gridCoord = new Vector2Int(col, row);

                    if (isBuildable)
                    {
                        BoxCollider2D collider = tile.AddComponent<BoxCollider2D>();
                        collider.size = Vector2.one * 0.95f;

                        BuildSlot slot = tile.AddComponent<BuildSlot>();
                        slot.isBuildable = true;
                        slot.gridCoord = new Vector2Int(col, row);
                    }

                    if (!counts.ContainsKey(category))
                        counts[category] = 0;
                    counts[category]++;
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            string summary = BuildCountSummary(counts);
            Debug.Log("[TD Map Tiles] 重建完成\n" + summary);

            EditorUtility.DisplayDialog(
                "Map Tiles Rebuilt",
                "地图已按分类重建。\n\n" + summary,
                "OK");
        }

        private static MapTileSpriteConfig GetOrCreateConfig()
        {
            MapTileSpriteConfig config =
                AssetDatabase.LoadAssetAtPath<MapTileSpriteConfig>(ConfigPath);
            if (config != null)
                return config;

            config = ScriptableObject.CreateInstance<MapTileSpriteConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        // ---------- Kenney 素材自动映射 ----------

        private static void AutoAssignKenneyTiles(MapTileSpriteConfig config, bool overwrite)
        {
            bool changed = false;

            changed |= Assign(config.tileNone, "024", overwrite, ref config.tileNone);
            changed |= Assign(config.tileLeftOfRoad, "092", overwrite, ref config.tileLeftOfRoad);
            changed |= Assign(config.tileRightOfRoad, "094", overwrite, ref config.tileRightOfRoad);
            changed |= Assign(config.tileAboveRoad, "070", overwrite, ref config.tileAboveRoad);
            changed |= Assign(config.tileBelowRoad, "116", overwrite, ref config.tileBelowRoad);

            changed |= Assign(config.tileInnerCornerTopLeft, "096", overwrite, ref config.tileInnerCornerTopLeft);
            changed |= Assign(config.tileInnerCornerTopRight, "095", overwrite, ref config.tileInnerCornerTopRight);
            changed |= Assign(config.tileInnerCornerBottomLeft, "073", overwrite, ref config.tileInnerCornerBottomLeft);
            changed |= Assign(config.tileInnerCornerBottomRight, "072", overwrite, ref config.tileInnerCornerBottomRight);

            changed |= Assign(config.tileOuterCornerTopLeft, "069", overwrite, ref config.tileOuterCornerTopLeft);
            changed |= Assign(config.tileOuterCornerTopRight, "071", overwrite, ref config.tileOuterCornerTopRight);
            changed |= Assign(config.tileOuterCornerBottomLeft, "115", overwrite, ref config.tileOuterCornerBottomLeft);
            changed |= Assign(config.tileOuterCornerBottomRight, "117", overwrite, ref config.tileOuterCornerBottomRight);

            changed |= Assign(config.roadSprite, "050", overwrite, ref config.roadSprite);

            // 配置里可能被手动换成了别的地砖（例如 tile124 / tile158），
            // 这里统一校正所有被引用素材的 PPU，避免尺寸不一致。
            NormalizeAllConfigSprites(config);

            if (changed)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
        }

        private static void NormalizeAllConfigSprites(MapTileSpriteConfig config)
        {
            NormalizeSprite(config.tileNone);
            NormalizeSprite(config.tileLeftOfRoad);
            NormalizeSprite(config.tileRightOfRoad);
            NormalizeSprite(config.tileAboveRoad);
            NormalizeSprite(config.tileBelowRoad);

            NormalizeSprite(config.tileInnerCornerTopLeft);
            NormalizeSprite(config.tileInnerCornerTopRight);
            NormalizeSprite(config.tileInnerCornerBottomLeft);
            NormalizeSprite(config.tileInnerCornerBottomRight);

            NormalizeSprite(config.tileOuterCornerTopLeft);
            NormalizeSprite(config.tileOuterCornerTopRight);
            NormalizeSprite(config.tileOuterCornerBottomLeft);
            NormalizeSprite(config.tileOuterCornerBottomRight);

            NormalizeSprite(config.roadSprite);
        }

        private static void NormalizeSprite(Sprite sprite)
        {
            if (sprite == null)
                return;

            string path = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(path))
                return;

            EnsurePixelPerUnitMatchesTexture(path, sprite);
        }

        private static bool Assign(
            Sprite current,
            string tileNumber,
            bool overwrite,
            ref Sprite target)
        {
            string path = "Assets/towerDefense_tile" + tileNumber + ".png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning("[TD Map Tiles] 找不到 Kenney 地砖 " + path);
                return false;
            }

            sprite = EnsurePixelPerUnitMatchesTexture(path, sprite);

            if (!overwrite && current != null)
                return false;

            if (current == sprite)
                return false;

            target = sprite;
            return true;
        }

        /// <summary>
        /// 把导入 PPU 设成图片的像素宽度，使一张正方形地砖刚好占 1 个世界单位。
        /// 这样无论素材是 64、128 还是 256 像素都不需要手动改。
        /// </summary>
        private static Sprite EnsurePixelPerUnitMatchesTexture(string path, Sprite sprite)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
                return sprite;

            if (texture.width != texture.height)
            {
                Debug.LogWarning("[TD Map Tiles] " + path + " 不是正方形，地砖可能对不齐");
            }

            float targetPixelsPerUnit = Mathf.Max(texture.width, texture.height);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null &&
                !Mathf.Approximately(importer.spritePixelsPerUnit, targetPixelsPerUnit))
            {
                importer.spritePixelsPerUnit = targetPixelsPerUnit;
                importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return sprite;
        }

        // ---------- 分类 ----------

        private static char[,] CreateGrid()
        {
            char[,] grid = new char[Rows, Cols];

            for (int row = 0; row < Rows; row++)
            for (int col = 0; col < Cols; col++)
                grid[row, col] = '#';

            FillHorizontal(grid, 4, 0, 17);
            FillVertical(grid, 17, 4, 8);
            FillHorizontal(grid, 8, 5, 17);
            FillVertical(grid, 5, 8, 12);
            FillHorizontal(grid, 12, 5, 24);

            grid[4, 0] = 'S';
            grid[12, 24] = 'C';

            return grid;
        }

        private static MapTileCategory Classify(char[,] grid, int row, int col)
        {
            bool north = IsRoadAt(grid, row - 1, col);
            bool south = IsRoadAt(grid, row + 1, col);
            bool west = IsRoadAt(grid, row, col - 1);
            bool east = IsRoadAt(grid, row, col + 1);

            // 内侧拐角：地块有两个相邻方向紧挨道路
            if (north && east) return MapTileCategory.InnerCornerBottomLeft;
            if (north && west) return MapTileCategory.InnerCornerBottomRight;
            if (south && east) return MapTileCategory.InnerCornerTopLeft;
            if (south && west) return MapTileCategory.InnerCornerTopRight;

            // 外侧拐角：地块不直接挨着道路，但位于某个道路拐角的斜对角
            if (!north && !south && !west && !east)
            {
                // delta 表示“拐角格相对当前地块的偏移”：
                // 拐角在地块西北 → 地块在拐角右下角，以此类推。
                if (IsOuterCorner(grid, row, col, -1, -1)) return MapTileCategory.OuterCornerBottomRight;
                if (IsOuterCorner(grid, row, col, -1, 1)) return MapTileCategory.OuterCornerBottomLeft;
                if (IsOuterCorner(grid, row, col, 1, -1)) return MapTileCategory.OuterCornerTopRight;
                if (IsOuterCorner(grid, row, col, 1, 1)) return MapTileCategory.OuterCornerTopLeft;
            }

            if (north) return MapTileCategory.BelowRoad;
            if (south) return MapTileCategory.AboveRoad;
            if (east) return MapTileCategory.LeftOfRoad;
            if (west) return MapTileCategory.RightOfRoad;
            return MapTileCategory.None;
        }

        /// <summary>
        /// 判断地块是否位于道路拐角的斜对角外侧：
        /// 例如地块在拐角的左上角，则拐角处的道路应从下方和右方经过。
        /// </summary>
        private static bool IsOuterCorner(
            char[,] grid,
            int row,
            int col,
            int deltaRow,
            int deltaCol)
        {
            int cornerRow = row + deltaRow;
            int cornerCol = col + deltaCol;
            if (!IsRoadAt(grid, cornerRow, cornerCol))
                return false;

            bool cornerNorth = IsRoadAt(grid, cornerRow - 1, cornerCol);
            bool cornerSouth = IsRoadAt(grid, cornerRow + 1, cornerCol);
            bool cornerWest = IsRoadAt(grid, cornerRow, cornerCol - 1);
            bool cornerEast = IsRoadAt(grid, cornerRow, cornerCol + 1);

            if (deltaRow == -1 && deltaCol == -1)
                return cornerNorth && cornerWest;
            if (deltaRow == -1 && deltaCol == 1)
                return cornerNorth && cornerEast;
            if (deltaRow == 1 && deltaCol == -1)
                return cornerSouth && cornerWest;
            return cornerSouth && cornerEast;
        }

        private static Sprite GetCategorySprite(MapTileSpriteConfig config, MapTileCategory category)
        {
            switch (category)
            {
                case MapTileCategory.LeftOfRoad: return config.tileLeftOfRoad;
                case MapTileCategory.RightOfRoad: return config.tileRightOfRoad;
                case MapTileCategory.AboveRoad: return config.tileAboveRoad;
                case MapTileCategory.BelowRoad: return config.tileBelowRoad;

                case MapTileCategory.InnerCornerTopLeft: return config.tileInnerCornerTopLeft;
                case MapTileCategory.InnerCornerTopRight: return config.tileInnerCornerTopRight;
                case MapTileCategory.InnerCornerBottomLeft: return config.tileInnerCornerBottomLeft;
                case MapTileCategory.InnerCornerBottomRight: return config.tileInnerCornerBottomRight;

                case MapTileCategory.OuterCornerTopLeft: return config.tileOuterCornerTopLeft;
                case MapTileCategory.OuterCornerTopRight: return config.tileOuterCornerTopRight;
                case MapTileCategory.OuterCornerBottomLeft: return config.tileOuterCornerBottomLeft;
                case MapTileCategory.OuterCornerBottomRight: return config.tileOuterCornerBottomRight;

                default: return config.tileNone;
            }
        }

        private static Color FallbackTint(MapTileCategory category)
        {
            switch (category)
            {
                case MapTileCategory.LeftOfRoad: return new Color(1f, 0.7f, 0.7f);
                case MapTileCategory.RightOfRoad: return new Color(0.7f, 0.8f, 1f);
                case MapTileCategory.AboveRoad: return new Color(1f, 1f, 0.65f);
                case MapTileCategory.BelowRoad: return new Color(0.65f, 1f, 0.9f);

                case MapTileCategory.InnerCornerTopLeft: return new Color(1f, 0.6f, 1f);
                case MapTileCategory.InnerCornerTopRight: return new Color(1f, 0.85f, 0.6f);
                case MapTileCategory.InnerCornerBottomLeft: return new Color(0.6f, 1f, 0.6f);
                case MapTileCategory.InnerCornerBottomRight: return new Color(0.6f, 0.9f, 1f);

                case MapTileCategory.OuterCornerTopLeft: return new Color(1f, 0.45f, 0.45f);
                case MapTileCategory.OuterCornerTopRight: return new Color(1f, 0.75f, 0.45f);
                case MapTileCategory.OuterCornerBottomLeft: return new Color(0.45f, 1f, 0.75f);
                case MapTileCategory.OuterCornerBottomRight: return new Color(0.7f, 0.7f, 1f);

                case MapTileCategory.Road: return Color.white;
                default: return Color.white;
            }
        }

        private static void ClearOldTileRoots(Transform mapRoot)
        {
            DestroyChild(mapRoot, "GroundTiles");
            DestroyChild(mapRoot, "PathTiles");
            DestroyChild(mapRoot, "BuildSlots");
        }

        private static void DestroyChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
                Object.DestroyImmediate(child.gameObject);
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static Vector3 WorldPosition(int col, int row)
        {
            return new Vector3(col + 0.5f, -(row + 0.5f), 0f);
        }

        private static bool IsRoad(char type)
        {
            return type == 'P' || type == 'S' || type == 'C';
        }

        private static bool IsRoadAt(char[,] grid, int row, int col)
        {
            if (row < 0 || row >= Rows || col < 0 || col >= Cols)
                return false;

            return IsRoad(grid[row, col]);
        }

        private static void FillHorizontal(char[,] grid, int row, int fromCol, int toCol)
        {
            int step = fromCol <= toCol ? 1 : -1;
            for (int col = fromCol; ; col += step)
            {
                grid[row, col] = 'P';
                if (col == toCol)
                    break;
            }
        }

        private static void FillVertical(char[,] grid, int col, int fromRow, int toRow)
        {
            int step = fromRow <= toRow ? 1 : -1;
            for (int row = fromRow; ; row += step)
            {
                grid[row, col] = 'P';
                if (row == toRow)
                    break;
            }
        }

        private static string BuildCountSummary(Dictionary<MapTileCategory, int> counts)
        {
            var lines = new List<string>();
            foreach (MapTileCategory category in System.Enum.GetValues(typeof(MapTileCategory)))
            {
                int count = counts.ContainsKey(category) ? counts[category] : 0;
                lines.Add(category + ": " + count);
            }

            return string.Join("\n", lines);
        }

        private static void ShowError(string message)
        {
            Debug.LogError("[TD Map Tiles] " + message);
            EditorUtility.DisplayDialog("Map Tiles Error", message, "OK");
        }
    }
}
