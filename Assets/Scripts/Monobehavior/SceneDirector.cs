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


    public static async void OpenScenesThroughLoadingScreen(string loadingScreen, params string[] sceneNames)
    {
        if (_isLoading)
            return;

        _isLoading = true;

        try
        {
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
            await UnloadSceneIfLoaded(loadingScreen);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static async Awaitable AddScenes(params string[] sceneNames)
    {
        List<AsyncOperation> loadOps = new();

        // Start loading all scenes
        foreach (var scene in sceneNames)
        {
            if (string.IsNullOrWhiteSpace(scene))
                continue;

            if (SceneManager.GetSceneByName(scene).isLoaded)
                continue;

            var op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogWarning($"{nameof(SceneDirector)} could not start loading scene '{scene}'. Check Build Settings scene list.");
                continue;
            }

            op.allowSceneActivation = false;
            loadOps.Add(op);
        }

        if (loadOps.Count == 0)
            return;

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
        Scene newScene = GetFirstLoadedScene(sceneNames);
        if (newScene.IsValid() && newScene.isLoaded)
            SceneManager.SetActiveScene(newScene);
    }

    private static async Awaitable UnloadAllScenesExcept(params string[] exceptionScenes)
    {
        var exceptions = new HashSet<string>(exceptionScenes ?? new string[0])
        {
            CORE,
            LOADINGSCREEN,
            WINLOADINGSCREEN,
            LOSELOADINGSCREEN
        };

        foreach (var scene in GetAllScenes())
        {
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            if (exceptions.Contains(scene.name)) continue;

            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(scene);
            if (unloadOperation == null)
            {
                Debug.LogWarning($"{nameof(SceneDirector)} could not unload scene '{scene.name}'. It may already be unloading or not be unloadable.");
                continue;
            }

            await Awaitable.FromAsyncOperation(unloadOperation);
        }
    }

    private static async Awaitable UnloadSceneIfLoaded(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(scene);
        if (unloadOperation == null)
            return;

        await Awaitable.FromAsyncOperation(unloadOperation);
    }

    private static Scene GetFirstLoadedScene(params string[] sceneNames)
    {
        if (sceneNames == null)
            return default;

        for (int i = 0; i < sceneNames.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(sceneNames[i]))
                continue;

            Scene scene = SceneManager.GetSceneByName(sceneNames[i]);
            if (scene.IsValid() && scene.isLoaded)
                return scene;
        }

        return default;
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
