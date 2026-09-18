using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BelowTheWing.EditorTools
{
    public static class SceneIdentities
    {
        [MenuItem("Below the Wing/Give the apron scene's objects their identities")]
        public static void GiveThemOut()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("Leaving the apron scene alone.");
                return;
            }

            ApronScene.GiveTheSceneObjectsTheirIdentities();
        }
    }
}
