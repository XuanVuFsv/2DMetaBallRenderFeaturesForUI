using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;
public class RenderFeatureSwitcher : MonoBehaviour
{
    public ScriptableRendererFeature danielilett;
    public ScriptableRendererFeature inferenceColor;
    public ScriptableRendererFeature blurBased;

    public TextMeshProUGUI displayTxt;

    private void Start()
    {
        SwitchRenderFeature(0);
    }

    public void SwitchRenderFeature(int featureIndex)
    {
        DisableAllRenderFeatures();

        switch (featureIndex)
        {
            case 0:
                displayTxt.text = "off";
                break;
            case 1:
                EnableRenderFeature(danielilett);
                displayTxt.text = "1 works";
                break;
            case 2:
                EnableRenderFeature(inferenceColor);
                displayTxt.text = "2 works";
                break;
            case 3:
                EnableRenderFeature(blurBased);
                displayTxt.text = "3 works";

                break;
            default:
                Debug.LogWarning("Invalid render feature index");
                break;
        }
    }

    private void EnableRenderFeature(ScriptableRendererFeature feature)
    {
        if (feature != null)
        {
            feature.SetActive(true);
        }
    }

    private void DisableAllRenderFeatures()
    {
        if (danielilett)
            danielilett.SetActive(false);
        if (inferenceColor)
            inferenceColor.SetActive(false);
        if (blurBased)
            blurBased.SetActive(false);
    }
}
