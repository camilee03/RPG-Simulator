using UnityEngine;
using UnityEngine.UI;

public class NonDefaultUI : MonoBehaviour
{
    [Header("General")]
    [SerializeField] PlayerEmotes playerEmotes;

    #region Drawing UI Elements

    [Header("Drawing UI Elements")]
    Whiteboard whiteboard;
    Color currentColor = Color.black;
    [SerializeField] ColorPicker colorPicker;

    public void InitializeDrawingElements(Whiteboard whiteboard)
    {
        this.whiteboard = whiteboard;
    }

    public void ChangeToFill()
    {
        whiteboard.ChangeTool(Whiteboard.Tool.Fill);
    }

    public void ChangeToErase()
    {
        whiteboard.ChangeTool(Whiteboard.Tool.Eraser);
    }

    public void ChangeToPen()
    {
        whiteboard.ChangeTool(Whiteboard.Tool.Pen);
    }

    public void Clear()
    {
        whiteboard.ClearBoard();
    }

    public void ChangePenWidth(float penWidth)
    {
        whiteboard.ChangePenWidth(penWidth);
    }

    public void OpenColorPicker()
    {
        colorPicker.gameObject.SetActive(true);
        whiteboard.ChangeTool(Whiteboard.Tool.None);
        playerEmotes.canDoEmotes = false;
    }

    public void SaveColor()
    {
        whiteboard.ChangeColor(colorPicker.GetColor());

        CloseColorPicker();
    }

    public void CloseColorPicker()
    {
        colorPicker.gameObject.SetActive(false);
        whiteboard.ChangeTool(Whiteboard.Tool.Pen);
        playerEmotes.canDoEmotes = false;
    }

    public void ChangeBGImage()
    {
        whiteboard.SetNewBackgroundImage();
    }

    #endregion
}
