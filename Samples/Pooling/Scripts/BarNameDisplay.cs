using UnityEngine;
using UnityEngine.UI;

namespace SBaier.DI.Examples.Pooling
{
	internal class BarNameDisplay : MonoBehaviour, Injectable, Initializable
    {
        [SerializeField]
        private Text _text;

		private Bar _bar;

		public void Inject(Resolver resolver)
		{
			_bar = resolver.Resolve<Bar>();
		}

		public void Initialize()
		{
			UpdateText();
		}

		private void UpdateText()
		{
			_text.text = _bar.ToString();
		}
    }
}
