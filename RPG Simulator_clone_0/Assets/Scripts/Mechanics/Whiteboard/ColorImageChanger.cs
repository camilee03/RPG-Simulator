using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorImageChanger : MonoBehaviour, IDragHandler, IPointerClickHandler
{
    [SerializeField] Image cursor;
    [SerializeField] ColorPicker colorPicker;

    private RawImage svImage;
    private RectTransform rectTransform, cursorTransform;

    private void Awake()
    {
        svImage = GetComponent<RawImage>();
        rectTransform = GetComponent<RectTransform>();

        cursorTransform = cursor.GetComponent<RectTransform>();
        cursorTransform.position = new Vector2(-(rectTransform.sizeDelta.x * 0.5f), -(rectTransform.sizeDelta.y * 0.5f));
    }

    private void UpdateColor(PointerEventData eventData)
    {
        Debug.Log($"Cursor position: {eventData.position}");

        Vector3 pos = rectTransform.InverseTransformPoint(eventData.position);

        float deltaX = rectTransform.sizeDelta.x * 0.5f;
        float deltaY = rectTransform.sizeDelta.y * 0.5f;

        pos.x = Mathf.Clamp(pos.x, -deltaX, deltaX);
        pos.y = Mathf.Clamp(pos.y, -deltaY, deltaY);

        float x = pos.x + deltaX;
        float y = pos.y + deltaY;

        float xNorm = x / rectTransform.sizeDelta.x;
        float yNorm = y / rectTransform.sizeDelta.y;

        cursorTransform.localPosition = pos;
        cursor.color = Color.HSVToRGB(0, 0, 1 - yNorm);

        colorPicker.SetSV(xNorm, yNorm);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateColor(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UpdateColor(eventData);
    }
}
