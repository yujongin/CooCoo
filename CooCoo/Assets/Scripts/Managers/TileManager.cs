using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private GameObject tilePrefab; // 타일 프리팹
    [SerializeField] private float tileSpacing = 3f; // 타일 간격 (플레이어 stepSize와 동일하게 설정)
    [SerializeField] private int tilesAhead = 8; // 플레이어 앞에 유지할 타일 개수
    [SerializeField] private int tilesBehind = 5; // 플레이어 뒤에 유지할 타일 개수
    [SerializeField] private int initialPoolSize = 20; // 초기 풀 크기
    
    // 오브젝트 풀링
    private Queue<GameObject> tilePool = new Queue<GameObject>(); // 사용 가능한 타일 풀
    private Dictionary<float, GameObject> activeTiles = new Dictionary<float, GameObject>(); // z 위치를 키로 하는 활성 타일
    
    private float previousPlayerZ;
    private Transform poolParent; // 풀링된 오브젝트의 부모

    void Start()
    {
        // 플레이어를 찾지 못했다면 자동으로 찾기
        if (player == null)
        {
            PlayerController playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                player = playerController.transform;
            }
        }

        // 풀 부모 오브젝트 생성
        poolParent = new GameObject("TilePool").transform;
        poolParent.SetParent(transform);

        // 초기 풀 생성
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject tile = CreateNewTile();
            ReturnTileToPool(tile);
        }

        // 초기 타일 배치
        if (player != null)
        {
            previousPlayerZ = player.position.z;
            InitializeTiles();
        }
    }

    void Update()
    {
        if (player == null) return;

        float currentPlayerZ = player.position.z;
        float zDelta = currentPlayerZ - previousPlayerZ;
        
        // 플레이어가 z 방향으로 이동했는지 확인
        if (Mathf.Abs(zDelta) >= tileSpacing * 0.9f)
        {
            UpdateTiles();
            previousPlayerZ = currentPlayerZ;
        }
    }

    /// <summary>
    /// 초기 타일 배치
    /// </summary>
    private void InitializeTiles()
    {
        float playerZ = player.position.z;
        
        // 플레이어 뒤부터 앞까지 타일 배치
        for (int i = -tilesBehind; i <= tilesAhead; i++)
        {
            float tileZ = playerZ + (i * tileSpacing);
            SpawnTileAt(tileZ);
        }
    }

    /// <summary>
    /// 플레이어 위치에 따라 타일 업데이트
    /// </summary>
    private void UpdateTiles()
    {
        float playerZ = player.position.z;
        float minZ = playerZ - (tilesBehind * tileSpacing);
        float maxZ = playerZ + (tilesAhead * tileSpacing);
        
        // 범위를 벗어난 타일 제거
        List<float> tilesToRemove = new List<float>();
        foreach (var kvp in activeTiles)
        {
            if (kvp.Key < minZ || kvp.Key > maxZ)
            {
                tilesToRemove.Add(kvp.Key);
            }
        }
        
        foreach (float z in tilesToRemove)
        {
            ReturnTileToPool(activeTiles[z]);
            activeTiles.Remove(z);
        }
        
        // 앞쪽에 타일이 부족하면 생성
        float currentMaxZ = GetMaxActiveTileZ();
        while (currentMaxZ < maxZ)
        {
            currentMaxZ += tileSpacing;
            SpawnTileAt(currentMaxZ);
        }
        
        // 뒤쪽에 타일이 부족하면 생성 (뒤로 이동한 경우)
        float currentMinZ = GetMinActiveTileZ();
        while (currentMinZ > minZ)
        {
            currentMinZ -= tileSpacing;
            SpawnTileAt(currentMinZ);
        }
    }

    /// <summary>
    /// 특정 z 위치에 타일 생성
    /// </summary>
    private void SpawnTileAt(float z)
    {
        // 이미 해당 위치에 타일이 있으면 스킵
        if (activeTiles.ContainsKey(z))
            return;
        
        GameObject tile = GetTileFromPool();
        Vector3 position = tile.transform.position;
        position.z = z;
        tile.transform.position = position;
        tile.SetActive(true);
        
        activeTiles[z] = tile;
    }

    /// <summary>
    /// 풀에서 타일 가져오기
    /// </summary>
    private GameObject GetTileFromPool()
    {
        GameObject tile;
        
        if (tilePool.Count > 0)
        {
            tile = tilePool.Dequeue();
        }
        else
        {
            // 풀이 비어있으면 새로 생성
            tile = CreateNewTile();
        }
        
        return tile;
    }

    /// <summary>
    /// 타일을 풀로 반환
    /// </summary>
    private void ReturnTileToPool(GameObject tile)
    {
        tile.SetActive(false);
        tile.transform.SetParent(poolParent);
        tilePool.Enqueue(tile);
    }

    /// <summary>
    /// 새 타일 생성
    /// </summary>
    private GameObject CreateNewTile()
    {
        GameObject tile;
        
        if (tilePrefab != null)
        {
            tile = Instantiate(tilePrefab, poolParent);
        }
        else
        {
            // 프리팹이 없으면 기본 큐브 생성 (임시)
            tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.transform.SetParent(poolParent);
            tile.name = "RunningTile";
        }
        
        tile.SetActive(false);
        return tile;
    }

    /// <summary>
    /// 활성 타일 중 가장 큰 z 값 반환
    /// </summary>
    private float GetMaxActiveTileZ()
    {
        if (activeTiles.Count == 0) return player.position.z;
        
        float maxZ = float.MinValue;
        foreach (float z in activeTiles.Keys)
        {
            if (z > maxZ)
                maxZ = z;
        }
        return maxZ;
    }

    /// <summary>
    /// 활성 타일 중 가장 작은 z 값 반환
    /// </summary>
    private float GetMinActiveTileZ()
    {
        if (activeTiles.Count == 0) return player.position.z;
        
        float minZ = float.MaxValue;
        foreach (float z in activeTiles.Keys)
        {
            if (z < minZ)
                minZ = z;
        }
        return minZ;
    }
}
