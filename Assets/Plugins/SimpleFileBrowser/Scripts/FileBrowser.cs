//#define WIN_DIR_CHECK_WITHOUT_TIMEOUT // When uncommented, Directory.Exists won't be wrapped inside a Task/Thread on Windows but we won't be able to set a timeout for unreachable directories/drives

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace SimpleFileBrowser
{
	public class FileBrowser : MonoBehaviour, IListViewAdapter
	{
		public enum Permission { Denied = 0, Granted = 1, ShouldAsk = 2 };
		public enum PickMode { Files = 0, Folders = 1, FilesAndFolders = 2 };

		#region Structs
#pragma warning disable 0649
		[Serializable]
		private struct FiletypeIcon
		{
			public string extension;
			public Sprite icon;
		}

		[Serializable]
		private struct QuickLink
		{
#if UNITY_EDITOR || ( !UNITY_WSA && !UNITY_WSA_10_0 )
			public Environment.SpecialFolder target;
#endif
			public string name;
			public Sprite icon;
		}
#pragma warning restore 0649
		#endregion

		#region Inner Classes
		public class Filter
		{
			public readonly string name;
			public readonly string[] extensions;
			public readonly HashSet<string> extensionsSet;
			public readonly string defaultExtension;
			public readonly bool allExtensionsHaveSingleSuffix; // 'false' when some extensions have multiple suffixes like ".tar.gz"

			internal Filter( string name )
			{
				this.name = name;
				extensions = null;
				extensionsSet = null;
				defaultExtension = null;
				allExtensionsHaveSingleSuffix = true;
			}

			public Filter( string name, string extension )
			{
				this.name = name;

				extension = extension.ToLowerInvariant();
				if( extension[0] != '.' )
					extension = "." + extension;

				extensions = new string[1] { extension };
				extensionsSet = new HashSet<string>() { extension };
				defaultExtension = extension;
				allExtensionsHaveSingleSuffix = ( extension.LastIndexOf( '.' ) == 0 );
			}

			public Filter( string name, params string[] extensions )
			{
				this.name = name;
				allExtensionsHaveSingleSuffix = true;

				for( int i = 0; i < extensions.Length; i++ )
				{
					extensions[i] = extensions[i].ToLowerInvariant();
					if( extensions[i][0] != '.' )
						extensions[i] = "." + extensions[i];

					allExtensionsHaveSingleSuffix &= ( extensions[i].LastIndexOf( '.' ) == 0 );
				}

				this.extensions = extensions;
				extensionsSet = new HashSet<string>( extensions );
				defaultExtension = extensions[0];
			}

			public bool MatchesExtension( string extension, bool extensionMayHaveMultipleSuffixes )
			{
				if( extensionsSet == null || extensionsSet.Contains( extension ) )
					return true;

				// When the provided extension may have multiple suffixes (e.g. ".tar.gz"), check if it ends with any of the
				// extensions in this filter (e.g. return true when this Filter has ".gz" and the provided extension is ".tar.gz")
				if( extensionMayHaveMultipleSuffixes )
				{
					for( int i = 0; i < extensions.Length; i++ )
					{
						if( extension.EndsWith( extensions[i], StringComparison.Ordinal ) )
						{
							extensionsSet.Add( extension );
							return true;
						}
					}
				}

				return false;
			}

			public override string ToString()
			{
				string result = string.Empty;

				if( name != null )
					result += name;

				if( extensions != null )
				{
					if( name != null )
						result += " (";

					for( int i = 0; i < extensions.Length; i++ )
					{
						if( i > 0 )
							result += ", " + extensions[i];
						else
							result += extensions[i];
					}

					if( name != null )
						result += ")";
				}

				return result;
			}
		}
		#endregion

		#region Constants
		private const int FILENAME_INPUT_FIELD_MAX_FILE_COUNT = 7;
		private const string SAF_PICK_FOLDER_QUICK_LINK_PATH = "SAF_PICK_FOLDER";
		#endregion

		#region Static Variables
		public static bool IsOpen { get; private set; }

		public static bool Success { get; private set; }
		public static string[] Result { get; private set; }

		[SerializeField]
		private UISkin m_skin;
#if UNITY_EDITOR
		private UISkin prevSkin;
#endif
		private int m_skinVersion = 0;
		private Sprite m_skinPrevDriveIcon, m_skinPrevFolderIcon;
		public static UISkin Skin
		{
			get { return Instance.m_skin; }
			set
			{
				if( value && Instance.m_skin != value )
				{
					Instance.m_skin = value;
					Instance.m_skinVersion = Instance.m_skin.Version;
					Instance.RefreshSkin();
				}
			}
		}

		private static bool m_askPermissions = true;
		public static bool AskPermissions
		{
			get { return m_askPermissions; }
			set { m_askPermissions = value; }
		}

		private static bool m_singleClickMode = false;
		public static bool SingleClickMode
		{
			get { return m_singleClickMode; }
			set { m_singleClickMode = value; }
		}

		private static FileSystemEntryFilter m_displayedEntriesFilter;
		public static event FileSystemEntryFilter DisplayedEntriesFilter
		{
			add
			{
				m_displayedEntriesFilter -= value;
				m_displayedEntriesFilter += value;

				if( m_instance )
				{
					m_instance.PersistFileEntrySelection();
					m_instance.RefreshFiles( false );
				}
			}
			remove
			{
				m_displayedEntriesFilter -= value;

				if( m_instance )
				{
					m_instance.PersistFileEntrySelection();
					m_instance.RefreshFiles( false );
				}
			}
		}

		private static bool m_showFileOverwriteDialog = true;
		public static bool ShowFileOverwriteDialog
		{
			get { return m_showFileOverwriteDialog; }
			set { m_showFileOverwriteDialog = value; }
		}

		private static bool m_checkWriteAccessToDestinationDirectory = false;
		public static bool CheckWriteAccessToDestinationDirectory
		{
			get { return m_checkWriteAccessToDestinationDirectory; }
			set { m_checkWriteAccessToDestinationDirectory = value; }
		}

#if UNITY_EDITOR || ( !UNITY_ANDROID && !UNITY_IOS && !UNITY_WSA && !UNITY_WSA_10_0 )
		private static float m_drivesRefreshInterval = 5f;
#else
		private static float m_drivesRefreshInterval = -1f;
#endif
		public static float DrivesRefreshInterval
		{
			get { return m_drivesRefreshInterval; }
			set { m_drivesRefreshInterval = value; }
		}

		public static bool ShowHiddenFiles
		{
			get { return Instance.showHiddenFilesToggle.isOn; }
			set { Instance.showHiddenFilesToggle.isOn = value; }
		}

		private static bool m_displayHiddenFilesToggle = true;
		public static bool DisplayHiddenFilesToggle
		{
			get { return m_displayHiddenFilesToggle; }
			set
			{
				if( m_displayHiddenFilesToggle != value )
				{
					m_displayHiddenFilesToggle = value;

					if( m_instance )
					{
						if( !value )
							m_instance.showHiddenFilesToggle.gameObject.SetActive( false );
						else if( m_instance.windowTR.sizeDelta.x >= m_instance.narrowScreenWidth )
						{
#if !UNITY_EDITOR && UNITY_ANDROID
							if( !FileBrowserHelpers.ShouldUseSAF )
#endif
							m_instance.showHiddenFilesToggle.gameObject.SetActive( true );
						}
					}
				}
			}
		}

		private static string m_allFilesFilterText = "All Files (.*)";
		public static string AllFilesFilterText
		{
			get { return m_allFilesFilterText; }
			set
			{
				if( m_allFilesFilterText != value )
				{
					string oldValue = m_allFilesFilterText;
					m_allFilesFilterText = value;

					if( m_instance )
					{
						Filter oldAllFilesFilter = m_instance.allFilesFilter;
						m_instance.allFilesFilter = new Filter( value );

						if( m_instance.filters.Count > 0 && m_instance.filters[0] == oldAllFilesFilter )
							m_instance.filters[0] = m_instance.allFilesFilter;

						if( m_instance.filtersDropdown.options[0].text == oldValue )
						{
							m_instance.filtersDropdown.options[0].text = value;
							m_instance.filtersDropdown.RefreshShownValue();
						}
					}
				}
			}
		}

		private static string m_foldersFilterText = "Folders";
		public static string FoldersFilterText
		{
			get { return m_foldersFilterText; }
			set
			{
				if( m_foldersFilterText != value )
				{
					string oldValue = m_foldersFilterText;
					m_foldersFilterText = value;

					if( m_instance && m_instance.filtersDropdown.options[0].text == oldValue )
					{
						m_instance.filtersDropdown.options[0].text = value;
						m_instance.filtersDropdown.RefreshShownValue();
					}
				}
			}
		}

		private static string m_pickFolderQuickLinkText = "Browse...";
		public static string PickFolderQuickLinkText
		{
			get { return m_pickFolderQuickLinkText; }
			set
			{
				if( m_pickFolderQuickLinkText != value )
				{
					m_pickFolderQuickLinkText = value;

					if( m_instance )
					{
						for( int i = 0; i < m_instance.allQuickLinks.Count; i++ )
						{
							FileBrowserQuickLink quickLink = m_instance.allQuickLinks[i];
							if( quickLink && quickLink.TargetPath == SAF_PICK_FOLDER_QUICK_LINK_PATH )
							{
								quickLink.SetQuickLink( Skin.DriveIcon, value, SAF_PICK_FOLDER_QUICK_LINK_PATH );
								break;
							}
						}
					}
				}
			}
		}

		private static FileBrowser m_instance = null;
		private static FileBrowser Instance
		{
			get
			{
				if( !m_instance )
				{
					m_instance = Instantiate( Resources.Load<GameObject>( "SimpleFileBrowserCanvas" ) ).GetComponent<FileBrowser>();
					DontDestroyOnLoad( m_instance.gameObject );
					m_instance.gameObject.SetActive( false );
				}

				return m_instance;
			}
		}
		#endregion

		#region Variables
#pragma warning disable 0649
		[Header( "Settings" )]
		[SerializeField]
		internal int minWidth = 380;
		[SerializeField]
		internal int minHeight = 300;

		[SerializeField]
		private float narrowScreenWidth = 380f;

		[SerializeField]
		private float quickLinksMaxWidthPercentage = 0.4f;

		[SerializeField]
		private bool sortFilesByName = true;

		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs( "excludeExtensions" )]
		private string[] excludedExtensions;

#pragma warning disable 0414
		[SerializeField]
		private QuickLink[] quickLinks;
		private static bool quickLinksInitialized;
#pragma warning restore 0414

		private readonly HashSet<string> excludedExtensionsSet = new HashSet<string>();

		[SerializeField]
		private bool generateQuickLinksForDrives = true;

		[SerializeField]
		private bool contextMenuShowDeleteButton = true;

		[SerializeField]
		private bool contextMenuShowRenameButton = true;

		[SerializeField]
		private bool showResizeCursor = true;

		[Header( "Internal References" )]

		[SerializeField]
		private FileBrowserMovement window;
		private RectTransform windowTR;

		[SerializeField]
		private RectTransform topViewNarrowScreen;

		[SerializeField]
		private RectTransform middleView;
		private Vector2 middleViewOriginalPosition;
		private Vector2 middleViewOriginalSize;

		[SerializeField]
		private RectTransform middleViewQuickLinks;
		private Vector2 middleViewQuickLinksOriginalSize;

		[SerializeField]
		private RectTransform middleViewFiles;

		[SerializeField]
		private RectTransform middleViewSeparator;

		[SerializeField]
		private FileBrowserItem itemPrefab;
		private readonly List<FileBrowserItem> allItems = new List<FileBrowserItem>( 16 );

		[SerializeField]
		private FileBrowserQuickLink quickLinkPrefab;
		private readonly List<FileBrowserQuickLink> allQuickLinks = new List<FileBrowserQuickLink>( 8 );

		[SerializeField]
		private Text titleText;

		[SerializeField]
		private Button backButton;

		[SerializeField]
		private Button forwardButton;

		[SerializeField]
		private Button upButton;

		[SerializeField]
		private Button moreOptionsButton;

		[SerializeField]
		private InputField pathInputField;

		[SerializeField]
		private RectTransform pathInputFieldSlotTop;

		[SerializeField]
		private RectTransform pathInputFieldSlotBottom;

		[SerializeField]
		private InputField searchInputField;

		[SerializeField]
		private RectTransform quickLinksContainer;

		[SerializeField]
		private ScrollRect quickLinksScrollRect;

		[SerializeField]
		private RectTransform filesContainer;

		[SerializeField]
		private ScrollRect filesScrollRect;

		[SerializeField]
		private RecycledListView listView;

		[SerializeField]
		private InputField filenameInputField;

		[SerializeField]
		private Text filenameInputFieldOverlayText;

		[SerializeField]
		private Image filenameImage;

		[SerializeField]
		private Dropdown filtersDropdown;

		[SerializeField]
		private RectTransform filtersDropdownContainer;

		[SerializeField]
		private Text filterItemTemplate;

		[SerializeField]
		private Toggle showHiddenFilesToggle;

		[SerializeField]
		private Text submitButtonText;

		[SerializeField]
		private Button[] allButtons;

		[SerializeField]
		private RectTransform moreOptionsContextMenuPosition;

		[SerializeField]
		private FileBrowserRenamedItem renameItem;

		[SerializeField]
		private FileBrowserContextMenu contextMenu;

		[SerializeField]
		private FileBrowserFileOperationConfirmationPanel fileOperationConfirmationPanel;

		[SerializeField]
		private FileBrowserAccessRestrictedPanel accessRestrictedPanel;

		[SerializeField]
		private FileBrowserCursorHandler resizeCursorHandler;
#pragma warning restore 0649

		internal RectTransform rectTransform;
		private Canvas canvas;

		private FileAttributes ignoredFileAttributes = FileAttributes.System;

		private FileSystemEntry[] allFileEntries;
		private readonly List<FileSystemEntry> validFileEntries = new List<FileSystemEntry>();
		private readonly List<int> selectedFileEntries = new List<int>( 4 );
		private readonly List<string> pendingFileEntrySelection = new List<string>();

		private readonly List<string> submittedFileEntryPaths = new List<string>( 4 );
		private readonly List<string> submittedFolderPaths = new List<string>( 4 ); // Used to check if all destination folders have write access
		private readonly List<FileSystemEntry> submittedFileEntriesToOverwrite = new List<FileSystemEntry>( 4 ); // Existing files selected by the user in save mode

#pragma warning disable 0414 // Value is assigned but never used on Android & iOS
		private int multiSelectionPivotFileEntry;
#pragma warning restore 0414
		private StringBuilder multiSelectionFilenameBuilder;

		private readonly List<Filter> filters = new List<Filter>();
		private Filter allFilesFilter;

		private bool showAllFilesFilter = true;

		// Single suffix: ".mp4", ".txt", etc.
		// Multiple suffixes: ".tar.gz", etc.
		private bool allFiltersHaveSingleSuffix = true;
		private bool allExcludedExtensionsHaveSingleSuffix = true;
		// When its value is 'true', file extensions will be handled in a more optimized way
		private bool AllExtensionsHaveSingleSuffix { get { return allFiltersHaveSingleSuffix && allExcludedExtensionsHaveSingleSuffix && m_skin.AllIconExtensionsHaveSingleSuffix; } }

		private string defaultInitialPath;

		private int currentPathIndex = -1;
		private readonly List<string> pathsFollowed = new List<string>();

		private HashSet<char> invalidFilenameChars;

		private float drivesNextRefreshTime;
#if !UNITY_EDITOR && UNITY_ANDROID
		private string driveQuickLinks;
#else
		private string[] driveQuickLinks;
#endif
		private int numberOfDriveQuickLinks;

#if !WIN_DIR_CHECK_WITHOUT_TIMEOUT && ( UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN )
		private readonly List<string> timedOutDirectoryExistsRequests = new List<string>( 2 );
#endif

		private bool canvasDimensionsChanged;

		private readonly CompareInfo textComparer = new CultureInfo( "en-US" ).CompareInfo;
		private readonly CompareOptions textCompareOptions = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

		// Required in RefreshFiles() function
		private PointerEventData nullPointerEventData;
		#endregion

		#region Properties
		private string m_currentPath = string.Empty;
		private string CurrentPath
		{
			get { return m_currentPath; }
			set
			{
				if( value != null )
				{
					value = value.Trim();
#if !UNITY_EDITOR && UNITY_ANDROID
					if( !FileBrowserHelpers.ShouldUseSAFForPath( value ) )
#endif
					value = GetPathWithoutTrailingDirectorySeparator( value );
				}

				if( string.IsNullOrEmpty( value ) )
				{
					pathInputField.text = m_currentPath;
					return;
				}

				if( m_currentPath != value )
				{
					if( !FileBrowserHelpers.DirectoryExists( value ) )
					{
						pathInputField.text = m_currentPath;
						return;
					}

					m_currentPath = value;
					pathInputField.text = m_currentPath;

					if( currentPathIndex == -1 || pathsFollowed[currentPathIndex] != m_currentPath )
					{
						currentPathIndex++;
						if( currentPathIndex < pathsFollowed.Count )
						{
							pathsFollowed[currentPathIndex] = value;
							for( int i = pathsFollowed.Count - 1; i >= currentPathIndex + 1; i-- )
								pathsFollowed.RemoveAt( i );
						}
						else
							pathsFollowed.Add( m_currentPath );
					}

					backButton.interactable = currentPathIndex > 0;
					forwardButton.interactable = currentPathIndex < pathsFollowed.Count - 1;
#if !UNITY_EDITOR && UNITY_ANDROID
					if( FileBrowserHelpers.ShouldUseSAF )
					{
						string parentPath = FileBrowserHelpers.GetDirectoryName( m_currentPath );
						upButton.interactable = !string.IsNullOrEmpty( parentPath ) && ( FileBrowserHelpers.ShouldUseSAFForPath( parentPath ) || FileBrowserHelpers.DirectoryExists( parentPath ) ); // DirectoryExists: Directory may not be accessible on Android 10+, this function checks that
					}
					else
#endif
					{
						try // When "C:/" or "C:" is typed instead of "C:\", an exception is thrown
						{
							upButton.interactable = Directory.GetParent( m_currentPath ) != null;
						}
						catch
						{
							upButton.interactable = false;
						}
					}

					m_searchString = string.Empty;
					searchInputField.text = m_searchString;

					multiSelectionPivotFileEntry = 0;
					filesScrollRect.verticalNormalizedPosition = 1;

					filenameImage.color = m_skin.InputFieldNormalBackgroundColor;
					if( m_pickerMode != PickMode.Files )
					{
						filenameInputField.text = string.Empty;
						filenameInputField.interactable = true;
					}

					// If a quick link points to this directory, highlight it
#if !UNITY_EDITOR && UNITY_ANDROID
					// Path strings aren't deterministic on Storage Access Framework but the paths' absolute parts usually are
					if( FileBrowserHelpers.ShouldUseSAFForPath( m_currentPath ) )
					{
						int SAFAbsolutePathSeparatorIndex = m_currentPath.LastIndexOf( '/' );
						if( SAFAbsolutePathSeparatorIndex >= 0 )
						{
							string absoluteSAFPath = m_currentPath.Substring( SAFAbsolutePathSeparatorIndex );
							for( int i = 0; i < allQuickLinks.Count; i++ )
								allQuickLinks[i].SetSelected( allQuickLinks[i].TargetPath == m_currentPath || allQuickLinks[i].TargetPath.EndsWith( absoluteSAFPath ) );
						}
						else
						{
							for( int i = 0; i < allQuickLinks.Count; i++ )
								allQuickLinks[i].SetSelected( allQuickLinks[i].TargetPath == m_currentPath );
						}
					}
					else
#endif
					{
						for( int i = 0; i < allQuickLinks.Count; i++ )
							allQuickLinks[i].SetSelected( allQuickLinks[i].TargetPath == m_currentPath );
					}
				}

				m_multiSelectionToggleSelectionMode = false;
				RefreshFiles( true );
			}
		}

		private string m_searchString = string.Empty;
		private string SearchString
		{
			get { return m_searchString; }
			set
			{
				if( m_searchString != value )
				{
					m_searchString = value;
					searchInputField.text = m_searchString;

					RefreshFiles( false );
				}
			}
		}

		private bool m_acceptNonExistingFilename = false; // Is set to true when showing save dialog for Files or FilesAndFolders, false otherwise
		private bool AcceptNonExistingFilename
		{
			get { return m_acceptNonExistingFilename; }
			set { m_acceptNonExistingFilename = value; }
		}

		private PickMode m_pickerMode = PickMode.Files;
		internal PickMode PickerMode
		{
			get { return m_pickerMode; }
			private set
			{
				m_pickerMode = value;

				if( m_pickerMode == PickMode.Folders )
				{
					filtersDropdown.options[0].text = FoldersFilterText;
					filtersDropdown.value = 0;
					filtersDropdown.interactable = false;
				}
				else
				{
					filtersDropdown.options[0].text = filters[0].ToString();
					filtersDropdown.interactable = true;
				}

				filtersDropdown.RefreshShownValue();

				Text placeholder = filenameInputField.placeholder as Text;
				if( placeholder )
					placeholder.gameObject.SetActive( m_pickerMode != PickMode.Folders );
			}
		}

		private bool m_allowMultiSelection;
		internal bool AllowMultiSelection
		{
			get { return m_allowMultiSelection; }
			private set { m_allowMultiSelection = value; }
		}

		private bool m_multiSelectionToggleSelectionMode;
		internal bool MultiSelectionToggleSelectionMode
		{
			get { return m_multiSelectionToggleSelectionMode; }
			private set
			{
				if( m_multiSelectionToggleSelectionMode != value )
				{
					m_multiSelectionToggleSelectionMode = value;

					for( int i = 0; i < allItems.Count; i++ )
					{
						if( allItems[i].gameObject.activeSelf )
							allItems[i].SetSelected( selectedFileEntries.Contains( allItems[i].Position ) );
					}
				}
			}
		}

		private string Title
		{
			get { return titleText.text; }
			set { titleText.text = value; }
		}

		private string SubmitButtonText
		{
			get { return submitButtonText.text; }
			set { submitButtonText.text = value; }
		}

		private string LastBrowsedFolder
		{
			get { return PlayerPrefs.GetString( "FBLastPath", null ); }
			set { PlayerPrefs.SetString( "FBLastPath", value ); }
		}
		#endregion

		#region Delegates
		public delegate void OnSuccess( string[] paths );
		public delegate void OnCancel();
		public delegate bool FileSystemEntryFilter( FileSystemEntry entry );
#if UNITY_EDITOR || UNITY_ANDROID
		public delegate void AndroidSAFDirectoryPickCallback( string rawUri, string name );
#endif

		private OnSuccess onSuccess;
		private OnCancel onCancel;
		#endregion

		#region Messages
		private void Awake()
		{
			m_instance = this;

			rectTransform = (RectTransform) transform;
			windowTR = (RectTransform) window.transform;
			canvas = GetComponent<Canvas>();

			middleViewOriginalPosition = middleView.anchoredPosition;
			middleViewOriginalSize = middleView.sizeDelta;
			middleViewQuickLinksOriginalSize = middleViewQuickLinks.sizeDelta;

			nullPointerEventData = new PointerEventData( null );

#if !UNITY_EDITOR && ( UNITY_ANDROID || UNITY_IOS || UNITY_WSA || UNITY_WSA_10_0 )
			defaultInitialPath = Application.persistentDataPath;
#else
			defaultInitialPath = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
#endif

#if !UNITY_EDITOR && UNITY_ANDROID
			if( FileBrowserHelpers.ShouldUseSAF )
			{
				// These UI elements have no use in Storage Access Framework mode (Android 10+)
				pathInputField.gameObject.SetActive( false );
				showHiddenFilesToggle.gameObject.SetActive( false );
			}
#endif

			SetExcludedExtensions( excludedExtensions );

			backButton.interactable = false;
			forwardButton.interactable = false;
			upButton.interactable = false;

			filenameInputField.onValidateInput += OnValidateFilenameInput;
			filenameInputField.onValueChanged.AddListener( OnFilenameInputChanged );

			allFilesFilter = new Filter( AllFilesFilterText );
			filters.Add( allFilesFilter );

			invalidFilenameChars = new HashSet<char>( Path.GetInvalidFileNameChars() )
			{
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			};

			window.Initialize( this );
			listView.SetAdapter( this );

			// Refresh the skin immediately
			m_skinVersion = m_skin.Version;
			RefreshSkin();

			if( !showResizeCursor )
				Destroy( resizeCursorHandler );

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			// On new Input System, scroll sensitivity is much higher than legacy Input system
			filesScrollRect.scrollSensitivity *= 0.25f;
			quickLinksContainer.GetComponentInParent<ScrollRect>().scrollSensitivity *= 0.25f;
			filtersDropdownContainer.GetComponent<ScrollRect>().scrollSensitivity *= 0.25f;
#endif
		}

		private void OnRectTransformDimensionsChange()
		{
			canvasDimensionsChanged = true;
		}

#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			// Refresh the skin in the next Update if it is changed via Unity Inspector at runtime
			if( UnityEditor.EditorApplication.isPlaying && m_skin != prevSkin )
			{
				if( !m_skin ) // Don't allow null UISkin
					m_skin = prevSkin;
				else
					m_skinVersion = m_skin.Version - 1;
			}
		}
#endif

		private void Update()
		{
			if( m_skin && m_skinVersion != m_skin.Version )
			{
				m_skinVersion = m_skin.Version;
				RefreshSkin();

#if UNITY_EDITOR
				prevSkin = m_skin;
#endif
			}
		}

		private void LateUpdate()
		{
			if( canvasDimensionsChanged )
			{
				canvasDimensionsChanged = false;

				Vector2 windowSize = windowTR.sizeDelta;
				EnsureWindowIsWithinBounds();
				if( windowTR.sizeDelta != windowSize )
					OnWindowDimensionsChanged( windowTR.sizeDelta );

				fileOperationConfirmationPanel.OnCanvasDimensionsChanged( rectTransform.sizeDelta );

				if( contextMenu.gameObject.activeSelf )
					contextMenu.Hide();
			}

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL || UNITY_WSA || UNITY_WSA_10_0
			// Handle keyboard shortcuts
			if( !EventSystem.current.currentSelectedGameObject )
			{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
				if( Keyboard.current != null )
#endif
				{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
					if( Keyboard.current[Key.Delete].wasPressedThisFrame )
#else
					if( Input.GetKeyDown( KeyCode.Delete ) )
#endif
						DeleteSelectedFiles();

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
					if( Keyboard.current[Key.F2].wasPressedThisFrame )
#else
					if( Input.GetKeyDown( KeyCode.F2 ) )
#endif
						RenameSelectedFile();

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
					if( Keyboard.current[Key.A].wasPressedThisFrame && IsCtrlKeyHeld() )
#else
					if( Input.GetKeyDown( KeyCode.A ) && IsCtrlKeyHeld() )
#endif
						SelectAllFiles();
				}
			}
#endif

			// 2 Text objects are used in the filename input field:
			// filenameInputField.textComponent: visible when editing the text, has Horizontal Overflow set to Wrap (cuts out words, ugly)
			// filenameInputFieldOverlayText: visible when not editing the text, has Horizontal Overflow set to Overflow (doesn't cut out words)
			if( EventSystem.current.currentSelectedGameObject == filenameInputField.gameObject )
			{
				if( filenameInputFieldOverlayText.enabled )
				{
					filenameInputFieldOverlayText.enabled = false;
					filenameInputField.textComponent.color = m_skin.InputFieldTextColor;
				}
			}
			else if( !filenameInputFieldOverlayText.enabled )
			{
				filenameInputFieldOverlayText.enabled = true;

				Color c = m_skin.InputFieldTextColor;
				c.a = 0f;
				filenameInputField.textComponent.color = c;
			}

			// Refresh drive quick links
#if UNITY_EDITOR || ( !UNITY_IOS && !UNITY_WSA && !UNITY_WSA_10_0 )
#if !UNITY_EDITOR && UNITY_ANDROID
			if( !FileBrowserHelpers.ShouldUseSAF )
#endif
			if( quickLinksInitialized && generateQuickLinksForDrives && m_drivesRefreshInterval >= 0f && Time.realtimeSinceStartup >= drivesNextRefreshTime )
			{
				drivesNextRefreshTime = Time.realtimeSinceStartup + m_drivesRefreshInterval;
				RefreshDriveQuickLinks();
			}
#endif
		}

		private void OnApplicationFocus( bool focus )
		{
			if( !focus )
				PersistFileEntrySelection();
			else
				RefreshFiles( true );
		}
		#endregion

		#region Interface Methods
		OnItemClickedHandler IListViewAdapter.OnItemClicked { get { return null; } set { } }

		int IListViewAdapter.Count { get { return validFileEntries.Count; } }
		float IListViewAdapter.ItemHeight { get { return m_skin.FileHeight; } }

		ListItem IListViewAdapter.CreateItem()
		{
			FileBrowserItem item = (FileBrowserItem) Instantiate( itemPrefab, filesContainer, false );
			item.SetFileBrowser( this, m_skin );
			allItems.Add( item );

			return item;
		}

		void IListViewAdapter.SetItemContent( ListItem item )
		{
			FileBrowserItem file = (FileBrowserItem) item;
			FileSystemEntry fileInfo = validFileEntries[item.Position];

			file.SetFile( GetIconForFileEntry( fileInfo ), fileInfo.Name, fileInfo.IsDirectory );
			file.SetSelected( selectedFileEntries.Contains( file.Position ) );
			file.SetHidden( ( fileInfo.Attributes & FileAttributes.Hidden ) == FileAttributes.Hidden );
		}
		#endregion

		#region Initialization Functions
		private void InitializeQuickLinks()
		{
			quickLinksInitialized = true;
			drivesNextRefreshTime = Time.realtimeSinceStartup + m_drivesRefreshInterval;

#if !UNITY_EDITOR && UNITY_ANDROID
			if( FileBrowserHelpers.ShouldUseSAF )
			{
				AddQuickLink( m_skin.DriveIcon, PickFolderQuickLinkText, SAF_PICK_FOLDER_QUICK_LINK_PATH );
				
				try
				{
					FetchPersistedSAFQuickLinks();
				}
				catch( Exception e )
				{
					Debug.LogException( e );
				}

				return;
			}
#endif

			if( generateQuickLinksForDrives )
			{
#if UNITY_EDITOR || ( !UNITY_IOS && !UNITY_WSA && !UNITY_WSA_10_0 )
				RefreshDriveQuickLinks();
#else
				AddQuickLink( m_skin.DriveIcon, "Files", Application.persistentDataPath );
#endif

#if UNITY_STANDALONE_OSX
				// Add a quick link for user directory on Mac OS
				string userDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );
				if( !string.IsNullOrEmpty( userDirectory ) )
					AddQuickLink( m_skin.DriveIcon, userDirectory.Substring( userDirectory.LastIndexOf( '/' ) + 1 ), userDirectory );
#endif
			}

#if UNITY_EDITOR || ( !UNITY_ANDROID && !UNITY_WSA && !UNITY_WSA_10_0 )
			for( int i = 0; i < quickLinks.Length; i++ )
			{
				QuickLink quickLink = quickLinks[i];
				string quickLinkPath = Environment.GetFolderPath( quickLink.target );
#if UNITY_STANDALONE_OSX
				// Documents folder must be appended manually on Mac OS
				if( quickLink.target == Environment.SpecialFolder.MyDocuments && !string.IsNullOrEmpty( quickLinkPath ) )
					quickLinkPath = Path.Combine( quickLinkPath, "Documents" );
#endif

				AddQuickLink( quickLink.icon, quickLink.name, quickLinkPath );
			}

			quickLinks = null;
#endif
		}

		private void RefreshDriveQuickLinks()
		{
			// Check if drives has changed since the last refresh
#if !UNITY_EDITOR && UNITY_ANDROID
			string drivesList = FileBrowserHelpers.AJC.CallStatic<string>( "GetExternalDrives", FileBrowserHelpers.Context );
			if( drivesList == driveQuickLinks || ( string.IsNullOrEmpty( drivesList ) && string.IsNullOrEmpty( driveQuickLinks ) ) )
				return;

			driveQuickLinks = drivesList;
#else
			string[] drives = Directory.GetLogicalDrives();

			if( driveQuickLinks != null && drives.Length == driveQuickLinks.Length )
			{
				bool drivesListHasntChanged = true;
				for( int i = 0; i < drives.Length; i++ )
				{
#if !WIN_DIR_CHECK_WITHOUT_TIMEOUT && ( UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN )
					if( timedOutDirectoryExistsRequests.Contains( drives[i] ) )
						continue;
#endif

					if( drives[i] != driveQuickLinks[i] )
					{
						drivesListHasntChanged = false;
						break;
					}
				}

				if( drivesListHasntChanged )
					return;
			}

			driveQuickLinks = drives;
#endif

			// Drives has changed, remove previous drive quick links
			for( ; numberOfDriveQuickLinks > 0; numberOfDriveQuickLinks-- )
			{
				Destroy( allQuickLinks[numberOfDriveQuickLinks - 1].gameObject );
				allQuickLinks.RemoveAt( numberOfDriveQuickLinks - 1 );
			}

			FileBrowserQuickLink[] customQuickLinks = allQuickLinks.Count > 0 ? allQuickLinks.ToArray() : null;
			allQuickLinks.Clear();

			quickLinksContainer.sizeDelta = Vector2.zero;

			// Create drive quick links
#if !UNITY_EDITOR && UNITY_ANDROID
			if( drivesList != null && drivesList.Length > 0 )
			{
				bool defaultPathInitialized = false;
				int driveIndex = 1;
				string[] drives = drivesList.Split( ':' );
				for( int i = 0; i < drives.Length; i++ )
				{
					try
					{
						//string driveName = new DirectoryInfo( drives[i] ).Name;
						//if( driveName.Length <= 1 )
						//{
						//	try
						//	{
						//		driveName = Directory.GetParent( drives[i] ).Name + "/" + driveName;
						//	}
						//	catch
						//	{
						//		driveName = "Drive " + driveIndex++;
						//	}
						//}	

						string driveName;
						if( !defaultPathInitialized )
						{
							defaultInitialPath = drives[i];
							defaultPathInitialized = true;

							driveName = "Primary Drive";
						}
						else
						{
							if( driveIndex == 1 )
								driveName = "External Drive";
							else
								driveName = "External Drive " + driveIndex;

							driveIndex++;
						}

						if( AddQuickLink( m_skin.DriveIcon, driveName, drives[i] ) )
							numberOfDriveQuickLinks++;
					}
					catch { }
				}
			}
#else
			for( int i = 0; i < drives.Length; i++ )
			{
				if( string.IsNullOrEmpty( drives[i] ) )
					continue;

#if UNITY_STANDALONE_OSX
				// There are a number of useless drives listed on Mac OS, filter them
				if( drives[i] == "/" )
				{
					if( AddQuickLink( m_skin.DriveIcon, "Root", drives[i] ) )
						numberOfDriveQuickLinks++;
				}
				else if( drives[i].StartsWith( "/Volumes/" ) && drives[i] != "/Volumes/Recovery" )
				{
					if( AddQuickLink( m_skin.DriveIcon, drives[i].Substring( drives[i].LastIndexOf( '/' ) + 1 ), drives[i] ) )
						numberOfDriveQuickLinks++;
				}
#else
				if( AddQuickLink( m_skin.DriveIcon, drives[i], drives[i] ) )
					numberOfDriveQuickLinks++;
#endif
			}
#endif

			// Reposition custom quick links
			if( customQuickLinks != null )
			{
				Vector2 anchoredPos = new Vector2( 0f, -quickLinksContainer.sizeDelta.y );
				for( int i = 0; i < customQuickLinks.Length; i++ )
				{
					customQuickLinks[i].TransformComponent.anchoredPosition = anchoredPos;
					anchoredPos.y -= m_skin.FileHeight;

					allQuickLinks.Add( customQuickLinks[i] );
				}

				quickLinksContainer.sizeDelta = new Vector2( 0f, -anchoredPos.y );
			}

			// Verify that current directory still exists
			try
			{
				if( !string.IsNullOrEmpty( m_currentPath ) && !FileBrowserHelpers.DirectoryExists( m_currentPath ) )
				{
					string currentPathRoot = Path.GetPathRoot( m_currentPath );
					if( !string.IsNullOrEmpty( currentPathRoot ) && FileBrowserHelpers.DirectoryExists( currentPathRoot ) )
						CurrentPath = currentPathRoot;
					else if( allQuickLinks.Count > 0 )
						CurrentPath = allQuickLinks[0].TargetPath;
				}
			}
			catch { }
		}

		private void RefreshSkin()
		{
			window.GetComponent<Image>().color = m_skin.WindowColor;
			middleView.GetComponent<Image>().color = m_skin.FilesListColor;
			middleViewSeparator.GetComponent<Image>().color = m_skin.FilesVerticalSeparatorColor;

			titleText.transform.parent.GetComponent<Image>().color = m_skin.TitleBackgroundColor;
			m_skin.ApplyTo( titleText, m_skin.TitleTextColor );

			backButton.image.color = m_skin.HeaderButtonsColor;
			forwardButton.image.color = m_skin.HeaderButtonsColor;
			upButton.image.color = m_skin.HeaderButtonsColor;
			moreOptionsButton.image.color = m_skin.HeaderButtonsColor;

			backButton.image.sprite = m_skin.HeaderBackButton;
			forwardButton.image.sprite = m_skin.HeaderForwardButton;
			upButton.image.sprite = m_skin.HeaderUpButton;
			moreOptionsButton.image.sprite = m_skin.HeaderContextMenuButton;

			Image windowResizeGizmo = resizeCursorHandler.GetComponent<Image>();
			windowResizeGizmo.color = m_skin.WindowResizeGizmoColor;
			windowResizeGizmo.sprite = m_skin.WindowResizeGizmo;

			m_skin.ApplyTo( filenameInputField );
			m_skin.ApplyTo( pathInputField );
			m_skin.ApplyTo( searchInputField );
			m_skin.ApplyTo( renameItem.InputField );
			m_skin.ApplyTo( filenameInputFieldOverlayText, m_skin.InputFieldTextColor );

			if( !EventSystem.current || EventSystem.current.currentSelectedGameObject != filenameInputField.gameObject )
			{
				Color c = m_skin.InputFieldTextColor;
				c.a = 0f;
				filenameInputField.textComponent.color = c;
			}

			for( int i = 0; i < allButtons.Length; i++ )
				m_skin.ApplyTo( allButtons[i] );

			m_skin.ApplyTo( filtersDropdown );
			m_skin.ApplyTo( showHiddenFilesToggle );

			m_skin.ApplyTo( quickLinksScrollRect.verticalScrollbar );
			m_skin.ApplyTo( filesScrollRect.verticalScrollbar );
			m_skin.ApplyTo( filtersDropdown.template.GetComponent<ScrollRect>().verticalScrollbar );

			for( int i = 0; i < allQuickLinks.Count; i++ )
			{
				allQuickLinks[i].OnSkinRefreshed( m_skin );
				allQuickLinks[i].TransformComponent.anchoredPosition = new Vector2( 0f, allQuickLinks[i].TransformComponent.GetSiblingIndex() * -m_skin.FileHeight );

				if( allQuickLinks[i].Icon.sprite == m_skinPrevDriveIcon )
					allQuickLinks[i].Icon.sprite = m_skin.DriveIcon;
				else if( allQuickLinks[i].Icon.sprite == m_skinPrevFolderIcon )
					allQuickLinks[i].Icon.sprite = m_skin.FolderIcon;
			}

			quickLinksContainer.sizeDelta = new Vector2( 0f, allQuickLinks.Count * m_skin.FileHeight );

			for( int i = 0; i < allItems.Count; i++ )
				allItems[i].OnSkinRefreshed( m_skin );

			renameItem.TransformComponent.sizeDelta = new Vector2( renameItem.TransformComponent.sizeDelta.x, m_skin.FileHeight );

			contextMenu.RefreshSkin( m_skin );
			fileOperationConfirmationPanel.RefreshSkin( m_skin );
			accessRestrictedPanel.RefreshSkin( m_skin );

			listView.OnSkinRefreshed();

			m_skinPrevDriveIcon = m_skin.DriveIcon;
			m_skinPrevFolderIcon = m_skin.FolderIcon;
		}
		#endregion

		#region Button Events
		public void OnBackButtonPressed()
		{
			if( currentPathIndex > 0 )
				CurrentPath = pathsFollowed[--currentPathIndex];
		}

		public void OnForwardButtonPressed()
		{
			if( currentPathIndex < pathsFollowed.Count - 1 )
				CurrentPath = pathsFollowed[++currentPathIndex];
		}

		public void OnUpButtonPressed()
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( FileBrowserHelpers.ShouldUseSAF )
			{
				string parentPath = FileBrowserHelpers.GetDirectoryName( m_currentPath );
				if( !string.IsNullOrEmpty( parentPath ) && ( FileBrowserHelpers.ShouldUseSAFForPath( parentPath ) || FileBrowserHelpers.DirectoryExists( parentPath ) ) ) // DirectoryExists: Directory may not be accessible on Android 10+, this function checks that
					CurrentPath = parentPath;
			}
			else
#endif
			{
				try // When "C:/" or "C:" is typed instead of "C:\", an exception is thrown
				{
					DirectoryInfo parentPath = Directory.GetParent( m_currentPath );
					if( parentPath != null )
						CurrentPath = parentPath.FullName;
				}
				catch
				{
				}
			}
		}

		public void OnMoreOptionsButtonClicked()
		{
			ShowContextMenuAt( rectTransform.InverseTransformPoint( moreOptionsContextMenuPosition.position ), true );
		}

		internal void OnContextMenuTriggered( Vector2 pointerPos )
		{
			filesScrollRect.velocity = Vector2.zero;

			Vector2 position;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( rectTransform, pointerPos, canvas.worldCamera, out position );

			ShowContextMenuAt( position, false );
		}

		private void ShowContextMenuAt( Vector2 position, bool isMoreOptionsMenu )
		{
			if( string.IsNullOrEmpty( m_currentPath ) )
				return;

			bool selectAllButtonVisible = isMoreOptionsMenu && m_allowMultiSelection && validFileEntries.Count > 0;
			bool deselectAllButtonVisible = isMoreOptionsMenu && selectedFileEntries.Count > 1;
			bool deleteButtonVisible = contextMenuShowDeleteButton && selectedFileEntries.Count > 0;
			bool renameButtonVisible = contextMenuShowRenameButton && selectedFileEntries.Count == 1;

			if( selectAllButtonVisible && m_pickerMode == PickMode.Files )
			{
				// In file selection mode, if only folders exist in the current path, "Select All" option shouldn't be visible
				selectAllButtonVisible = false;
				for( int i = 0; i < validFileEntries.Count; i++ )
				{
					if( !validFileEntries[i].IsDirectory )
					{
						selectAllButtonVisible = true;
						break;
					}
				}
			}

			contextMenu.Show( selectAllButtonVisible, deselectAllButtonVisible, deleteButtonVisible, renameButtonVisible, position, isMoreOptionsMenu );
		}

		public void OnSubmitButtonClicked()
		{
			string[] result = null;
			string filenameInput = filenameInputField.text.Trim();

			submittedFileEntryPaths.Clear();
			submittedFolderPaths.Clear();
			submittedFileEntriesToOverwrite.Clear();

			if( filenameInput.Length == 0 )
			{
				if( m_pickerMode == PickMode.Files )
				{
					filenameImage.color = m_skin.InputFieldInvalidBackgroundColor;
					return;
				}
				else
				{
					result = new string[1] { m_currentPath };
					submittedFolderPaths.Add( m_currentPath );
				}
			}

			if( result == null )
			{
				if( m_allowMultiSelection && selectedFileEntries.Count > 1 )
				{
					// When multiple files are selected via file browser UI, filenameInputField is not interactable and will show
					// only the first FILENAME_INPUT_FIELD_MAX_FILE_COUNT entries for performance reasons. We should iterate over
					// selectedFileEntries instead of filenameInputField

					// Beforehand, check if a folder is selected in file selection mode. If so, open that directory
					if( m_pickerMode == PickMode.Files )
					{
						for( int i = 0; i < selectedFileEntries.Count; i++ )
						{
							if( validFileEntries[selectedFileEntries[i]].IsDirectory )
							{
								CurrentPath = validFileEntries[selectedFileEntries[i]].Path;
								return;
							}
						}
					}

					result = new string[selectedFileEntries.Count];
					for( int i = 0; i < selectedFileEntries.Count; i++ )
					{
						result[i] = validFileEntries[selectedFileEntries[i]].Path;

						if( validFileEntries[selectedFileEntries[i]].IsDirectory )
							submittedFolderPaths.Add( result[i] );
						else if( m_acceptNonExistingFilename )
						{
							submittedFileEntriesToOverwrite.Add( validFileEntries[selectedFileEntries[i]] );

							if( !submittedFolderPaths.Contains( m_currentPath ) )
								submittedFolderPaths.Add( m_currentPath );
						}
					}
				}
				else
				{
					// When multiple files aren't selected via file browser UI, we must consider the rare case where user manually enters
					// multiple filenames to filenameInputField in format "file1" "file2" and so on. So, we must parse filenameInputField

					for( int startIndex = 0, nextStartIndex = 0; startIndex < filenameInput.Length; startIndex = nextStartIndex )
					{
						int filenameLength = ExtractFilenameFromInput( filenameInput, ref startIndex, out nextStartIndex );
						if( filenameLength == 0 )
							continue;

						string filename = filenameInput.Substring( startIndex, filenameLength ).Trim();
						if( !VerifyFilename( filename ) )
						{
							// Check if user has entered a full path to input field instead of just a filename. Even if it's the case, don't immediately accept the full path,
							// first verify that it doesn't point to a file/folder that is ignored by the file browser
							try
							{
								if( FileBrowserHelpers.DirectoryExists( filename ) )
								{
									FileSystemEntry fileEntry = new FileSystemEntry( filename, FileBrowserHelpers.GetFilename( filename ), "", true );
									if( FileSystemEntryMatchesFilters( fileEntry, AllExtensionsHaveSingleSuffix ) )
									{
										if( m_pickerMode == PickMode.Files )
										{
											CurrentPath = filename;
											return;
										}
										else
										{
											submittedFileEntryPaths.Add( filename );
											submittedFolderPaths.Add( filename );

											continue;
										}
									}
								}
								else if( m_pickerMode != PickMode.Folders && FileBrowserHelpers.FileExists( filename ) )
								{
									string fullPathFilename = FileBrowserHelpers.GetFilename( filename );
									FileSystemEntry fileEntry = new FileSystemEntry( filename, fullPathFilename, GetExtensionFromFilename( fullPathFilename, AllExtensionsHaveSingleSuffix ), false );
									if( FileSystemEntryMatchesFilters( fileEntry, AllExtensionsHaveSingleSuffix ) )
									{
										submittedFileEntryPaths.Add( filename );
										submittedFileEntriesToOverwrite.Add( fileEntry );

										if( m_acceptNonExistingFilename )
											submittedFolderPaths.Add( FileBrowserHelpers.GetDirectoryName( filename ) );

										continue;
									}
								}
							}
							catch { }

							// Filename contains invalid characters or is completely whitespace
							filenameImage.color = m_skin.InputFieldInvalidBackgroundColor;
							return;
						}

						try
						{
							int fileEntryIndex = FilenameToFileEntryIndex( filename );
							if( fileEntryIndex < 0 )
							{
								if( m_pickerMode != PickMode.Folders )
								{
									bool isAllFilesFilterActive = filters[filtersDropdown.value].extensions == null;
									if( !m_acceptNonExistingFilename || !isAllFilesFilterActive )
									{
										// File couldn't be found but perhaps filename is missing the extension, check if any of the files match the filename without extension
										for( int i = 0; i < validFileEntries.Count; i++ )
										{
											if( !validFileEntries[i].IsDirectory && validFileEntries[i].Name.Length >= filename.Length + 2 && validFileEntries[i].Name[filename.Length] == '.' )
											{
												if( validFileEntries[i].Name.StartsWith( filename ) ) // Case-sensitive filename query
												{
													fileEntryIndex = i;
													break;
												}
												else if( textComparer.IsPrefix( validFileEntries[i].Name, filename, textCompareOptions ) ) // Case-insensitive filename query
												{
													// Don't exit the loop immediately because case-sensitive query takes precedence, we need to check all files to see if there's a case-sensitive match
													fileEntryIndex = i;
												}
											}
										}
									}

									if( m_acceptNonExistingFilename && fileEntryIndex < 0 && !isAllFilesFilterActive )
									{
										// In file saving mode, make sure that nonexisting files' extensions match one of the required extensions
										string fileExtension = GetExtensionFromFilename( filename, AllExtensionsHaveSingleSuffix );
										if( string.IsNullOrEmpty( fileExtension ) || !filters[filtersDropdown.value].MatchesExtension( fileExtension, !AllExtensionsHaveSingleSuffix ) )
										{
											filename = Path.ChangeExtension( filename, filters[filtersDropdown.value].defaultExtension );
											fileEntryIndex = FilenameToFileEntryIndex( filename );
										}
									}
								}
							}

							if( fileEntryIndex >= 0 ) // This is an existing file/folder
							{
								if( validFileEntries[fileEntryIndex].IsDirectory && m_pickerMode == PickMode.Files )
								{
									// Selected a directory in file selection mode, open that directory
									CurrentPath = validFileEntries[fileEntryIndex].Path;
									return;
								}
								else
								{
									submittedFileEntryPaths.Add( validFileEntries[fileEntryIndex].Path );

									if( validFileEntries[fileEntryIndex].IsDirectory )
										submittedFolderPaths.Add( validFileEntries[fileEntryIndex].Path );
									else if( m_acceptNonExistingFilename )
									{
										submittedFileEntriesToOverwrite.Add( validFileEntries[fileEntryIndex] );

										if( !submittedFolderPaths.Contains( m_currentPath ) )
											submittedFolderPaths.Add( m_currentPath );
									}
								}
							}
							else // File/folder doesn't exist
							{
								if( !m_acceptNonExistingFilename )
								{
									filenameImage.color = m_skin.InputFieldInvalidBackgroundColor;
									return;
								}
								else
								{
#if !UNITY_EDITOR && UNITY_ANDROID
									if( FileBrowserHelpers.ShouldUseSAFForPath( m_currentPath ) )
									{
										if( m_pickerMode == PickMode.Folders )
											submittedFileEntryPaths.Add( FileBrowserHelpers.CreateFolderInDirectory( m_currentPath, filename ) );
										else
											submittedFileEntryPaths.Add( FileBrowserHelpers.CreateFileInDirectory( m_currentPath, filename ) );
									}
									else
#endif
									{
										submittedFileEntryPaths.Add( Path.Combine( m_currentPath, filename ) );

										if( !submittedFolderPaths.Contains( m_currentPath ) )
											submittedFolderPaths.Add( m_currentPath );
									}
								}
							}
						}
						catch( ArgumentException e )
						{
							filenameImage.color = m_skin.InputFieldInvalidBackgroundColor;
							Debug.LogException( e );
							return;
						}
					}

					if( submittedFileEntryPaths.Count == 0 )
					{
						filenameImage.color = m_skin.InputFieldInvalidBackgroundColor;
						return;
					}

					result = submittedFileEntryPaths.ToArray();
				}
			}

			if( result != null )
			{
				if( m_checkWriteAccessToDestinationDirectory )
				{
					for( int i = 0; i < submittedFolderPaths.Count; i++ )
					{
						if( !string.IsNullOrEmpty( submittedFolderPaths[i] ) && !CheckDirectoryWriteAccess( submittedFolderPaths[i] ) )
						{
							accessRestrictedPanel.Show();
							return;
						}
					}
				}

				if( m_showFileOverwriteDialog && submittedFileEntriesToOverwrite.Count > 0 )
				{
					fileOperationConfirmationPanel.Show( this, submittedFileEntriesToOverwrite, FileBrowserFileOperationConfirmationPanel.OperationType.Overwrite, () => OnOperationSuccessful( result ) );
					return;
				}

				OnOperationSuccessful( result );
			}
		}

		public void OnCancelButtonClicked()
		{
			OnOperationCanceled( true );
		}
		#endregion

		#region Other Events
		private void OnOperationSuccessful( string[] paths )
		{
			Success = true;
			Result = paths;

			Hide();

			if( !string.IsNullOrEmpty( m_currentPath ) )
				LastBrowsedFolder = m_currentPath;

			OnSuccess _onSuccess = onSuccess;
			onSuccess = null;
			onCancel = null;

			if( _onSuccess != null )
				_onSuccess( paths );
		}

		private void OnOperationCanceled( bool invokeCancelCallback )
		{
			Success = false;
			Result = null;

			Hide();

			if( !string.IsNullOrEmpty( m_currentPath ) )
				LastBrowsedFolder = m_currentPath;

			OnCancel _onCancel = onCancel;
			onSuccess = null;
			onCancel = null;

			if( invokeCancelCallback && _onCancel != null )
				_onCancel();
		}

		public void OnPathChanged( string newPath )
		{
			// Fixes harmless NullReferenceException that occurs when Play button is clicked while SimpleFileBrowserCanvas prefab is open in prefab mode
			// https://github.com/yasirkula/UnitySimpleFileBrowser/issues/30
			if( !canvas )
				return;

			CurrentPath = newPath;
		}

		public void OnSearchStringChanged( string newSearchString )
		{
			if( !canvas ) // Same as OnPathChanged
				return;

			PersistFileEntrySelection();
			SearchString = newSearchString;
		}

		public void OnFilterChanged()
		{
			if( !canvas ) // Same as OnPathChanged
				return;

			bool extensionsSingleSuffixModeChanged = false;

			if( filters != null && filtersDropdown.value < filters.Count )
			{
				bool allExtensionsHadSingleSuffix = AllExtensionsHaveSingleSuffix;
				allFiltersHaveSingleSuffix = filters[filtersDropdown.value].allExtensionsHaveSingleSuffix;
				extensionsSingleSuffixModeChanged = ( AllExtensionsHaveSingleSuffix != allExtensionsHadSingleSuffix );
			}

			PersistFileEntrySelection();
			RefreshFiles( extensionsSingleSuffixModeChanged );
		}

		public void OnShowHiddenFilesToggleChanged()
		{
			if( !canvas ) // Same as OnPathChanged
				return;

			PersistFileEntrySelection();
			RefreshFiles( false );
		}

		public void OnItemSelected( FileBrowserItem item, bool isDoubleClick )
		{
			if( item == null )
				return;

			if( item is FileBrowserQuickLink )
			{
#if !UNITY_EDITOR && UNITY_ANDROID
				if( ( (FileBrowserQuickLink) item ).TargetPath == SAF_PICK_FOLDER_QUICK_LINK_PATH )
					FileBrowserHelpers.AJC.CallStatic( "PickSAFFolder", FileBrowserHelpers.Context, new FBDirectoryReceiveCallbackAndroid( OnSAFDirectoryPicked ) );
				else
#endif
				CurrentPath = ( (FileBrowserQuickLink) item ).TargetPath;

				return;
			}

			if( m_multiSelectionToggleSelectionMode )
			{
				// In file selection mode, we shouldn't include folders in the multi-selection
				if( item.IsDirectory && m_pickerMode == PickMode.Files && !selectedFileEntries.Contains( item.Position ) )
					return;

				// If a file/folder is double clicked in multi-selection mode, instead of opening that file/folder, we want to toggle its selected state
				isDoubleClick = false;
			}

			if( !isDoubleClick )
			{
				if( !m_allowMultiSelection )
				{
					selectedFileEntries.Clear();
					selectedFileEntries.Add( item.Position );
				}
				else
				{
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL || UNITY_WSA || UNITY_WSA_10_0
					// When Shift key is held, all items from the pivot item to the clicked item will be selected
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
					if( Keyboard.current != null && Keyboard.current.shiftKey.isPressed )
#else
					if( Input.GetKey( KeyCode.LeftShift ) || Input.GetKey( KeyCode.RightShift ) )
#endif
					{
						multiSelectionPivotFileEntry = Mathf.Clamp( multiSelectionPivotFileEntry, 0, validFileEntries.Count - 1 );

						selectedFileEntries.Clear();

						for( int i = multiSelectionPivotFileEntry; i < item.Position; i++ )
							selectedFileEntries.Add( i );

						for( int i = multiSelectionPivotFileEntry; i > item.Position; i-- )
							selectedFileEntries.Add( i );

						selectedFileEntries.Add( item.Position );
					}
					else
#endif
					{
						multiSelectionPivotFileEntry = item.Position;

						// When in toggle selection mode or Control/Command key is held, individual items can be multi-selected
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL || UNITY_WSA || UNITY_WSA_10_0
						if( m_multiSelectionToggleSelectionMode || IsCtrlKeyHeld() )
#else
						if( m_multiSelectionToggleSelectionMode )
#endif
						{
							if( !selectedFileEntries.Contains( item.Position ) )
								selectedFileEntries.Add( item.Position );
							else
							{
								selectedFileEntries.Remove( item.Position );

								if( selectedFileEntries.Count == 0 )
									MultiSelectionToggleSelectionMode = false;
							}
						}
						else
						{
							selectedFileEntries.Clear();
							selectedFileEntries.Add( item.Position );
						}
					}
				}

				UpdateFilenameInputFieldWithSelection();
			}

			for( int i = 0; i < allItems.Count; i++ )
			{
				if( allItems[i].gameObject.activeSelf )
					allItems[i].SetSelected( selectedFileEntries.Contains( allItems[i].Position ) );
			}

			if( selectedFileEntries.Count > 0 && ( isDoubleClick || ( SingleClickMode && !m_multiSelectionToggleSelectionMode ) ) )
			{
				if( !item.IsDirectory )
				{
					// Submit selection
					OnSubmitButtonClicked();
				}
				else
				{
					// Enter the directory
#if !UNITY_EDITOR && UNITY_ANDROID
					if( FileBrowserHelpers.ShouldUseSAFForPath( m_currentPath ) )
					{
						for( int i = 0; i < validFileEntries.Count; i++ )
						{
							FileSystemEntry fileInfo = validFileEntries[i];
							if( fileInfo.IsDirectory && fileInfo.Name == item.Name )
							{
								CurrentPath = fileInfo.Path;
								return;
							}
						}
					}
					else
#endif
					CurrentPath = Path.Combine( m_currentPath, item.Name );
				}
			}
		}

		public void OnItemHeld( FileBrowserItem item )
		{
			if( item is FileBrowserQuickLink )
				OnItemSelected( item, false );
			else if( m_allowMultiSelection && ( !item.IsDirectory || m_pickerMode != PickMode.Files ) ) // Holding a folder in file selection mode should do nothing
			{
				if( !MultiSelectionToggleSelectionMode )
				{
					if( m_pickerMode == PickMode.Files )
					{
						// If some folders are selected in file selection mode, deselect these folders before enabling the selection toggles because otherwise,
						// user won't be able to deselect the selected folders without exiting MultiSelectionToggleSelectionMode
						for( int i = selectedFileEntries.Count - 1; i >= 0; i-- )
						{
							if( validFileEntries[selectedFileEntries[i]].IsDirectory )
								selectedFileEntries.RemoveAt( i );
						}
					}

					MultiSelectionToggleSelectionMode = true;
				}

				if( !selectedFileEntries.Contains( item.Position ) )
					OnItemSelected( item, false );
			}
		}

#if !UNITY_EDITOR && UNITY_ANDROID
		private void OnSAFDirectoryPicked( string rawUri, string name )
		{
			if( !string.IsNullOrEmpty( rawUri ) )
			{
				if( AddQuickLink( m_skin.FolderIcon, name, rawUri ) )
					CurrentPath = rawUri;
			}
		}

		private void FetchPersistedSAFQuickLinks()
		{
			string resultRaw = FileBrowserHelpers.AJC.CallStatic<string>( "FetchSAFQuickLinks", FileBrowserHelpers.Context );
			if( resultRaw == "0" )
				return;

			int separatorIndex = resultRaw.LastIndexOf( "<>" );
			if( separatorIndex <= 0 )
			{
				Debug.LogError( "Entry count does not exist" );
				return;
			}

			int entryCount = 0;
			for( int i = separatorIndex + 2; i < resultRaw.Length; i++ )
			{
				char ch = resultRaw[i];
				if( ch < '0' && ch > '9' )
				{
					Debug.LogError( "Couldn't parse entry count" );
					return;
				}

				entryCount = entryCount * 10 + ( ch - '0' );
			}

			if( entryCount <= 0 )
				return;

			bool defaultPathInitialized = false;

			separatorIndex = 0;
			for( int i = 0; i < entryCount; i++ )
			{
				int nextSeparatorIndex = resultRaw.IndexOf( "<>", separatorIndex );
				if( nextSeparatorIndex <= 0 )
				{
					Debug.LogError( "Entry name is empty" );
					return;
				}

				string entryName = resultRaw.Substring( separatorIndex, nextSeparatorIndex - separatorIndex );

				separatorIndex = nextSeparatorIndex + 2;
				nextSeparatorIndex = resultRaw.IndexOf( "<>", separatorIndex );
				if( nextSeparatorIndex <= 0 )
				{
					Debug.LogError( "Entry rawUri is empty" );
					return;
				}

				string rawUri = resultRaw.Substring( separatorIndex, nextSeparatorIndex - separatorIndex );

				separatorIndex = nextSeparatorIndex + 2;

				if( AddQuickLink( m_skin.FolderIcon, entryName, rawUri ) && !defaultPathInitialized )
				{
					defaultInitialPath = rawUri;
					defaultPathInitialized = true;
				}
			}
		}
#endif

		private char OnValidateFilenameInput( string text, int charIndex, char addedChar )
		{
			if( addedChar == '\n' )
			{
				OnSubmitButtonClicked();
				return '\0';
			}

			return addedChar;
		}

		private void OnFilenameInputChanged( string text )
		{
			filenameInputFieldOverlayText.text = text;
			filenameImage.color = m_skin.InputFieldNormalBackgroundColor;
		}
		#endregion

		#region Helper Functions
		public void Show( string initialPath, string initialFilename )
		{
			if( AskPermissions )
				RequestPermission();

			if( !quickLinksInitialized )
				InitializeQuickLinks();

			selectedFileEntries.Clear();
			m_multiSelectionToggleSelectionMode = false;

			m_searchString = string.Empty;
			searchInputField.text = m_searchString;

			filesScrollRect.verticalNormalizedPosition = 1;

			IsOpen = true;
			Success = false;
			Result = null;

			gameObject.SetActive( true );

			CurrentPath = GetInitialPath( initialPath );

			filenameInputField.text = initialFilename ?? string.Empty;
			filenameInputField.interactable = true;
			filenameImage.color = m_skin.InputFieldNormalBackgroundColor;
		}

		public void Hide()
		{
			IsOpen = false;

			currentPathIndex = -1;
			pathsFollowed.Clear();

			backButton.interactable = false;
			forwardButton.interactable = false;
			upButton.interactable = false;

			gameObject.SetActive( false );
		}

		public void RefreshFiles( bool pathChanged )
		{
			bool allExtensionsHaveSingleSuffix = AllExtensionsHaveSingleSuffix;

			if( pathChanged )
			{
				if( !string.IsNullOrEmpty( m_currentPath ) )
					allFileEntries = FileBrowserHelpers.GetEntriesInDirectory( m_currentPath, allExtensionsHaveSingleSuffix );
				else
					allFileEntries = null;
			}

			selectedFileEntries.Clear();

			if( !showHiddenFilesToggle.isOn )
				ignoredFileAttributes |= FileAttributes.Hidden;
			else
				ignoredFileAttributes &= ~FileAttributes.Hidden;

			validFileEntries.Clear();

			if( allFileEntries != null )
			{
				if( sortFilesByName )
				{
					// Sort the files and folders in the following order:
					// 1. Directories come before files
					// 2. Directories and files are sorted by their names
					Array.Sort( allFileEntries, ( entry1, entry2 ) =>
					{
						if( entry1.IsDirectory != entry2.IsDirectory )
							return entry1.IsDirectory ? -1 : 1;
						else
							return entry1.Name.CompareTo( entry2.Name );
					} );
				}

				for( int i = 0; i < allFileEntries.Length; i++ )
				{
					try
					{
						FileSystemEntry item = allFileEntries[i];
						if( FileSystemEntryMatchesFilters( item, allExtensionsHaveSingleSuffix ) )
							validFileEntries.Add( item );
					}
					catch( Exception e )
					{
						Debug.LogException( e );
					}
				}
			}

			// Restore the selection
			if( pendingFileEntrySelection.Count > 0 )
			{
				for( int i = 0; i < pendingFileEntrySelection.Count; i++ )
				{
					string pendingFileEntry = pendingFileEntrySelection[i];
					for( int j = 0; j < validFileEntries.Count; j++ )
					{
						if( validFileEntries[j].Name == pendingFileEntry )
						{
							selectedFileEntries.Add( j );
							break;
						}
					}
				}

				pendingFileEntrySelection.Clear();
			}

			if( !filenameInputField.interactable && selectedFileEntries.Count <= 1 )
			{
				filenameInputField.interactable = true;

				if( selectedFileEntries.Count == 0 )
					filenameInputField.text = string.Empty;
			}

			listView.UpdateList();

			// Prevent the case where all the content stays offscreen after changing the search string
			EnsureScrollViewIsWithinBounds();
		}

		// Returns whether or not the FileSystemEntry passes the file browser's filters and should be displayed in the files list
		private bool FileSystemEntryMatchesFilters( FileSystemEntry item, bool allExtensionsHaveSingleSuffix )
		{
			if( !item.IsDirectory )
			{
				if( m_pickerMode == PickMode.Folders )
					return false;

				if( ( item.Attributes & ignoredFileAttributes ) != 0 )
					return false;

				string extension = item.Extension;
				if( excludedExtensionsSet.Contains( extension ) )
					return false;
				else if( !allExtensionsHaveSingleSuffix )
				{
					for( int j = 0; j < excludedExtensions.Length; j++ )
					{
						if( extension.EndsWith( excludedExtensions[j], StringComparison.Ordinal ) )
						{
							excludedExtensionsSet.Add( extension );
							continue;
						}
					}
				}

				if( !filters[filtersDropdown.value].MatchesExtension( extension, !allExtensionsHaveSingleSuffix ) )
					return false;
			}
			else
			{
				if( ( item.Attributes & ignoredFileAttributes ) != 0 )
					return false;
			}

			if( m_searchString.Length > 0 && textComparer.IndexOf( item.Name, m_searchString, textCompareOptions ) < 0 )
				return false;

			if( m_displayedEntriesFilter != null && !m_displayedEntriesFilter( item ) )
				return false;

			return true;
		}

		// Quickly selects all files and folders in the current directory
		public void SelectAllFiles()
		{
			if( !m_allowMultiSelection || validFileEntries.Count == 0 )
				return;

			multiSelectionPivotFileEntry = 0;

			selectedFileEntries.Clear();

			if( m_pickerMode != PickMode.Files )
			{
				for( int i = 0; i < validFileEntries.Count; i++ )
					selectedFileEntries.Add( i );
			}
			else
			{
				// Don't select folders in file picking mode if MultiSelectionToggleSelectionMode is enabled or about to be enabled
				for( int i = 0; i < validFileEntries.Count; i++ )
				{
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL || UNITY_WSA || UNITY_WSA_10_0
					if( !m_multiSelectionToggleSelectionMode || !validFileEntries[i].IsDirectory )
#else
					if( !validFileEntries[i].IsDirectory )
#endif
						selectedFileEntries.Add( i );
				}
			}

#if !UNITY_EDITOR && !UNITY_STANDALONE && !UNITY_WSA && !UNITY_WSA_10_0
			MultiSelectionToggleSelectionMode = true;
#endif

			UpdateFilenameInputFieldWithSelection();
			listView.UpdateList();
		}

		// Quickly deselects all files and folders in the current directory
		public void DeselectAllFiles()
		{
			if( selectedFileEntries.Count == 0 )
				return;

			selectedFileEntries.Clear();
			MultiSelectionToggleSelectionMode = false;

			filenameInputField.text = string.Empty;
			filenameInputField.interactable = true;

			listView.UpdateList();
		}

		// Prompts user to create a new folder in the current directory
		public void CreateNewFolder()
		{
			StartCoroutine( CreateNewFolderCoroutine() );
		}

		private IEnumerator CreateNewFolderCoroutine()
		{
			filesScrollRect.verticalNormalizedPosition = 1f;
			filesScrollRect.velocity = Vector2.zero;

			if( selectedFileEntries.Count > 0 )
			{
				selectedFileEntries.Clear();
				MultiSelectionToggleSelectionMode = false;

				filenameInputField.text = string.Empty;
				filenameInputField.interactable = true;

				listView.UpdateList();
			}

			filesScrollRect.movementType = ScrollRect.MovementType.Unrestricted;

			// The easiest way to insert a new item to the top of the list view is to just shift
			// the list view downwards. However, it doesn't always work if we don't shift it twice
			yield return null;
			filesContainer.anchoredPosition = new Vector2( 0f, -m_skin.FileHeight );
			yield return null;
			filesContainer.anchoredPosition = new Vector2( 0f, -m_skin.FileHeight );

			( (RectTransform) renameItem.transform ).anchoredPosition = new Vector2( 1f, m_skin.FileHeight );
			renameItem.Show( string.Empty, m_skin.FileSelectedBackgroundColor, m_skin.FolderIcon, ( folderName ) =>
			{
				filesScrollRect.movementType = ScrollRect.MovementType.Clamped;
				filesContainer.anchoredPosition = Vector2.zero;

				if( string.IsNullOrEmpty( folderName ) )
					return;

				FileBrowserHelpers.CreateFolderInDirectory( CurrentPath, folderName );

				pendingFileEntrySelection.Clear();
				pendingFileEntrySelection.Add( folderName );

				RefreshFiles( true );

				if( m_pickerMode != PickMode.Files )
					filenameInputField.text = folderName;

				// Focus on the newly created folder
				int fileEntryIndex = Mathf.Max( 0, FilenameToFileEntryIndex( folderName ) );
				filesScrollRect.verticalNormalizedPosition = validFileEntries.Count > 1 ? ( 1f - (float) fileEntryIndex / ( validFileEntries.Count - 1 ) ) : 1f;
			} );
		}

		// Prompts user to rename the selected file/folder
		public void RenameSelectedFile()
		{
			if( selectedFileEntries.Count != 1 )
				return;

			MultiSelectionToggleSelectionMode = false;

			int fileEntryIndex = selectedFileEntries[0];
			FileSystemEntry fileInfo = validFileEntries[fileEntryIndex];

			// Check if selected file is currently visible in ScrollRect
			// We consider it visible if both the previous file entry and the next file entry are visible
			bool prevFileEntryVisible = false, nextFileEntryVisible = false;
			for( int i = 0; i < allItems.Count; i++ )
			{
				if( !allItems[i].gameObject.activeSelf )
					continue;

				if( allItems[i].Position == fileEntryIndex - 1 )
				{
					prevFileEntryVisible = true;

					if( prevFileEntryVisible && nextFileEntryVisible )
						break;
				}
				else if( allItems[i].Position == fileEntryIndex + 1 )
				{
					nextFileEntryVisible = true;

					if( prevFileEntryVisible && nextFileEntryVisible )
						break;
				}
			}

			if( !prevFileEntryVisible || !nextFileEntryVisible )
				filesScrollRect.verticalNormalizedPosition = validFileEntries.Count > 1 ? ( 1f - (float) fileEntryIndex / ( validFileEntries.Count - 1 ) ) : 1f;

			filesScrollRect.velocity = Vector2.zero;

			( (RectTransform) renameItem.transform ).anchoredPosition = new Vector2( 1f, -fileEntryIndex * m_skin.FileHeight );
			renameItem.Show( fileInfo.Name, m_skin.FileSelectedBackgroundColor, GetIconForFileEntry( fileInfo ), ( newName ) =>
			{
				if( string.IsNullOrEmpty( newName ) || newName == fileInfo.Name )
					return;

				if( fileInfo.IsDirectory )
					FileBrowserHelpers.RenameDirectory( fileInfo.Path, newName );
				else
					FileBrowserHelpers.RenameFile( fileInfo.Path, newName );

				pendingFileEntrySelection.Clear();
				pendingFileEntrySelection.Add( newName );

				RefreshFiles( true );

				if( ( fileInfo.IsDirectory && m_pickerMode != PickMode.Files ) || ( !fileInfo.IsDirectory && m_pickerMode != PickMode.Folders ) )
					filenameInputField.text = newName;
			} );
		}

		// Prompts user to delete the selected files & folders
		public void DeleteSelectedFiles()
		{
			if( selectedFileEntries.Count == 0 )
				return;

			selectedFileEntries.Sort();

			fileOperationConfirmationPanel.Show( this, validFileEntries, selectedFileEntries, FileBrowserFileOperationConfirmationPanel.OperationType.Delete, () =>
			{
				for( int i = selectedFileEntries.Count - 1; i >= 0; i-- )
				{
					FileSystemEntry fileInfo = validFileEntries[selectedFileEntries[i]];
					if( fileInfo.IsDirectory )
						FileBrowserHelpers.DeleteDirectory( fileInfo.Path );
					else
						FileBrowserHelpers.DeleteFile( fileInfo.Path );
				}

				selectedFileEntries.Clear();

				MultiSelectionToggleSelectionMode = false;
				RefreshFiles( true );
			} );
		}

		// Makes sure that the selection persists after Refreshing the file entries
		private void PersistFileEntrySelection()
		{
			pendingFileEntrySelection.Clear();
			for( int i = 0; i < selectedFileEntries.Count; i++ )
				pendingFileEntrySelection.Add( validFileEntries[selectedFileEntries[i]].Name );
		}

		private bool AddQuickLink( Sprite icon, string name, string path )
		{
			if( string.IsNullOrEmpty( path ) )
				return false;

#if !UNITY_EDITOR && UNITY_ANDROID
			if( !FileBrowserHelpers.ShouldUseSAFForPath( path ) )
#endif
			{
#if !WIN_DIR_CHECK_WITHOUT_TIMEOUT && ( UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN )
				if( !CheckDirectoryExistsWithTimeout( path ) )
#else
				if( !Directory.Exists( path ) )
#endif
					return false;

				path = GetPathWithoutTrailingDirectorySeparator( path.Trim() );
			}

			// Don't add quick link if it already exists
			for( int i = 0; i < allQuickLinks.Count; i++ )
			{
				if( allQuickLinks[i].TargetPath == path )
					return false;
			}

			FileBrowserQuickLink quickLink = (FileBrowserQuickLink) Instantiate( quickLinkPrefab, quickLinksContainer, false );
			quickLink.SetFileBrowser( this, m_skin );

			if( icon != null )
				quickLink.SetQuickLink( icon, name, path );
			else
				quickLink.SetQuickLink( m_skin.FolderIcon, name, path );

			Vector2 anchoredPos = new Vector2( 0f, -quickLinksContainer.sizeDelta.y );

			quickLink.TransformComponent.anchoredPosition = anchoredPos;
			anchoredPos.y -= m_skin.FileHeight;

			quickLinksContainer.sizeDelta = new Vector2( 0f, -anchoredPos.y );

			allQuickLinks.Add( quickLink );

			return true;
		}

		private void ClearQuickLinksInternal()
		{
			Vector2 anchoredPos = Vector2.zero;
			for( int i = 0; i < allQuickLinks.Count; i++ )
			{
				if( allQuickLinks[i].TargetPath == SAF_PICK_FOLDER_QUICK_LINK_PATH )
				{
					allQuickLinks[i].TransformComponent.anchoredPosition = anchoredPos;
					anchoredPos.y -= m_skin.FileHeight;
				}
				else
				{
					Destroy( allQuickLinks[i].gameObject );
					allQuickLinks.RemoveAt( i-- );
				}
			}

			quickLinksContainer.sizeDelta = new Vector2( 0f, -anchoredPos.y );

			quickLinksInitialized = true;
			generateQuickLinksForDrives = false;
		}

		// Makes sure that scroll view's contents are within scroll view's bounds
		private void EnsureScrollViewIsWithinBounds()
		{
			// When scrollbar is snapped to the very bottom of the scroll view, sometimes OnScroll alone doesn't work
			if( filesScrollRect.verticalNormalizedPosition <= Mathf.Epsilon )
				filesScrollRect.verticalNormalizedPosition = 0.0001f;

			filesScrollRect.OnScroll( nullPointerEventData );
		}

		internal void EnsureWindowIsWithinBounds()
		{
			Vector2 canvasSize = rectTransform.sizeDelta;
			Vector2 windowSize = windowTR.sizeDelta;

			if( windowSize.x < minWidth )
				windowSize.x = minWidth;
			if( windowSize.y < minHeight )
				windowSize.y = minHeight;

			if( windowSize.x > canvasSize.x )
				windowSize.x = canvasSize.x;
			if( windowSize.y > canvasSize.y )
				windowSize.y = canvasSize.y;

			Vector2 windowPos = windowTR.anchoredPosition;
			Vector2 canvasHalfSize = canvasSize * 0.5f;
			Vector2 windowHalfSize = windowSize * 0.5f;
			Vector2 windowBottomLeft = windowPos - windowHalfSize + canvasHalfSize;
			Vector2 windowTopRight = windowPos + windowHalfSize + canvasHalfSize;

			if( windowBottomLeft.x < 0f )
				windowPos.x -= windowBottomLeft.x;
			else if( windowTopRight.x > canvasSize.x )
				windowPos.x -= windowTopRight.x - canvasSize.x;

			if( windowBottomLeft.y < 0f )
				windowPos.y -= windowBottomLeft.y;
			else if( windowTopRight.y > canvasSize.y )
				windowPos.y -= windowTopRight.y - canvasSize.y;

			windowTR.anchoredPosition = windowPos;
			windowTR.sizeDelta = windowSize;
		}

		// Handles responsive user interface
		internal void OnWindowDimensionsChanged( Vector2 size )
		{
			float windowWidth = size.x;
			float quickLinksWidth = Mathf.Min( middleViewQuickLinksOriginalSize.x, windowWidth * quickLinksMaxWidthPercentage );

			if( middleViewQuickLinks.sizeDelta.x != quickLinksWidth )
			{
				middleViewQuickLinks.sizeDelta = new Vector2( quickLinksWidth, middleViewQuickLinksOriginalSize.y );
				middleViewFiles.anchoredPosition = new Vector2( quickLinksWidth, 0f );
				middleViewFiles.sizeDelta = new Vector2( -quickLinksWidth, middleViewQuickLinksOriginalSize.y );
				middleViewSeparator.anchoredPosition = new Vector2( quickLinksWidth, 0f );
			}

#if !UNITY_EDITOR && UNITY_ANDROID
			// Responsive layout doesn't affect any other visible UI elements on Storage Access Framework
			if( FileBrowserHelpers.ShouldUseSAF )
				return;
#endif

			if( windowWidth >= narrowScreenWidth )
			{
				if( pathInputField.transform.parent == pathInputFieldSlotBottom )
				{
					pathInputField.transform.SetParent( pathInputFieldSlotTop, false );

					middleView.anchoredPosition = middleViewOriginalPosition;
					middleView.sizeDelta = middleViewOriginalSize;

					showHiddenFilesToggle.gameObject.SetActive( m_displayHiddenFilesToggle );

					listView.OnViewportDimensionsChanged();
					EnsureScrollViewIsWithinBounds();
				}
			}
			else
			{
				if( pathInputField.transform.parent == pathInputFieldSlotTop )
				{
					pathInputField.transform.SetParent( pathInputFieldSlotBottom, false );

					float topViewAdditionalHeight = topViewNarrowScreen.sizeDelta.y;
					middleView.anchoredPosition = middleViewOriginalPosition - new Vector2( 0f, topViewAdditionalHeight * 0.5f );
					middleView.sizeDelta = middleViewOriginalSize - new Vector2( 0f, topViewAdditionalHeight );

					// Responsive layout for narrow screens doesn't include "Show Hidden Files" toggle.
					// We simply hide it because I think creating a new row for it would be an overkill
					showHiddenFilesToggle.gameObject.SetActive( false );

					listView.OnViewportDimensionsChanged();
					EnsureScrollViewIsWithinBounds();
				}
			}
		}

		internal Sprite GetIconForFileEntry( FileSystemEntry fileInfo )
		{
			return m_skin.GetIconForFileEntry( fileInfo, !AllExtensionsHaveSingleSuffix );
		}

		internal static string GetExtensionFromFilename( string filename, bool extractOnlyLastSuffix )
		{
			int length = filename.Length;

			if( extractOnlyLastSuffix )
			{
				// We are only interested in the last suffix of the extension
				for( int i = length - 2; i >= 0; i-- )
				{
					if( filename[i] == '.' )
						return filename.Substring( i, length - i ).ToLowerInvariant();
				}
			}
			else
			{
				// We are interested in all suffixes of the extension
				for( int i = 0, upperLimit = length - 2; i <= upperLimit; i++ )
				{
					if( filename[i] == '.' )
						return filename.Substring( i, length - i ).ToLowerInvariant();
				}
			}

			return string.Empty;
		}

		private string GetPathWithoutTrailingDirectorySeparator( string path )
		{
			if( string.IsNullOrEmpty( path ) )
				return null;

			// Credit: http://stackoverflow.com/questions/6019227/remove-the-last-character-if-its-directoryseparatorchar-with-c-sharp
			try
			{
				if( Path.GetDirectoryName( path ) != null )
				{
					char lastChar = path[path.Length - 1];
					if( lastChar == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar )
						path = path.Substring( 0, path.Length - 1 );
				}
			}
			catch
			{
			}

			return path;
		}

		private void UpdateFilenameInputFieldWithSelection()
		{
			// Refresh filenameInputField as follows:
			// 0 files selected: *blank*
			// 1 file selected: file.Name
			// 2+ files selected: "file1.Name" "file2.Name" ... (up to FILENAME_INPUT_FIELD_MAX_FILE_COUNT filenames are displayed for performance reasons)
			int filenameContributingFileCount = 0;
			if( m_pickerMode != PickMode.Files )
				filenameContributingFileCount = selectedFileEntries.Count;
			else
			{
				for( int i = 0; i < selectedFileEntries.Count; i++ )
				{
					if( !validFileEntries[selectedFileEntries[i]].IsDirectory )
					{
						filenameContributingFileCount++;

						if( filenameContributingFileCount >= FILENAME_INPUT_FIELD_MAX_FILE_COUNT )
							break;
					}
				}
			}

			filenameInputField.interactable = selectedFileEntries.Count <= 1;

			if( filenameContributingFileCount == 0 )
			{
				// If multiple files were previously selected, clear the input field. If a single file was selected, preserve the filename
				if( filenameInputField.text.StartsWith( "\"" ) )
					filenameInputField.text = string.Empty;
			}
			else
			{
				if( filenameContributingFileCount > 1 )
				{
					if( multiSelectionFilenameBuilder == null )
						multiSelectionFilenameBuilder = new StringBuilder( 75 );
					else
						multiSelectionFilenameBuilder.Length = 0;
				}

				for( int i = 0, fileCount = 0; i < selectedFileEntries.Count; i++ )
				{
					FileSystemEntry selectedFile = validFileEntries[selectedFileEntries[i]];
					if( m_pickerMode != PickMode.Files || !selectedFile.IsDirectory )
					{
						if( filenameContributingFileCount == 1 )
						{
							filenameInputField.text = selectedFile.Name;
							break;
						}
						else
						{
							multiSelectionFilenameBuilder.Append( "\"" ).Append( selectedFile.Name ).Append( "\" " );

							if( ++fileCount >= FILENAME_INPUT_FIELD_MAX_FILE_COUNT )
							{
								multiSelectionFilenameBuilder.Append( "..." );
								break;
							}
						}
					}
				}

				if( filenameContributingFileCount > 1 )
					filenameInputField.text = multiSelectionFilenameBuilder.ToString();
			}
		}

		// Extracts filenames from input field. Input can be in 2 formats:
		// 1 filename: file.Name
		// 2+ filenames: "file1.Name" "file2.Name" ...
		// Returns the length of the iterated filename
		private int ExtractFilenameFromInput( string input, ref int startIndex, out int nextStartIndex )
		{
			if( !m_allowMultiSelection || input[startIndex] != '"' )
			{
				// Single file is selected, return it
				nextStartIndex = input.Length;
				return input.Length - startIndex;
			}

			// Seems like multiple files are selected

			// Filename is " (a single quotation mark), very unlikely to happen but probably possible on some platforms
			if( startIndex + 1 >= input.Length )
			{
				nextStartIndex = input.Length;
				return 1;
			}

			int filenameEndIndex = input.IndexOf( '"', startIndex + 1 );
			while( true )
			{
				// 1st iteration: filename is "abc
				// 2nd iteration: filename is "abc"def
				if( filenameEndIndex == -1 )
				{
					nextStartIndex = input.Length;
					return input.Length - startIndex;
				}

				// 1st iteration: filename is abc (extracted from "abc")
				// 2nd iteration: filename is abc"def (extracted from "abc"def")
				if( filenameEndIndex == input.Length - 1 || input[filenameEndIndex + 1] == ' ' )
				{
					startIndex++;

					nextStartIndex = filenameEndIndex + 1;
					while( nextStartIndex < input.Length && input[nextStartIndex] == ' ' )
						nextStartIndex++;

					return filenameEndIndex - startIndex;
				}

				// Filename contains a " char
				filenameEndIndex = input.IndexOf( '"', filenameEndIndex + 1 );
			}
		}

		// Checks if a substring of the input field points to an existing file
		private int FilenameToFileEntryIndex( string filename )
		{
			// Case-sensitive search result takes precedence, so case-insensitive search result is returned only if a case-sensitive match isn't found
			int caseInsensitiveResult = -1;
			for( int i = 0; i < validFileEntries.Count; i++ )
			{
				if( validFileEntries[i].Name.Length == filename.Length )
				{
					if( filename == validFileEntries[i].Name ) // Case-sensitive filename query
						return i;
					else if( textComparer.Compare( filename, validFileEntries[i].Name, textCompareOptions ) == 0 ) // Case-insensitive filename query
						caseInsensitiveResult = i;
				}
			}

			return caseInsensitiveResult;
		}

		// Verifies that filename doesn't contain any invalid characters
		private bool VerifyFilename( string filename )
		{
			bool isWhitespace = true;
			for( int i = 0; i < filename.Length; i++ )
			{
				char ch = filename[i];
				if( invalidFilenameChars.Contains( ch ) )
					return false;

				if( isWhitespace && !char.IsWhiteSpace( ch ) )
					isWhitespace = false;
			}

			return !isWhitespace;
		}

		// Credit: http://answers.unity3d.com/questions/898770/how-to-get-the-width-of-ui-text-with-horizontal-ov.html
		private int CalculateLengthOfDropdownText( string str )
		{
			Font font = filterItemTemplate.font;
			font.RequestCharactersInTexture( str, filterItemTemplate.fontSize, filterItemTemplate.fontStyle );

			int totalLength = 0;
			for( int i = 0; i < str.Length; i++ )
			{
				CharacterInfo characterInfo;
				if( !font.GetCharacterInfo( str[i], out characterInfo, filterItemTemplate.fontSize ) )
					totalLength += 5;

				totalLength += characterInfo.advance;
			}

			return totalLength;
		}

		private string GetInitialPath( string initialPath )
		{
			if( !string.IsNullOrEmpty( initialPath ) && !FileBrowserHelpers.DirectoryExists( initialPath ) && FileBrowserHelpers.FileExists( initialPath ) )
			{
				// Path points to a file, use its parent directory's path instead
				initialPath = FileBrowserHelpers.GetDirectoryName( initialPath );
			}

			if( string.IsNullOrEmpty( initialPath ) || !FileBrowserHelpers.DirectoryExists( initialPath ) )
			{
				if( CurrentPath.Length > 0 )
					initialPath = CurrentPath;
				else
				{
					string lastBrowsedFolder = LastBrowsedFolder;
					if( !string.IsNullOrEmpty( lastBrowsedFolder ) && FileBrowserHelpers.DirectoryExists( lastBrowsedFolder ) )
						initialPath = lastBrowsedFolder;
					else
						initialPath = defaultInitialPath;
				}
			}

			m_currentPath = string.Empty; // Needed to correctly reset the pathsFollowed

			return initialPath;
		}

#if !WIN_DIR_CHECK_WITHOUT_TIMEOUT && ( UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN )
		private bool CheckDirectoryExistsWithTimeout( string path, int timeout = 750 )
		{
			if( timedOutDirectoryExistsRequests.Contains( path ) )
				return false;

			// Directory.Exists freezes for ~15 seconds for unreachable network drives on Windows, set a timeout using threads
			bool directoryExists = false;
			try
			{
#if NET_STANDARD_2_0 || NET_4_6
				// Credit: https://stackoverflow.com/a/52661569/2373034
				System.Threading.Tasks.Task task = new System.Threading.Tasks.Task( () => directoryExists = Directory.Exists( path ) );
				task.Start();
				if( !task.Wait( timeout ) )
					timedOutDirectoryExistsRequests.Add( path );
#else
				// Credit: https://stackoverflow.com/q/1232953/2373034
				System.Threading.Thread thread = new System.Threading.Thread( new System.Threading.ThreadStart( () => directoryExists = Directory.Exists( path ) ) );
				thread.Start();
				if( !thread.Join( timeout ) )
				{
					timedOutDirectoryExistsRequests.Add( path );
					thread.Abort();
				}
#endif
			}
			catch
			{
				directoryExists = Directory.Exists( path );
			}

			return directoryExists;
		}
#endif

		private bool CheckDirectoryWriteAccess( string path )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( FileBrowserHelpers.ShouldUseSAFForPath( path ) )
				return true;
#endif
			string tempFilePath = Path.Combine( path, "__fsWrite.tmp" );
			try
			{
				File.Create( tempFilePath ).Close();
				File.Delete( tempFilePath );

				return true;
			}
			catch
			{
				return false;
			}
			finally
			{
				try
				{
					File.Delete( tempFilePath );
				}
				catch { }
			}
		}

		// Check if Control/Command key is held
		private bool IsCtrlKeyHeld()
		{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
#if UNITY_EDITOR_OSX || ( !UNITY_EDITOR && UNITY_STANDALONE_OSX )
			return Keyboard.current != null && ( Keyboard.current.leftCommandKey.isPressed || Keyboard.current.rightCommandKey.isPressed );
#else
			return Keyboard.current != null && Keyboard.current.ctrlKey.isPressed;
#endif
#else
#if UNITY_EDITOR_OSX || ( !UNITY_EDITOR && UNITY_STANDALONE_OSX )
			return Input.GetKey( KeyCode.LeftCommand ) || Input.GetKey( KeyCode.RightCommand );
#else
			return Input.GetKey( KeyCode.LeftControl ) || Input.GetKey( KeyCode.RightControl );
#endif
#endif
		}
		#endregion

		#region File Browser Functions (static)
		public static bool ShowSaveDialog( OnSuccess onSuccess, OnCancel onCancel,
										   PickMode pickMode, bool allowMultiSelection = false,
										   string initialPath = null, string initialFilename = null,
										   string title = "Save", string saveButtonText = "Save" )
		{
			return ShowDialogInternal( onSuccess, onCancel, pickMode, allowMultiSelection, pickMode != PickMode.Folders, initialPath, initialFilename, title, saveButtonText );
		}

		public static bool ShowLoadDialog( OnSuccess onSuccess, OnCancel onCancel,
										   PickMode pickMode, bool allowMultiSelection = false,
										   string initialPath = null, string initialFilename = null,
										   string title = "Load", string loadButtonText = "Select" )
		{
			return ShowDialogInternal( onSuccess, onCancel, pickMode, allowMultiSelection, false, initialPath, initialFilename, title, loadButtonText );
		}

		private static bool ShowDialogInternal( OnSuccess onSuccess, OnCancel onCancel,
												PickMode pickMode, bool allowMultiSelection, bool acceptNonExistingFilename,
												string initialPath, string initialFilename, string title, string submitButtonText )
		{
			// Instead of ignoring this dialog request, let's just override the currently visible dialog's properties
			//if( Instance.gameObject.activeSelf )
			//{
			//	Debug.LogError( "Error: Multiple dialogs are not allowed!" );
			//	return false;
			//}

			Instance.onSuccess = onSuccess;
			Instance.onCancel = onCancel;

			Instance.PickerMode = pickMode;
			Instance.AllowMultiSelection = allowMultiSelection;
			Instance.Title = title;
			Instance.SubmitButtonText = submitButtonText;
			Instance.AcceptNonExistingFilename = acceptNonExistingFilename;

			Instance.Show( initialPath, initialFilename );

			return true;
		}

		public static void HideDialog( bool invokeCancelCallback = false )
		{
			Instance.OnOperationCanceled( invokeCancelCallback );
		}

		public static IEnumerator WaitForSaveDialog( PickMode pickMode, bool allowMultiSelection = false,
													 string initialPath = null, string initialFilename = null,
													 string title = "Save", string saveButtonText = "Save" )
		{
			if( !ShowSaveDialog( null, null, pickMode, allowMultiSelection, initialPath, initialFilename, title, saveButtonText ) )
				yield break;

			while( Instance.gameObject.activeSelf )
				yield return null;
		}

		public static IEnumerator WaitForLoadDialog( PickMode pickMode, bool allowMultiSelection = false,
													 string initialPath = null, string initialFilename = null,
													 string title = "Load", string loadButtonText = "Select" )
		{
			if( !ShowLoadDialog( null, null, pickMode, allowMultiSelection, initialPath, initialFilename, title, loadButtonText ) )
				yield break;

			while( Instance.gameObject.activeSelf )
				yield return null;
		}

		public static bool AddQuickLink( string name, string path, Sprite icon = null )
		{
			if( string.IsNullOrEmpty( path ) || !FileBrowserHelpers.DirectoryExists( path ) )
				return false;

			if( !quickLinksInitialized )
			{
				// Fetching the list of external drives is only possible with the READ_EXTERNAL_STORAGE permission granted on Android
				if( AskPermissions )
					RequestPermission();

				Instance.InitializeQuickLinks();
			}

			return Instance.AddQuickLink( icon, name, path );
		}

		public static void ClearQuickLinks()
		{
			Instance.ClearQuickLinksInternal();
		}

		public static void SetExcludedExtensions( params string[] excludedExtensions )
		{
			Instance.excludedExtensions = excludedExtensions ?? new string[0];
			Instance.excludedExtensionsSet.Clear();
			Instance.allExcludedExtensionsHaveSingleSuffix = true;

			if( excludedExtensions != null )
			{
				for( int i = 0; i < excludedExtensions.Length; i++ )
				{
					excludedExtensions[i] = excludedExtensions[i].ToLowerInvariant();
					if( excludedExtensions[i][0] != '.' )
						excludedExtensions[i] = "." + excludedExtensions[i];

					Instance.excludedExtensionsSet.Add( excludedExtensions[i] );
					Instance.allExcludedExtensionsHaveSingleSuffix &= ( excludedExtensions[i].LastIndexOf( '.' ) == 0 );
				}
			}
		}

		public static void SetFilters( bool showAllFilesFilter )
		{
			SetFilters( showAllFilesFilter, (string[]) null );
		}

		public static void SetFilters( bool showAllFilesFilter, IEnumerable<string> filters )
		{
			SetFiltersPreProcessing( showAllFilesFilter );

			if( filters != null )
			{
				foreach( string filter in filters )
				{
					if( !string.IsNullOrEmpty( filter ) )
						Instance.filters.Add( new Filter( null, filter ) );
				}
			}

			SetFiltersPostProcessing();
		}

		public static void SetFilters( bool showAllFilesFilter, params string[] filters )
		{
			SetFiltersPreProcessing( showAllFilesFilter );

			if( filters != null )
			{
				for( int i = 0; i < filters.Length; i++ )
				{
					if( !string.IsNullOrEmpty( filters[i] ) )
						Instance.filters.Add( new Filter( null, filters[i] ) );
				}
			}

			SetFiltersPostProcessing();
		}

		public static void SetFilters( bool showAllFilesFilter, IEnumerable<Filter> filters )
		{
			SetFiltersPreProcessing( showAllFilesFilter );

			if( filters != null )
			{
				foreach( Filter filter in filters )
				{
					if( filter != null && filter.defaultExtension.Length > 0 )
						Instance.filters.Add( filter );
				}
			}

			SetFiltersPostProcessing();
		}

		public static void SetFilters( bool showAllFilesFilter, params Filter[] filters )
		{
			SetFiltersPreProcessing( showAllFilesFilter );

			if( filters != null )
			{
				for( int i = 0; i < filters.Length; i++ )
				{
					if( filters[i] != null && filters[i].defaultExtension.Length > 0 )
						Instance.filters.Add( filters[i] );
				}
			}

			SetFiltersPostProcessing();
		}

		private static void SetFiltersPreProcessing( bool showAllFilesFilter )
		{
			Instance.showAllFilesFilter = showAllFilesFilter;

			Instance.filters.Clear();

			if( showAllFilesFilter )
				Instance.filters.Add( Instance.allFilesFilter );
		}

		private static void SetFiltersPostProcessing()
		{
			List<Filter> filters = Instance.filters;

			if( filters.Count == 0 )
				filters.Add( Instance.allFilesFilter );

			int maxFilterStrLength = 100;
			List<string> dropdownValues = new List<string>( filters.Count );
			for( int i = 0; i < filters.Count; i++ )
			{
				string filterStr = filters[i].ToString();
				dropdownValues.Add( filterStr );

				maxFilterStrLength = Mathf.Max( maxFilterStrLength, Instance.CalculateLengthOfDropdownText( filterStr ) );
			}

			Vector2 size = Instance.filtersDropdownContainer.sizeDelta;
			size.x = maxFilterStrLength + 28;
			Instance.filtersDropdownContainer.sizeDelta = size;

			Instance.filtersDropdown.ClearOptions();
			Instance.filtersDropdown.AddOptions( dropdownValues );
			Instance.filtersDropdown.value = 0;

			Instance.allFiltersHaveSingleSuffix = filters[0].allExtensionsHaveSingleSuffix;
		}

		public static bool SetDefaultFilter( string defaultFilter )
		{
			if( string.IsNullOrEmpty( defaultFilter ) )
			{
				if( Instance.showAllFilesFilter )
				{
					Instance.filtersDropdown.value = 0;
					Instance.filtersDropdown.RefreshShownValue();

					return true;
				}

				return false;
			}

			defaultFilter = defaultFilter.ToLowerInvariant();
			if( defaultFilter[0] != '.' )
				defaultFilter = "." + defaultFilter;

			for( int i = 0; i < Instance.filters.Count; i++ )
			{
				HashSet<string> extensions = Instance.filters[i].extensionsSet;
				if( extensions != null && extensions.Contains( defaultFilter ) )
				{
					Instance.filtersDropdown.value = i;
					Instance.filtersDropdown.RefreshShownValue();

					return true;
				}
			}

			return false;
		}

		public static Permission CheckPermission()
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			Permission result = (Permission) FileBrowserHelpers.AJC.CallStatic<int>( "CheckPermission", FileBrowserHelpers.Context );
			if( result == Permission.Denied && (Permission) PlayerPrefs.GetInt( "FileBrowserPermission", (int) Permission.ShouldAsk ) == Permission.ShouldAsk )
				result = Permission.ShouldAsk;

			return result;
#else
			return Permission.Granted;
#endif
		}

		public static Permission RequestPermission()
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			object threadLock = new object();
			lock( threadLock )
			{
				FBPermissionCallbackAndroid nativeCallback = new FBPermissionCallbackAndroid( threadLock );

				FileBrowserHelpers.AJC.CallStatic( "RequestPermission", FileBrowserHelpers.Context, nativeCallback, PlayerPrefs.GetInt( "FileBrowserPermission", (int) Permission.ShouldAsk ) );

				if( nativeCallback.Result == -1 )
					System.Threading.Monitor.Wait( threadLock );

				if( (Permission) nativeCallback.Result != Permission.ShouldAsk && PlayerPrefs.GetInt( "FileBrowserPermission", -1 ) != nativeCallback.Result )
				{
					PlayerPrefs.SetInt( "FileBrowserPermission", nativeCallback.Result );
					PlayerPrefs.Save();
				}

				return (Permission) nativeCallback.Result;
			}
#else
			return Permission.Granted;
#endif
		}
		#endregion
	}
}