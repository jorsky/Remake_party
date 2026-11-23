using System;
using System.IO;
using RemakeParty.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if REMAKE_URP
using UnityEngine.Rendering.Universal;
#endif

namespace RemakeParty.EditorTools
{
    /// <summary>
    /// Runs automatically the first time the project opens in Unity:
    /// sets up the Universal Render Pipeline and creates the Main scene with the Match in it.
    /// Run it again any time from the menu: Remake Party > Set Up Project.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string SettingsFolder = "Assets/Settings";
        private const string PipelinePath = SettingsFolder + "/URP-Pipeline.asset";
        private const string RendererPath = SettingsFolder + "/URP-Renderer.asset";

        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists(ScenePath)) Run();
            };
        }

        [MenuItem("Remake Party/Set Up Project")]
        public static void Run()
        {
            SetUpRenderPipeline();
            CreateMainScene();
            Debug.Log("Remake Party: project set up. Press Play to watch a Match.");
        }

        [MenuItem("Remake Party/Open Main Scene")]
        public static void OpenMainScene()
        {
            if (!File.Exists(ScenePath)) Run();
            else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        private static void SetUpRenderPipeline()
        {
#if REMAKE_URP
            try
            {
                if (GraphicsSettings.defaultRenderPipeline != null) return;

                EnsureFolder(SettingsFolder);

                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
                if (renderer == null)
                {
                    renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(renderer, RendererPath);
                }

                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
                if (pipeline == null)
                {
                    pipeline = UniversalRenderPipelineAsset.Create(renderer);
                    AssetDatabase.CreateAsset(pipeline, PipelinePath);
                }

                GraphicsSettings.defaultRenderPipeline = pipeline;

                int current = QualitySettings.GetQualityLevel();
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = pipeline;
                }
                QualitySettings.SetQualityLevel(current, false);

                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                // The grey box still renders without URP, so carry on.
                Debug.LogWarning($"Remake Party: couldn't set up URP automatically ({e.Message}). Using the built-in renderer.");
            }
#endif
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void CreateMainScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder("Assets/Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var match = new GameObject("Match");
            match.AddComponent<MatchDirector>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }
    }
}
