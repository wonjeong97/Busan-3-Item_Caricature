using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ItemZoneManager : MonoBehaviour
{
    [SerializeField] 
    private List<GameObject> categoryContainers;
    
    [SerializeField] 
    private List<Button> tabButtons;

    private void Start()
    {
        for (int i = 0; i < tabButtons.Count; i++)
        {
            Button button = tabButtons[i];
            
            if (!button)
            {
                Debug.LogWarning("Tab button is missing at index: " + i);
                continue;
            }

            // 클로저(Closure) 변수 캡처 문제를 피하기 위해 지역 변수에 인덱스 복사
            int index = i;
            button.onClick.AddListener(() => ChangeTab(index));
        }

        // 씬 시작 시 첫 번째 탭(음식)을 기본으로 활성화
        ChangeTab(0);
    }

    public void ChangeTab(int categoryIndex)
    {
        for (int i = 0; i < categoryContainers.Count; i++)
        {
            GameObject container = categoryContainers[i];
            
            if (container)
            {
                container.SetActive(i == categoryIndex);
            }
        }

        for (int i = 0; i < tabButtons.Count; i++)
        {
            Button button = tabButtons[i];
            
            if (button)
            {
                // 선택된 탭 버튼은 상호작용을 비활성화하여 눌려있는 듯한 시각적 피드백 제공
                button.interactable = (i != categoryIndex);
            }
        }
    }
}