
namespace SubstanceToTexture {
	/// <summary>
	/// Settings data based on UnityEditor.SettingsManagement package.
	/// </summary>
	static class Settings {
		// menus
		internal const string ToolsBasepath = "Tools/";
		internal const string ProjectSettingsMenu = ToolsBasepath + nameof(SubstanceToTexture);
		internal const string GenerateBySelectionMenu = ToolsBasepath + nameof(SubstanceToTexture) + "/Generate By Selection";

		// categories
		internal const string GeneralCategory = "General";
		internal const string GeneralCategoryKey = "general.";

		// package name
		internal const string PackageName = "com.gentlymadstudios.substancetotexture";

		/// <summary>
		/// The instance to our settings file.
		/// </summary>
		private static UnityEditor.SettingsManagement.Settings _instance = null;
		internal static UnityEditor.SettingsManagement.Settings Instance {
			get {
				if (_instance == null) {
					_instance = new UnityEditor.SettingsManagement.Settings(PackageName);
					Save();
				}

				return _instance;
			}
		}

		public static void Save() {
			Instance.Save();
		}
	}
}
