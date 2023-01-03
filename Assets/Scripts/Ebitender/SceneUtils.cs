using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#nullable enable

namespace Ebitender
{
	public static class SceneUtils
	{
		public static void TrimAdditiveScene(Scene scene)
		{
			foreach (var go in scene.GetRootGameObjects())
			{
				var audioListeners = go.GetComponentsInChildren<AudioListener>(true);
				foreach (var al in audioListeners)
				{
#if UNITY_EDITOR
					UnityEngine.Object.DestroyImmediate(al);
#else
					UnityEngine.Object.Destroy(al);
#endif
				}
				var eventSystems = go.GetComponentsInChildren<EventSystem>(true);
				foreach (var es in eventSystems)
				{
#if UNITY_EDITOR
					UnityEngine.Object.DestroyImmediate(es.gameObject);
#else
					UnityEngine.Object.Destroy(es.gameObject);
#endif
				}
			}
		}
	}
}