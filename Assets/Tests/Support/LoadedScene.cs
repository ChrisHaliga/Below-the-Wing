using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BelowTheWing.Tests.Support
{
    public static class LoadedScene
    {
        public static IEnumerator Open(string name, int settleFrames = 3)
        {
            SceneManager.LoadScene(name, LoadSceneMode.Single);

            for (var frame = 0; frame < settleFrames; frame++)
            {
                yield return null;
            }
        }

        public static IEnumerator Close()
        {
            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.IsListening)
                {
                    NetworkManager.Singleton.Shutdown();
                }

                Object.DestroyImmediate(NetworkManager.Singleton.gameObject);
            }

            var clean = SceneManager.CreateScene($"Clean {Random.Range(0, int.MaxValue)}");
            SceneManager.SetActiveScene(clean);

            for (var loaded = SceneManager.sceneCount - 1; loaded >= 0; loaded--)
            {
                var scene = SceneManager.GetSceneAt(loaded);

                if (scene != clean && scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }

            yield return null;
        }
    }
}
