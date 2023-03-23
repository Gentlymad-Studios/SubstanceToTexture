using UnityEditor;
using UnityEditor.SettingsManagement;

namespace SubstanceToTexture {
	/// <summary>
	/// Settings wrapper to simplify the creation of settings
	/// </summary>
	/// <typeparam name="T">The value type of the setting that should be created/managed</typeparam>
	class SettingsWrapper<T> : UserSetting<T> {
		/// <summary>
		/// Constructor with predefined settings and scope (see base instructions)
		/// </summary>
		/// <param name="key">The string based settings key</param>
		/// <param name="value">The actual settings value</param>
		public SettingsWrapper(string key, T value) : base(Settings.Instance, key, value, SettingsScope.Project) { }
	}
}
