using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public class SnowBlockManager : MonoBehaviour
{
    public static SnowBlockManager instance;
    public Material white;
    public Material red;
    public Material defaultMaterial;

    public float cooldown;
    public int radius;
    public GameObject tilePrefab;
    public GameObject tileParent;
    public Dictionary<HexCoordinates, SnowBlock> snowBlocks;
    public Dictionary<HexCoordinates, SnowBlock> interior;
    public Dictionary<HexCoordinates, SnowBlock> edges;
    public Dictionary<HexCoordinates, SnowBlock> droppableEdges;
    public Dictionary<HexCoordinates, SnowBlock> fallen;
    public Dictionary<HexCoordinates, SnowBlock> boundaries;

    public HexCoordinates connectionPoint;
    private float cooldownTimer;

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer < 0 && droppableEdges.Count > 0)
        {
            cooldownTimer = cooldown;
            RandomDropSnowBlock();
        }
    }

    private void OnEnable()
    {
        if (instance == null) instance = this;
        //DontDestroyOnLoad(gameObject);
        //Destroy(gameObject);
    }

    private void OnDestroy()
    {
        snowBlocks.Clear();
    }

    /// <summary>
    ///     初始化雪块管理器
    /// </summary>
    private void Initialize()
    {
        interior = new Dictionary<HexCoordinates, SnowBlock>();
        edges = new Dictionary<HexCoordinates, SnowBlock>();
        droppableEdges = new Dictionary<HexCoordinates, SnowBlock>();
        fallen = new Dictionary<HexCoordinates, SnowBlock>();
        boundaries = new Dictionary<HexCoordinates, SnowBlock>();

        snowBlocks = Tile.GenerateTiles(radius + 1, tilePrefab, tileParent).ToDictionary(kvp => kvp.Key, kvp =>
            new SnowBlock
            {
                coordinates = kvp.Key,
                type = Type.Interior,
                gameObject = kvp.Value
            });

        // 更新边缘
        foreach (HexCoordinates point in snowBlocks.Keys)
            if (Mathf.Abs(point.X) + Mathf.Abs(point.Y) + Mathf.Abs(point.Z) == 2 * radius)
                snowBlocks[point].type = Type.DroppableEdge;

        // 更新边界
        foreach (HexCoordinates point in snowBlocks.Keys)
            if (Mathf.Abs(point.X) + Mathf.Abs(point.Y) + Mathf.Abs(point.Z) == 2 * radius + 2)
            {
                snowBlocks[point].type = Type.Boundary;
                snowBlocks[point].gameObject.SetActive(false);
            }

        Dictionary<HexCoordinates, SnowBlock> connectablePoints = snowBlocks.Where(o => o.Value.type == Type.Interior)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        Random random = new();
        var index = random.Next(connectablePoints.Count);
        connectionPoint = connectablePoints.Keys.ElementAt(index);

        cooldownTimer = cooldown;
    }

    /// <summary>
    ///     随机掉落雪块
    /// </summary>
    private void RandomDropSnowBlock()
    {
        Random random = new();
        var index = random.Next(droppableEdges.Count);
        HexCoordinates point = droppableEdges.Keys.ElementAt(index);

        SnowBlock.Drop(ref snowBlocks, point);
    }
}

[Serializable]
public class SnowBlock
{
    public GameObject gameObject;
    private Type _type;
    public HexCoordinates coordinates;

    public Type type
    {
        get => _type;
        set
        {
            if (SnowBlockManager.instance.interior.ContainsKey(coordinates))
                SnowBlockManager.instance.interior.Remove(coordinates);
            if (SnowBlockManager.instance.boundaries.ContainsKey(coordinates))
                SnowBlockManager.instance.boundaries.Remove(coordinates);
            if (SnowBlockManager.instance.fallen.ContainsKey(coordinates))
                SnowBlockManager.instance.fallen.Remove(coordinates);
            if (SnowBlockManager.instance.edges.ContainsKey(coordinates))
                SnowBlockManager.instance.edges.Remove(coordinates);
            if (SnowBlockManager.instance.droppableEdges.ContainsKey(coordinates))
                SnowBlockManager.instance.droppableEdges.Remove(coordinates);
            switch (value)
            {
                case Type.Interior:
                    SnowBlockManager.instance.interior.Add(coordinates, this);
                    break;
                case Type.Boundary:
                    SnowBlockManager.instance.boundaries.Add(coordinates, this);
                    break;
                case Type.Fallen:
                    SnowBlockManager.instance.fallen.Add(coordinates, this);
                    break;
                case Type.Edge:
                    SnowBlockManager.instance.edges.Add(coordinates, this);
                    break;
                case Type.DroppableEdge:
                    SnowBlockManager.instance.droppableEdges.Add(coordinates, this);
                    break;
            }

            _type = value;
        }
    }

    /// <summary>
    ///     掉落雪块
    /// </summary>
    /// <param name="allSnowBlocks">所有雪块的字典</param>
    /// <param name="droppedSnowBlock">要掉落的雪块的坐标</param>
    public static void Drop(ref Dictionary<HexCoordinates, SnowBlock> allSnowBlocks, HexCoordinates droppedSnowBlock)
    {
        SnowBlockManager.instance.StartCoroutine(Drop(allSnowBlocks[droppedSnowBlock].gameObject));
        allSnowBlocks[droppedSnowBlock].type = Type.Fallen;
        foreach (HexCoordinates neighbor in droppedSnowBlock.GetNeighbors())
            if (SnowBlockManager.instance.interior.Count + SnowBlockManager.instance.edges.Count +
                SnowBlockManager.instance.droppableEdges.Count > 1)
            {
                if (allSnowBlocks.ContainsKey(neighbor) && allSnowBlocks[neighbor].type != Type.Fallen &&
                    allSnowBlocks[neighbor].type != Type.Boundary &&
                    neighbor != SnowBlockManager.instance.connectionPoint)
                    UpdateEdges(ref allSnowBlocks, neighbor, SnowBlockManager.instance.connectionPoint);
            }
            else
            {
                if (allSnowBlocks.ContainsKey(neighbor) && allSnowBlocks[neighbor].type != Type.Fallen &&
                    allSnowBlocks[neighbor].type != Type.Boundary)
                    UpdateEdges(ref allSnowBlocks, neighbor, SnowBlockManager.instance.connectionPoint);
            }
    }

    /// <summary>
    ///     更新边缘
    /// </summary>
    /// <param name="allSnowBlocks">所有雪块的字典</param>
    /// <param name="point">要更新的雪块的坐标</param>
    /// <param name="connectionPoint">连接点的坐标</param>
    private static void UpdateEdges(ref Dictionary<HexCoordinates, SnowBlock> allSnowBlocks, HexCoordinates point,
        HexCoordinates connectionPoint)
    {
        var canFall = false;
        foreach (HexCoordinates neighbor in point.GetNeighbors())
        {
            canFall = true;
            if (allSnowBlocks.ContainsKey(neighbor) && allSnowBlocks[neighbor].type != Type.Fallen &&
                allSnowBlocks[neighbor].type != Type.Boundary)
                if (CoordinateUtils.Pathfinding(connectionPoint, neighbor,
                        new Dictionary<HexCoordinates, GameObject> { { point, null } }, new(),
                        SnowBlockManager.instance.snowBlocks
                            .Where(o => o.Value.type == Type.Boundary || o.Value.type == Type.Fallen)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.gameObject)) == null)
                {
                    canFall = false;
                    break;
                }
        }

        if (canFall)
            allSnowBlocks[point].type = Type.DroppableEdge;
        else
            allSnowBlocks[point].type = Type.Edge;
    }

    private static IEnumerator Drop(GameObject snowBlock)
    {
        var material = snowBlock.transform.Find("Floor").GetComponent<Renderer>();
        material.material = SnowBlockManager.instance.red;
        yield return new WaitForSeconds(0.5f);
        material.material = SnowBlockManager.instance.defaultMaterial;
        yield return new WaitForSeconds(0.5f);
        material.material = SnowBlockManager.instance.red;
        yield return new WaitForSeconds(0.4f);
        material.material = SnowBlockManager.instance.defaultMaterial;
        yield return new WaitForSeconds(0.4f);
        material.material = SnowBlockManager.instance.red;
        yield return new WaitForSeconds(0.3f);
        material.material = SnowBlockManager.instance.defaultMaterial;
        yield return new WaitForSeconds(0.3f);
        material.material = SnowBlockManager.instance.red;
        yield return new WaitForSeconds(0.2f);
        material.material = SnowBlockManager.instance.defaultMaterial;
        yield return new WaitForSeconds(0.2f);
        material.material = SnowBlockManager.instance.red;
        yield return new WaitForSeconds(0.1f);
        material.material = SnowBlockManager.instance.defaultMaterial;
        yield return new WaitForSeconds(0.1f);
        material.material = SnowBlockManager.instance.red;
        yield return new WaitForSeconds(0.05f);
        material.material = SnowBlockManager.instance.defaultMaterial;
        yield return new WaitForSeconds(0.05f);
        material.material = SnowBlockManager.instance.red;
        yield return new WaitForSeconds(0.03f);
        material.material = SnowBlockManager.instance.defaultMaterial;
        yield return new WaitForSeconds(0.03f);
        material.material = SnowBlockManager.instance.red;
        yield return new WaitForSeconds(0.01f);
        snowBlock.transform.position += new Vector3(0, -10, 0);
    }
}

public enum Type
{
    Interior,
    Edge,
    DroppableEdge,
    Fallen,
    Boundary
}