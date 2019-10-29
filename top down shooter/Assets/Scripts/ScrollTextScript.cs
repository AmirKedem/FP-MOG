using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class ScrollTextScript : MonoBehaviour
{
    RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update() 
    {
        rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, rectTransform.offsetMax.y - LayoutUtility.GetPreferredHeight(rectTransform));
    }
}
