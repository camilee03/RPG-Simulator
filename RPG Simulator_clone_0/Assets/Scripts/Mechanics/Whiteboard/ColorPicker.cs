using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{
    private float currentHue, currentSat, currentVal;

    [SerializeField] RawImage hueImage, satValImage, outputImage;
    [SerializeField] Slider hueSlider;
    [SerializeField] TMP_InputField hexInputText;

    private Texture2D hueTexture, svTexture, outputTexture;

    Color colorToChange;

    private void Start()
    {
        CreateHueImage();
        CreateSVTexture();
        CreateOutputImage();

        UpdateOutputImage();
    }

    #region Setup & Updates

    private void CreateHueImage()
    {
        hueTexture = new(1, 16)
        {
            wrapMode = TextureWrapMode.Clamp,
            name = "HueTexture",
        };

        for (int i = 0; i < hueTexture.height; i++)
        {
            hueTexture.SetPixel(0, i, Color.HSVToRGB((float)i / hueTexture.height, 1, 1));
        }

        hueTexture.Apply();
        currentHue = 0;
        hueImage.texture = hueTexture;
    }

    private void CreateSVTexture()
    {
        svTexture = new(16, 16)
        {
            wrapMode = TextureWrapMode.Clamp,
            name = "SatValTexture",
        };

        for (int y = 0; y < svTexture.height; y++)
        {
            for (int x = 0; x < svTexture.width; x++)
            {
                svTexture.SetPixel(x, y, Color.HSVToRGB(currentHue, (float)x / svTexture.width, (float)y / svTexture.height));
            }
        }

        svTexture.Apply();
        currentSat = 0;
        currentVal = 0;
        satValImage.texture = svTexture;
    }

    private void CreateOutputImage()
    {
        outputTexture = new(1, 16)
        {
            wrapMode = TextureWrapMode.Clamp,
            name = "OutputTexture",
        };

        Color currentColor = Color.HSVToRGB(currentHue, currentSat, currentVal);

        for (int i = 0; i < outputTexture.height; i++)
        {
            outputTexture.SetPixel(0, i, currentColor);
        }

        outputTexture.Apply();
        outputImage.texture = outputTexture;
    }

    private void UpdateOutputImage()
    {
        Color currentColor = Color.HSVToRGB(currentHue, currentSat, currentVal);

        for (int i = 0; i < outputTexture.height; i++)
        {
            outputTexture.SetPixel(0, i, currentColor);
        }

        outputTexture.Apply();

        hexInputText.text = ColorUtility.ToHtmlStringRGB(currentColor);
        colorToChange = currentColor;
    }

    public void SetSV(float S, float V)
    {
        currentSat = S;
        currentVal = V;

        UpdateOutputImage();
    }

    public void UpdateSVImage()
    {
        currentHue = hueSlider.value;

        for (int y = 0;  y < svTexture.height; y++)
        {
            for (int x = 0; x < svTexture.width; x++)
            {
                svTexture.SetPixel(x, y, Color.HSVToRGB(currentHue, (float)x / svTexture.width, (float)y /  svTexture.height));
            }
        }

        svTexture.Apply();
        UpdateOutputImage();
    }

    #endregion

    public Color GetColor()
    {
        return colorToChange;
    }

    public void OnHexTextInput(string text)
    {
        if (hexInputText.text.Length < 6) return;

        if (ColorUtility.TryParseHtmlString($"#{text}", out Color newColor))
        {
            Color.RGBToHSV(newColor, out currentHue, out currentSat, out currentVal);
        }

        hueSlider.value = currentHue;
        hexInputText.text = "";

        UpdateOutputImage();
    }
}
