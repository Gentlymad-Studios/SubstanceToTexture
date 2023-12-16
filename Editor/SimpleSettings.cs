using UnityEditor;
using UnityEngine;

namespace SubstanceToTexture {
	public class SimpleSettings<T> : ScriptableSingleton<T> where T : SimpleSettings<T> {
		public void OnEnable() {
			hideFlags &= ~HideFlags.NotEditable;
		}

		public void Save() {
			base.Save(true);
		}
	}
}
