using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleFileBrowser
{
	[Serializable]
	public struct FiletypeIcon
	{
		public string extension;
		public Sprite icon;
	}

	[CreateAssetMenu( fileName = "UI Skin", menuName = "yasirkula/SimpleFileBrowser/UI Skin", order = 111 )]
	public class UISkin : ScriptableObject
	{
		private int m_version = 0;
		public int Version { get { return m_version; } }

		[ContextMenu( "Refresh UI" )]
		private void Invalidate()
		{
			m_version = UnityEngine.Random.Range( int.MinValue / 2, int.MaxValue / 2 );
			initializedFiletypeIcons = false;
		}

#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			// Refresh all UIs that use this skin
			Invalidate();
		}
#endif

#pragma warning disable 0649
		[Header( "General" )]
		[SerializeField]
		private Font m_font;
		public Font Font
		{
			get { return m_font; }
			set { if( m_font != value ) { m_font = value; m_version++; } }
		}

		[SerializeField]
		private int m_fontSize = 14;
		public int FontSize
		{
			get { return m_fontSize; }
			set { if( m_fontSize != value ) { m_fontSize = value; m_version++; } }
		}

		[Header( "File Browser Window" )]
		[SerializeField]
		private Color m_windowColor = Color.grey;
		public Color WindowColor
		{
			get { return m_windowColor; }
			set { if( m_windowColor != value ) { m_windowColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_filesListColor = Color.white;
		public Color FilesListColor
		{
			get { return m_filesListColor; }
			set { if( m_filesListColor != value ) { m_filesListColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_filesVerticalSeparatorColor = Color.grey;
		public Color FilesVerticalSeparatorColor
		{
			get { return m_filesVerticalSeparatorColor; }
			set { if( m_filesVerticalSeparatorColor != value ) { m_filesVerticalSeparatorColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_titleBackgroundColor = Color.black;
		public Color TitleBackgroundColor
		{
			get { return m_titleBackgroundColor; }
			set { if( m_titleBackgroundColor != value ) { m_titleBackgroundColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_titleTextColor = Color.white;
		public Color TitleTextColor
		{
			get { return m_titleTextColor; }
			set { if( m_titleTextColor != value ) { m_titleTextColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_windowResizeGizmoColor = Color.black;
		public Color WindowResizeGizmoColor
		{
			get { return m_windowResizeGizmoColor; }
			set { if( m_windowResizeGizmoColor != value ) { m_windowResizeGizmoColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_headerButtonsColor = Color.white;
		public Color HeaderButtonsColor
		{
			get { return m_headerButtonsColor; }
			set { if( m_headerButtonsColor != value ) { m_headerButtonsColor = value; m_version++; } }
		}

		[SerializeField]
		private Sprite m_windowResizeGizmo;
		public Sprite WindowResizeGizmo
		{
			get { return m_windowResizeGizmo; }
			set { if( m_windowResizeGizmo != value ) { m_windowResizeGizmo = value; m_version++; } }
		}

		[SerializeField]
		private Sprite m_headerBackButton;
		public Sprite HeaderBackButton
		{
			get { return m_headerBackButton; }
			set { if( m_headerBackButton != value ) { m_headerBackButton = value; m_version++; } }
		}

		[SerializeField]
		private Sprite m_headerForwardButton;
		public Sprite HeaderForwardButton
		{
			get { return m_headerForwardButton; }
			set { if( m_headerForwardButton != value ) { m_headerForwardButton = value; m_version++; } }
		}

		[SerializeField]
		private Sprite m_headerUpButton;
		public Sprite HeaderUpButton
		{
			get { return m_headerUpButton; }
			set { if( m_headerUpButton != value ) { m_headerUpButton = value; m_version++; } }
		}

		[SerializeField]
		private Sprite m_headerContextMenuButton;
		public Sprite HeaderContextMenuButton
		{
			get { return m_headerContextMenuButton; }
			set { if( m_headerContextMenuButton != value ) { m_headerContextMenuButton = value; m_version++; } }
		}

		[Header( "Input Fields" )]
		[SerializeField]
		private Color m_inputFieldNormalBackgroundColor = Color.white;
		public Color InputFieldNormalBackgroundColor
		{
			get { return m_inputFieldNormalBackgroundColor; }
			set { if( m_inputFieldNormalBackgroundColor != value ) { m_inputFieldNormalBackgroundColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_inputFieldInvalidBackgroundColor = Color.red;
		public Color InputFieldInvalidBackgroundColor
		{
			get { return m_inputFieldInvalidBackgroundColor; }
			set { if( m_inputFieldInvalidBackgroundColor != value ) { m_inputFieldInvalidBackgroundColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_inputFieldTextColor = Color.black;
		public Color InputFieldTextColor
		{
			get { return m_inputFieldTextColor; }
			set { if( m_inputFieldTextColor != value ) { m_inputFieldTextColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_inputFieldPlaceholderTextColor = new Color( 0f, 0f, 0f, 0.5f );
		public Color InputFieldPlaceholderTextColor
		{
			get { return m_inputFieldPlaceholderTextColor; }
			set { if( m_inputFieldPlaceholderTextColor != value ) { m_inputFieldPlaceholderTextColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_inputFieldSelectedTextColor = Color.blue;
		public Color InputFieldSelectedTextColor
		{
			get { return m_inputFieldSelectedTextColor; }
			set { if( m_inputFieldSelectedTextColor != value ) { m_inputFieldSelectedTextColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_inputFieldCaretColor = Color.black;
		public Color InputFieldCaretColor
		{
			get { return m_inputFieldCaretColor; }
			set { if( m_inputFieldCaretColor != value ) { m_inputFieldCaretColor = value; m_version++; } }
		}

		[SerializeField]
		private Sprite m_inputFieldBackground;
		public Sprite InputFieldBackground
		{
			get { return m_inputFieldBackground; }
			set { if( m_inputFieldBackground != value ) { m_inputFieldBackground = value; m_version++; } }
		}

		[Header( "Buttons" )]
		[SerializeField]
		private Color m_buttonColor = Color.white;
		public Color ButtonColor
		{
			get { return m_buttonColor; }
			set { if( m_buttonColor != value ) { m_buttonColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_buttonTextColor = Color.black;
		public Color ButtonTextColor
		{
			get { return m_buttonTextColor; }
			set { if( m_buttonTextColor != value ) { m_buttonTextColor = value; m_version++; } }
		}

		[SerializeField]
		private Sprite m_buttonBackground;
		public Sprite ButtonBackground
		{
			get { return m_buttonBackground; }
			set { if( m_buttonBackground != value ) { m_buttonBackground = value; m_version++; } }
		}

		[Header( "Dropdowns" )]
		[SerializeField]
		private Color m_dropdownColor = Color.white;
		public Color DropdownColor
		{
			get { return m_dropdownColor; }
			set { if( m_dropdownColor != value ) { m_dropdownColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_dropdownTextColor = Color.black;
		public Color DropdownTextColor
		{
			get { return m_dropdownTextColor; }
			set { if( m_dropdownTextColor != value ) { m_dropdownTextColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_dropdownArrowColor = Color.black;
		public Color DropdownArrowColor
		{
			get { return m_dropdownArrowColor; }
			set { if( m_dropdownArrowColor != value ) { m_dropdownArrowColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_dropdownCheckmarkColor = Color.black;
		public Color DropdownCheckmarkColor
		{
			get { return m_dropdownCheckmarkColor; }
			set { if( m_dropdownCheckmarkColor != value ) { m_dropdownCheckmarkColor = value; m_version++; } }
		}

		[SerializeField]
		private Sprite m_dropdownBackground;
		public Sprite DropdownBackground
		{
			get { return m_dropdownBackground; }
			set { if( m_dropdownBackground != value ) { m_dropdownBackground = value; m_version++; } }
		}

		[SerializeField]
		private Sprite m_dropdownArrow;
		public Sprite DropdownArrow
		{
			get { return m_dropdownArrow; }
			set { if( m_dropdownArrow != value ) { m_dropdownArrow = value; m_version++; } }
		}

		[SerializeField]
		private Sprite m_dropdownCheckmark;
		public Sprite DropdownCheckmark
		{
			get { return m_dropdownCheckmark; }
			set { if( m_dropdownCheckmark != value ) { m_dropdownCheckmark = value; m_version++; } }
		}

		[Header( "Toggles" )]
		[SerializeField]
		private Color m_toggleColor = Color.white;
		public Color ToggleColor
		{
			get { return m_toggleColor; }
			set { if( m_toggleColor != value ) { m_toggleColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_toggleTextColor = Color.black;
		public Color ToggleTextColor
		{
			get { return m_toggleTextColor; }
			set { if( m_toggleTextColor != value ) { m_toggleTextColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_toggleCheckmarkColor = Color.black;
		public Color ToggleCheckmarkColor
		{
			get { return m_toggleCheckmarkColor; }
			set { if( m_toggleCheckmarkColor != value ) { m_toggleCheckmarkColor = value; m_version++; } }
		}

		[SerializeField]
		private Sprite m_toggleBackground;
		public Sprite ToggleBackground
		{
			get { return m_toggleBackground; }
			set { if( m_toggleBackground != value ) { m_toggleBackground = value; m_version++; } }
		}

		[SerializeField]
		private Sprite m_toggleCheckmark;
		public Sprite ToggleCheckmark
		{
			get { return m_toggleCheckmark; }
			set { if( m_toggleCheckmark != value ) { m_toggleCheckmark = value; m_version++; } }
		}

		[Header( "Scrollbars" )]
		[SerializeField]
		private Color m_scrollbarBackgroundColor = Color.grey;
		public Color ScrollbarBackgroundColor
		{
			get { return m_scrollbarBackgroundColor; }
			set { if( m_scrollbarBackgroundColor != value ) { m_scrollbarBackgroundColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_scrollbarColor = Color.black;
		public Color ScrollbarColor
		{
			get { return m_scrollbarColor; }
			set { if( m_scrollbarColor != value ) { m_scrollbarColor = value; m_version++; } }
		}

		[Header( "Files" )]
		[SerializeField]
		private float m_fileHeight = 30f;
		public float FileHeight
		{
			get { return m_fileHeight; }
			set { if( m_fileHeight != value ) { m_fileHeight = value; m_version++; } }
		}

		[SerializeField]
		private float m_fileIconsPadding = 6f;
		public float FileIconsPadding
		{
			get { return m_fileIconsPadding; }
			set { if( m_fileIconsPadding != value ) { m_fileIconsPadding = value; m_version++; } }
		}

		[SerializeField]
		private Color m_fileNormalBackgroundColor = Color.clear;
		public Color FileNormalBackgroundColor
		{
			get { return m_fileNormalBackgroundColor; }
			set { if( m_fileNormalBackgroundColor != value ) { m_fileNormalBackgroundColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_fileAlternatingBackgroundColor = Color.clear;
		public Color FileAlternatingBackgroundColor
		{
			get { return m_fileAlternatingBackgroundColor; }
			set { if( m_fileAlternatingBackgroundColor != value ) { m_fileAlternatingBackgroundColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_fileHoveredBackgroundColor = Color.cyan;
		public Color FileHoveredBackgroundColor
		{
			get { return m_fileHoveredBackgroundColor; }
			set { if( m_fileHoveredBackgroundColor != value ) { m_fileHoveredBackgroundColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_fileSelectedBackgroundColor = Color.blue;
		public Color FileSelectedBackgroundColor
		{
			get { return m_fileSelectedBackgroundColor; }
			set { if( m_fileSelectedBackgroundColor != value ) { m_fileSelectedBackgroundColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_fileNormalTextColor = Color.black;
		public Color FileNormalTextColor
		{
			get { return m_fileNormalTextColor; }
			set { if( m_fileNormalTextColor != value ) { m_fileNormalTextColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_fileSelectedTextColor = Color.black;
		public Color FileSelectedTextColor
		{
			get { return m_fileSelectedTextColor; }
			set { if( m_fileSelectedTextColor != value ) { m_fileSelectedTextColor = value; m_version++; } }
		}

		[Header( "File Icons" )]
		[SerializeField]
		private Sprite m_folderIcon;
		public Sprite FolderIcon
		{
			get { return m_folderIcon; }
			set { if( m_folderIcon != value ) { m_folderIcon = value; m_version++; } }
		}

		[SerializeField]
		private Sprite m_driveIcon;
		public Sprite DriveIcon
		{
			get { return m_driveIcon; }
			set { if( m_driveIcon != value ) { m_driveIcon = value; m_version++; } }
		}

		[SerializeField]
		private Sprite m_defaultFileIcon;
		public Sprite DefaultFileIcon
		{
			get { return m_defaultFileIcon; }
			set { if( m_defaultFileIcon != value ) { m_defaultFileIcon = value; m_version++; } }
		}

		[SerializeField]
		private FiletypeIcon[] m_filetypeIcons;
		public FiletypeIcon[] FiletypeIcons
		{
			get { return m_filetypeIcons; }
			set
			{
				if( m_filetypeIcons != value )
				{
					m_filetypeIcons = value;
					initializedFiletypeIcons = false;
					m_version++;
				}
			}
		}

		[NonSerialized] // Never save this value during domain reload (it's sometimes saved even though it's private)
		private bool initializedFiletypeIcons = false;
		private Dictionary<string, Sprite> filetypeToIcon;

		[NonSerialized]
		private bool m_allIconExtensionsHaveSingleSuffix = true;
		public bool AllIconExtensionsHaveSingleSuffix
		{
			get
			{
				if( !initializedFiletypeIcons )
					InitializeFiletypeIcons();

				return m_allIconExtensionsHaveSingleSuffix;
			}
		}

		[SerializeField]
		private Sprite m_fileMultiSelectionToggleOffIcon;
		public Sprite FileMultiSelectionToggleOffIcon
		{
			get { return m_fileMultiSelectionToggleOffIcon; }
			set { if( m_fileMultiSelectionToggleOffIcon != value ) { m_fileMultiSelectionToggleOffIcon = value; m_version++; } }
		}

		[SerializeField]
		private Sprite m_fileMultiSelectionToggleOnIcon;
		public Sprite FileMultiSelectionToggleOnIcon
		{
			get { return m_fileMultiSelectionToggleOnIcon; }
			set { if( m_fileMultiSelectionToggleOnIcon != value ) { m_fileMultiSelectionToggleOnIcon = value; m_version++; } }
		}

		[Header( "Context Menu" )]
		[SerializeField]
		private Color m_contextMenuBackgroundColor = Color.grey;
		public Color ContextMenuBackgroundColor
		{
			get { return m_contextMenuBackgroundColor; }
			set { if( m_contextMenuBackgroundColor != value ) { m_contextMenuBackgroundColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_contextMenuTextColor = Color.black;
		public Color ContextMenuTextColor
		{
			get { return m_contextMenuTextColor; }
			set { if( m_contextMenuTextColor != value ) { m_contextMenuTextColor = value; m_version++; } }
		}

		[SerializeField]
		private Color m_contextMenuSeparatorColor = Color.black;
		public Color ContextMenuSeparatorColor
		{
			get { return m_contextMenuSeparatorColor; }
			set { if( m_contextMenuSeparatorColor != value ) { m_contextMenuSeparatorColor = value; m_version++; } }
		}

		[Header( "Popup Panels" )]
		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs( "m_deletePanelBackgroundColor" )]
		private Color m_popupPanelsBackgroundColor = Color.grey;
		public Color PopupPanelsBackgroundColor
		{
			get { return m_popupPanelsBackgroundColor; }
			set { if( m_popupPanelsBackgroundColor != value ) { m_popupPanelsBackgroundColor = value; m_version++; } }
		}

		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs( "m_deletePanelTextColor" )]
		private Color m_popupPanelsTextColor = Color.black;
		public Color PopupPanelsTextColor
		{
			get { return m_popupPanelsTextColor; }
			set { if( m_popupPanelsTextColor != value ) { m_popupPanelsTextColor = value; m_version++; } }
		}

		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs( "m_deletePanelBackground" )]
		private Sprite m_popupPanelsBackground;
		public Sprite PopupPanelsBackground
		{
			get { return m_popupPanelsBackground; }
			set { if( m_popupPanelsBackground != value ) { m_popupPanelsBackground = value; m_version++; } }
		}
#pragma warning restore 0649

		public void ApplyTo( Text text, Color textColor )
		{
			text.color = textColor;
			text.font = m_font;
			text.fontSize = m_fontSize;
		}

		public void ApplyTo( InputField inputField )
		{
			inputField.image.color = m_inputFieldNormalBackgroundColor;
			inputField.image.sprite = m_inputFieldBackground;
			inputField.selectionColor = m_inputFieldSelectedTextColor;
			inputField.caretColor = m_inputFieldCaretColor;

			ApplyTo( inputField.textComponent, m_inputFieldTextColor );
			if( inputField.placeholder as Text )
				ApplyTo( (Text) inputField.placeholder, m_inputFieldPlaceholderTextColor );
		}

		public void ApplyTo( Button button )
		{
			button.image.color = m_buttonColor;
			button.image.sprite = m_buttonBackground;

			ApplyTo( button.GetComponentInChildren<Text>(), m_buttonTextColor );
		}

		public void ApplyTo( Dropdown dropdown )
		{
			dropdown.image.color = m_dropdownColor;
			dropdown.image.sprite = m_dropdownBackground;
			dropdown.template.GetComponent<Image>().color = m_dropdownColor;

			Image dropdownArrow = dropdown.transform.Find( "Arrow" ).GetComponent<Image>();
			dropdownArrow.color = m_dropdownArrowColor;
			dropdownArrow.sprite = m_dropdownArrow;

			ApplyTo( dropdown.captionText, m_dropdownTextColor );
			ApplyTo( dropdown.itemText, m_dropdownTextColor );

			Transform dropdownItem = dropdown.itemText.transform.parent;
			dropdownItem.Find( "Item Background" ).GetComponent<Image>().color = m_dropdownColor;

			Image dropdownCheckmark = dropdownItem.Find( "Item Checkmark" ).GetComponent<Image>();
			dropdownCheckmark.color = m_dropdownCheckmarkColor;
			dropdownCheckmark.sprite = m_dropdownCheckmark;
		}

		public void ApplyTo( Toggle toggle )
		{
			toggle.image.color = m_toggleColor;
			toggle.image.sprite = m_toggleBackground;
			toggle.graphic.color = m_toggleCheckmarkColor;
			( (Image) toggle.graphic ).sprite = m_toggleCheckmark;

			ApplyTo( toggle.GetComponentInChildren<Text>(), m_toggleTextColor );
		}

		public void ApplyTo( Scrollbar scrollbar )
		{
			scrollbar.GetComponent<Image>().color = m_scrollbarBackgroundColor;
			scrollbar.image.color = m_scrollbarColor;
		}

		public Sprite GetIconForFileEntry( FileSystemEntry fileInfo, bool extensionMayHaveMultipleSuffixes )
		{
			if( !initializedFiletypeIcons )
				InitializeFiletypeIcons();

			Sprite icon;
			if( fileInfo.IsDirectory )
				return m_folderIcon;
			else if( filetypeToIcon.TryGetValue( fileInfo.Extension, out icon ) )
				return icon;
			else if( extensionMayHaveMultipleSuffixes )
			{
				for( int i = 0; i < m_filetypeIcons.Length; i++ )
				{
					if( fileInfo.Extension.EndsWith( m_filetypeIcons[i].extension, StringComparison.Ordinal ) )
					{
						filetypeToIcon[fileInfo.Extension] = m_filetypeIcons[i].icon;
						return m_filetypeIcons[i].icon;
					}
				}
			}

			filetypeToIcon[fileInfo.Extension] = m_defaultFileIcon;
			return m_defaultFileIcon;
		}

		private void InitializeFiletypeIcons()
		{
			initializedFiletypeIcons = true;

			if( filetypeToIcon == null )
				filetypeToIcon = new Dictionary<string, Sprite>( 128 );
			else
				filetypeToIcon.Clear();

			m_allIconExtensionsHaveSingleSuffix = true;

			for( int i = 0; i < m_filetypeIcons.Length; i++ )
			{
				m_filetypeIcons[i].extension = m_filetypeIcons[i].extension.ToLowerInvariant();
				if( m_filetypeIcons[i].extension[0] != '.' )
					m_filetypeIcons[i].extension = "." + m_filetypeIcons[i].extension;

				filetypeToIcon[m_filetypeIcons[i].extension] = m_filetypeIcons[i].icon;

				m_allIconExtensionsHaveSingleSuffix &= ( m_filetypeIcons[i].extension.LastIndexOf( '.' ) == 0 );
			}
		}
	}
}