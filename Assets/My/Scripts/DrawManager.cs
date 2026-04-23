using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wonjeong.Utils;

public class DrawManager : MonoBehaviour
{
    public enum OutgoingDirection
    {
        Top,
        TopLeft
    }

    [Header("Animation Settings")]
    [SerializeField] private OutgoingDirection outgoingDirection;
    
    [Header("Page References")]
    [SerializeField] private GameObject page1;
    [SerializeField] private GameObject page2;

    [Header("UI References")]
    [SerializeField] private RectTransform drawZoneRect;
    [SerializeField] private Image imageResult;

    [Header("Button References")]
    [SerializeField] private Button completeButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button hangButton;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioClip completeSound;
    [SerializeField] private AudioClip hangSound;

    private Sprite _spriteForPage2;
    private Sprite _spriteForDisplay2;

    private void Start()
    {
        if (completeButton)
        {
            completeButton.onClick.AddListener(OnCompleteButtonClicked);
            completeButton.gameObject.SetActive(false);
            StartCoroutine(ShowCompleteButtonRoutine());
        }
        else
        {
            Debug.LogWarning("completeButton is missing. Fallback to ignoring delay logic.");
        }

        if (backButton)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
        else
        {
            Debug.LogWarning("backButton is missing.");
        }

        if (hangButton)
        {
            hangButton.onClick.AddListener(OnHangButtonClicked);
        }
        else
        {
            Debug.LogWarning("hangButton is missing.");
        }

        if (page1) page1.SetActive(true);
        if (page2) page2.SetActive(false);
    }

    private IEnumerator ShowCompleteButtonRoutine()
    {
        yield return new WaitForSeconds(3f);

        if (completeButton)
        {
            completeButton.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("completeButton is missing during delay routine. Fallback to aborting.");
        }
    }

    public void OnCompleteButtonClicked()
    {
        if (!page1 || !page2 || !drawZoneRect || !imageResult)
        {
            Debug.LogError("Required references are missing in DrawManager. Fallback to aborting capture.");
            return;
        }

        if (GameManager.Instance && completeSound)
        {
            GameManager.Instance.PlayEffectSound(completeSound);
        }

        StartCoroutine(CaptureAndTransition());
    }

    public void OnBackButtonClicked()
    {
        if (!page1 || !page2)
        {
            Debug.LogError("Page references are missing in DrawManager.");
            return;
        }

        if (page2.activeSelf)
        {
            page2.SetActive(false);
            page1.SetActive(true);
        }
        else
        {
            SceneManager.LoadScene(GameConstants.TitleScene);
        }
    }

    public void OnHangButtonClicked()
    {
        if (!_spriteForDisplay2)
        {
            Debug.LogError("No captured sprite found to hang. Fallback to aborting display update.");
            return;
        }

        if (hangButton)
        {
            hangButton.interactable = false;
        }

        if (GameManager.Instance && hangSound)
        {
            GameManager.Instance.PlayEffectSound(hangSound);
        }

        StartCoroutine(AnimateAndHangRoutine());
    }

   private IEnumerator CaptureAndTransition()
    {
        yield return CoroutineData.WaitForEndOfFrame;

        int originalWidth = Mathf.RoundToInt(drawZoneRect.rect.width);
        int originalHeight = Mathf.RoundToInt(drawZoneRect.rect.height);

        // --- 동적 패딩 계산 로직 ---
        float maxProtrusion = 0f;
        float extentsX = originalWidth / 2f;
        float extentsY = originalHeight / 2f;

        Vector3[] itemCorners = new Vector3[4];
        
        foreach (DraggableItem item in drawZoneRect.GetComponentsInChildren<DraggableItem>())
        {
            RectTransform itemRect = item.GetComponent<RectTransform>();
            if (!itemRect) continue;

            itemRect.GetWorldCorners(itemCorners);
            for (int i = 0; i < 4; i++)
            {
                // 월드 좌표의 모서리 점들을 도화지(drawZoneRect) 기준의 로컬 좌표계로 변환함
                Vector3 localPoint = drawZoneRect.InverseTransformPoint(itemCorners[i]);
                
                // 도화지 절반 크기(extents)를 넘어선 돌출 길이를 계산함
                float protrusionX = Mathf.Abs(localPoint.x) - extentsX;
                float protrusionY = Mathf.Abs(localPoint.y) - extentsY;

                if (protrusionX > maxProtrusion) maxProtrusion = protrusionX;
                if (protrusionY > maxProtrusion) maxProtrusion = protrusionY;
            }
        }

        // 도화지 밖으로 삐져나간 최대 길이에 안전 여유분(50픽셀)을 더해 최종 패딩 확정
        int padding = Mathf.Max(0, Mathf.CeilToInt(maxProtrusion)) + 50; 
        // ------------------------

        int captureWidth = originalWidth + (padding * 2);
        int captureHeight = originalHeight + (padding * 2);

        if (originalWidth <= 0 || originalHeight <= 0)
        {
            Debug.LogError("DrawZone Rect size is invalid. Capture aborted.");
            yield break;
        }

        RenderTexture renderTexture = RenderTexture.GetTemporary(captureWidth, captureHeight, 24, RenderTextureFormat.ARGB32);

        Vector3 hiddenPos = new Vector3(10000f, 10000f, 0f);
        
        GameObject camObj = new GameObject("HiddenCaptureCamera");
        camObj.transform.position = hiddenPos + new Vector3(0, 0, -10f);
        Camera captureCam = camObj.AddComponent<Camera>();
        captureCam.orthographic = true;
        captureCam.orthographicSize = captureHeight / 2f;
        captureCam.clearFlags = CameraClearFlags.SolidColor;
        captureCam.backgroundColor = Color.clear;
        captureCam.targetTexture = renderTexture;

        GameObject canvasObj = new GameObject("HiddenCaptureCanvas");
        canvasObj.transform.position = hiddenPos;
        Canvas tempCanvas = canvasObj.AddComponent<Canvas>();
        tempCanvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(captureWidth, captureHeight);

        GameObject clonedDrawZone = Instantiate(drawZoneRect.gameObject, canvasObj.transform);
        RectTransform clonedRect = clonedDrawZone.GetComponent<RectTransform>();
        clonedRect.anchoredPosition = Vector2.zero;
        clonedRect.anchorMin = new Vector2(0.5f, 0.5f);
        clonedRect.anchorMax = new Vector2(0.5f, 0.5f);
        clonedRect.pivot = new Vector2(0.5f, 0.5f);
        clonedRect.localScale = Vector3.one;
        clonedRect.sizeDelta = new Vector2(originalWidth, originalHeight);

        // 1차 캡처: Page 2를 위해 정중앙의 원본 도화지 크기만큼만 정확하게 잘라냄 (마스크 효과 유지)
        captureCam.Render();
        RenderTexture.active = renderTexture;
        Texture2D texWithBg = new Texture2D(originalWidth, originalHeight, TextureFormat.RGBA32, false);
        texWithBg.ReadPixels(new Rect(padding, padding, originalWidth, originalHeight), 0, 0);
        texWithBg.Apply();

        // 2차 캡처: Display 2를 위해 마스크 해제 후 삐져나간 아이템들을 여백이 포함된 전체 영역으로 찍어냄
        Mask mask = clonedDrawZone.GetComponent<Mask>();
        if (mask) mask.enabled = false;

        RectMask2D rectMask = clonedDrawZone.GetComponent<RectMask2D>();
        if (rectMask) rectMask.enabled = false;

        Image bgImage = clonedDrawZone.GetComponent<Image>();
        if (bgImage)
        {
            bgImage.enabled = false;
        }
        else
        {
            Debug.LogWarning("Background Image component is missing on cloned DrawZone.");
        }
        
        captureCam.Render();
        Texture2D texWithoutBg = new Texture2D(captureWidth, captureHeight, TextureFormat.RGBA32, false);
        texWithoutBg.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        texWithoutBg.Apply();
        
        RenderTexture.active = null;

        Destroy(clonedDrawZone);
        Destroy(canvasObj);
        Destroy(camObj);
        RenderTexture.ReleaseTemporary(renderTexture);

        if (_spriteForPage2)
        {
            if (_spriteForPage2.texture) DestroyImmediate(_spriteForPage2.texture, true);
            DestroyImmediate(_spriteForPage2, true);
        }
        
        if (_spriteForDisplay2)
        {
            if (_spriteForDisplay2.texture) DestroyImmediate(_spriteForDisplay2.texture, true);
            DestroyImmediate(_spriteForDisplay2, true);
        }

        Vector2 pivot = new Vector2(0.5f, 0.5f);
        
        _spriteForPage2 = Sprite.Create(texWithBg, new Rect(0, 0, originalWidth, originalHeight), pivot);
        _spriteForDisplay2 = Sprite.Create(texWithoutBg, new Rect(0, 0, captureWidth, captureHeight), pivot);

        imageResult.sprite = _spriteForPage2;

        page1.SetActive(false);
        page2.SetActive(true);
    }
   
    private IEnumerator AnimateAndHangRoutine()
    {
        float duration = 1.5f;
        float elapsed = 0f;

        RectTransform imgRect = imageResult.rectTransform;
        Vector2 startPos = imgRect.anchoredPosition;
        Vector2 targetPos;

        // 선택된 방향에 따른 타겟 오프셋 계산 (예: TopLeft 선택 시 X:-1500, Y:1500 반환)
        switch (outgoingDirection)
        {
            case OutgoingDirection.Top:
                targetPos = startPos + new Vector2(0f, 1500f);
                break;
            case OutgoingDirection.TopLeft:
                targetPos = startPos + new Vector2(-1500f, 1500f);
                break;
            default:
                targetPos = startPos + new Vector2(0f, 1500f);
                break;
        }

        Vector3 startScale = imgRect.localScale;
        Vector3 targetScale = Vector3.zero;

        Color startColor = imageResult.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f); 

            imgRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, easeT);
            imgRect.localScale = Vector3.Lerp(startScale, targetScale, easeT);
            imageResult.color = Color.Lerp(startColor, targetColor, easeT);

            yield return null;
        }

        if (Display2Manager.Instance)
        {
            Display2Manager.Instance.UpdateDisplayImage(_spriteForDisplay2);
        }
        else
        {
            Debug.LogError("Display2Manager Instance not found. Fallback to aborting display update.");
        }

        SceneManager.LoadScene(GameConstants.TitleScene);
    }
}