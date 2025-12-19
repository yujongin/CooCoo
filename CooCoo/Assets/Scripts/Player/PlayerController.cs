using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // 한 칸 이동 크기 (격자 크기)
    [SerializeField] private float stepSize = 3f;

    // 터치 / 슬라이드 입력 관련 변수
    private Vector2 touchStartPos;
    private Vector2 touchEndPos;

    // 슬라이드로 인식할 최소 거리 (픽셀 단위)
    [SerializeField] private float swipeThreshold = 50f;

    // 이동 시간 (초 단위)
    [SerializeField] private float moveDuration = 0.3f;

    // 이동 중인지 확인하는 플래그
    private bool isMoving = false;

    void Start()
    {
        
    }

    void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// 터치 / 마우스 입력을 감지해서 슬라이드 시작/끝을 기록한다.
    /// (새 Input System 사용)
    /// </summary>
    private void HandleInput()
    {
        // 게임이 진행 중이 아닐 때는 입력 무시
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
        {
            return;
        }
        
        // 게임 시작 직후 짧은 시간 동안 입력 무시 (버튼 클릭 입력이 게임 입력으로 전달되는 것을 방지)
        if (GameManager.Instance.ShouldIgnoreInput)
        {
            return;
        }

        // 모바일 터치 입력 (새 Input System)
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                // UI 위에서 터치했는지 확인
                if (IsPointerOverUI(touch.position.ReadValue()))
                {
                    return;
                }
                touchStartPos = touch.position.ReadValue();
            }
            else if (touch.press.wasReleasedThisFrame)
            {
                // UI 위에서 터치를 떼었는지 확인
                if (IsPointerOverUI(touch.position.ReadValue()))
                {
                    return;
                }
                touchEndPos = touch.position.ReadValue();
                OnSwipe(touchStartPos, touchEndPos);
            }

            // 터치가 연결돼 있으면 마우스 입력은 굳이 볼 필요 없음
            return;
        }

        // 에디터/PC용 마우스 입력 (새 Input System, 테스트 용도)
        if (Mouse.current != null)
        {
            var mouse = Mouse.current;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                // UI 위에서 클릭했는지 확인
                if (IsPointerOverUI(mouse.position.ReadValue()))
                {
                    return;
                }
                touchStartPos = mouse.position.ReadValue();
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                // UI 위에서 클릭을 떼었는지 확인
                if (IsPointerOverUI(mouse.position.ReadValue()))
                {
                    return;
                }
                touchEndPos = mouse.position.ReadValue();
                OnSwipe(touchStartPos, touchEndPos);
            }
        }
    }

    /// <summary>
    /// 특정 스크린 좌표가 UI 위에 있는지 확인
    /// </summary>
    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        // EventSystem이 없으면 UI 체크 불가 (false 반환)
        if (EventSystem.current == null)
        {
            return false;
        }

        // 새 Input System을 사용하는 경우
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;

        // GraphicRaycaster를 사용하여 UI 요소 확인
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // UI 요소가 하나라도 있으면 true
        return results.Count > 0;
    }

    /// <summary>
    /// 슬라이드(또는 탭)가 끝났을 때 호출된다.
    /// 여기서 방향을 판정하고, 해당 방향으로 한 칸 이동시킨다.
    /// </summary>
    /// <param name="start">슬라이드 시작 위치(스크린 좌표)</param>
    /// <param name="end">슬라이드 끝 위치(스크린 좌표)</param>
    private void OnSwipe(Vector2 start, Vector2 end)
    {
        // 이동 중이면 새로운 입력 무시
        if (isMoving)
        {
            return;
        }
        // Debug.Log("OnSwipe");
        Vector3 moveDir = GetMoveDirectionFromSwipe(start, end);

        // 움직일 방향이 0이 아니면 한 칸 이동
        if (moveDir != Vector3.zero)
        {
            StartCoroutine(MoveCoroutine(moveDir));
        }
    }

    /// <summary>
    /// 슬라이드 방향(또는 탭)을 기반으로 월드 좌표 이동 방향을 계산한다.
    /// - 탭 또는 매우 짧은 슬라이드: z+ 방향 (앞으로 한 칸)
    /// - 위로 슬라이드: z+
    /// - 아래로 슬라이드: z-
    /// - 오른쪽 슬라이드: x+
    /// - 왼쪽 슬라이드: x-
    /// </summary>
    private Vector3 GetMoveDirectionFromSwipe(Vector2 start, Vector2 end)
    {
        Vector2 delta = end - start;

        // 탭 또는 너무 짧은 슬라이드 → 위로 슬라이드와 동일하게 z+
        if (delta.magnitude < swipeThreshold)
        {
            return new Vector3(0f, 0f, 1f);
        }

        // 가로/세로 중 더 큰 축으로 방향 결정
        if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
        {
            // 세로 슬라이드
            if (delta.y > 0f)
            {
                // 위로 슬라이드 → z+
                return new Vector3(0f, 0f, 1f);
            }
            else
            {
                // 아래로 슬라이드 → z-
                return new Vector3(0f, 0f, -1f);
            }
        }
        else
        {
            // 가로 슬라이드
            if (delta.x > 0f)
            {
                // 오른쪽 슬라이드 → x+
                return new Vector3(1f, 0f, 0f);
            }
            else
            {
                // 왼쪽 슬라이드 → x-
                return new Vector3(-1f, 0f, 0f);
            }
        }
    }

    /// <summary>
    /// 주어진 방향으로 stepSize만큼 부드럽게 한 칸 이동한다. (코루틴)
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
        
        // 점프 높이 (선택적으로 조정 가능)
        float jumpHeight = 1f;
        
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
        
        // 이동 방향에 따라 GameManager에 알림
        if (GameManager.Instance != null)
        {
            if (direction.z > 0f)
            {
                // z+ 방향으로 이동
                GameManager.Instance.OnPlayerMovedForward();
            }
            else if (direction.z < 0f)
            {
                // z- 방향으로 이동
                GameManager.Instance.OnPlayerMovedBackward();
            }
        }
    }
}
