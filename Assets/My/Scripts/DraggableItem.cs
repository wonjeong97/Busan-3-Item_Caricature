using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{   
    private static DraggableItem focusedItem;
    
    [SerializeField] private CanvasGroup canvasGroup;
    
    private Transform originalParent;
    private RectTransform rectTransform;
    private Canvas rootCanvas;

   // 제스처 관련 변수
    private bool isPlacedInDrawZone;
    private bool isGestureActive;
    private float initialPinchDistance;
    private Vector3 initialScale;
    private float initialAngle;
    private Vector3 initialRotation;
    
    // 롱 프레스 감지를 위한 변수
    private float pointerDownTimer;
    private bool isPointerDown;
    private bool hasLongPressed;
    private const float HoldTimeThreshold = 1.0f;
    
    private const float MinScale = 0.5f;
    private const float MaxScale = 8.0f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        // 생성 시점의 부모(그리드)를 영구 기억
        originalParent = transform.parent; 
    }

    private void Update()
    {
        /**
         * @description 도화지에 배치된 아이템의 제스처 및 롱프레스를 처리함.
         * 충돌 검사 대신 정적 포커스 락을 사용하여, 드래그 중인 아이템 뒤의 객체가 반응하는 멀티터치 버그를 차단함.
         */
        if (!isPlacedInDrawZone)
        {
            return;
        }

        if (isPointerDown && !hasLongPressed)
        {
            pointerDownTimer += Time.deltaTime;
            
            if (pointerDownTimer >= HoldTimeThreshold)
            {
                hasLongPressed = true;
                transform.SetAsFirstSibling();
            }
        }

        // 자신이 현재 포커스된 단 하나의 아이템이 아니라면 제스처 연산을 무시함
        if (focusedItem != this)
        {
            isGestureActive = false;
            return;
        }

        if (Input.touchCount >= 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (t1.phase == TouchPhase.Began)
            {
                // 포커스를 가진 상태에서 두 번째 터치가 감지되면 즉시 제스처를 활성화함
                isGestureActive = true;
                
                Vector2 currDir = t1.position - t0.position;
                initialPinchDistance = currDir.magnitude;
                initialScale = transform.localScale;
                initialAngle = Vector2.SignedAngle(Vector2.up, currDir);
                initialRotation = transform.localEulerAngles;
                
                transform.SetAsLastSibling();
            }

            if (isGestureActive)
            {
                Vector2 currDir = t1.position - t0.position;
                
                if (initialPinchDistance > 0f)
                {
                    float scaleFactor = currDir.magnitude / initialPinchDistance;
                    Vector3 targetScale = initialScale * scaleFactor;
                    
                    targetScale.x = Mathf.Clamp(targetScale.x, MinScale, MaxScale);
                    targetScale.y = Mathf.Clamp(targetScale.y, MinScale, MaxScale);
                    targetScale.z = Mathf.Clamp(targetScale.z, MinScale, MaxScale);
                    
                    transform.localScale = targetScale;
                }

                float currAngle = Vector2.SignedAngle(Vector2.up, currDir);
                float angleDiff = currAngle - initialAngle;
                
                Vector3 newRotation = initialRotation;
                newRotation.z += angleDiff;
                transform.localEulerAngles = newRotation;
            }
            
            if (t0.phase == TouchPhase.Ended || t0.phase == TouchPhase.Canceled ||
                t1.phase == TouchPhase.Ended || t1.phase == TouchPhase.Canceled)
            {
                isGestureActive = false;
            }
        }
        else
        {
            isGestureActive = false;
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
    
    private bool IsTopmostItem(Vector2 screenPosition)
    {
        /**
         * @description 특정 화면 좌표에서 이 객체가 UI 계층의 최상단에 있는지 검사하여 중복 제스처 인식을 방지함.
         * @param screenPosition 검사할 터치 화면 좌표.
         * @return 최상단 객체일 경우 true 반환.
         */
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // UGUI의 Raycast는 화면에 가장 가깝게 그려지는(최상단) 객체부터 순서대로 리스트를 반환함
        if (results.Count > 0 && results[0].gameObject)
        {
            // 터치된 객체 혹은 그 부모가 자기 자신(DraggableItem)인지 확인
            DraggableItem topmostItem = results[0].gameObject.GetComponentInParent<DraggableItem>();
            return topmostItem == this;
        }
        
        return false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        /**
         * @description 아이템 터치 시 포커스 권한을 획득함.
         * 멀티터치 도중 다른 손가락이 아이템을 만져 권한을 훔쳐가는 것을 막기 위해, 첫 터치일 때만 권한을 줌.
         * @param eventData 포인터 이벤트 데이터.
         */
        if (Input.touchCount <= 1)
        {
            focusedItem = this;
        }

        if (isPlacedInDrawZone)
        {
            transform.SetAsLastSibling();
            
            isPointerDown = true;
            pointerDownTimer = 0f;
            hasLongPressed = false;
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (focusedItem != this)
        {
            return;
        }

        isPointerDown = false;

        if (!isPlacedInDrawZone)
        {
            GameObject clone = Instantiate(gameObject, originalParent);
            clone.name = gameObject.name;
            
            // 그리드 레이아웃 내에서 원래 아이템의 순서를 그대로 유지하기 위해 인덱스를 복사함
            clone.transform.SetSiblingIndex(transform.GetSiblingIndex());
            clone.transform.localScale = Vector3.one;
            clone.transform.localEulerAngles = Vector3.zero;
        }

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

    public void OnEndDrag(PointerEventData eventData)
    {
        if (focusedItem != this)
        {
            return;
        }

        if (canvasGroup)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        Camera cam = eventData.pressEventCamera;
        Vector2 itemScreenCenter = RectTransformUtility.WorldToScreenPoint(cam, rectTransform.position);

        PointerEventData centerEventData = new PointerEventData(EventSystem.current)
        {
            position = itemScreenCenter
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(centerEventData, raycastResults);

        bool isOverDrawZone = false;
        Transform drawZoneTransform = null;

        foreach (RaycastResult result in raycastResults)
        {
            if (result.gameObject && result.gameObject.CompareTag("DrawZone"))
            {
                isOverDrawZone = true;
                drawZoneTransform = result.gameObject.transform;
                break;
            }
        }

        if (isOverDrawZone && drawZoneTransform)
        {
            isPlacedInDrawZone = true;
            transform.SetParent(drawZoneTransform);
            transform.SetAsLastSibling();
            
            // 이후 실수로 도화지 밖으로 드래그했을 때 트레이로 돌아가지 않도록 부모 기준을 갱신함
            originalParent = drawZoneTransform;
        }
        else
        {
            if (!isPlacedInDrawZone)
            {
                // 트레이에서 처음 꺼낸 아이템이 도화지 밖에 떨어지면 파괴함
                Destroy(gameObject);
            }
            else
            {
                // 도화지에 이미 안착했던 아이템이 밖으로 나가면 도화지 내 위치로 안전하게 복귀함
                transform.SetParent(originalParent);
                transform.localScale = Vector3.one;
                transform.localEulerAngles = Vector3.zero;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        /**
         * @description 제스처 중이거나 포커스가 없는 경우 드래그 위치 이동을 차단함.
         * @param eventData 포인터 이벤트 데이터.
         */
        if (focusedItem != this || isGestureActive)
        {
            return;
        }

        Vector3 worldPoint;
        Camera cam = eventData.pressEventCamera;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, cam, out worldPoint))
        {
            rectTransform.position = worldPoint;
        }
    }
}