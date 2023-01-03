using System.Threading;
using UnityEngine;

#nullable enable

namespace Ebitender
{
	public static class ThreadUtils
	{
		private static int _mainThreadId;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void InitStatic()
		{
			_mainThreadId = Thread.CurrentThread.ManagedThreadId;
		}

		public static bool isMainThread => (Thread.CurrentThread.ManagedThreadId == _mainThreadId);
	}
}