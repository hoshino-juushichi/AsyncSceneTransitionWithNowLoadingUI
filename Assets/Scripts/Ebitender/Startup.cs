using System.Data.Common;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityEngine.Video;

#nullable enable

namespace Ebitender
{
	public static class Startup
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void InitializeBeforeSceneLoad()
		{
			var go = new GameObject();
			go.name = "GameManager Stuff";
			go.AddComponent<SceneTransitionController>();

			AsyncOperationHandle<GameObject> op = Addressables.LoadAssetAsync<GameObject>("now_loading.prefab");
			op.WaitForCompletion();
			GameObject.Instantiate(op.Result);
		}

	}
}