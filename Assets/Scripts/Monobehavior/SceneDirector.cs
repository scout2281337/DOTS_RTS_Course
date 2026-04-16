using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneDirector : Singleton<SceneDirector>
{
    [Header("SceneDirector")]
    public static string CORE = "CoreScene";
    public static string BATTLE = "BattleScene";
    public static string MAINMENU = "MainMenuLobby";

    [SerializeField] private bool _isFirstLaunch = true;

    public static string CITY = "City";

    private const string LOADINGSCREEN = "LoadingScreen";

    private static bool _isLoading = false;



    public static async void OpenSceneThroughLoadingScreen(string sceneName)
    {
        if (_isLoading) return;
        _isLoading = true;

        // Load loading screen
        await Awaitable.FromAsyncOperation(SceneManager.LoadSceneAsync(LOADINGSCREEN, LoadSceneMode.Additive));
        await Awaitable.WaitForSecondsAsync(1f); // Wait until UI is fully faded in

        // Unloading previous scenes
        await UnloadScenes(sceneName);

        // Start loading target scene
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        loadOp.allowSceneActivation = false; // We don't activate yet, so that we can wait for loading screen animation

        // Wait until BOTH: Scene is loaded and Animation is completed
        Awaitable timer = Awaitable.WaitForSecondsAsync(5f);
        while (loadOp.progress < 0.9f || !timer.IsCompleted)
        {
            await Awaitable.NextFrameAsync();
        }

        // Activate scene and wait one frame so activation completes
        loadOp.allowSceneActivation = true;
        await Awaitable.NextFrameAsync();

        var newScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(newScene);

        // Delay before unloading for fading animation,
        await Awaitable.WaitForSecondsAsync(1f);
        await Awaitable.FromAsyncOperation(SceneManager.UnloadSceneAsync(LOADINGSCREEN));

        _isLoading = false;
    }

    private static async Task UnloadScenes(string ExceptionScene)
    {
        foreach (var scene in GetAllScenes())
        {
            if (scene.name == LOADINGSCREEN
                || scene.name == CORE
                || scene.name == ExceptionScene) continue;

            await Awaitable.FromAsyncOperation(SceneManager.UnloadSceneAsync(scene));
        }
    }

    private static Scene[] GetAllScenes()
    {
        int count = SceneManager.sceneCount;
        Scene[] scenes = new Scene[count];

        for (int i = 0; i < count; i++)
            scenes[i] = SceneManager.GetSceneAt(i);

        return scenes;
    }

    private async void LoadMainMenu()
    {
        await UnloadScenes("None");

        AsyncOperation loadMenu = SceneManager.LoadSceneAsync(MAINMENU, LoadSceneMode.Additive);
        loadMenu.allowSceneActivation = false;

        AsyncOperation loadCity = SceneManager.LoadSceneAsync(CITY, LoadSceneMode.Additive);
        loadCity.allowSceneActivation = false;

        while (loadMenu.progress < 0.9f || loadCity.progress < 0.9f)
        {
            await Awaitable.NextFrameAsync();
        }

        // Activate scene and wait one frame so activation completes
        loadMenu.allowSceneActivation = true;
        loadCity.allowSceneActivation = true;
        await Awaitable.NextFrameAsync();
    }

    protected override void Awake()
    {
        base.Awake();

        if (_isFirstLaunch)
            LoadMainMenu();
    }
}