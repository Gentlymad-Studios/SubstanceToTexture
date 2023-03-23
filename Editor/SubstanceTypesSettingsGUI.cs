using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;
using static SubstanceToTexture.Settings;
using static SubstanceToTexture.Generator;
using UnityEditorInternal;

namespace SubstanceToTexture {
	/// <summary>
	/// Special UI for the substance types list.
	/// </summary>
	public class SubstanceTypesSettingsGUI {
		private const float HeightSpacing = 2f;
		private static ReorderableList _reorderableList = null;

		public static void OnGUI() {
			EditorGUI.BeginChangeCheck();

			if (_reorderableList == null) {
				_reorderableList = new ReorderableList(substanceTypes.value, typeof(SubstanceType), false, false, true, true);
				_reorderableList.drawElementCallback += OnDraw;
				_reorderableList.elementHeightCallback += OnElementHeight;
				_reorderableList.onAddCallback += OnAdd;
				_reorderableList.onRemoveCallback += OnRemove;
				_reorderableList.onReorderCallback += OnReorder;

			}
			_reorderableList.DoLayoutList();
			SettingsGUILayout.DoResetContextMenuForLastRect(substanceTypes);

			if (EditorGUI.EndChangeCheck()) {
				substanceTypes.ApplyModifiedProperties();
				Save();
			}

		}

		private static void OnReorder(ReorderableList list) {
			substanceTypes.ApplyModifiedProperties();
			Save();
		}

		private static void OnRemove(ReorderableList list) {
			ReorderableList.defaultBehaviours.DoRemoveButton(list);
			substanceTypes.ApplyModifiedProperties();
			Save();
		}

		private static void OnAdd(ReorderableList list) {
			ReorderableList.defaultBehaviours.DoAddButton(list);
			substanceTypes.ApplyModifiedProperties();
			Save();
		}
		 
		private static float OnElementHeight(int index) {
			return 14f * (EditorGUIUtility.singleLineHeight + HeightSpacing);
		}

		private static void OnDraw(Rect rect, int index, bool isActive, bool isFocused) {
			Rect r = new Rect(rect);
			r.height = EditorGUIUtility.singleLineHeight;
			EditorGUI.BeginChangeCheck();
			SubstanceType substanceType = substanceTypes.value[index];
			EditorGUI.LabelField(r, substanceType.identifier, EditorStyles.boldLabel);
			r.y += r.height + HeightSpacing;
			substanceType.identifier = EditorGUI.TextField(r, nameof(substanceType.identifier), substanceType.identifier);
			r.y += r.height + HeightSpacing;
			substanceType.filename = EditorGUI.TextField(r, nameof(substanceType.filename), substanceType.filename);
			r.y += r.height + HeightSpacing;
			substanceType.inputName = EditorGUI.TextField(r, nameof(substanceType.inputName), substanceType.inputName);
			r.y += r.height + HeightSpacing;
			substanceType.inputGraphUrl = EditorGUI.TextField(r, nameof(substanceType.inputGraphUrl), substanceType.inputGraphUrl);
			r.y += r.height + HeightSpacing;
			substanceType.colorSpace = (SubstanceType.ColorSpace)EditorGUI.EnumPopup(r, nameof(substanceType.colorSpace), substanceType.colorSpace);
			r.y += r.height + HeightSpacing;
			substanceType.outputPostfix = EditorGUI.TextField(r, nameof(substanceType.outputPostfix), substanceType.outputPostfix);
			r.y += r.height + HeightSpacing;
			substanceType.watchDirectory = EditorGUI.TextField(r, nameof(substanceType.watchDirectory), substanceType.watchDirectory);
			r.y += r.height + HeightSpacing;
			substanceType.mipmapEnabled = EditorGUI.Toggle(r, "import: "+nameof(substanceType.mipmapEnabled), substanceType.mipmapEnabled);
			r.y += r.height + HeightSpacing;
			substanceType.textureCompression = (TextureImporterCompression)EditorGUI.EnumPopup(r, "import: " + nameof(substanceType.textureCompression), substanceType.textureCompression);
			r.y += r.height + HeightSpacing;
			substanceType.maxTextureSize = EditorGUI.IntField(r, "import: " + nameof(substanceType.maxTextureSize), substanceType.maxTextureSize);
			r.y += r.height + HeightSpacing;
			substanceType.textureType = (TextureImporterType)EditorGUI.EnumPopup(r, "import: " + nameof(substanceType.textureType), substanceType.textureType);
			r.y += r.height + HeightSpacing;
			substanceType.pixelsPerUnit = EditorGUI.IntField(r, "import: " + nameof(substanceType.pixelsPerUnit), substanceType.pixelsPerUnit);
			r.y += r.height + HeightSpacing;
			substanceType.alphaIsTransparency = EditorGUI.Toggle(r, "import: " + nameof(substanceType.alphaIsTransparency), substanceType.alphaIsTransparency);

			// would also work here).
			if (EditorGUI.EndChangeCheck()) {
				substanceTypes.ApplyModifiedProperties();
				Save();
			}

		}
	}
}
