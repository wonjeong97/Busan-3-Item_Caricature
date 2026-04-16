using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, IPointerClickHandler
{
    /// <summary>
    /// 버튼 클릭 이벤트를 감지하여 효과음을 재생합니다.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Button button = GetComponent<Button>();

        if (!button)
        {
            Debug.LogWarning("Button 컴포넌트가 존재하지 않습니다. UIButtonSound는 Button과 함께 사용되어야 합니다.");
            return;
        }

        if (!GameManager.Instance)
        {
            Debug.LogWarning("GameManager 인스턴스가 존재하지 않아 사운드를 재생할 수 없습니다.");
            return;
        }

        GameManager.Instance.PlayUIClickSound();
    }
}