using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneDirector : Singleton<SceneDirector>
{
    [Header("SceneDirector")]
    public static string CORE = "CoreScene";
    public static string BATTLE = "BattleScene";
    public static string MAINMENU = "MainMenuLobby";
    public static string CITY = "City";

    public const string LOADINGSCREEN = "LoadingScreen";
    public const string WINLOADINGSCREEN = "WinLoadingScreen";
    public const string LOSELOADINGSCREEN = "LoseLoadingScreen";

    [SerializeField] private bool _isFirstLaunch = true;

    private static bool _isLoading = false;


    public static async void OpenScenesThroughLoadingScreen(string loadingScreen = LOADINGSCREEN, params string[] sceneNames)
    {
        if (_isLoading) return;
        _isLoading = true;

        // Loading loading screen
        await AddScenes(loadingScreen);

        // Wait for fade in
        await Awaitable.WaitForSecondsAsync(1f);

        // Unload previous scenes
        await UnloadAllScenesExcept(sceneNames);

        // Loading all scenes with timer
        Awaitable timer = Awaitable.WaitForSecondsAsync(5f);
        Awaitable scenes = AddScenes(sceneNames);
        while (!timer.IsCompleted || !scenes.IsCompleted)
        {
            await Awaitable.NextFrameAsync();
        }

        await Awaitable.WaitForSecondsAsync(1f);
        await Awaitable.FromAsyncOperation(SceneManager.UnloadSceneAsync(LOADINGSCREEN));

        _isLoading = false;
    }

    private static async Awaitable AddScenes(params string[] sceneNames)
    {
        List<AsyncOperation> loadOps = new();

        // Start loading all scenes
        foreach (var scene in sceneNames)
        {
            var op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            op.allowSceneActivation = false;
            loadOps.Add(op);
        }

        // Wait until timer is up and all scenes are ready
        while (!loadOps.All(op => op.progress >= 0.9f))
        {
            await Awaitable.NextFrameAsync();
        }

        // Activate all scenes
        foreach (var op in loadOps)
            op.allowSceneActivation = true;

        await Awaitable.NextFrameAsync(); // wait one frame to make sure all scenes are fully loaded

        // Set first scene as active
        var newScene = SceneManager.GetSceneByName(sceneNames[0]);
        SceneManager.SetActiveScene(newScene);
    }

    private static async Awaitable UnloadAllScenesExcept(params string[] exceptionScenes)
    {
        var exceptions = new HashSet<string>(exceptionScenes) { LOADINGSCREEN, CORE };

        foreach (var scene in GetAllScenes())
        {
            if (exceptions.Contains(scene.name)) continue;

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
        await AddScenes(MAINMENU, CITY);
    }


    protected override void Awake()
    {
        base.Awake();

        if (_isFirstLaunch) LoadMainMenu();
    }
}
