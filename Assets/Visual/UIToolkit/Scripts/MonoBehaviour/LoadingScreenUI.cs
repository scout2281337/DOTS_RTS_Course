using UnityEngine;
using UnityEngine.UIElements;

public class LoadingScreenUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet[] styleSheets;
    [SerializeField] private ViewController UIController;
    [SerializeField] private LoadingScreenSO[] loadingScreenConfig;

    private VisualElement loadingScreen;
    private VisualElement blackScreen;
    private VisualElement firstSlide;
    private VisualElement secondSlide;
    private VisualElement thirdSlide;


    private void BuildLoadingScreen()
    {
        VisualElement root = uiDocument.rootVisualElement;
        root.Clear();

        foreach (StyleSheet sheet in UIController.defaultStyleSheet.styles)
        {
            root.styleSheets.Add(sheet);
        }
        foreach (StyleSheet sheet in styleSheets)
        {
            root.styleSheets.Add(sheet);
        }

        loadingScreen = UITK.AddElement(root, "loadingScreen");

        LoadingScreenSO randomScreenCFG = loadingScreenConfig[Random.Range(0, loadingScreenConfig.Length)];

        blackScreen = UITK.AddElement(loadingScreen, "blackScreen");
        blackScreen.style.display = DisplayStyle.None;

        firstSlide = BuildFirstSlide(randomScreenCFG);
        firstSlide.style.display = DisplayStyle.None;

        secondSlide = BuildSecondSlide(randomScreenCFG);
        secondSlide.style.display = DisplayStyle.None;

        thirdSlide = BuildThirdSlide(randomScreenCFG);
        thirdSlide.style.display = DisplayStyle.None;
    }

    private VisualElement BuildFirstSlide(LoadingScreenSO randomScreenCFG)
    {
        var firstSlide = UITK.AddElement(loadingScreen, "Slide", "firstSlide");

        var firstSlideText = UITK.AddElement<Label>(firstSlide, "H1", "SlideText", "firstSlideText");
        firstSlideText.text = randomScreenCFG.firstSlideText;

        return firstSlide;
    }

    private VisualElement BuildSecondSlide(LoadingScreenSO randomScreenCFG)
    {
        var secondSlide = UITK.AddElement(loadingScreen, "Slide", "secondSlide");

        var secondSlideText = UITK.AddElement<Label>(secondSlide, "H1", "SlideText", "secondSlideText");
        secondSlideText.text = randomScreenCFG.secondSlideText;

        return secondSlide;
    }

    private VisualElement BuildThirdSlide(LoadingScreenSO randomScreenCFG)
    {
        var thirdSlide = UITK.AddElement(loadingScreen, "Slide", "thirdSlide");

        var topSection = UITK.AddElement(thirdSlide, "Frame", "thirdTopSection");
        var midSection = UITK.AddElement(thirdSlide, "MidSection", "thirdMidSection");
        var bottomSection = UITK.AddElement(thirdSlide, "Frame", "thirdBottomSection");

        var thirdSlideText = UITK.AddElement<Label>(midSection, "H1", "SlideText", "thirdSlideText");
        thirdSlideText.text = randomScreenCFG.thirdSlideText;

        var slideUndertext = UITK.AddElement<Label>(midSection, "P1", "SlideUndertext", "thirdSlideUndertext");
        slideUndertext.text = randomScreenCFG.thirdSlideUndertext;

        return thirdSlide;
    }

    private async void AnimateLoadingScreen()
    {
        await Awaitable.NextFrameAsync();
        await Awaitable.NextFrameAsync();
        var duration = 1.0f;

        blackScreen.style.display = DisplayStyle.Flex;
        blackScreen.AddToClassList("Activated");
        await Awaitable.WaitForSecondsAsync(duration);
        blackScreen.style.display = DisplayStyle.None;

        firstSlide.style.display = DisplayStyle.Flex;
        firstSlide.AddToClassList("Activated");
        await Awaitable.WaitForSecondsAsync(duration);
        firstSlide.style.display = DisplayStyle.None;

        secondSlide.style.display = DisplayStyle.Flex;
        secondSlide.AddToClassList("Activated");
        await Awaitable.WaitForSecondsAsync(duration);
        secondSlide.style.display = DisplayStyle.None;

        thirdSlide.style.display = DisplayStyle.Flex;
        thirdSlide.AddToClassList("Activated");
    }


    private void Awake()
    {
        BuildLoadingScreen();
        AnimateLoadingScreen();
    }
}
