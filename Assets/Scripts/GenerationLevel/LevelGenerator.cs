using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = System.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum GroundType { Empty, Walkable, Lava, Rock }

public struct Cell
{
    public GroundType ground;
    public int height;          // для walkable
}

public sealed class LevelGenerator : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int width = 64;
    [SerializeField] private int depth = 40;
    [SerializeField] private float cellSize = 1f;

    [Header("Seed")]
    [SerializeField] private int seed = 12345;

    [Header("Shape (walkable island)")]
    [SerializeField, Range(0.05f, 0.95f)] private float islandFill = 0.55f;
    [SerializeField] private int smoothIterations = 5;
    [SerializeField] private int birthLimit = 5;   // CA rule
    [SerializeField] private int deathLimit = 4;   // CA rule
    [SerializeField] private int extraBlobs = 6;   // дополнительные “пятна”
    [SerializeField] private Vector2 blobRadiusRange = new(3f, 8f);

    [Header("Zones")]
    [SerializeField] private int lavaDistance = 7;      // ширина лавы вокруг walkable
    [SerializeField] private int rockBand = 2;          // камень вдоль лавы/границ

    [Header("Heights / Terraces")]
    [SerializeField] private int baseWalkableHeight = 0;
    [SerializeField] private int terraceCount = 4;              // “пятна повышения”
    [SerializeField] private Vector2 terraceRadiusRange = new(2f, 5f);
    [SerializeField] private int maxHeight = 2;                 // максимум высоты террас

    [Header("Obstacles (optional)")]
    [SerializeField, Range(0, 1)] private float obstacleChance = 0.05f;

    [Header("Prefabs")]
    [SerializeField] private LevelPalette palette;

    [Header("Spawn root")]
    [SerializeField] private Transform spawnedRoot;

    [Header("Minimap (optional)")]
    [SerializeField] private bool generateMinimap = true;
    [SerializeField] private int pixelsPerCell = 2;
    [SerializeField] private SpriteRenderer minimapRenderer;
    [SerializeField] private Color walkableColor = new(0.8f, 0.8f, 0.8f, 1);
    [SerializeField] private Color lavaColor = new(1.0f, 0.55f, 0.0f, 1);
    [SerializeField] private Color rockColor = new(0.08f, 0.08f, 0.08f, 1);
    [SerializeField] private Color emptyColor = new(0, 0, 0, 0);

    private Cell[,] _cells;
    private bool[,] _walkableMask;
    private Random _rng;

    private static readonly Vector2Int[] Dir4 =
    {
        new(1,0), new(-1,0), new(0,1), new(0,-1)
    };

    [Button("Generate")]
    public void Generate()
    {
        if (palette == null) return;

        _rng = new Random(seed);

        EnsureSpawnRoot();
        ClearSpawned();

        _cells = new Cell[width, depth];

        // 1) Walkable mask (island) + smooth + connected
        _walkableMask = BuildWalkableMask();
        MakeMaskConnected(_walkableMask);

        // 2) Zones: lava ring + rock band
        BuildZonesFromMask(_walkableMask);

        // 3) Heights / terraces + stairs
        BuildHeights(_walkableMask);

        // 4) Spawn
        SpawnAll();

        // 5) Minimap
        if (generateMinimap) BuildMinimap();
    }

    #region Mask generation

    private bool[,] BuildWalkableMask()
    {
        bool[,] m = new bool[width, depth];

        // Базовый “остров”: эллипс по центру + вероятностная граница
        Vector2 c = new Vector2((width - 1) * 0.5f, (depth - 1) * 0.5f);
        float rx = width * 0.33f;
        float rz = depth * 0.33f;

        for (int x = 0; x < width; x++)
        for (int z = 0; z < depth; z++)
        {
            float nx = (x - c.x) / rx;
            float nz = (z - c.y) / rz;
            float d = nx * nx + nz * nz; // <1 внутри эллипса

            // вероятность быть walkable выше в центре
            double p = islandFill * Mathf.Clamp01(1.15f - d);
            m[x, z] = _rng.NextDouble() < p;
        }

        // Добавим несколько “блобов” чтобы форма стала интереснее
        for (int i = 0; i < extraBlobs; i++)
        {
            int bx = _rng.Next(2, width - 2);
            int bz = _rng.Next(2, depth - 2);
            float r = Mathf.Lerp(blobRadiusRange.x, blobRadiusRange.y, (float)_rng.NextDouble());
            PaintBlob(m, bx, bz, r, true);
        }

        // Сглаживание CA
        for (int it = 0; it < smoothIterations; it++)
            m = SmoothCA(m, birthLimit, deathLimit);

        // Небольшая гарантия: центр должен быть walkable
        m[(int)c.x, (int)c.y] = true;

        return m;
    }

    private void PaintBlob(bool[,] m, int cx, int cz, float radius, bool value)
    {
        int r = Mathf.CeilToInt(radius);
        float r2 = radius * radius;

        for (int x = cx - r; x <= cx + r; x++)
        for (int z = cz - r; z <= cz + r; z++)
        {
            if (!InBounds(x, z)) continue;
            float dx = x - cx;
            float dz = z - cz;
            if (dx * dx + dz * dz <= r2)
                m[x, z] = value;
        }
    }

    private bool[,] SmoothCA(bool[,] m, int birth, int death)
    {
        bool[,] n = new bool[width, depth];
        for (int x = 0; x < width; x++)
        for (int z = 0; z < depth; z++)
        {
            int alive = CountAlive8(m, x, z);
            if (m[x, z])
                n[x, z] = alive >= death;
            else
                n[x, z] = alive > birth;
        }
        return n;
    }

    private int CountAlive8(bool[,] m, int x, int z)
    {
        int c = 0;
        for (int dx = -1; dx <= 1; dx++)
        for (int dz = -1; dz <= 1; dz++)
        {
            if (dx == 0 && dz == 0) continue;
            int xx = x + dx;
            int zz = z + dz;
            if (!InBounds(xx, zz)) { c++; continue; } // за границей считаем "стена"
            if (m[xx, zz]) c++;
        }
        return c;
    }

    private void MakeMaskConnected(bool[,] m)
    {
        // Находим старт (любую walkable, лучше ближе к центру)
        Vector2Int start = new Vector2Int(width / 2, depth / 2);
        if (!m[start.x, start.y])
        {
            bool found = false;
            for (int r = 1; r < Mathf.Max(width, depth) && !found; r++)
            {
                for (int x = start.x - r; x <= start.x + r && !found; x++)
                for (int z = start.y - r; z <= start.y + r && !found; z++)
                    if (InBounds(x, z) && m[x, z]) { start = new Vector2Int(x, z); found = true; }
            }
            if (!found) { m[width / 2, depth / 2] = true; return; }
        }

        bool[,] visited = new bool[width, depth];
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(start);
        visited[start.x, start.y] = true;

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            foreach (var d in Dir4)
            {
                int nx = p.x + d.x;
                int nz = p.y + d.y;
                if (!InBounds(nx, nz)) continue;
                if (visited[nx, nz]) continue;
                if (!m[nx, nz]) continue;
                visited[nx, nz] = true;
                q.Enqueue(new Vector2Int(nx, nz));
            }
        }

        // Все walkable, которые не visited, убираем (чтобы был один остров)
        for (int x = 0; x < width; x++)
        for (int z = 0; z < depth; z++)
            if (m[x, z] && !visited[x, z])
                m[x, z] = false;
    }

    #endregion

    #region Zones

    private void BuildZonesFromMask(bool[,] walkable)
    {
        // init
        for (int x = 0; x < width; x++)
        for (int z = 0; z < depth; z++)
        {
            _cells[x, z].ground = walkable[x, z] ? GroundType.Walkable : GroundType.Empty;
            _cells[x, z].height = baseWalkableHeight;
        }

        // distance to walkable (BFS)
        int[,] dist = DistanceToWalkable(walkable);

        // lava ring
        for (int x = 0; x < width; x++)
        for (int z = 0; z < depth; z++)
        {
            if (walkable[x, z]) continue;
            int d = dist[x, z];
            if (d > 0 && d <= lavaDistance)
                _cells[x, z].ground = GroundType.Lava;
        }

        // rock band: по границе лавы/пустоты и по краям
        for (int x = 0; x < width; x++)
        for (int z = 0; z < depth; z++)
        {
            if (_cells[x, z].ground != GroundType.Empty) continue;

            // если рядом лава или walkable — делаем rock на некоторую ширину
            int near = DistanceToNonEmpty(_cells, x, z, rockBand);
            if (near >= 0)
                _cells[x, z].ground = GroundType.Rock;
        }
    }

    private int[,] DistanceToWalkable(bool[,] walkable)
    {
        int[,] dist = new int[width, depth];
        for (int x = 0; x < width; x++)
        for (int z = 0; z < depth; z++)
            dist[x, z] = -1;

        Queue<Vector2Int> q = new Queue<Vector2Int>();

        for (int x = 0; x < width; x++)
        for (int z = 0; z < depth; z++)
        {
            if (!walkable[x, z]) continue;
            dist[x, z] = 0;
            q.Enqueue(new Vector2Int(x, z));
        }

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            int cd = dist[p.x, p.y];

            foreach (var d in Dir4)
            {
                int nx = p.x + d.x;
                int nz = p.y + d.y;
                if (!InBounds(nx, nz)) continue;
                if (dist[nx, nz] != -1) continue;
                dist[nx, nz] = cd + 1;
                q.Enqueue(new Vector2Int(nx, nz));
            }
        }

        return dist;
    }

    private int DistanceToNonEmpty(Cell[,] cells, int x, int z, int max)
    {
        // Если в радиусе max есть lava/walkable => rock
        for (int r = 1; r <= max; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                int xx1 = x + dx;
                int zz1 = z + r;
                int zz2 = z - r;

                if (InBounds(xx1, zz1) && cells[xx1, zz1].ground != GroundType.Empty) return r;
                if (InBounds(xx1, zz2) && cells[xx1, zz2].ground != GroundType.Empty) return r;
            }

            for (int dz = -r + 1; dz <= r - 1; dz++)
            {
                int zz = z + dz;
                int xx1 = x + r;
                int xx2 = x - r;

                if (InBounds(xx1, zz) && cells[xx1, zz].ground != GroundType.Empty) return r;
                if (InBounds(xx2, zz) && cells[xx2, zz].ground != GroundType.Empty) return r;
            }
        }
        return -1;
    }

    #endregion

    #region Heights + stairs

    private void BuildHeights(bool[,] walkable)
    {
        // базовая высота уже выставлена
        // создаём несколько “террас” на walkable
        List<Vector2Int> candidates = CollectWalkableCells(walkable);

        for (int i = 0; i < terraceCount && candidates.Count > 0; i++)
        {
            var center = candidates[_rng.Next(0, candidates.Count)];
            float radius = Mathf.Lerp(terraceRadiusRange.x, terraceRadiusRange.y, (float)_rng.NextDouble());
            int dh = 1 + _rng.Next(0, Mathf.Max(1, maxHeight)); // 1..maxHeight
            RaiseBlob(center.x, center.y, radius, dh, walkable);
        }

        // сгладим высоты немного (чтобы не было иголок)
        SmoothHeights(walkable, iterations: 2);
    }

    private List<Vector2Int> CollectWalkableCells(bool[,] walkable)
    {
        var list = new List<Vector2Int>(width * depth / 2);
        for (int x = 0; x < width; x++)
        for (int z = 0; z < depth; z++)
            if (walkable[x, z]) list.Add(new Vector2Int(x, z));
        return list;
    }

    private void RaiseBlob(int cx, int cz, float radius, int delta, bool[,] walkable)
    {
        int r = Mathf.CeilToInt(radius);
        float r2 = radius * radius;

        for (int x = cx - r; x <= cx + r; x++)
        for (int z = cz - r; z <= cz + r; z++)
        {
            if (!InBounds(x, z)) continue;
            if (!walkable[x, z]) continue;

            float dx = x - cx;
            float dz = z - cz;
            if (dx * dx + dz * dz > r2) continue;

            _cells[x, z].height = Mathf.Min(_cells[x, z].height + delta, baseWalkableHeight + maxHeight);
        }
    }

    private void SmoothHeights(bool[,] walkable, int iterations)
    {
        for (int it = 0; it < iterations; it++)
        {
            int[,] next = new int[width, depth];
            for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
                next[x, z] = _cells[x, z].height;

            for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
            {
                if (!walkable[x, z]) continue;

                int sum = _cells[x, z].height;
                int cnt = 1;

                foreach (var d in Dir4)
                {
                    int nx = x + d.x;
                    int nz = z + d.y;
                    if (!InBounds(nx, nz)) continue;
                    if (!walkable[nx, nz]) continue;
                    sum += _cells[nx, nz].height;
                    cnt++;
                }

                int avg = Mathf.RoundToInt((float)sum / cnt);
                // ограничиваем, чтобы перепады не улетали
                next[x, z] = Mathf.Clamp(avg, baseWalkableHeight, baseWalkableHeight + maxHeight);
            }

            for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
                _cells[x, z].height = next[x, z];
        }
    }

    #endregion

    #region Spawn

    private void SpawnAll()
    {
        Vector3 origin = transform.position;

        for (int x = 0; x < width; x++)
        for (int z = 0; z < depth; z++)
        {
            var cell = _cells[x, z];
            if (cell.ground == GroundType.Empty) continue;

            Vector3 basePos = origin + new Vector3(x * cellSize, 0f, z * cellSize);

            switch (cell.ground)
            {
                case GroundType.Walkable:
                {
                    // floor at height
                    var prefab = palette.Pick(palette.walkableFloors, _rng);
                    if (prefab != null)
                    {
                        Vector3 p = basePos + Vector3.up * (cell.height * cellSize);
                        Spawn(prefab, p, Quaternion.identity, spawnedRoot);
                    }

                    // obstacles on walkable
                    if (palette.obstacles != null && palette.obstacles.Count > 0 && _rng.NextDouble() < obstacleChance)
                    {
                        var ob = palette.Pick(palette.obstacles, _rng);
                        if (ob != null)
                        {
                            Vector3 p = basePos + Vector3.up * ((cell.height + 1) * cellSize * 0.5f);
                            Spawn(ob, p, Quaternion.identity, spawnedRoot);
                        }
                    }

                    // stairs: place on edges where neighbor is +1 height
                    TrySpawnStairs(origin, x, z);

                    break;
                }

                case GroundType.Lava:
                {
                    var prefab = palette.Pick(palette.lavaFloors, _rng);
                    if (prefab != null)
                        Spawn(prefab, basePos, Quaternion.identity, spawnedRoot);
                    break;
                }

                case GroundType.Rock:
                {
                    var prefab = palette.Pick(palette.rockBlocks, _rng);
                    if (prefab != null)
                        Spawn(prefab, basePos, Quaternion.identity, spawnedRoot);
                    break;
                }
            }
        }
    }

    private GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetPositionAndRotation(pos, rot);
            Undo.RegisterCreatedObjectUndo(go, "Generate Level");
            return go;
        }
#endif
        return Instantiate(prefab, pos, rot, parent);
    }

    
    private void TrySpawnStairs(Vector3 origin, int x, int z)
    {
        if (palette.stairUpPrefab == null) return;

        var a = _cells[x, z];
        if (a.ground != GroundType.Walkable) return;
 
        // Ставим ступеньку, если сосед walkable на +1
        foreach (var d in Dir4)
        {
            int nx = x + d.x;
            int nz = z + d.y;
            if (!InBounds(nx, nz)) continue;

            var b = _cells[nx, nz];
            if (b.ground != GroundType.Walkable) continue;

            int dh = b.height - a.height;
            if (dh != 1) continue;

            // позиция: в клетке "нижней" площадки на её высоте
            Vector3 basePos = origin + new Vector3(x * cellSize, a.height * cellSize, z * cellSize);

            // поворот по направлению к более высокой клетке
            Quaternion rot = DirToRotation(d);

            Spawn(palette.stairUpPrefab, basePos, rot, spawnedRoot);

            // важный момент: чтобы не спавнить 2 ступеньки на одну грань,
            // можно выйти после первой найденной
            return;
        }
    }

    private static Quaternion DirToRotation(Vector2Int d)
    {
        // считаем, что stair prefab "смотрит" в +Z по умолчанию
        if (d == new Vector2Int(0, 1)) return Quaternion.identity;               // +Z
        if (d == new Vector2Int(0, -1)) return Quaternion.Euler(0, 180, 0);      // -Z
        if (d == new Vector2Int(1, 0)) return Quaternion.Euler(0, 90, 0);        // +X
        return Quaternion.Euler(0, -90, 0);                                      // -X
    }

    private void EnsureSpawnRoot()
    {
        if (spawnedRoot != null) return;
        var go = new GameObject("Spawned");
        go.transform.SetParent(transform, false);
        spawnedRoot = go.transform;
    }

    private void ClearSpawned()
    {
        if (spawnedRoot == null) return;
        for (int i = spawnedRoot.childCount - 1; i >= 0; i--)
        {
            var child = spawnedRoot.GetChild(i);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
    }

    #endregion

    #region Minimap

    private void BuildMinimap()
    {
        int texW = width * pixelsPerCell;
        int texH = depth * pixelsPerCell;

        var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        // чтобы подсветить высоты — найдём min/max walkable height
        int minH = int.MaxValue, maxH = int.MinValue;
        for (int x = 0; x < width; x++)
        for (int z = 0; z < depth; z++)
        {
            if (_cells[x, z].ground != GroundType.Walkable) continue;
            minH = Mathf.Min(minH, _cells[x, z].height);
            maxH = Mathf.Max(maxH, _cells[x, z].height);
        }
        if (minH == int.MaxValue) { minH = 0; maxH = 1; }

        for (int z = 0; z < depth; z++)
        for (int x = 0; x < width; x++)
        {
            var c = _cells[x, z];
            Color col = c.ground switch
            {
                GroundType.Walkable => walkableColor,
                GroundType.Lava => lavaColor,
                GroundType.Rock => rockColor,
                _ => emptyColor
            };

            if (c.ground == GroundType.Walkable)
            {
                float t = Mathf.InverseLerp(minH, Mathf.Max(minH + 1, maxH), c.height);
                float shade = Mathf.Lerp(0.85f, 1.15f, t);
                col *= shade;
                col.a = 1f;
            }

            int px0 = x * pixelsPerCell;
            int py0 = z * pixelsPerCell;

            for (int py = 0; py < pixelsPerCell; py++)
            for (int px = 0; px < pixelsPerCell; px++)
                tex.SetPixel(px0 + px, py0 + py, col);
        }

        tex.Apply(false, false);

        if (minimapRenderer != null)
        {
            var spr = Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), pixelsPerCell);
            minimapRenderer.sprite = spr;
        }
    }

    #endregion

    #region Utils

    private bool InBounds(int x, int z) => (uint)x < (uint)width && (uint)z < (uint)depth;

    #endregion
}



[CreateAssetMenu(menuName = "Level/Level Palette", fileName = "LevelPalette")]
public sealed class LevelPalette : ScriptableObject
{
    [Header("Ground")]
    public List<GameObject> walkableFloors = new();
    public List<GameObject> lavaFloors = new();

    [Header("Rock / Walls")]
    public List<GameObject> rockBlocks = new();     // “черные” вокруг
    public List<GameObject> wallBlocks = new();     // если нужно отдельно, можно оставить пустым

    [Header("Decor / Obstacles (optional)")]
    public List<GameObject> obstacles = new();

    [Header("Stairs")]
    public GameObject stairUpPrefab; // префаб ступеньки "вверх по направлению"

    public GameObject Pick(List<GameObject> list, Random rng)
    {
        if (list == null || list.Count == 0) return null;
        return list[rng.Next(0, list.Count)];
    }

    public GameObject PickWeighted(List<GameObject> list, Random rng)
        => Pick(list, rng);
}
