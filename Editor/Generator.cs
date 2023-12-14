using UnityEditor.SettingsManagement;
using System.IO;
using UnityEditor;
using System;
using System.Diagnostics;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SubstanceToTexture {

	using StringSetting = SettingsWrapper<string>;
	using SubstanceTypeSetting = SettingsWrapper<List<SubstanceType>>;
	using static Settings;

	/// <summary>
	/// The actual Generation class that handles the generation of substance outputs based on a command line tool.
	/// </summary>
	public class Generator {
		private static bool _isInitalized = false;
		internal static bool isGeneratingTexture = false;

		[UserSetting(GeneralCategory, nameof(pathToSBSRenderTool), "Path to the folder where the sbsrender.exe file resides.")]
		internal static StringSetting pathToSBSRenderTool = new StringSetting(GeneralCategoryKey + nameof(pathToSBSRenderTool), "../tools/sbsrender/sbsrender.exe");
		private static string _cachedpathToSBSRenderTool = null;

		[UserSetting(GeneralCategory, nameof(pathToSubstances), "Path to the folder where all .sbsar files are kept")]
		internal static StringSetting pathToSubstances = new StringSetting(GeneralCategoryKey + nameof(pathToSubstances), "../tools/sbsrender/substances/");
		private static string _cachedFullSubstancePath = null;

		[UserSetting(GeneralCategory, nameof(outputDirectory), "Directory where outputs should be created.")]
		internal static StringSetting outputDirectory = new StringSetting(GeneralCategoryKey + nameof(outputDirectory), "Generated");
		private static string _cachedOutputDirectory = null;

		[UserSetting(GeneralCategory, nameof(parameterDelimiter), "Parameter delimiter, used to identify the map type.")]
		internal static StringSetting parameterDelimiter = new StringSetting(GeneralCategoryKey + nameof(parameterDelimiter), "_");
		private static string _cachedParameterDelimiter = null;

		[UserSetting(GeneralCategory, nameof(substancesFileExtension), "Valid file extension for substances.")]
		internal static StringSetting substancesFileExtension = new StringSetting(GeneralCategoryKey + nameof(substancesFileExtension), ".sbsar");
		private static string _cachedFileExtension = null;

		[UserSetting]
		internal static SubstanceTypeSetting substanceTypes = new SubstanceTypeSetting(GeneralCategoryKey + nameof(substanceTypes), new List<SubstanceType>());
		private static Dictionary<string, SubstanceType> _substanceTypesLookup = null;
		private static Dictionary<string, SubstanceType> _substanceTypesOutputLookup = null;
		internal static Dictionary<string, SubstanceType> SubstanceTypesLookup {
			get {
				Initialize();
				return _substanceTypesLookup;
			}
		}
		internal static Dictionary<string, SubstanceType> SubstanceTypesOutputLookup {
			get {
				Initialize();
				return _substanceTypesOutputLookup;
			}
		}

		[UserSettingBlock(nameof(substanceTypes))]
#pragma warning disable IDE0051 // Remove unused private members
		private static void SubstanceTypesGUI(string searchContext) {
			SubstanceTypesSettingsGUI.OnGUI();
		}
#pragma warning restore IDE0051 // Remove unused private members

		/// <summary>
		/// initialize & cache values
		/// </summary>
		public static void Initialize(bool force=false) {
			if (!_isInitalized || force) {
				_cachedFullSubstancePath = Path.GetFullPath(pathToSubstances.value);
				_cachedFileExtension = substancesFileExtension.value;
				_cachedOutputDirectory = outputDirectory.value;
				_cachedParameterDelimiter = parameterDelimiter.value;
				_cachedpathToSBSRenderTool = Path.GetFullPath(pathToSBSRenderTool.value);

				InitializeSubstanceLookup();

				_isInitalized = true;
			}
		}

		/// <summary>
		/// Initialize the substance type lookup
		/// </summary>
		internal static void InitializeSubstanceLookup() {
			// create dictionary or clear it
			if (_substanceTypesLookup == null) {
				_substanceTypesLookup = new Dictionary<string, SubstanceType>();
			} else {
				_substanceTypesLookup.Clear();
			}
			if (_substanceTypesOutputLookup == null) {
				_substanceTypesOutputLookup = new Dictionary<string, SubstanceType>();
			} else {
				_substanceTypesOutputLookup.Clear();
			}

			// go over actual substance directory to check if the substances actually exist
			if (Directory.Exists(_cachedFullSubstancePath)) {
				//UnityEngine.Debug.Log(_cachedFullSubstancePath);

				string[] substances = Directory.GetFiles(_cachedFullSubstancePath, "*"+_cachedFileExtension);
				//UnityEngine.Debug.Log("substances" + substances.Length);
				
				for (int i = 0; i < substances.Length; i++) {
					string filename = Path.GetFileNameWithoutExtension(substances[i]);

					for (int j=0; j< substanceTypes.value.Count; j++) {
						SubstanceType substanceType= substanceTypes.value[j];
						// if the filename matches we have found this type!
						if (filename == substanceType.filename) {
							//UnityEngine.Debug.Log("substances ident " + substanceType.identifier);
							if (!_substanceTypesLookup.ContainsKey(substanceType.identifier)) {
								_substanceTypesLookup.Add(substanceType.identifier, substanceType);
								_substanceTypesOutputLookup.Add(substanceType.outputPostfix, substanceType);
							}
							break;
						}
					}
				}
			}

		}

		/// <summary>
		/// Menu action to create/update textures based on the active selection
		/// </summary>
		[MenuItem(GenerateBySelectionMenu)]
		private static void CreateTextureBySelection() {
			if (!isGeneratingTexture) {
				Initialize(true);
				List<string> texturePaths = new List<string>();
				foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets)) {
					string path = AssetDatabase.GetAssetPath(obj);
					if (!string.IsNullOrEmpty(path) && File.Exists(path) && AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Texture2D)) {
						texturePaths.Add(path);
						//return;
					}
				}
				if (texturePaths.Count > 0) {
					GenerateBySingleTextureAsync(texturePaths);
				}
			}
		}

		/// <summary>
		/// Generate substance outputs per single texture in an asynchronous fashion.
		/// </summary>
		/// <param name="texturePath">The path to the texture that should be used.</param>
		public static async void GenerateBySingleTextureAsync(List<string> texturePaths) {
			Initialize();
			isGeneratingTexture = true;
			Task task = new Task(() => {
				foreach (string texturePath in texturePaths) {
					GenerateBySingleTexture(texturePath);
				}
			});
			task.Start();
			await task;
			AssetDatabase.Refresh();
			RepaintProjectView();
			isGeneratingTexture = false;
		}

		/// <summary>
		/// Method to force a repaint of the project view.
		/// </summary>
		public static void RepaintProjectView() {
			Type buildSettingsType = Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");
			UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(buildSettingsType);
			if (windows != null && windows.Length > 0) {
				foreach (UnityEngine.Object obj in windows) {
					EditorWindow window = (EditorWindow)obj;
					if (window)
						window.Repaint();
				}
			}
		}

		/// <summary>
		/// Generate substance outputs for a single texture.
		/// </summary>
		/// <param name="texturePath"></param>
		private static void GenerateBySingleTexture(string texturePath) {
			if (!string.IsNullOrEmpty(texturePath) && File.Exists(texturePath)) {
				// split the received file name by the specified delimiter
				string[] parameters = Path.GetFileNameWithoutExtension(texturePath).Split(_cachedParameterDelimiter);

				// if we have more than one entry after the split, we'll likely want to convert textures.
				if (parameters.Length > 1) {

					// check if the detected substances is named equally to the extracted parameter?
					string identifier = parameters[parameters.Length - 1];

					if (_substanceTypesLookup.ContainsKey(identifier)) {

						SubstanceType substanceType = _substanceTypesLookup[identifier];
						string pathToContainingFolder = Path.GetDirectoryName(texturePath);

						string outputDirectory = Path.Combine(pathToContainingFolder, _cachedOutputDirectory);
						if (!Directory.Exists(outputDirectory)) {
							Directory.CreateDirectory(outputDirectory);
						}

						ExecuteGenerationProcess(substanceType, texturePath, outputDirectory, substanceType.identifier, substanceType.outputPostfix);
					}

				}
			}
		}

		/// <summary>
		/// Start the actual generation process.
		/// </summary>
		/// <param name="substanceType"></param>
		/// <param name="texturePath"></param>
		/// <param name="outputFilepath"></param>
		public static void ExecuteGenerationProcess(BaseSubstanceType substanceType, string texturePath, string outputDirectory, string identifier=null, string outputPostFix=null) {

			string outputName = Path.GetFileNameWithoutExtension(texturePath);
			if (identifier != null) {
				outputName = outputName.Replace(identifier, "");
			}

			if (outputPostFix != null) {
				outputName += outputPostFix;
			}

			string arguments = $"render ";
			arguments += $"--input-graph {substanceType.inputGraphUrl} ";
			arguments += $"--set-entry {substanceType.inputName}@{"\"" + Path.GetFullPath(texturePath)+ "\""} ";
			arguments += $"--output-format {substanceType.outputFileType} ";
			arguments += $"--output-colorspace {substanceType.colorSpace} ";
			arguments += $"--output-name {"\"" + outputName + "\""} ";
			arguments += $"--output-path {"\"" + Path.GetFullPath(outputDirectory) + "\""} ";
			arguments += $"{"\"" + Path.Combine(_cachedFullSubstancePath, substanceType.filename+_cachedFileExtension) + "\""}";

			Process process= new Process();
			process.StartInfo.FileName = _cachedpathToSBSRenderTool;
			process.StartInfo.Arguments = arguments;
			process.StartInfo.WorkingDirectory = Path.GetDirectoryName(_cachedpathToSBSRenderTool);
			// hide the window, so the generation is less distracting
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;

			process.Start();
			process.WaitForExit();
		}

	}

}
