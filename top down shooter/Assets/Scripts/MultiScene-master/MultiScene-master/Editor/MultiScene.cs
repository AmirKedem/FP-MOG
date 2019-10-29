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
