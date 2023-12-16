using System.IO;
using UnityEditor;
using System;
using System.Diagnostics;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace SubstanceToTexture {

	using static Settings;
	using static Settings.SubstanceInstruction;

	/// <summary>
	/// The actual Generation class that handles the generation of substance outputs based on a command line tool.
	/// </summary>
	public class Generator {
		private static bool _isInitalized = false;
		internal static List<string> processingIdentifiers = new List<string>();
		private static bool _isProcessing = false;
		private static string _cachedFullSubstancePath;
		private static object _cachedFileExtension;
		private static string _cachedpathToSBSRenderTool;
		private static string _cachedDataPath;
		private static List<string> _cachedInvalidFileExtensions;

		private static Dictionary<string, SubstanceInstruction> _cachedInstructions = new Dictionary<string, SubstanceInstruction>();
		private static List<List<string>> _processingQueue = new List<List<string>>();

		/// <summary>
		/// initialize & cache values
		/// </summary>
		public static void Initialize(bool force = false) {
			if (!_isInitalized || force) {
				_cachedFullSubstancePath = Path.GetFullPath(Settings.instance.pathToSubstances);
				_cachedFileExtension = Settings.instance.substancesFileExtension;
				_cachedpathToSBSRenderTool = Path.GetFullPath(Settings.instance.pathToSBSRenderTool);
				_cachedDataPath = Path.GetFullPath(Application.dataPath);
				_cachedInvalidFileExtensions = Settings.instance.globalInvalidFileExtensions;

				// cache all instructions into a lookup
				_cachedInstructions.Clear();
				foreach (SubstanceInstruction instruction in Settings.instance.instructions) {
					if (!_cachedInstructions.ContainsKey(instruction.identifier)) {
						_cachedInstructions.Add(instruction.identifier, instruction);
					}
				}

				_isInitalized = true;
			}
		}

		[MenuItem("Tools/" + nameof(SubstanceToTexture) + "/ProcessAll")]
		private static async void ProcessAll() {
			Initialize();
			List<string> identifiers = _cachedInstructions.Where(x => !x.Value.runOnlyManually).Select(x => x.Key).ToList();
			await ProcessSubstancesAsync(identifiers);
		}

		/// <summary>
		/// Generate substance outputs per single texture in an asynchronous fashion.
		/// </summary>
		/// <param name="texturePath">The path to the texture that should be used.</param>
		public static async Task ProcessSubstancesAsync(List<string> identifiers) {
			Initialize();

			if (processingIdentifiers.Count > 0) {

				// remove all identifiers we are already working on...
				for(int i=identifiers.Count-1; i>=0; i--) {
					string identifier = identifiers[i];
					foreach (string processingIdentifier in processingIdentifiers) {
						if (identifier == processingIdentifier) {
							identifiers.Remove(identifier);
							break;
						}
					}
				}

				// add the remaining to the queue
				if (identifiers.Count > 0) {
					_processingQueue.Add(identifiers);
					UnityEngine.Debug.Log("Already processing substances, queueing!");
				} else {
					UnityEngine.Debug.Log("Already processing substances, please wait!");
				}

				while (_isProcessing) {
					await Task.Delay(300);
				}
				return;
			}

			processingIdentifiers = identifiers;
			if (processingIdentifiers.Count == 0) {
				return;
			}

			_isProcessing = true;

			// a list of all processed files including a reference to texture settings that need to be enforced
			// since this requires calling Unity APIs, we'll keep this list to process it after we are done with our off thread work!
			List<(OutputTextureSettings, string)> allFilesNeedingEnforcement = new List<(OutputTextureSettings, string)>();
			string allLogMessages = "";
			string allErrorMessages = "";

			// take the file generation of the main thread!
			Task task = new Task(() => {
				foreach (string identifier in identifiers) {
					ProcessSubstance(identifier, out List<(OutputTextureSettings, string)> filesNeedingEnforcement, out string errorMessage, out string logMessage);
					allErrorMessages += errorMessage;
					allLogMessages += logMessage;
					allFilesNeedingEnforcement.AddRange(filesNeedingEnforcement);
				}
			});
			task.Start();
			await task;

			if (!string.IsNullOrEmpty(allErrorMessages)) {
				UnityEngine.Debug.LogWarning(allErrorMessages);
			}
			if (!string.IsNullOrEmpty(allLogMessages)) {
				UnityEngine.Debug.Log(allLogMessages);
			}

			await Task.Yield();
			AssetDatabase.Refresh(); // files were created, better inform unity about it!

			// make sure to enforce all texture settings
			foreach((OutputTextureSettings, string) file in allFilesNeedingEnforcement) {
				await EnforceTextureSettings(file.Item1, file.Item2);
			}

			AssetDatabase.Refresh(); // files were created, better inform unity about it!
			RepaintProjectView();
			processingIdentifiers.Clear();
			await Task.Yield();

			// in case we have an open queue, work on it
			if(_processingQueue.Count > 0) {
				List<string> queuedIdentifiers = _processingQueue[0];
				_processingQueue.RemoveAt(0);
				UnityEngine.Debug.Log($"Working on queued Identifiers: {string.Join(", ", queuedIdentifiers)}");
				await ProcessSubstancesAsync(queuedIdentifiers);
			}

			if(_processingQueue.Count == 0) {
				_isProcessing = false;
			}
		}

		/// <summary>
		/// enforce all texture setting for a given file and the target settings
		/// </summary>
		/// <param name="settings">The target settings</param>
		/// <param name="path">The path to the file</param>
		/// <returns></returns>
		private static async Task EnforceTextureSettings(OutputTextureSettings settings, string path) {
			bool fileExists = File.Exists(path);
			bool isPathInsideAssetsFolder = false;
			if (fileExists) {
				isPathInsideAssetsFolder = path.Contains(_cachedDataPath);
			}

			if (isPathInsideAssetsFolder) {
				path = path.Substring(_cachedDataPath.Length - 6);

				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
				await Task.Yield();

				TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

				if (SetTextureSettings(importer, settings)) {
					importer.SaveAndReimport();
					await Task.Yield();
				}

			} else {
				UnityEngine.Debug.LogWarning($"The file at path {path} {(!fileExists ? "does not exist" : "exists but is not part of the assets folder!")}");
			}
		}

		/// <summary>
		/// Set the texture import settings for a  given importer and the corresponding target settings
		/// </summary>
		/// <param name="importer"></param>
		/// <param name="settings"></param>
		/// <returns></returns>
		private static bool SetTextureSettings(TextureImporter importer, OutputTextureSettings settings) {
			bool importerDirty = false;

			if (settings.textureType != importer.textureType) {
				importer.textureType = settings.textureType;
				importerDirty = true;
			}

			if (settings.sRGB != importer.sRGBTexture) {
				importer.sRGBTexture = settings.sRGB;
				importerDirty = true;
			}

			if (settings.textureCompression != importer.textureCompression) {
				importer.textureCompression = settings.textureCompression;
				importerDirty = true;
			}

			if (settings.maxTextureSize != importer.maxTextureSize) {
				importer.maxTextureSize = settings.maxTextureSize;
				importerDirty = true;
			}

			if (settings.mipmapEnabled != importer.mipmapEnabled) {
				importer.mipmapEnabled = settings.mipmapEnabled;
				importerDirty = true;
			}

			if (settings.alphaIsTransparency != importer.alphaIsTransparency) {
				importer.alphaIsTransparency = settings.alphaIsTransparency;
				importerDirty = true;
			}

			if (settings.textureType == TextureImporterType.Sprite && settings.pixelsPerUnit != importer.spritePixelsPerUnit) {
				importer.spritePixelsPerUnit = settings.pixelsPerUnit;
				importerDirty = true;
			}

			if (settings.pixelsPerUnit != importer.spritePixelsPerUnit) {
				importer.spritePixelsPerUnit = settings.pixelsPerUnit;
				importerDirty = true;
			}

			return importerDirty;
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
		/// Checks wether the given filename is not valid
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="globalNamePartials"></param>
		/// <param name="specificNamePartials"></param>
		/// <returns></returns>
		private static bool InvalidName(ref string filename, ref string filenameWithoutExtension, ref string inputPostfix, ref string[] globalNamePartials, ref string[] specificNamePartials) {

			bool InvalidName(ref string filename, ref string[] namePartials) {
				if (namePartials == null || namePartials.Length == 0) {
					return false;
				}

				foreach (string namePartial in namePartials) {
					if (filename.Contains(namePartial)) {
						return false;
					}
				}
				return true;
			}

			if (!filenameWithoutExtension.EndsWith(inputPostfix)) {
				return true;
			}

			foreach (string invalidExtension in _cachedInvalidFileExtensions) {
				if (filename.Contains(invalidExtension)) {
					return true;
				}
			}

			bool invalid = InvalidName(ref filename, ref globalNamePartials);
			invalid = !invalid ? InvalidName(ref filename, ref specificNamePartials) : invalid;
			return invalid;
		}

		/// <summary>
		/// Generate substance outputs for a single texture.
		/// </summary>
		/// <param name="texturePath"></param>
		private static void ProcessSubstance(string identifier, out List<(OutputTextureSettings,string)> filesNeedingEnforcement, out string errorMessage, out string logMessage) {
			filesNeedingEnforcement = new List<(OutputTextureSettings, string)>();
			errorMessage = "";
			logMessage = "";

			if (!_cachedInstructions.ContainsKey(identifier)) {
				errorMessage = $"Identifier {identifier} not found!\r\n";
				return;
			}

			SubstanceInstruction instruction = _cachedInstructions[identifier];


			if (instruction.inAndOutInfos.Count == 0) {
				errorMessage = $"Instruction ({identifier}) has no input or output info!\r\n";
				return;
			}

			foreach (InAndOutInfo inAndOutInfo in instruction.inAndOutInfos) {
				string inputFolder = Path.GetFullPath(inAndOutInfo.inputFolder);

				if (!Directory.Exists(inputFolder)) {
					errorMessage += $"Instruction ({identifier}) input folder does not exist: {inputFolder}\r\n";
					continue;
				}

				string[] files = Directory.GetFiles(inputFolder,"*.*", inAndOutInfo.searchOption);

				foreach (string file in files) {

					string fileNameWithExtension = Path.GetFileName(file);
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);

					if (InvalidName(ref fileNameWithExtension, ref fileNameWithoutExtension, ref inAndOutInfo.inputPostfix, ref instruction.globalWhitelistedNamePartials, ref inAndOutInfo.whitelistedNamePartials)) {
						//logMessage += $"{fileNameWithExtension} is an invalid file name (not whitelisted) [path: {file}]\r\n";
						continue;
					}
			
					// create output folders
					string outputFolder = Path.GetFullPath(inAndOutInfo.outputFolder);
					if (!Directory.Exists(outputFolder)) {
						logMessage += $"{outputFolder} does not exist and will be created!\r\n";
						Directory.CreateDirectory(outputFolder);
					}

					if (!string.IsNullOrWhiteSpace(inAndOutInfo.inputPostfix)) {
						fileNameWithoutExtension = fileNameWithoutExtension.Substring(0, fileNameWithoutExtension.Length - inAndOutInfo.inputPostfix.Length);
					}

					if (!string.IsNullOrWhiteSpace(inAndOutInfo.outputPostfix)) {
						fileNameWithoutExtension += inAndOutInfo.outputPostfix;
					}

					// render the substance 
					ExecuteRenderProcess(instruction.substance, Path.GetFullPath(file), fileNameWithoutExtension, outputFolder);

					// add processed file to list of files
					string expectedFilepath = Path.Combine(outputFolder, fileNameWithoutExtension + "." + instruction.substance.outputFileType);

					// check if we need to enforce any settings
					OutputTextureSettings outputTextureSettings = null;
					if (inAndOutInfo.textureSettings.enforceTextureSettings) {
						// do we have specific settings to enforce?
						outputTextureSettings = inAndOutInfo.textureSettings;
					} else if (instruction.globalTextureSettings.enforceTextureSettings) {
						// do we have global settings to enforce?
						outputTextureSettings = instruction.globalTextureSettings;
					}

					if (outputTextureSettings != null) {
						outputTextureSettings.sRGB = instruction.substance.colorSpace == BaseSubstanceType.ColorSpace.sRGB;
						filesNeedingEnforcement.Add((outputTextureSettings, expectedFilepath));
					} /*else {
						logMessage += $"No texture override for {file}\r\n";
					}*/
				}
			}
		}

		/// <summary>
		/// Start the actual generation process.
		/// </summary>
		/// <param name="substanceType"></param>
		/// <param name="outputFilename"></param>
		/// <param name="outputFilepath"></param>
		public static void ExecuteRenderProcess(BaseSubstanceType substanceType, string inputFilePath, string outputFilename, string outputFolder) {
			string arguments = $"render ";
			arguments += $"--input-graph {substanceType.inputGraphUrl} ";
			arguments += $"--set-entry {substanceType.inputName}@{"\"" + inputFilePath + "\""} ";
			arguments += $"--output-format {substanceType.outputFileType} ";
			arguments += $"--output-colorspace {substanceType.colorSpace} ";
			arguments += $"--output-name {"\"" + outputFilename + "\""} ";
			arguments += $"--output-path {"\"" + outputFolder + "\""} ";
			arguments += $"{"\"" + Path.Combine(_cachedFullSubstancePath, substanceType.filename + _cachedFileExtension) + "\""}";

			File.AppendAllText(Path.Combine(_cachedDataPath,"debug.txt"), arguments);
			Process process = new Process();
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
