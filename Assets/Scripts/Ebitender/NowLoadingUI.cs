using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ebitender
{
	public class NowLoadingUI : MonoBehaviour
	{
		[SerializeField] private GameObject _nowLoading = null;
		[SerializeField] private Text _text = null;

		private static NowLoadingUI _instance;
		public static NowLoadingUI instance => _instance;

		float _elapsed;
		string[] _dots = new string[] { "", ".", "..", "..." };

		void Awake()
		{
			_instance = this;
			GameObject.DontDestroyOnLoad(gameObject);
		}

		void OnDestroy()
		{
			_instance = null;
		}

		void Update()
		{
			if (!_nowLoading.activeSelf)
			{
				return;
			}
			_elapsed += Time.deltaTime;
			_text.text = "Now Loading" + _dots[(int)(_elapsed / 0.25f) & 3];
		}

		public bool isActive
		{
			get => _nowLoading.activeSelf;
			set => _nowLoading.SetActive(value);
		}
	}
}