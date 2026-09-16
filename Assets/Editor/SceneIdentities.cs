using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BelowTheWing.EditorTools
{
    public static class SceneIdentities
    {
        const string ScenePath = "Assets/Scenes/Apron.unity";

        [MenuItem("Below the Wing/Give the apron scene's objects their identities")]
        public static void GiveThemOut()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("Leaving the apron scene alone.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}
