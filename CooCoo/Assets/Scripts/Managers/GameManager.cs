using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum GameState
{
    Ready,      // 게임 시작 전 (시작 버튼 대기)
    Playing,    // 게임 진행 중
    GameOver    // 게임 오버
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private GameObject startButton; // 시작 버튼 UI (Inspector에서 할당)
    
    // 게임 오버 UI
    [SerializeField] private GameObject gameOverPanel; // 게임 오버 패널 (Inspector에서 할당)
    [SerializeField] private Button restartButton; // 다시하기 버튼 (Inspector에서 할당)

    // 게임 오브젝트 참조 (Inspector에서 할당)
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform enemyTransform;
    
    // 초기 위치 저장 (재시작 시 사용)
    private Vector3 initialPlayerPosition;
    private Vector3 initialEnemyPosition;

    // 적과 플레이어의 z값 거리 차이가 이 값 이하가 되면 게임 오버
    [SerializeField] private float gameOverZDistanceThreshold = 6f;
    
    private int score = 0;
    private int bestScore = 0; // 최고 기록
    private const int SCORE_INCREMENT_VALUE = 10; // z+ 방향으로 3씩 이동할 때마다 점수 10씩 증가/감소

    private GameState gameState = GameState.Ready;
    public bool IsPlaying => gameState == GameState.Playing;
    
    // 게임 시작 후 입력 무시 시간 (버튼 클릭 입력이 게임 입력으로 전달되는 것을 방지)
    private float inputIgnoreTime = 0.2f;
    private float gameStartTime = 0f;
    public bool ShouldIgnoreInput => Time.time - gameStartTime < inputIgnoreTime;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateScoreText();
        
        // 초기 위치 저장
        if (playerTransform != null)
        {
            initialPlayerPosition = playerTransform.position;
        }
        if (enemyTransform != null)
        {
            initialEnemyPosition = enemyTransform.position;
        }
        
        // 게임 오버 UI 초기에는 비활성화
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    void Update()
    {
        // 게임 진행 중일 때만 거리 체크
        if (!IsPlaying)
        {
            return;
        }

        // 플레이어/적 Transform이 할당되어 있지 않으면 아무 것도 하지 않음
        if (playerTransform == null || enemyTransform == null)
        {
            return;
        }

        // z 값 거리 차이 체크
        float playerZ = playerTransform.position.z;
        float enemyZ = enemyTransform.position.z;
        float distanceZ = Mathf.Abs(playerZ - enemyZ);

        if (distanceZ <= gameOverZDistanceThreshold)
        {
            GameOver();
        }
    }

    /// <summary>
    /// UI의 시작 버튼에서 호출될 메서드
    /// </summary>
    public void StartGame()
    {
        if (gameState != GameState.Ready)
        {
            return;
        }

        gameState = GameState.Playing;
        gameStartTime = Time.time; // 게임 시작 시간 기록
        
        // 시작 버튼 비활성화
        if (startButton != null)
        {
            startButton.SetActive(false);
        }
        
        // 필요하면 여기서 추가 초기화 로직(예: 위치 리셋)도 가능
    }

    public void GameOver()
    {
        if (gameState == GameState.GameOver)
        {
            return;
        }

        gameState = GameState.GameOver;
        
        // 게임 오버 UI 패널 표시
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// 다시하기 버튼에서 호출될 메서드
    /// 게임을 초기 상태로 리셋하고 재시작
    /// </summary>
    public void RestartGame()
    {
        // 점수 초기화
        score = 0;
        bestScore = 0;
        UpdateScoreText();
        
        // 게임 상태를 Ready로 변경
        gameState = GameState.Ready;
        
        // 게임 오버 UI 숨기기
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // 다시하기 버튼 비활성화
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
        }
        
        // 플레이어와 적 위치 리셋
        if (playerTransform != null)
        {
            playerTransform.position = initialPlayerPosition;
        }
        if (enemyTransform != null)
        {
            enemyTransform.position = initialEnemyPosition;
        }
        
        // 시작 버튼 다시 표시
        if (startButton != null)
        {
            startButton.SetActive(true);
        }
        
        // TODO: 활성 폭탄 정리, 코루틴 정리 등 추가 정리 작업이 필요할 수 있음
    }

    /// <summary>
    /// 플레이어가 z+ 방향으로 이동했을 때 호출되는 메서드
    /// </summary>
    public void OnPlayerMovedForward()
    {
        AddScore(SCORE_INCREMENT_VALUE);
    }

    /// <summary>
    /// 플레이어가 z- 방향으로 이동했을 때 호출되는 메서드
    /// </summary>
    public void OnPlayerMovedBackward()
    {
        AddScore(-SCORE_INCREMENT_VALUE);
    }

    public void AddScore(int points)
    {
        score += points;
        
        // 최고 기록 업데이트
        if (score > bestScore)
        {
            bestScore = score;
        }
        
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            // 최고 기록만 표시
            scoreText.text = bestScore.ToString() + "m";
        }
    }
}
