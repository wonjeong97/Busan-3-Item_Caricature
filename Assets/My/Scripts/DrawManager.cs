using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wonjeong.Utils;

public class DrawManager : MonoBehaviour
{
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

    // 런타임에 생성한 스프라이트만 추적하여 안전하게 해제
    private Sprite _capturedSprite;

    private void Start()
    {
        if (completeButton)
            completeButton.onClick.AddListener(OnCompleteButtonClicked);

        if (backButton)
            backButton.onClick.AddListener(OnBackButtonClicked);

        if (hangButton)
            hangButton.onClick.AddListener(OnHangButtonClicked);

        if (page1) page1.SetActive(true);
        if (page2) page2.SetActive(false);
    }

    public void OnCompleteButtonClicked()
    {
        if (!page1 || !page2 || !drawZoneRect || !imageResult)
        {
            Debug.LogError("Required references are missing in DrawManager.");
            return;
        }

        StartCoroutine(CaptureAndTransition());
    }

    public void OnBackButtonClicked()
    {
        if (!page1 || !page2) return;

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
        if (!imageResult)
        {
            Debug.LogError("ImageResult reference is missing.");
            return;
        }

        if (!imageResult.sprite)
        {
            Debug.LogError("No captured sprite found to hang.");
            return;
        }

        if (Display2Manager.Instance)
            Display2Manager.Instance.UpdateDisplayImage(imageResult.sprite);
        else
            Debug.LogError("Display2Manager Instance not found.");

        SceneManager.LoadScene(GameConstants.TitleScene);
    }

    private IEnumerator CaptureAndTransition()
    {
        yield return CoroutineData.WaitForEndOfFrame;

        int width = Mathf.RoundToInt(drawZoneRect.rect.width);
        int height = Mathf.RoundToInt(drawZoneRect.rect.height);

        if (width <= 0 || height <= 0)
        {
            Debug.LogError("DrawZone Rect size is invalid. Capture aborted.");
            yield break;
        }

        // 1. 독립된 캡처용 렌더 텍스처 생성
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);

        // 2. 메인 씬의 렌더링과 겹치지 않도록 화면 밖 아주 먼 곳에 임시 카메라(사진관) 세팅
        Vector3 hiddenPos = new Vector3(10000f, 10000f, 0f);
        
        GameObject camObj = new GameObject("HiddenCaptureCamera");
        camObj.transform.position = hiddenPos + new Vector3(0, 0, -10f);
        Camera captureCam = camObj.AddComponent<Camera>();
        captureCam.orthographic = true;
        captureCam.orthographicSize = height / 2f;
        captureCam.clearFlags = CameraClearFlags.SolidColor;
        captureCam.backgroundColor = Color.clear; // 도화지 외곽을 투명하게 처리
        captureCam.targetTexture = renderTexture;

        // 3. 사진을 찍을 임시 캔버스(World Space) 생성
        GameObject canvasObj = new GameObject("HiddenCaptureCanvas");
        canvasObj.transform.position = hiddenPos;
        Canvas tempCanvas = canvasObj.AddComponent<Canvas>();
        tempCanvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(width, height);

        // 4. 현재 완성된 도화지(DrawZone)의 완벽한 UI 복제본을 생성하여 임시 캔버스 중앙에 배치
        GameObject clonedDrawZone = Instantiate(drawZoneRect.gameObject, canvasObj.transform);
        RectTransform clonedRect = clonedDrawZone.GetComponent<RectTransform>();
        clonedRect.anchoredPosition = Vector2.zero;
        clonedRect.anchorMin = new Vector2(0.5f, 0.5f);
        clonedRect.anchorMax = new Vector2(0.5f, 0.5f);
        clonedRect.pivot = new Vector2(0.5f, 0.5f);
        clonedRect.localScale = Vector3.one;
        clonedRect.sizeDelta = new Vector2(width, height);

        // 5. 임시 카메라 렌더링 실행 (UI 컴포넌트의 마스킹, 회전, 스케일이 모두 정상적으로 구워짐)
        captureCam.Render();

        // 6. 렌더 텍스처에서 최종 픽셀 데이터 추출
        RenderTexture.active = renderTexture;
        Texture2D captured = new Texture2D(width, height, TextureFormat.RGBA32, false);
        captured.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        captured.Apply();
        RenderTexture.active = null;

        // 7. 임시 촬영 객체들 즉시 철거 (1프레임 내에 생성/파괴되므로 화면 깜빡임 없음)
        Destroy(clonedDrawZone);
        Destroy(canvasObj);
        Destroy(camObj);
        RenderTexture.ReleaseTemporary(renderTexture);

        // 8. 기존 런타임 이미지 메모리 해제 후 결과 화면에 반영
        if (_capturedSprite)
        {
            if (_capturedSprite.texture)
            {
                DestroyImmediate(_capturedSprite.texture, true);
            }
            DestroyImmediate(_capturedSprite, true);
        }

        _capturedSprite = Sprite.Create(captured, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        imageResult.sprite = _capturedSprite;

        page1.SetActive(false);
        page2.SetActive(true);
    }
}
