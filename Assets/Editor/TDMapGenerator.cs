using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.EditorTools
{
    /// <summary>
    /// 地图生成器：在 Unity 编辑器菜单中运行一次，
    /// 自动生成塔防地图场景、占位美术资源与路径数据。
    /// 用法：菜单 Tools > Tower Defense > Generate Map & Main Scene
    /// </summary>
    public static class TDMapGenerator
    {
        private const int Cols = 25;
        private const int Rows = 14;
        private const int SpriteSize = 32;
        private const string SpriteFolder = "Assets/Sprites/Map";
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        // 敌人行走路径的网格坐标（列, 行），顺序即移动顺序
        private static readonly Vector2Int[] WaypointCoords =
        {
            new Vector2Int(0, 4),   // 入口
            new Vector2Int(17, 4),  // 第一次右转
            new Vector2Int(17, 8),  // 第二次转向（向下后向左）
            new Vector2Int(5, 8),   // 第三次转向（向左后向下）
            new Vector2Int(5, 12),  // 第四次转向（向下后向右）
            new Vector2Int(24, 12), // 核心基地
        };

        private static Sprite groundSprite;
        private static Sprite buildableSprite;
        private static Sprite pathSprite;
        private static Sprite spawnMarkerSprite;
        private static Sprite coreMarkerSprite;

        [MenuItem("Tools/Tower Defense/Generate Map & Main Scene", priority = 1)]
        public static void GenerateMap()
        {
            if (TDMenuGuard.IsInPlayMode())
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsureFolderExists(SpriteFolder);
            CreateSprites();

            char[,] grid = CreateGrid();
            CreateMapHierarchy(grid);
            CreateMainCamera();

            if (File.Exists(ToAbsolutePath(MainScenePath)))
                AssetDatabase.DeleteAsset(MainScenePath);

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MainScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log("[TD Map] 地图已生成并保存到 " + MainScenePath);
            EditorUtility.DisplayDialog(
                "TD Map",
                "地图已生成并保存到 Assets/Scenes/Main.unity。\n\n" +
                "当前场景已经切换到 Main，可以直接查看路径与建造点。",
                "OK");
        }

        private static void CreateSprites()
        {
            // 格子地面：深绿
            groundSprite = CreateSpriteAsset("ground", IsInsideSquare, new Color32(43, 66, 43, 255));
            // 可建造地块：浅绿
            buildableSprite = CreateSpriteAsset("buildable", IsInsideSquare, new Color32(64, 108, 56, 255));
            // 道路：土色
            pathSprite = CreateSpriteAsset("path", IsInsideSquare, new Color32(146, 128, 78, 255));
            // 出生点标记：紫色菱形
            spawnMarkerSprite = CreateSpriteAsset("spawn_marker", IsInsideDiamond, new Color32(178, 116, 208, 255));
            // 核心标记：亮黄色圆形
            coreMarkerSprite = CreateSpriteAsset("core_marker", IsInsideCircle, new Color32(255, 216, 92, 255));
        }

        private static char[,] CreateGrid()
        {
            char[,] grid = new char[Rows, Cols];

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    grid[row, col] = '#'; // # = 普通地面
                }
            }

            FillHorizontal(grid, 4, 0, 17);
            FillVertical(grid, 17, 4, 8);
            FillHorizontal(grid, 8, 5, 17);
            FillVertical(grid, 5, 8, 12);
            FillHorizontal(grid, 12, 5, 24);

            grid[4, 0] = 'S';   // 敌人入口
            grid[12, 24] = 'C'; // 核心基地

            // 让路径四周的普通地面变成可建造地块 '.'
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    if (grid[row, col] != '#')
                        MarkBuildableNeighbors(grid, row, col);
                }
            }

            return grid;
        }

        private static void CreateMapHierarchy(char[,] grid)
        {
            GameObject mapRoot = new GameObject("Map");
            Transform groundRoot = CreateChild(mapRoot.transform, "GroundTiles");
            Transform pathRoot = CreateChild(mapRoot.transform, "PathTiles");
            Transform buildRoot = CreateChild(mapRoot.transform, "BuildSlots");

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    char type = grid[row, col];
                    bool isBuildable = type == '.';
                    bool isPath = type == 'P' || type == 'S' || type == 'C';

                    Sprite sprite = isBuildable
                        ? buildableSprite
                        : isPath
                            ? pathSprite
                            : groundSprite;

                    string prefix = isBuildable ? "Build" : isPath ? "Path" : "Ground";
                    Transform parent = isBuildable ? buildRoot : isPath ? pathRoot : groundRoot;

                    GameObject tile = new GameObject(prefix + "_" + col + "_" + row);
                    tile.transform.SetParent(parent, false);
                    tile.transform.localPosition = WorldPosition(col, row);

                    SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.sortingOrder = 0;

                    if (isBuildable)
                    {
                        BoxCollider2D collider = tile.AddComponent<BoxCollider2D>();
                        collider.size = Vector2.one * 0.95f;

                        BuildSlot slot = tile.AddComponent<BuildSlot>();
                        slot.isBuildable = true;
                        slot.gridCoord = new Vector2Int(col, row);
                    }
                }
            }

            CreateMarkers(mapRoot.transform);
            CreateWaypoints(mapRoot.transform);
        }

        private static void CreateMarkers(Transform mapRoot)
        {
            Transform markersRoot = CreateChild(mapRoot, "Markers");

            CreateMarkerObject(markersRoot, "SpawnPortal", WaypointCoords[0], spawnMarkerSprite);
            CreateMarkerObject(markersRoot, "CoreCrystal", WaypointCoords[WaypointCoords.Length - 1], coreMarkerSprite);
        }

        private static void CreateMarkerObject(
            Transform parent,
            string objectName,
            Vector2Int coord,
            Sprite sprite)
        {
            GameObject marker = new GameObject(objectName);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = WorldPosition(coord.x, coord.y);
            marker.transform.localScale = Vector3.one * 0.9f;

            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 5;
        }

        private static void CreateWaypoints(Transform mapRoot)
        {
            Transform waypointsRoot = CreateChild(mapRoot, "Waypoints");
            Transform[] waypointTransforms = new Transform[WaypointCoords.Length];

            for (int i = 0; i < WaypointCoords.Length; i++)
            {
                GameObject waypoint = new GameObject("Wp" + i);
                waypoint.transform.SetParent(waypointsRoot, false);
                waypoint.transform.localPosition = WorldPosition(
                    WaypointCoords[i].x,
                    WaypointCoords[i].y);
                waypointTransforms[i] = waypoint.transform;
            }

            GameObject pathManagerObject = new GameObject("PathManager");
            pathManagerObject.transform.SetParent(mapRoot, false);

            PathManager pathManager = pathManagerObject.AddComponent<PathManager>();
            pathManager.SetWaypoints(waypointTransforms);
        }

        private static void CreateMainCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(Cols * 0.5f, -Rows * 0.5f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Rows * 0.5f + 0.7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.12f, 1f);

            cameraObject.AddComponent<AudioListener>();
        }

        private static void MarkBuildableNeighbors(char[,] grid, int row, int col)
        {
            TryMarkBuildable(grid, row - 1, col);
            TryMarkBuildable(grid, row + 1, col);
            TryMarkBuildable(grid, row, col - 1);
            TryMarkBuildable(grid, row, col + 1);
        }

        private static void TryMarkBuildable(char[,] grid, int row, int col)
        {
            if (row < 0 || row >= Rows || col < 0 || col >= Cols)
                return;

            if (grid[row, col] == '#')
                grid[row, col] = '.';
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

        private static Vector3 WorldPosition(int col, int row)
        {
            return new Vector3(col + 0.5f, -(row + 0.5f), 0f);
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static Sprite CreateSpriteAsset(string baseName, Func<Vector2, bool> insideShape, Color color)
        {
            string pngPath = SpriteFolder + "/" + baseName + ".png";
            string spriteAssetPath = SpriteFolder + "/" + baseName + ".asset";

            if (File.Exists(ToAbsolutePath(spriteAssetPath)))
                AssetDatabase.DeleteAsset(spriteAssetPath);
            if (File.Exists(ToAbsolutePath(pngPath)))
                AssetDatabase.DeleteAsset(pngPath);

            Texture2D texture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[SpriteSize * SpriteSize];

            for (int y = 0; y < SpriteSize; y++)
            {
                for (int x = 0; x < SpriteSize; x++)
                {
                    float normalizedX = ((x + 0.5f) / SpriteSize - 0.5f) * 2f;
                    float normalizedY = ((y + 0.5f) / SpriteSize - 0.5f) * 2f;
                    pixels[y * SpriteSize + x] =
                        insideShape(new Vector2(normalizedX, normalizedY))
                            ? color
                            : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(ToAbsolutePath(pngPath), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("无法读取贴图导入设置: " + pngPath);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = SpriteSize;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            Texture2D importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            if (importedTexture == null)
                throw new InvalidOperationException("贴图导入失败: " + pngPath);

            Sprite sprite = Sprite.Create(
                importedTexture,
                new Rect(0f, 0f, SpriteSize, SpriteSize),
                new Vector2(0.5f, 0.5f),
                SpriteSize);

            AssetDatabase.CreateAsset(sprite, spriteAssetPath);
            return sprite;
        }

        private static void EnsureFolderExists(string folderPath)
        {
            string normalized = folderPath.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(normalized))
                return;

            string parent = Path.GetDirectoryName(normalized);
            if (string.IsNullOrEmpty(parent))
                return;

            EnsureFolderExists(parent);

            string leafName = Path.GetFileName(normalized);
            AssetDatabase.CreateFolder(parent, leafName);
        }

        private static string ToAbsolutePath(string projectRelativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath));
        }

        private static bool IsInsideSquare(Vector2 point)
        {
            return true;
        }

        private static bool IsInsideCircle(Vector2 point)
        {
            return point.sqrMagnitude <= 1f;
        }

        private static bool IsInsideDiamond(Vector2 point)
        {
            return Mathf.Abs(point.x) + Mathf.Abs(point.y) <= 1.05f;
        }
    }
}
