using System;
using UnityEditor;

namespace SubstanceToTexture {
	/// <summary>
	/// The substance (.sbar) type.
	/// This holds all the specific values that need to be adjusted for every substance export procedure.
	/// </summary>
	[Serializable]
	public class SubstanceType {

		/// <summary>
		/// identifier
		/// </summary>
		public bool mipmapEnabled = false;

		/// <summary>
		/// identifier
		/// </summary>
		public int maxTextureSize = 256;
		
		/// <summary>
		/// identifier
		/// </summary>
		public int pixelsPerUnit = 2048;
		
		/// <summary>
		/// identifier
		/// </summary>
		public bool alphaIsTransparency = false;

		/// <summary>
		/// identifier
		/// </summary>
		public TextureImporterCompression textureCompression = TextureImporterCompression.Uncompressed;
		
		/// <summary>
		/// identifier
		/// </summary>
		public TextureImporterType textureType = TextureImporterType.Sprite;

		/// <summary>
		/// identifier
		/// </summary>
		public string watchDirectory = "Assets/GameAssets/UI/Textures/StylizedUI";

		/// <summary>
		/// identifier
		/// </summary>
		public string identifier = "alpha";

		/// <summary>
		/// The url/name to the embedded graph
		/// </summary>
		public string inputGraphUrl = "generator";

		/// <summary>
		/// The .sbsar file name
		/// </summary>
		public string filename = "MaskMap3";

		/// <summary>
		/// The input texture variable name as it is required by the substance
		/// </summary>
		public string inputName = "input";

		/// <summary>
		/// The color space the outputted texture should be generated in.
		/// </summary>
		public ColorSpace colorSpace = ColorSpace.Raw;

		/// <summary>
		/// the post suffix of the output after it was generated
		/// </summary>
		public string outputPostfix = "data";

		/// <summary>
		/// The color space types that can be exported to.
		/// </summary>
		public enum ColorSpace {
			sRGB = 0,
			Raw = 1,
		}
	}
}
