using UnityEditor;
using UnityEditor.SettingsManagement;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace SubstanceToTexture {

	using StringSetting = SettingsWrapper<string>;
	using BoolSetting = SettingsWrapper<bool>;
	using static Settings;
	using static Generator;

	/// <summary>
	/// An AssetPostprocessor to automatically create textures, if there were changes to the source file.
	/// Additionally this updates/creates texture import settings automatically.
	/// </summary>
	public class TexturePostprocessor : AssetPostprocessor {

		[UserSetting(GeneralCategory, nameof(directoryToWatch), "The base path to watch for texture changes")]
		internal static StringSetting directoryToWatch = new StringSetting(GeneralCategoryKey + nameof(directoryToWatch), "Assets/GameAssets/UI");
		[UserSetting(GeneralCategory, nameof(enablePostProcessor), "Enable post processor if you are done setting everything up.")]
		internal static BoolSetting enablePostProcessor = new BoolSetting(GeneralCategoryKey + nameof(enablePostProcessor), false);

		private static readonly Dictionary<string, string> SplitLookup = new Dictionary<string, string>();
		private static readonly Dictionary<string, string> FilenameLookup = new Dictionary<string, string>();
		private static readonly List<string> PathsToProcess = new List<string>();
		private static string _filename;
		private static string _lastSplit;
		private static bool _hasUpdateMethod = false;
		private static double _nextTick = 0;
		private static double _tickRate = 1;

		private void OnPreprocessTexture() { 
			if (enablePostProcessor.value && assetPath.StartsWith(directoryToWatch.value)) {

				// cache filenames to avoid string operations on already known files
				if (!FilenameLookup.ContainsKey(assetPath)) {
					_filename = Path.GetFileNameWithoutExtension(assetPath);
					FilenameLookup.Add(assetPath, _filename);
				} else {
					_filename = FilenameLookup[assetPath];
				}

				// cache last split based on the file name
				if (!SplitLookup.ContainsKey(_filename)) {
					string[] splits = _filename.Split(parameterDelimiter);
					_lastSplit = splits[splits.Length - 1];
					SplitLookup.Add(_filename, _lastSplit);
				} else {
					_lastSplit = SplitLookup[_filename];
				}

				// check if we have hit an identifier in our substance types
				if (SubstanceTypesLookup.ContainsKey(_lastSplit)) {
					// yes, so we'll re/generate the texture
					SubstanceType substanceType = SubstanceTypesLookup[_lastSplit];
					if (string.IsNullOrEmpty(substanceType.watchDirectory) || assetPath.StartsWith(substanceType.watchDirectory)) {
						// we are working asynchronously, so there might already be a process underway to generate textures
						// this is why we'll just add the path to the list of paths to process
						// we'll be temporarily hooked into unitys editor update loop to check when we are allowed to start processing.
						PathsToProcess.Add(assetPath);
						//UnityEngine.Debug.Log("generate"+assetPath);
						if (!_hasUpdateMethod) {
							_nextTick = EditorApplication.timeSinceStartup + _tickRate;
							EditorApplication.update -= OnEditorApplicationUpdate;
							EditorApplication.update += OnEditorApplicationUpdate;
							_hasUpdateMethod = true;
						}
					}
				// otherwise: check if we hit an output postfix
				} else if(SubstanceTypesOutputLookup.ContainsKey(_lastSplit)) {
					// yes, so we'll adjust the required texture settings
					SubstanceType substanceType = SubstanceTypesOutputLookup[_lastSplit];
					if (string.IsNullOrEmpty(substanceType.watchDirectory) || assetPath.StartsWith(substanceType.watchDirectory)) {
						TextureImporter textureImporter = (TextureImporter)assetImporter;
						//UnityEngine.Debug.Log("import" + assetPath);
						UpdateTextureImporter(textureImporter, substanceType);
					}
				}

			}
		}

		private static void OnEditorApplicationUpdate() {
			if (EditorApplication.timeSinceStartup > _nextTick) {
				StartTextureGeneration();
				_nextTick = EditorApplication.timeSinceStartup + _tickRate;
			}
		}

		private static void StartTextureGeneration() {
			if (!isGeneratingTexture) {
				GenerateBySingleTextureAsync(PathsToProcess.Distinct().ToList());
				PathsToProcess.Clear();
				EditorApplication.update -= OnEditorApplicationUpdate;
				_hasUpdateMethod = false;
			}
		}

		private static void UpdateTextureImporter(TextureImporter textureImporter, SubstanceType substanceType) {
			bool importerDirty = false;

			if (substanceType.textureType != textureImporter.textureType) {
				textureImporter.textureType = substanceType.textureType;
				importerDirty = true;
			}

			bool targetValue = substanceType.colorSpace == SubstanceType.ColorSpace.sRGB ? true : false;
			if (targetValue != textureImporter.sRGBTexture) {
				textureImporter.sRGBTexture = targetValue;
				importerDirty = true;
			}

			if (substanceType.textureCompression != textureImporter.textureCompression) {
				textureImporter.textureCompression = substanceType.textureCompression;
				importerDirty = true;
			}

			if (substanceType.maxTextureSize != textureImporter.maxTextureSize) {
				textureImporter.maxTextureSize = substanceType.maxTextureSize;
				importerDirty = true;
			}

			if (substanceType.mipmapEnabled != textureImporter.mipmapEnabled) {
				textureImporter.mipmapEnabled = substanceType.mipmapEnabled;
				importerDirty = true;
			}

			if (substanceType.alphaIsTransparency != textureImporter.alphaIsTransparency) {
				textureImporter.alphaIsTransparency = substanceType.alphaIsTransparency;
				importerDirty = true;
			}

			if (substanceType.textureType == TextureImporterType.Sprite && substanceType.pixelsPerUnit != textureImporter.spritePixelsPerUnit) {
				textureImporter.spritePixelsPerUnit = substanceType.pixelsPerUnit;
				importerDirty = true;
			}

			if (substanceType.pixelsPerUnit != textureImporter.spritePixelsPerUnit) {
				textureImporter.spritePixelsPerUnit = substanceType.pixelsPerUnit;
				importerDirty = true;
			}

			if (importerDirty) {
				EditorUtility.SetDirty(textureImporter);
				textureImporter.SaveAndReimport();
			}
		}
	}
}
