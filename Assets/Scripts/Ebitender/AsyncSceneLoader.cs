using System;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using Unity.Burst.CompilerServices;

#nullable enable

namespace Ebitender
{
	public class AsyncSceneLoader
	{
		private CancellationTokenSource? _cancellation;

		private bool _isProcessing;
		public bool isProcessing => _isProcessing;

		public int debugDelay { get; set; } = 0;
		public bool debugYieldPrint { get; set; } = false;

		public AsyncSceneLoader()
		{
		}

		private void CheckPerform(string methodName)
		{
			if (!ThreadUtils.isMainThread)
			{
				Debug.LogError($"{methodName} should be called in main thread.");
				throw new InvalidOperationException($"{methodName} should be called in main thread.");
			}
			if (_isProcessing)
			{
				Debug.LogError($"{methodName} other task running.");
				throw new InvalidOperationException($"{methodName} other task running.");
			}
		}

		public async Task LoadSceneBySceneManagerAsync(string sceneName, LoadSceneMode mode, Action<Scene>? onLoadCompleted = null)
		{
			CheckPerform("LoadSceneBySceneManagerAsync");

			_isProcessing = true;
			SynchronizationContext context = SynchronizationContext.Current;
			await Task.Run(() =>
			{
				context.Post(async _ => await LoadSceneBySceneManagerAsyncInternal(sceneName, mode, onLoadCompleted), null);
			});
		}

		public async Task LoadSceneByAddressablesAsync(string address, LoadSceneMode mode, Action<SceneInstance>? onLoadCompleted = null)
		{
			CheckPerform("LoadSceneByAddressablesAsync");

			_isProcessing = true;
			SynchronizationContext context = SynchronizationContext.Current;
			await Task.Run(() =>
			{
				context.Post(async _ => await LoadSceneByAddressablesAsyncInternal(address, mode, onLoadCompleted), null);
			});
		}

		private async Task LoadSceneBySceneManagerAsyncInternal(string sceneName, LoadSceneMode mode, Action<Scene>? onLoadCompleted = null)
		{
			if (_cancellation == null)
			{
				_cancellation = new CancellationTokenSource();
				try
				{
					await LoadSceneBySceneManagerAsyncCore(_cancellation.Token, sceneName, mode, onLoadCompleted);
				}
				catch (OperationCanceledException ex)
				{
					if (ex.CancellationToken == _cancellation.Token)
					{
						Debug.Log("AsyncSceneLoader.LoadSceneBySceneManagerAsyncInternal Task cancelled");
					}
				}
				finally
				{
					_cancellation = null;
					_isProcessing = false;
				}
			}
			else
			{
				_cancellation.Cancel();
				_cancellation = null;
				_isProcessing = false;
			}
		}

		private async Task LoadSceneByAddressablesAsyncInternal(string address, LoadSceneMode mode, Action<SceneInstance>? onLoadCompleted = null)
		{
			if (_cancellation == null)
			{
				_cancellation = new CancellationTokenSource();
				try
				{
					await LoadSceneByAddressablesAsyncCore(_cancellation.Token, address, mode, onLoadCompleted);
				}
				catch (OperationCanceledException ex)
				{
					if (ex.CancellationToken == _cancellation.Token)
					{
						Debug.Log("AsyncSceneLoader.LoadSceneByAddressablesAsyncInternal Task cancelled");
					}
				}
				finally
				{
					_cancellation = null;
					_isProcessing = false;
				}
			}
			else
			{
				_cancellation.Cancel();
				_cancellation = null;
				_isProcessing = false;
			}
		}

		private async Task LoadSceneBySceneManagerAsyncCore(CancellationToken token, string sceneName, LoadSceneMode mode, Action<Scene>? onLoadCompleted)
		{
			token.ThrowIfCancellationRequested();
			if (token.IsCancellationRequested)
			{
				return;
			}
			int count = 0;
			float startTime = Time.time;

			await Task.Yield();

			var asyncOperation = SceneManager.LoadSceneAsync(sceneName, mode);
			asyncOperation.allowSceneActivation = false;

			while (true)
			{
				token.ThrowIfCancellationRequested();
				if (token.IsCancellationRequested)
				{
					return;
				}

				if (debugDelay != 0)
				{
					if ((int)((Time.time - startTime) * 1000) < debugDelay)
						goto Skip;
				}

				if (asyncOperation.progress >= 0.9f)
				{
					break;
				}
			Skip:

				if (debugYieldPrint)
				{
					Debug.Log($"AsyncSceneLoader.LoadSceneBySceneManagerAsyncCore yield:{count}");
				}

				count++;
				await Task.Yield();
			}

			asyncOperation.allowSceneActivation = true;

			while (true)
			{
				token.ThrowIfCancellationRequested();
				if (token.IsCancellationRequested)
				{
					return;
				}
				if (asyncOperation.isDone)
				{
					break;
				}

				if (debugYieldPrint)
				{
					Debug.Log($"AsyncSceneLoader.LoadSceneBySceneManagerAsyncCore yield:{count}");
				}

				count++;
				await Task.Yield();
			}
			onLoadCompleted?.Invoke(SceneManager.GetSceneByName(sceneName));
		}

		private async Task LoadSceneByAddressablesAsyncCore(CancellationToken token, string address, LoadSceneMode mode, Action<SceneInstance>? onLoadCompleted)
		{
			token.ThrowIfCancellationRequested();
			if (token.IsCancellationRequested)
			{
				return;
			}
			int count = 0;
			float startTime = Time.time;

			await Task.Yield();

			var asyncOperation = Addressables.LoadSceneAsync(address, mode, activateOnLoad: false);

			while (true)
			{
				token.ThrowIfCancellationRequested();
				if (token.IsCancellationRequested)
				{
					return;
				}

				if (debugDelay != 0)
				{
					if ((int)((Time.time - startTime) * 1000) < debugDelay)
						goto Skip;
				}

				if (asyncOperation.IsDone)
				{
					break;
				}
			Skip:

				if (debugYieldPrint)
				{
					Debug.Log($"AsyncSceneLoader.LoadSceneByAddressablesAsyncCore yield:{count}");
				}

				count++;
				await Task.Yield();
			}

			var asyncOperation2 = asyncOperation.Task.Result.ActivateAsync();
			while (true)
			{
				token.ThrowIfCancellationRequested();
				if (token.IsCancellationRequested)
				{
					return;
				}
				if (asyncOperation2.isDone)
				{
					break;
				}

				if (debugYieldPrint)
				{
					Debug.Log($"AsyncSceneLoader.LoadSceneByAddressablesAsyncCore yield:{count}");
				}

				count++;
				await Task.Yield();
			}
			onLoadCompleted?.Invoke(asyncOperation.Task.Result);
		}

		public async Task UnloadSceneBySceneManagerAsync(Scene scene, Action? onUnloadCompleted = null)
		{
			CheckPerform("UnloadSceneBySceneManagerAsync");

			_isProcessing = true;
			SynchronizationContext context = SynchronizationContext.Current;
			await Task.Run(() =>
			{
				context.Post(async _ => await UnloadSceneBySceneManagerAsyncInternal(scene, onUnloadCompleted), null);
			});
		}

		public async Task UnloadSceneByAddressablesAsync(SceneInstance sceneInstance, Action? onUnloadCompleted = null)
		{
			CheckPerform("UnloadSceneByAddressablesAsync");

			_isProcessing = true;
			SynchronizationContext context = SynchronizationContext.Current;
			await Task.Run(() =>
			{
				context.Post(async _ => await UnloadSceneByAddressablesAsyncInternal(sceneInstance, onUnloadCompleted), null);
			});
		}

		private async Task UnloadSceneBySceneManagerAsyncInternal(Scene scene, Action? onUnloadCompleted = null)
		{
			if (_cancellation == null)
			{
				_cancellation = new CancellationTokenSource();
				try
				{
					await UnloadSceneBySceneManagerAsyncCore(_cancellation.Token, scene, onUnloadCompleted);
				}
				catch (OperationCanceledException ex)
				{
					if (ex.CancellationToken == _cancellation.Token)
					{
						Debug.Log("AsyncSceneLoader.UnloadSceneBySceneManagerAsyncInternal Task cancelled");
					}
				}
				finally
				{
					_cancellation = null;
					_isProcessing = false;
				}
			}
			else
			{
				_cancellation.Cancel();
				_cancellation = null;
				_isProcessing = false;
			}
		}

		private async Task UnloadSceneByAddressablesAsyncInternal(SceneInstance sceneInstance, Action? onUnloadCompleted = null)
		{
			if (_cancellation == null)
			{
				_cancellation = new CancellationTokenSource();
				try
				{
					await UnloadSceneByAddressablesAsyncCore(_cancellation.Token, sceneInstance, onUnloadCompleted);
				}
				catch (OperationCanceledException ex)
				{
					if (ex.CancellationToken == _cancellation.Token)
					{
						Debug.Log("AsyncSceneLoader.UnloadSceneByAddressablesAsyncInternal Task cancelled");
					}
				}
				finally
				{
					_cancellation = null;
					_isProcessing = false;
				}
			}
			else
			{
				_cancellation.Cancel();
				_cancellation = null;
				_isProcessing = false;
			}
		}

		private async Task UnloadSceneBySceneManagerAsyncCore(CancellationToken token, Scene scene, Action? onUnloadCompleted)
		{
			token.ThrowIfCancellationRequested();
			if (token.IsCancellationRequested)
			{
				return;
			}
			int count = 0;
			float startTime = Time.time;

			await Task.Yield();

			var asyncOperation = SceneManager.UnloadSceneAsync(scene);
			asyncOperation.allowSceneActivation = false;

			while (true)
			{
				token.ThrowIfCancellationRequested();
				if (token.IsCancellationRequested)
				{
					return;
				}
				if (debugDelay != 0)
				{
					if ((int)((Time.time - startTime) * 1000) < debugDelay)
						goto Skip;
				}

				if (asyncOperation.progress >= 0.9f)
				{
					break;
				}
			Skip:

				if (debugYieldPrint)
				{
					Debug.Log($"AsyncSceneLoader.UnloadSceneBySceneManagerAsyncCore yield:{count}");
				}

				count++;
				await Task.Yield();
			}
			asyncOperation.allowSceneActivation = true;

			while (true)
			{
				token.ThrowIfCancellationRequested();
				if (token.IsCancellationRequested)
				{
					return;
				}
				if (asyncOperation.isDone)
				{
					break;
				}

				if (debugYieldPrint)
				{
					Debug.Log($"AsyncSceneLoader.UnloadSceneBySceneManagerAsyncCore yield:{count}");
				}

				count++;
				await Task.Yield();
			}

			onUnloadCompleted?.Invoke();
		}

		private async Task UnloadSceneByAddressablesAsyncCore(CancellationToken token, SceneInstance sceneInstance, Action? onUnloadCompleted)
		{
			token.ThrowIfCancellationRequested();
			if (token.IsCancellationRequested)
			{
				return;
			}
			int count = 0;
			float startTime = Time.time;

			await Task.Yield();

			var asyncOperation = Addressables.UnloadSceneAsync(sceneInstance);

			while (true)
			{
				token.ThrowIfCancellationRequested();
				if (token.IsCancellationRequested)
				{
					return;
				}
				if (debugDelay != 0)
				{
					if ((int)((Time.time - startTime) * 1000) < debugDelay)
						goto Skip;
				}

				if (asyncOperation.IsDone)
				{
					break;
				}
			Skip:

				if (debugYieldPrint)
				{
					Debug.Log($"AsyncSceneLoader.UnloadSceneByAddressablesAsyncCore yield:{count}");
				}

				count++;
				await Task.Yield();
			}

			onUnloadCompleted?.Invoke();
		}
	}
}