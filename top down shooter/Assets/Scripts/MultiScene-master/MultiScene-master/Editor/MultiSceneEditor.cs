using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Callbacks;

namespace MS
{
    [CustomEditor(typeof(MultiScene))]
    public class ConfigurationEditor : Editor
    {
        [OnOpenAsset(1)]
        public static bool OpenAssetHandler(int instanceID, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);

            if (obj is MultiScene)
            {
                Open((MultiScene)obj);
                return true; // open handled
            }

            return false; // open not handled
        }

        [MenuItem("Assets/Create/MultiScene")]
        static void CreateMultiScene()
        {
            var multiScene = New();

            // ProjectWindowUtil is not fully documented, but is the only way I could figure out
            // to get the Right Click > Create asset behavior to match what the
            // CreateAssetMenu attribute does. I couldn't use CreateAssetMenu because 
            // I needed to do some work to initialize the instance outside of the constructor.

            ProjectWindowUtil.CreateAsset(multiScene, "New MultiScene.asset");
        }

        static MultiScene New()
        {
            var conf = ScriptableObject.CreateInstance<MultiScene>();
            Update(conf);
            return conf;
        }

        static void Update(MultiScene multiScene)
        {
            multiScene.UpdateSceneSetups(EditorSceneManager.GetSceneManagerSetup());
        }

        static void Open(MultiScene multiScene)
        {
            bool cancelled = !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            if (cancelled)
            {
                return;
            }

            EditorSceneManager.RestoreSceneManagerSetup(multiScene.ToSceneSetups());
        }

        void Undo(string name)
        {
            UnityEditor.Undo.RecordObject(target, name);
            EditorUtility.SetDirty(target);
        }

        public override void OnInspectorGUI()
        {
            var multiScene = (MultiScene)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open", GUILayout.ExpandWidth(false)))
                {
                    Open(multiScene);
                }

                if (GUILayout.Button("Update", GUILayout.ExpandWidth(false)))
                {
                    bool confirm = EditorUtility.DisplayDialog("Update Existing Configuration?", "Are you sure you want to overwrite the existing scene configuration?", "Update", "Cancel");

                    if (confirm)
                    {
                        Undo("Update Multiscene");
                        Update(multiScene);
                    }
                }
            }

            GUILayout.Label(string.Format("{0} Scenes", multiScene.sceneSetups.Length), EditorStyles.boldLabel);
            foreach (var sceneSetup in multiScene.sceneSetups)
            {
                using (var sceneSetupScope = new EditorGUILayout.VerticalScope())
                {
                    var scenePath = AssetDatabase.GUIDToAssetPath(sceneSetup.guid);
                    var filename = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                    GUILayout.Label(filename, EditorStyles.boldLabel);
                    GUILayout.Label(string.Format("path: {0}", scenePath));
                    GUILayout.Label(string.Format("guid: {0}", sceneSetup.guid));
                    GUILayout.Label(string.Format("Active: {0}", sceneSetup.isActive ? "Yes" : "No"));
                    GUILayout.Label(string.Format("Loaded: {0}", sceneSetup.isLoaded ? "Yes" : "No"));
                    GUILayout.Space(10);
                }
            }
        }
    }
}
