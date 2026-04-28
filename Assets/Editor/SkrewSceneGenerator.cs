#if UNITY_EDITOR
using System.IO;
using ScrewGame.Core;
using ScrewGame.Diagnostics;
using ScrewGame.StateMachines;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScrewGame.EditorTools
{
    /// <summary>
    /// One-shot generator that creates the three baseline scenes
    /// (Bootstrap, Menu, Gameplay), wires up singletons, and registers the
    /// scenes in <c>EditorBuildSettings</c>.
    ///
    /// Hand-authoring <c>.unity</c> YAML is fragile across Unity versions, so
    /// we let the Unity API do it. Run <b>Tools ▸ Skrew ▸ Generate Scenes</b>
    /// once after pulling — subsequent runs overwrite the generated scenes
    /// (any manual additions inside them will be lost).
    /// </summary>
    public static class SkrewSceneGenerator
    {
        private const string SceneFolder    = "Assets/Scenes/Generated";
        private const string BootstrapPath  = SceneFolder + "/Bootstrap.unity";
        private const string MenuPath       = SceneFolder + "/Menu.unity";
        private const string GameplayPath   = SceneFolder + "/Gameplay.unity";

        [MenuItem("Tools/Skrew/Generate Scenes")]
        public static void GenerateAll()
        {
            if (!EditorUtility.DisplayDialog(
                    "Generate Skrew scenes?",
                    $"This will overwrite:\n  {BootstrapPath}\n  {MenuPath}\n  {GameplayPath}\n\nProceed?",
                    "Generate",
                    "Cancel"))
                return;

            Directory.CreateDirectory(SceneFolder);

            CreateBootstrapScene();
            CreateMenuScene();
            CreateGameplayScene();
            RegisterInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SkrewSceneGenerator] Done. Open Bootstrap.unity and press Play to run the smoke test.");
        }

        // -----------------------------------------------------------------
        // Bootstrap — singletons + SmokeTest runner
        // -----------------------------------------------------------------
        private static void CreateBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var managers = new GameObject("_Managers");
            managers.AddComponent<SupabaseManager>();
            managers.AddComponent<GameStateMachine>();

            var diagnostics = new GameObject("_Diagnostics");
            diagnostics.AddComponent<SmokeTest>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, BootstrapPath);
        }

        // -----------------------------------------------------------------
        // Menu — placeholder canvas + camera, no logic yet
        // -----------------------------------------------------------------
        private static void CreateMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var canvasGo = new GameObject("MenuCanvas",
                typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler),
                typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            // EventSystem so any future UI works out of the box.
            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MenuPath);
        }

        // -----------------------------------------------------------------
        // Gameplay — placeholder table layout (camera, canvas, state machine)
        // -----------------------------------------------------------------
        private static void CreateGameplayScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // The state machine lives here too so gameplay-only test runs work
            // even without going through Bootstrap first.
            var managers = new GameObject("_GameplayManagers");
            managers.AddComponent<GameStateMachine>();

            var canvasGo = new GameObject("HUDCanvas",
                typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler),
                typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameplayPath);
        }

        // -----------------------------------------------------------------
        // Build settings — Bootstrap first (entry scene)
        // -----------------------------------------------------------------
        private static void RegisterInBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapPath, true),
                new EditorBuildSettingsScene(MenuPath,      true),
                new EditorBuildSettingsScene(GameplayPath,  true),
            };
            EditorBuildSettings.scenes = scenes;
        }

        // -----------------------------------------------------------------
        // Convenience: jump to Bootstrap
        // -----------------------------------------------------------------
        [MenuItem("Tools/Skrew/Open Bootstrap Scene")]
        public static void OpenBootstrap()
        {
            if (!File.Exists(BootstrapPath))
            {
                Debug.LogError("Bootstrap scene not found. Run Tools ▸ Skrew ▸ Generate Scenes first.");
                return;
            }
            EditorSceneManager.OpenScene(BootstrapPath, OpenSceneMode.Single);
        }
    }
}
#endif
