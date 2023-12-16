using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SubstanceToTexture {
	[FilePath("ProjectSettings/" + nameof(SubstanceToTexture) + ".asset", FilePathAttribute.Location.ProjectFolder)]
	public class Settings : SimpleSettings<Settings> {
		/// <summary>
		/// the display path in the project settings window
		/// </summary>
		private const string WINDOW_PATH = "Tools/" + nameof(SubstanceToTexture);

		[Tooltip("Where is the sbsrender.exe located?")]
		public string pathToSBSRenderTool = "../tools/sbsrender/sbsrender.exe";
		[Tooltip("Where are all substances located that we can operate on?")]
		public string pathToSubstances = "../tools/sbsrender/substances/";
		[Tooltip("What is the file extension of the substances?")]
		public string substancesFileExtension = ".sbsar";
		public List<string> globalInvalidFileExtensions = new List<string>() { ".meta" };

		[Serializable]
		public class SubstanceInstruction {
			[Serializable]
			public class OutputTextureSettings {
				[Tooltip("should we actually enforce any settings or do nothing?")]
				public bool enforceTextureSettings = false;
				[Tooltip("Should mipmaps be generated?")]
				public bool mipmapEnabled = false;
				[Tooltip("What is the max. allowed texture size? Needs to be power of 2.")]
				public int maxTextureSize = 256;
				[Tooltip("What is the max. pixelsPerUnit?")]
				public int pixelsPerUnit = 2048;
				[Tooltip("Is the alpha channel to be treated as transparency?")]
				public bool alphaIsTransparency = false;
				[Tooltip("Should the texture be compressed?")]
				public TextureImporterCompression textureCompression = TextureImporterCompression.Uncompressed;
				[Tooltip("What kind of texture type should we impose?")]
				public TextureImporterType textureType = TextureImporterType.Sprite;
				[NonSerialized]
				public bool sRGB = false;
			}

			[Serializable]
			public class InAndOutInfo {
				[Tooltip("the folder where we should look for input files")]
				public string inputFolder;
				[Tooltip("The input postfix that should be added to a processed file.")]
				public string inputPostfix;
				[Tooltip("should we look into subfolders as well?")]
				public SearchOption searchOption = SearchOption.TopDirectoryOnly;
				[Tooltip("If this is not empty, if the filename does not contain any of the given elements, this input will be ignored.")]
				public string[] whitelistedNamePartials;
				[Tooltip("The folder where outputted files should be exported to.")]
				public string outputFolder;
				[Tooltip("The output postfix that should be added to a processed file.")]
				public string outputPostfix;
				[Header("Import Settings Enforcement")]
				[Tooltip("Specific texture settings that should be enforced on the outputted texture.")]
				public OutputTextureSettings textureSettings;
			}

			[Serializable]
			public class BaseSubstanceType {
				[Tooltip("File type of the outputted texture/ image file (png, tga, etc.)")]
				public string outputFileType = "tga";
				[Tooltip("Name of the graph inside the .sbsar that should be run/ used for processing.")]
				public string inputGraphUrl = "generator";
				[Tooltip("Filename of the substance to be used (WITHOUT fileending)")]
				public string filename = "MaskMap3";
				[Tooltip("The name of the input to be used when generating")]
				public string inputName = "input";
				[Tooltip("The color space this should be rendered/processed in. (Raw or sRGB)")]
				public ColorSpace colorSpace = ColorSpace.Raw;

				public enum ColorSpace {
					sRGB = 0,
					Raw = 1,
				}
			}

			[Header("General")]
			[Tooltip("A unique identifier, so this set of instructions can be called from everywhere.")]
			public string identifier;
			[Tooltip("Should this be run automatically or only when triggered manually?")]
			public bool runOnlyManually = false;
			[Header("Input & Output Information")]
			[Tooltip("Info about the inputs and outputs")]
			public List<InAndOutInfo> inAndOutInfos;
			[Tooltip("If this is not empty, if any input filename does not contain any of the given elements, this input will be ignored.")]
			public string[] globalWhitelistedNamePartials;
			[Header("Substance Settings")]
			public BaseSubstanceType substance;
			[Header("Texture Settings Enforcement")]
			[Tooltip("Specific texture settings that should be enforced on the outputted texture GLOBALLY (will be overriden, if a specific texture settings is makred as active)")]
			public OutputTextureSettings globalTextureSettings;
		}

		[Tooltip("A list of a instructions to process textures using substance files (.sbsar)")]
		public List<SubstanceInstruction> instructions;

		[SettingsProvider]
		private static SettingsProvider RegisterInProjectSettings() {
			return new SimpleSettingsProvider<Settings>(WINDOW_PATH);
		}
	}
}
