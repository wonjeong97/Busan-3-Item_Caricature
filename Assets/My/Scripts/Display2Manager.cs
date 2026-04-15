using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class Display2Manager : MonoBehaviour
{
    private static Display2Manager instance;

    [SerializeField]
    private Image frameImage;

    private string savePath;
    private const string FileName = "LastCapturedImage.png";

    public static Display2Manager Instance => instance;

    private void Awake()
    {
        /**
         * @description 싱글톤 인스턴스를 설정하고, 저장 경로 초기화 및 이전 이미지를 로드함.
         */
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

    /**
     * @description 새로운 이미지를 화면에 표시하고 동시에 디스크에 물리 파일로 저장함.
     * @param newSprite 액자에 표시할 새로운 이미지 스프라이트.
     */
    public void UpdateDisplayImage(Sprite newSprite)
    {
        if (!frameImage || !newSprite)
        {
            return;
        }

        frameImage.sprite = newSprite;
        FitToCanvas(newSprite.texture);
        SaveImageToDisk(newSprite.texture);
    }

    /**
     * @description 이미지를 캔버스에 비율 유지하며 꽉 차도록 배치함.
     * 앵커 정중앙, 위치 (0, 0), 너비 또는 높이 중 작은 쪽 기준으로 스케일.
     * @param texture 크기 계산에 사용할 원본 텍스처.
     */
    private void FitToCanvas(Texture2D texture)
    {
        if (!frameImage || texture == null) return;

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
}