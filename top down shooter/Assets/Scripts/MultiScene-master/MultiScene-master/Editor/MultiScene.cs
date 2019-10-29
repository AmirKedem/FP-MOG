using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MS
{
    public class MultiScene : ScriptableObject
    {
        public GUIDSceneSetup[] sceneSetups;

        public void UpdateSceneSetups(SceneSetup[] newSceneSetups)
        {
            var ss = new GUIDSceneSetup[newSceneSetups.Length];
            for (int i = 0; i < newSceneSetups.Length; i++)
            {
                ss[i] = new GUIDSceneSetup(newSceneSetups[i]);
            }
            sceneSetups = ss;
        }

        public SceneSetup[] ToSceneSetups()
        {
            var result = new SceneSetup[sceneSetups.Length];
            for (int i = 0; i < sceneSetups.Length; i++)
            {
                result[i] = sceneSetups[i].ToSceneSetup();
            }
            return result;
        }

        public string ReturnScenesNames()
        {
            string ret = string.Format("{0} Scenes", sceneSetups.Length);
            
            foreach (var sceneSetup in sceneSetups)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneSetup.guid);
                var filename = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                ret += "\n" + filename + " - MultiScene";
                ret += "\n" + string.Format("path: {0}", scenePath);
                ret += "\n" + string.Format("guid: {0}", sceneSetup.guid);
                ret += "\n" + string.Format("Active: {0}", sceneSetup.isActive ? "Yes" : "No");
                ret += "\n" + string.Format("Loaded: {0}", sceneSetup.isLoaded ? "Yes" : "No");
                ret += "\n";
            }
            
            return ret;
        }
    }

    // MultiScene's SceneSetup uses an AssetDatabase GUID instead of a Scene path.
    [Serializable]
    public struct GUIDSceneSetup
    {
        public string guid;
        public bool isActive;
        public bool isLoaded;

        public GUIDSceneSetup(SceneSetup sceneSetup)
        {
            guid = AssetDatabase.AssetPathToGUID(sceneSetup.path);
            isActive = sceneSetup.isActive;
            isLoaded = sceneSetup.isLoaded;
        }

        public SceneSetup ToSceneSetup()
        {
            var sceneSetup = new SceneSetup();
            sceneSetup.path = AssetDatabase.GUIDToAssetPath(guid);
            sceneSetup.isActive = isActive;
            sceneSetup.isLoaded = isLoaded;
            return sceneSetup;
        }
    }
}
