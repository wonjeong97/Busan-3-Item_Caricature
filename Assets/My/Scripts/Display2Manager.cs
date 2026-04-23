using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class Display2Manager : MonoBehaviour
{   
    public enum IncomingDirection
    {
        Bottom,
        BottomRight
    }
    
    private static Display2Manager instance;
    
    [Header("Animation Settings")]
    [SerializeField] private IncomingDirection incomingDirection;

    [SerializeField]
    private Image frameImage;

    private string savePath;
    private const string FileName = "LastCapturedImage.png";

    public static Display2Manager Instance => instance;

    private void Awake()
    {
        if (instance)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // 기기별로 안전한 영구 저장 경로 설정
        savePath = Path.Combine(Application.persistentDataPath, FileName);

        if (Display.displays.Length > 1)
        {
            Display.displays[1].Activate();
        }

        // 프로그램 시작 시 마지막으로 저장된 이미지 불러오기
        LoadImageFromDisk();
    }

    public void UpdateDisplayImage(Sprite newSprite)
    {
        if (!frameImage || !newSprite)
        {
            return;
        }

        GameObject oldFrameObj = Instantiate(frameImage.gameObject, frameImage.transform.parent);
        oldFrameObj.transform.SetAsFirstSibling();

        frameImage.sprite = newSprite;
        FitToCanvas(newSprite.texture);
        SaveImageToDisk(newSprite.texture);

        StartCoroutine(SlideInRoutine(oldFrameObj));
    }

    /**
     * @description 이미지를 캔버스에 비율 유지하며 꽉 차도록 배치함.
     * 앵커 정중앙, 위치 (0, 0), 너비 또는 높이 중 작은 쪽 기준으로 스케일.
     * @param texture 크기 계산에 사용할 원본 텍스처.
     */
    private void FitToCanvas(Texture2D texture)
    {
        if (!frameImage || !texture) return;

        RectTransform canvasRect = frameImage.canvas.GetComponent<RectTransform>();
        float canvasW = canvasRect.rect.width;
        float canvasH = canvasRect.rect.height;

        float scale = Mathf.Min(canvasW / texture.width, canvasH / texture.height);

        RectTransform rt = frameImage.rectTransform;
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(texture.width * scale, texture.height * scale);
    }

    /**
     * @description 텍스처 데이터를 PNG 형식으로 인코딩하여 지정된 경로에 저장함.
     * @param texture 저장할 이미지의 Texture2D 데이터.
     */
    private void SaveImageToDisk(Texture2D texture)
    {
        if (!texture)
        {
            return;
        }

        try
        {
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(savePath, bytes);
            Debug.Log("Image saved to: " + savePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save image: " + e.Message);
        }
    }

    /**
     * @description 저장된 파일이 존재할 경우, 이를 읽어와 Texture2D 및 Sprite로 변환하여 화면에 표시함.
     */
    private void LoadImageFromDisk()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("No saved image found at start.");
            return;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(savePath);
            Texture2D texture = new Texture2D(2, 2);
            
            if (texture.LoadImage(bytes))
            {
                Rect rect = new Rect(0, 0, texture.width, texture.height);
                Sprite savedSprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));

                if (frameImage)
                {
                    frameImage.sprite = savedSprite;
                    FitToCanvas(texture);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load image: " + e.Message);
        }
    }
    
    private IEnumerator SlideInRoutine(GameObject oldFrameObj)
    {
        float duration = 0.8f;
        float elapsed = 0f;

        RectTransform newRt = frameImage.rectTransform;
        Vector2 centerPos = newRt.anchoredPosition; 
        Vector2 startPos;
        Vector2 exitPos;
        
        switch (incomingDirection)
        {
            case IncomingDirection.Bottom:
                startPos = centerPos + new Vector2(0f, -1500f);
                exitPos = centerPos + new Vector2(0f, 1500f); // 위로 밀려남
                break;
            case IncomingDirection.BottomRight:
                startPos = centerPos + new Vector2(1500f, -1500f);
                exitPos = centerPos + new Vector2(-1500f, 1500f); // 좌상단으로 밀려남
                break;
            default:
                startPos = centerPos + new Vector2(0f, -1500f);
                exitPos = centerPos + new Vector2(0f, 1500f);
                break;
        }
        
        newRt.anchoredPosition = startPos;

        RectTransform oldRt = null;
        if (oldFrameObj)
        {
            oldRt = oldFrameObj.GetComponent<RectTransform>();
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            // 새 이미지는 화면 밖에서 중앙으로 들어옴
            newRt.anchoredPosition = Vector2.Lerp(startPos, centerPos, easeT);
            
            // 기존 이미지는 중앙에서 화면 밖으로 밀려남
            if (oldRt)
            {
                oldRt.anchoredPosition = Vector2.Lerp(centerPos, exitPos, easeT);
            }
            
            yield return null;
        }

        newRt.anchoredPosition = centerPos;

        if (oldFrameObj)
        {
            Destroy(oldFrameObj);
        }
    }
}