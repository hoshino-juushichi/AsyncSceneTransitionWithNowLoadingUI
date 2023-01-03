using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ebitender
{
	public class SceneTransitionController : MonoBehaviour
	{
		private static SceneTransitionController _instance;
		public static SceneTransitionController instance => _instance;

		private AsyncSceneLoader _asyncSceneLoader;
		public AsyncSceneLoader asyncSceneLoader => _asyncSceneLoader;

		void Awake()
		{
			_instance = this;
			_asyncSceneLoader= new AsyncSceneLoader();
			_asyncSceneLoader.debugYieldPrint = true;
			_asyncSceneLoader.debugDelay = 1000;

			GameObject.DontDestroyOnLoad(gameObject);
		}

		void OnDestroy()
		{
			_instance = null;
		}
	}
}