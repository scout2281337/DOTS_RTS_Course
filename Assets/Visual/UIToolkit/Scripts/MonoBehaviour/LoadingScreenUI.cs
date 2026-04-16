using UnityEngine;
using UnityEngine.UIElements;

public class LoadingScreenUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private StyleSheet[] _styleSheets;
    [SerializeField] private LoadingScreenSO[] _loadingScreenConfig;

    private VisualElement _loadingScreen;
    private VisualElement _blackScreen;
    private VisualElement _firstSlide;
    private VisualElement _secondSlide;
    private VisualElement _thirdSlide;


    private void BuildLoadingScreen()
    {
        VisualElement root = _uiDocument.rootVisualElement;
        root.Clear();

        ViewController _UIController = ViewController.Instance;
        foreach (StyleSheet sheet in _UIController.DefaultStyleSheet.BaseStyles)
        {
            root.styleSheets.Add(sheet);
        }
        foreach (StyleSheet sheet in _styleSheets)
        {
            root.styleSheets.Add(sheet);
        }

        _loadingScreen = UITK.AddElement(root, "loadingScreen");

        LoadingScreenSO randomScreenCFG = _loadingScreenConfig[Random.Range(0, _loadingScreenConfig.Length)];

        _blackScreen = UITK.AddElement(_loadingScreen, "blackScreen");
        _blackScreen.style.display = DisplayStyle.None;

        _firstSlide = BuildFirstSlide(randomScreenCFG);
        _firstSlide.style.display = DisplayStyle.None;

        _secondSlide = BuildSecondSlide(randomScreenCFG);
        _secondSlide.style.display = DisplayStyle.None;

        _thirdSlide = BuildThirdSlide(randomScreenCFG);
        _thirdSlide.style.display = DisplayStyle.None;
    }

    private VisualElement BuildFirstSlide(LoadingScreenSO randomScreenCFG)
    {
        var firstSlide = UITK.AddElement(_loadingScreen, "Slide", "firstSlide");

        var firstSlideText = UITK.AddElement<Label>(firstSlide, "H1", "SlideText", "firstSlideText");
        firstSlideText.text = randomScreenCFG.FirstSlideText;

        return firstSlide;
    }

    private VisualElement BuildSecondSlide(LoadingScreenSO randomScreenCFG)
    {
        var secondSlide = UITK.AddElement(_loadingScreen, "Slide", "secondSlide");

        var secondSlideText = UITK.AddElement<Label>(secondSlide, "H1", "SlideText", "secondSlideText");
        secondSlideText.text = randomScreenCFG.SecondSlideText;

        return secondSlide;
    }

    private VisualElement BuildThirdSlide(LoadingScreenSO randomScreenCFG)
    {
        var thirdSlide = UITK.AddElement(_loadingScreen, "Slide", "thirdSlide");

        var topSection = UITK.AddElement(thirdSlide, "Frame", "thirdTopSection");
        var midSection = UITK.AddElement(thirdSlide, "MidSection", "thirdMidSection");
        var bottomSection = UITK.AddElement(thirdSlide, "Frame", "thirdBottomSection");

        var thirdSlideText = UITK.AddElement<Label>(midSection, "H1", "SlideText", "thirdSlideText");
        thirdSlideText.text = randomScreenCFG.ThirdSlideText;

        var slideUndertext = UITK.AddElement<Label>(midSection, "P1", "SlideUndertext", "thirdSlideUndertext");
        slideUndertext.text = randomScreenCFG.ThirdSlideUndertext;

        return thirdSlide;
    }

    private async void AnimateLoadingScreen()
    {
        await Awaitable.NextFrameAsync();
        await Awaitable.NextFrameAsync();
        var duration = 1.0f;

        _blackScreen.style.display = DisplayStyle.Flex;
        _blackScreen.AddToClassList("Activated");
        await Awaitable.WaitForSecondsAsync(duration);
        _blackScreen.style.display = DisplayStyle.None;

        _firstSlide.style.display = DisplayStyle.Flex;
        _firstSlide.AddToClassList("Activated");
        await Awaitable.WaitForSecondsAsync(duration);
        _firstSlide.style.display = DisplayStyle.None;

        _secondSlide.style.display = DisplayStyle.Flex;
        _secondSlide.AddToClassList("Activated");
        await Awaitable.WaitForSecondsAsync(duration);
        _secondSlide.style.display = DisplayStyle.None;

        _thirdSlide.style.display = DisplayStyle.Flex;
        _thirdSlide.AddToClassList("Activated");
    }


    private void Awake()
    {
        BuildLoadingScreen();
        AnimateLoadingScreen();
    }
}
