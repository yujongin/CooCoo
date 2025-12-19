using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float stepSize = 3f; // 한 칸 이동 크기 (플레이어와 동일)
    [SerializeField] private float moveDuration = 0.3f; // 이동 시간 (플레이어와 동일)
    [SerializeField] private float moveInterval = 1f; // 이동 간격 (1초)
    [SerializeField] private float jumpHeight = 1f; // 점프 높이 (플레이어와 동일)
    
    private bool isMoving = false;
    private Coroutine moveCoroutine;

    void Start()
    {
        // 1초마다 자동으로 z+ 방향으로 이동 시작
        StartCoroutine(AutoMoveCoroutine());
    }

    void Update()
    {
        
    }

    /// <summary>
    /// 1초마다 자동으로 z+ 방향으로 이동하는 코루틴
    /// </summary>
    private IEnumerator AutoMoveCoroutine()
    {
        while (true)
        {
            // 게임이 진행 중이 아닐 때는 대기
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                yield return null;
                continue;
            }

            yield return new WaitForSeconds(moveInterval);
            
            // 이동 중이 아니면 z+ 방향으로 이동
            if (!isMoving)
            {
                Vector3 direction = new Vector3(0f, 0f, 1f); // z+ 방향
                moveCoroutine = StartCoroutine(MoveCoroutine(direction));
            }
        }
    }

    /// <summary>
    /// 주어진 방향으로 stepSize만큼 부드럽게 한 칸 이동한다. (플레이어와 동일한 움직임)
    /// 점프하는 것처럼 Y축도 약간 올라갔다가 내려오는 효과를 추가한다.
    /// </summary>
    private IEnumerator MoveCoroutine(Vector3 direction)
    {
        isMoving = true;

        // 이동 방향을 바라보도록 회전
        Vector3 lookDir = new Vector3(direction.x, 0f, direction.z);
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
        }

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + direction.normalized * stepSize;
        
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveDuration;
            
            // 부드러운 이동을 위한 easing (EaseOutQuad 같은 효과)
            float easedT = 1f - (1f - t) * (1f - t);
            
            // X, Z축은 선형 보간
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, easedT);
            
            // Y축은 포물선 운동 (점프 효과)
            float yOffset = jumpHeight * 4f * t * (1f - t); // 포물선 공식
            currentPosition.y = startPosition.y + yOffset;
            
            transform.position = currentPosition;
            
            yield return null;
        }

        // 정확한 목표 위치로 설정
        transform.position = targetPosition;
        isMoving = false;
    }
}
