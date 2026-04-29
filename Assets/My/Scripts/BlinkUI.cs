using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class BlinkUI : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private float fadeDuration;
    private float elapsedTime;

    /// <summary>
    /// 컴포넌트 초기화 및 의존성 해결.
    /// </summary>
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (!canvasGroup)
        {
            Debug.LogWarning("CanvasGroup 누락. 자동 추가됨.");
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        fadeDuration = 1f;
    }

    /// <summary>
    /// 매 프레임 알파값 갱신.
    /// 코루틴 오버헤드 대체를 위한 상태 변경.
    /// </summary>
    private void Update()
    {
        // 극단적 최적화 대상인 Update 루프 내부이므로 ReferenceEquals 사용.
        if (object.ReferenceEquals(canvasGroup, null))
        {
            return;
        }

        // TODO: Time.timeScale이 0일 때 깜빡임을 유지해야 한다면 unscaledDeltaTime으로 변경할 것.
        elapsedTime += Time.deltaTime;
        
        // 프레임 변동에 독립적인 수학적 왕복 계산.
        // elapsedTime=0.0 -> PingPong=0.0 -> 결과=1.0 (시작 시)
        // elapsedTime=1.0 -> PingPong=1.0 -> 결과=0.0 (페이드 아웃 완료)
        canvasGroup.alpha = 1f - (Mathf.PingPong(elapsedTime, fadeDuration) / fadeDuration);
    }

    /// <summary>
    /// 객체 파괴 시 참조 해제.
    /// </summary>
    private void OnDestroy()
    {
        // 가비지 컬렉터 수집 명시적 유도.
        if (canvasGroup)
        {
            canvasGroup = null;
        }
    }
}