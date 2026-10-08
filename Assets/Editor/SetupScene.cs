using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RepoCity;

public static class SetupScene
{
    const string ScenePath = "Assets/Scenes/RepoCity.unity";

    [MenuItem("Repo City/Create Main Scene")]
    public static void Create()
    {
        System.IO.Directory.CreateDirectory("Assets/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var bootstrapperGo = new GameObject("Bootstrapper");
        bootstrapperGo.AddComponent<SceneBootstrapper>();

        EditorSceneManager.SaveScene(scene, ScenePath);

        var existing = EditorBuildSettings.scenes;
        bool alreadyListed = false;
        foreach (var s in existing)
        {
            if (s.path == ScenePath) alreadyListed = true;
        }
        if (!alreadyListed)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(existing);
            list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        Debug.Log($"SetupScene: created {ScenePath} with Bootstrapper, added to Build Settings.");
    }
}
