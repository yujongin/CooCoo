using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombSpawner : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private GameObject bombPrefab; // 폭탄 프리팹
    [SerializeField] private float spawnInterval = 1f; // 스폰 간격 (1초)
    [SerializeField] private float stepSize = 3f; // 한 칸 크기 (플레이어 stepSize와 동일)
    [SerializeField] private float launchAngle = 45f; // 발사 각도 (도 단위)
    [SerializeField] private float flightTimeMultiplier = 0.5f; // 비행 시간 배수 (작을수록 빠름, 기본 0.5 = 2배 빠름)
    [SerializeField] private int initialPoolSize = 10; // 초기 풀 크기
    
    // 오브젝트 풀링
    private Queue<GameObject> bombPool = new Queue<GameObject>(); // 사용 가능한 폭탄 풀
    private List<GameObject> activeBombs = new List<GameObject>(); // 활성 폭탄 리스트
    private Transform poolParent; // 풀링된 오브젝트의 부모
    
    // 목표 위치 표시용
    private GameObject targetIndicator; // 목표 위치 표시 큐브

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
        poolParent = new GameObject("BombPool").transform;
        poolParent.SetParent(transform);

        // 초기 풀 생성
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject bomb = CreateNewBomb();
            ReturnBombToPool(bomb);
        }

        // 1초마다 폭탄 스폰 시작
        StartCoroutine(SpawnBombCoroutine());
    }

    void Update()
    {
        
    }

    /// <summary>
    /// 1초마다 폭탄을 스폰하는 코루틴
    /// </summary>
    private IEnumerator SpawnBombCoroutine()
    {
        while (true)
        {
            // 게임이 진행 중이 아닐 때는 대기
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                yield return null;
                continue;
            }

            yield return new WaitForSeconds(spawnInterval);
            
            if (player != null)
            {
                SpawnBomb();
            }
        }
    }

    /// <summary>
    /// 적의 위치에서 플레이어 방향으로 폭탄 스폰
    /// </summary>
    private void SpawnBomb()
    {
        // 적 찾기 (한 명만)
        EnemyController enemy = FindFirstObjectByType<EnemyController>();
        
        if (enemy == null)
        {
            return; // 적이 없으면 스폰하지 않음
        }
        
        SpawnBombFromEnemy(enemy.transform);
    }

    /// <summary>
    /// 특정 적의 위치에서 플레이어 방향으로 폭탄 스폰 (포물선 운동)
    /// </summary>
    private void SpawnBombFromEnemy(Transform enemy)
    {
        // 적에서 플레이어로의 방향 계산
        Vector3 directionToPlayer = (player.position - enemy.position);
        directionToPlayer.y = 0; // Y축은 0으로 (수평 방향만)
        Vector3 horizontalDirection = directionToPlayer.normalized;
        
        // 1칸 또는 2칸 앞 (랜덤 선택)
        int distance = Random.Range(1, 3); // 1 또는 2
        float horizontalDistance = distance * stepSize;
        
        // 목표 지점 계산 (플레이어 방향으로 거리만큼 앞)
        Vector3 targetPosition = player.position + horizontalDirection * horizontalDistance;
        targetPosition.y = player.position.y; // 플레이어와 같은 높이
        
        // 목표 위치에 빨간 큐브 표시
        ShowTargetIndicator(targetPosition);
        
        // 적의 위치에서 폭탄 스폰
        Vector3 spawnPosition = enemy.position;
        spawnPosition.y += 1f; // 적의 머리 위에서 발사
        
        // 풀에서 폭탄 가져오기
        GameObject bomb = GetBombFromPool();
        bomb.transform.position = spawnPosition;
        bomb.transform.rotation = Quaternion.identity;
        bomb.SetActive(true);
        
        // Rigidbody 가져오기
        Rigidbody rb = bomb.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 포물선 운동을 위한 초기 속도 계산 (비행 시간 조절 포함)
            Vector3 velocity = CalculateLaunchVelocityWithTime(spawnPosition, targetPosition, launchAngle, flightTimeMultiplier);
            rb.linearVelocity = velocity;
            rb.angularVelocity = Vector3.zero;
        }
        
        activeBombs.Add(bomb);
    }

    /// <summary>
    /// 포물선 운동을 위한 초기 속도 계산 (비행 시간 조절 포함)
    /// 목표 위치에 정확히 도달하면서도 속도를 조절할 수 있음
    /// </summary>
    private Vector3 CalculateLaunchVelocityWithTime(Vector3 startPos, Vector3 targetPos, float angle, float timeMultiplier)
    {
        // 수평 거리와 방향 계산
        Vector3 toTarget = targetPos - startPos;
        Vector3 horizontalDirection = toTarget;
        horizontalDirection.y = 0;
        float horizontalDistance = horizontalDirection.magnitude;
        
        if (horizontalDistance < 0.1f)
        {
            // 거리가 너무 가까우면 위로만 던지기
            return new Vector3(0, 10f, 0);
        }
        
        // 수직 거리 계산
        float verticalDistance = targetPos.y - startPos.y;
        
        // 중력 가속도
        float gravity = Mathf.Abs(Physics.gravity.y);
        
        // 먼저 기본 비행 시간 계산 (각도를 고려한 근사치)
        // 발사 각도를 라디안으로 변환
        float angleRad = angle * Mathf.Deg2Rad;
        float cosAngle = Mathf.Cos(angleRad);
        float sinAngle = Mathf.Sin(angleRad);
        
        // 기본 속도 계산 (각도를 고려)
        float sin2Angle = Mathf.Sin(2f * angleRad);
        float cos2Angle = cosAngle * cosAngle;
        float denominator = horizontalDistance * sin2Angle - 2f * verticalDistance * cos2Angle;
        
        float baseVelocity;
        if (Mathf.Abs(denominator) < 0.01f || denominator <= 0)
        {
            baseVelocity = Mathf.Sqrt(gravity * horizontalDistance / sin2Angle);
        }
        else
        {
            float velocitySquared = gravity * horizontalDistance * horizontalDistance / denominator;
            baseVelocity = Mathf.Sqrt(Mathf.Max(0, velocitySquared));
        }
        
        // 기본 비행 시간 계산: t = d / (v0 * cos(θ))
        float baseFlightTime = horizontalDistance / (baseVelocity * cosAngle);
        
        // 비행 시간 배수 적용 (작을수록 빠름)
        float flightTime = baseFlightTime * timeMultiplier;
        
        // 목표 위치에 정확히 도달하도록 속도 재계산
        // 수평: v0x = d / t
        // 수직: v0y = (h + 0.5 * g * t^2) / t
        float horizontalVelocity = horizontalDistance / flightTime;
        float verticalVelocity = (verticalDistance + 0.5f * gravity * flightTime * flightTime) / flightTime;
        
        // 수평 방향 정규화
        Vector3 horizontalDir = horizontalDirection.normalized;
        
        // 초기 속도 벡터 계산
        Vector3 velocity = horizontalDir * horizontalVelocity;
        velocity.y = verticalVelocity;
        
        return velocity;
    }

    /// <summary>
    /// 풀에서 폭탄 가져오기
    /// </summary>
    private GameObject GetBombFromPool()
    {
        GameObject bomb;
        
        if (bombPool.Count > 0)
        {
            bomb = bombPool.Dequeue();
        }
        else
        {
            // 풀이 비어있으면 새로 생성
            bomb = CreateNewBomb();
        }
        
        return bomb;
    }

    /// <summary>
    /// 폭탄을 풀로 반환
    /// </summary>
    public void ReturnBombToPool(GameObject bomb)
    {
        if (bomb == null) return;
        
        bomb.SetActive(false);
        bomb.transform.SetParent(poolParent);
        
        if (activeBombs.Contains(bomb))
        {
            activeBombs.Remove(bomb);
        }
        
        bombPool.Enqueue(bomb);
    }

    /// <summary>
    /// 새 폭탄 생성
    /// </summary>
    private GameObject CreateNewBomb()
    {
        GameObject bomb;
        
        if (bombPrefab != null)
        {
            bomb = Instantiate(bombPrefab, poolParent);
        }
        else
        {
            // 프리팹이 없으면 기본 구 생성 (임시)
            bomb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bomb.transform.SetParent(poolParent);
            bomb.name = "Bomb";
            
            // Rigidbody 추가 (떨어지는 효과를 위해)
            Rigidbody rb = bomb.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = bomb.AddComponent<Rigidbody>();
            }
        }
        
        // Bomb 컴포넌트에 스포너 참조 전달
        Bomb bombScript = bomb.GetComponent<Bomb>();
        if (bombScript != null)
        {
            bombScript.SetSpawner(this);
        }
        
        bomb.SetActive(false);
        return bomb;
    }

    /// <summary>
    /// 목표 위치에 빨간 큐브 표시
    /// </summary>
    private void ShowTargetIndicator(Vector3 position)
    {
        // 기존 표시기가 있으면 제거
        if (targetIndicator != null)
        {
            Destroy(targetIndicator);
        }
        
        // 빨간 큐브 생성
        targetIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        targetIndicator.name = "TargetIndicator";
        targetIndicator.transform.position = position;
        targetIndicator.transform.localScale = Vector3.one * 0.5f; // 작은 큐브
        
        // 빨간색 머티리얼 설정
        Renderer renderer = targetIndicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material redMaterial = new Material(Shader.Find("Standard"));
            redMaterial.color = Color.red;
            renderer.material = redMaterial;
        }
        
        // Collider 제거 (시각적 표시만)
        Collider collider = targetIndicator.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }
}
