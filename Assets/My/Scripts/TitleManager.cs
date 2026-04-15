using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("Page References")]
    [SerializeField] private GameObject page1;
    [SerializeField] private GameObject page2;

    [Header("Button References")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button nextButton;

    [Header("Settings")]
    [SerializeField] private string nextSceneName;

    private void Start()
    {
        if (startButton)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        if (backButton)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        if (nextButton)
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        // 초기 상태 설정
        if (page1) page1.SetActive(true);
        if (page2) page2.SetActive(false);
    }
    
    public void OnStartButtonClicked()
    {
        if (!page1 || !page2)
        {
            return;
        }

        page1.SetActive(false);
        page2.SetActive(true);
    }


    public void OnBackButtonClicked()
    {
        if (!page1 || !page2)
        {
            return;
        }

        page2.SetActive(false);
        page1.SetActive(true);
    }
    
    public void OnNextButtonClicked()
    {
        SceneManager.LoadScene(GameConstants.DrawScene);
    }
}