using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [SerializeField] private CanvasGroup canvasGroup;
    
    private Transform originalParent;
    private RectTransform rectTransform;
    private Canvas rootCanvas;

    // 제스처 관련 변수
    private bool isPlacedInDrawZone = false;
    private float initialPinchDistance;
    private Vector3 initialScale;
    private float initialAngle;
    private Vector3 initialRotation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        // 생성 시점의 부모(그리드)를 영구 기억
        originalParent = transform.parent; 
    }

    private void Update()
    {
        if (!isPlacedInDrawZone) return;

        // 두 개 이상의 터치가 있을 때만 제스처 처리
        if (Input.touchCount >= 2)
        {
            HandleMultiTouchGesture();
        }
    }

    /**
     * @description 두 손가락의 거리 변화와 각도 변화를 계산하여 트랜스폼에 반영함.
     */
    private void HandleMultiTouchGesture()
    {
        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        // 이전 프레임의 터치 위치 계산
        Vector2 t0Prev = t0.position - t0.deltaPosition;
        Vector2 t1Prev = t1.position - t1.deltaPosition;

        // 현재 벡터와 이전 벡터 계산
        Vector2 prevDir = t1Prev - t0Prev;
        Vector2 currDir = t1.position - t0.position;

        // --- 1. 확대/축소 (Pinch Zoom) ---
        float prevMag = prevDir.magnitude;
        float currMag = currDir.magnitude;

        // 초기 거리와 현재 거리 비율로 스케일 조정
        if (t1.phase == TouchPhase.Began)
        {
            initialPinchDistance = currMag;
            initialScale = transform.localScale;
        }
        else if (initialPinchDistance > 0f)
        {
            float scaleFactor = currMag / initialPinchDistance;
            transform.localScale = initialScale * scaleFactor;
        }

        // --- 2. 회전 (Rotation) ---
        if (t1.phase == TouchPhase.Began)
        {
            initialAngle = Vector2.SignedAngle(Vector2.up, currDir);
            initialRotation = transform.eulerAngles;
        }
        else
        {
            float currAngle = Vector2.SignedAngle(Vector2.up, currDir);
            float angleDiff = currAngle - initialAngle;
            
            // localEulerAngles를 사용하여 정확한 회전값 적용
            Vector3 newRotation = initialRotation;
            newRotation.z += angleDiff;
            transform.localEulerAngles = newRotation;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 제스처 조작 시작 시 최상단으로 이동
        if (isPlacedInDrawZone)
        {
            transform.SetAsLastSibling();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 이미 배치된 아이템은 제스처 모드에서만 동작하도록 드래그 차단
        if (isPlacedInDrawZone) return; 

        if (rootCanvas)
        {
            transform.SetParent(rootCanvas.transform);
            transform.SetAsLastSibling();
        }

        if (canvasGroup)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.6f;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPlacedInDrawZone) return; // 배치된 후엔 일반 드래그 차단

        Vector3 worldPoint;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out worldPoint))
        {
            rectTransform.position = worldPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlacedInDrawZone) return; // 배치된 후엔 EndDrag 처리 불필요

        if (canvasGroup)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        bool isOverDrawZone = false;
        foreach (RaycastResult result in raycastResults)
        {
            if (result.gameObject && result.gameObject.CompareTag("DrawZone"))
            {
                isOverDrawZone = true;
                break;
            }
        }

        if (isOverDrawZone)
        {
            // 도화지에 안착 성공 -> 제스처 기능 활성화
            isPlacedInDrawZone = true;
            Debug.Log("Item placed & gesture activated.");
        }
        else
        {
            // 도화지 밖이면 원래 그리드로 복귀
            transform.SetParent(originalParent);
        }
    }
}