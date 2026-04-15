using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneDirector
{
    public const string EssentialsSceneName = "EssentialsScene";
    public const string BattleSceneName = "BattleScene";
    public const string MainMenuName = "MainMenuLobby";

    private const string LoadingSceneName = "LoadingScreen";

    private static bool isLoading = false;


    public static async void OpenSceneThroughLoadingScreen(string sceneName)
    {
        if (isLoading) return;
        isLoading = true;

        // Load loading screen
        await Awaitable.FromAsyncOperation(SceneManager.LoadSceneAsync(LoadingSceneName, LoadSceneMode.Additive));
        await Awaitable.WaitForSecondsAsync(1f); // Wait until UI is fully faded in

        // Unload previous scenes
        foreach (var scene in GetAllScenes())
        {
            if (scene.name == LoadingSceneName 
                || scene.name == EssentialsSceneName 
                || scene.name == sceneName) continue;
            
            await Awaitable.FromAsyncOperation(SceneManager.UnloadSceneAsync(scene));
        }

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
        await Awaitable.FromAsyncOperation(SceneManager.UnloadSceneAsync(LoadingSceneName));

        isLoading = false;
    }


    private static Scene[] GetAllScenes()
    {
        int count = SceneManager.sceneCount;
        Scene[] scenes = new Scene[count];

        for (int i = 0; i < count; i++)
            scenes[i] = SceneManager.GetSceneAt(i);

        return scenes;
    }
}