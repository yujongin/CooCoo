using UnityEngine;

public class Bomb : MonoBehaviour
{
    private BombSpawner spawner;
    private Rigidbody rb;
    private bool hasLanded = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        hasLanded = false;
    }

    void Update()
    {
        // 폭탄이 땅에 떨어졌는지 확인
        if (!hasLanded && transform.position.y < 2.5f)
        {
            hasLanded = true;
            OnLand();
        }
    }

    /// <summary>
    /// 폭탄이 무언가와 충돌했을 때 호출
    /// 플레이어와 부딪히면 게임 오버 처리
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter(Collision collision)
    {
        if (hasLanded)
        {
            return;
        }

        // 플레이어와 충돌하면 게임 오버
        if (collision.collider.CompareTag("Player"))
        {
            hasLanded = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }

            ReturnToPool();
        }
    }

    /// <summary>
    /// 스포너 참조 설정
    /// </summary>
    public void SetSpawner(BombSpawner spawner)
    {
        this.spawner = spawner;
    }

    /// <summary>
    /// 폭탄이 땅에 떨어졌을 때 호출
    /// </summary>
    private void OnLand()
    {
        // 여기에 폭탄 폭발 로직 추가 가능
        // 예: 이펙트, 데미지 등
        
        // 일정 시간 후 풀로 반환 (또는 즉시 반환)
        // Invoke(nameof(ReturnToPool), 2f);
        ReturnToPool();
    }

    /// <summary>
    /// 풀로 반환
    /// </summary>
    private void ReturnToPool()
    {
        if (spawner != null)
        {
            // Rigidbody 초기화
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            hasLanded = false;
            spawner.ReturnBombToPool(gameObject);
        }
        else
        {
            // 스포너가 없으면 파괴
            Destroy(gameObject);
        }
    }
}
