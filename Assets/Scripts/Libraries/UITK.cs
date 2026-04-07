using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

public static class UITK
{
    #region Constant Variables 
    public const string UITABLE = "UI";

    #endregion

    #region Element Creation
    public static VisualElement AddElement(VisualElement parent, params string[] classNames)
    {
        return AddElement<VisualElement>(parent, classNames);
    }

    public static T AddElement<T>(VisualElement parent, params string[] classNames) where T : VisualElement, new()
    {
        var element = CreateElement<T>(classNames);
        parent.Add(element);
        return element;
    }

    public static VisualElement CreateElement(params string[] classNames)
    {
        return CreateElement<VisualElement>(classNames);
    }

    public static T CreateElement<T>(params string[] classNames) where T : VisualElement, new()
    {
        var element = new T();
        foreach (var className in classNames)
            element.AddToClassList(className);

        return element;
    }
    #endregion

    #region Element Utilities
    public static void ToggleScreen(VisualElement element, out bool isVisible)
    {
        isVisible = element.resolvedStyle.display == DisplayStyle.Flex;

        if (isVisible)
        {
            element.style.display = DisplayStyle.None;
            isVisible = false;
        }
        else
        {
            element.style.display = DisplayStyle.Flex;
            isVisible = true;
        }
    }

    public static void ToggleScreenWSound(VisualElement element, AudioSource source, AudioClip soundOn, AudioClip soundOff)
    {
        ToggleScreen(element, out bool isVisible);

        if (isVisible)
        {
            if (soundOn != null)
                source.PlayOneShot(soundOn);
        }
        else
        {
            if (soundOff != null)
                source.PlayOneShot(soundOff);
        }
    }

    public static void ParallaxOffset(VisualElement element, Vector2 mousePos, float intensity)
    {
        Vector2 elementCenterInPanel = element.worldBound.center;
        Vector2 mousePositionInPanel = new(Input.mousePosition.x, 1080 - Input.mousePosition.y);

        Vector2 offsetFromMouse = mousePositionInPanel - elementCenterInPanel;
        Vector2 parallaxOffset = offsetFromMouse * -intensity;

        element.style.left = parallaxOffset.x;
        element.style.top = parallaxOffset.y;
    }

    public static void TrackUIToWorldPosition(Vector3 position, VisualElement element, Camera cam, Vector2 offset = default)
    {
        Vector3 screenPos = cam.WorldToScreenPoint(position);

        if (screenPos.z <= 0)
        {
            element.style.display = DisplayStyle.None;
            return;
        }

        element.style.display = DisplayStyle.Flex;
        
        element.style.left = screenPos.x + offset.x;
        element.style.top = Screen.height - screenPos.y + offset.y;
        element.style.translate = new Translate(-960, -540);
    }

    public static VisualElement[] CreateChromaticAberration(VisualElement target, float intensity = 0.5f)
    {
        // Essentials
        VisualElement parent = target.parent;
        Texture2D texture = target.resolvedStyle.backgroundImage.texture;
        Color baseTint = target.resolvedStyle.unityBackgroundImageTintColor; // Getting original color
        string[] names = { "COLayerRed", "COLayerGreen", "COLayerBlue" }; // For class assigning
        Color[] colors = {
            Color.red + (Color.white * (1 - intensity)),
            Color.green + (Color.white * (1 - intensity)),
            Color.blue + (Color.white * (1 - intensity))}; // We are adding white to make layer less monochrome based on intensity

        VisualElement COGroup = UITK.AddElement(parent, "COGroup");
        VisualElement[] layers = new VisualElement[3];

        //RGB Layers
        for (int i = 0; i < layers.Length; i++)
        {
            var layer = UITK.AddElement(COGroup, names[i], "COLayer");

            layer.style.position = Position.Absolute;
            layer.style.backgroundImage = new StyleBackground(texture);
            layer.style.unityBackgroundImageTintColor = new Color(
                baseTint.r * colors[i].r,
                baseTint.g * colors[i].g,
                baseTint.b * colors[i].b);
            layers[i] = layer;
        }

        COGroup.Add(target); // Reparenting target, so that it would be on top of COLayers

        return layers;
    }
    #endregion

    #region Localization 
    public static LocalizedString LocalizeStringUITK(TextElement element, string table, string key)
    {
        var localString = new LocalizedString(table, key);
        localString.StringChanged += (value) => element.text = value;

        return localString;
    }

    public static LocalizedString LocalizeStringUITK(TextElement element, string table, string key, string addition)
    {
        var localString = new LocalizedString(table, key);
        localString.StringChanged += (value) => element.text = value + addition;

        return localString;
    }

    public static Label AddHintBox(VisualElement element)
    {
        var hintBox = UITK.AddElement<Label>(element, "HintBox", "SubText");
        hintBox.pickingMode = PickingMode.Ignore;
        hintBox.BringToFront();

        element.RegisterCallback<PointerEnterEvent>(evt =>
        {
            hintBox.style.display = DisplayStyle.Flex;
        });

        element.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            hintBox.style.display = DisplayStyle.None;
        });

        return hintBox;
    }

    public static Label AddHintBox(VisualElement element, string hint)
    {
        var hintBox = AddHintBox(element);
        hintBox.text = hint;

        return hintBox;
    }

    public static Label AddLocalizedHintBox(VisualElement element, string table, string key)
    {
        var hintBox = AddHintBox(element);
        UITK.LocalizeStringUITK(hintBox, table, key);

        return hintBox;
    }
    #endregion
}