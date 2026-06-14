using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow
{
    public static class HyphlowRuntimeBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BootstrapOnRuntimeLoad()
        {
            EnsureHyphlowReady();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void EnsureHyphlowReady()
        {
            if (hyphlowRoot != null)
            {
                return;
            }

            var managerPrefab = Resources.Load<GameObject>(_pathToHyphlowManagerPrefab);
            hyphlowRoot = UnityObj.Instantiate(managerPrefab);
            UnityObj.DontDestroyOnLoad(hyphlowRoot);
            hyphlowRoot.name = managerPrefab.name;

            RootBootstrapper.EnsureRoot();
            var atMyceliaRoot = RootBootstrapper.Root.gameObject;
            hyphlowRoot.transform.SetParent(atMyceliaRoot.transform, true);
        }

        private static GameObject hyphlowRoot;

        private static readonly string _pathToHyphlowManagerPrefab = 
            "Runtime/Prefabs/Hyphlow";

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureHyphlowReady();
        }
    }
}