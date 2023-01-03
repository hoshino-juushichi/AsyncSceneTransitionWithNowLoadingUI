using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.ResourceProviders;
using System.Threading.Tasks;
using static Ebitender.SceneController;


#nullable enable

namespace Ebitender
{
	public class SceneController : MonoBehaviour
	{
		public enum LoaderType
		{
			SceneManager,
			Addressables,
		};
		[SerializeField] LoaderType _loaderType;
		[SerializeField] string _nextSceneName = string.Empty;
		[SerializeField] string _nextSceneAddress = string.Empty;
		[SerializeField] string _additiveSceneName = string.Empty;
		[SerializeField] string _additiveSceneAddress = string.Empty;

		[SerializeField] Button _buttonChangeScene = null!;
		[SerializeField] Button _buttonLoadAdditive = null!;
		[SerializeField] Button _buttonUnloadAdditive = null!;
		[SerializeField] InputField _inputFieldDebugDelay = null!;

		Scene? _addedScene;
		SceneInstance? _addedSceneInstance;

		private void Start()
		{
			_buttonChangeScene.onClick.AddListener(async () =>
			{
				if (_loaderType == LoaderType.SceneManager)
				{
					_ = SceneTransitionController.instance.asyncSceneLoader.LoadSceneBySceneManagerAsync(_nextSceneName, LoadSceneMode.Single);
				}
				else
				{
					_ = SceneTransitionController.instance.asyncSceneLoader.LoadSceneByAddressablesAsync(_nextSceneAddress, LoadSceneMode.Single);
				}

				int count = 0;
				NowLoadingUI.instance.isActive = true;
				while (SceneTransitionController.instance.asyncSceneLoader.isProcessing)
				{
					Debug.Log($"SceneController ChangeScene yield:{count}");
					await Task.Yield();
				}
				NowLoadingUI.instance.isActive = false;

			});

			_buttonLoadAdditive.onClick.AddListener(async () =>
			{
				_buttonLoadAdditive.interactable = false;

				var audioListener = Camera.main.GetComponent<AudioListener>();
				if (audioListener != null)
				{
					audioListener.enabled = false;
				}

				if (_loaderType == LoaderType.SceneManager)
				{
					_ = SceneTransitionController.instance.asyncSceneLoader.LoadSceneBySceneManagerAsync(_additiveSceneName, LoadSceneMode.Additive, onLoadCompleted: (scene) =>
					{
						_addedScene = scene;
						SceneUtils.TrimAdditiveScene(_addedScene.Value);
						_buttonUnloadAdditive.interactable = true;
					});
				}
				else
				{
					_ = SceneTransitionController.instance.asyncSceneLoader.LoadSceneByAddressablesAsync(_additiveSceneAddress, LoadSceneMode.Additive, onLoadCompleted: (sceneInstance) =>
					{
						_addedSceneInstance = sceneInstance;
						SceneUtils.TrimAdditiveScene(_addedSceneInstance.Value.Scene);
						_buttonUnloadAdditive.interactable = true;
					});
				}

				int count = 0;
				NowLoadingUI.instance.isActive = true;
				while (SceneTransitionController.instance.asyncSceneLoader.isProcessing)
				{
					Debug.Log($"SceneController LoadAdditive yield:{count}");
					count++;
					await Task.Yield();
				}
				NowLoadingUI.instance.isActive = false;
				if (audioListener != null)
				{
					audioListener.enabled = false;
				}

			});

			_buttonUnloadAdditive.interactable = false;
			_buttonUnloadAdditive.onClick.AddListener(async () =>
			{
				_buttonUnloadAdditive.interactable = false;

				if (_loaderType == LoaderType.SceneManager)
				{
					if (_addedScene == null)
						throw new InvalidOperationException("No Scene");

					_ = SceneTransitionController.instance.asyncSceneLoader.UnloadSceneBySceneManagerAsync(_addedScene.Value, onUnloadCompleted: () =>
					{
						_addedScene = null;
						_buttonLoadAdditive.interactable = true;
					});
				}
				else
				{
					if (_addedSceneInstance == null)
						throw new InvalidOperationException("No Scene Instance");

					_ = SceneTransitionController.instance.asyncSceneLoader.UnloadSceneByAddressablesAsync(_addedSceneInstance.Value, onUnloadCompleted: () =>
					{
						_addedSceneInstance = null;
						_buttonLoadAdditive.interactable = true;
					});
				}

				int count = 0;
				NowLoadingUI.instance.isActive = true;
				while (SceneTransitionController.instance.asyncSceneLoader.isProcessing)
				{
					Debug.Log($"SceneController UnloadAdditive yield:{count}");
					count++;
					await Task.Yield();
				}
				NowLoadingUI.instance.isActive = false;

			});

			_inputFieldDebugDelay.text = SceneTransitionController.instance.asyncSceneLoader.debugDelay.ToString();
			_inputFieldDebugDelay.onValueChanged.AddListener((value) =>
			{
				if (int.TryParse(value, out var milliseconds))
				{
					SceneTransitionController.instance.asyncSceneLoader.debugDelay = milliseconds;
				}
			});

		}
	}
}