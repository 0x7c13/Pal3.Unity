= Simple File Browser (v1.5.9) =

Online documentation & example code available at: https://github.com/yasirkula/UnitySimpleFileBrowser
E-mail: yasirkula@gmail.com

### ABOUT
This plugin helps you show save/load dialogs during gameplay with its uGUI based file browser.


### HOW TO
The file browser can be shown either as a save dialog or a load dialog. In load mode, the returned path(s) always lead to existing files or folders. In save mode, the returned path(s) can point to non-existing files, as well.

File browser comes bundled with two premade skins in the Skins directory: LightSkin and DarkSkin. New UISkins can be created via "Assets-Create-yasirkula-SimpleFileBrowser-UI Skin". A UISkin can be assigned to the file browser in two ways:

- By changing SimpleFileBrowserCanvas prefab's Skin field
- By changing the value of FileBrowser.Skin property from a C# script


### NEW INPUT SYSTEM SUPPORT
This plugin supports Unity's new Input System but it requires some manual modifications (if both the legacy and the new input systems are active at the same time, no changes are needed):

- the plugin mustn't be installed as a package, i.e. it must reside inside the Assets folder and not the Packages folder (it can reside inside a subfolder of Assets like Assets/Plugins)
- if Unity 2019.2.5 or earlier is used, add ENABLE_INPUT_SYSTEM compiler directive to "Player Settings/Scripting Define Symbols" (these symbols are platform specific, so if you change the active platform later, you'll have to add the compiler directive again)
- add "Unity.InputSystem" assembly to "SimpleFileBrowser.Runtime" Assembly Definition File's "Assembly Definition References" list
- open SimpleFileBrowserCanvas prefab, select EventSystem child object and replace StandaloneInputModule component with InputSystemUIInputModule component (or, if your scene(s) already have EventSystem objects, you can delete SimpleFileBrowserCanvas prefab's EventSystem child object)


### FAQ
- Android build fails, it says "error: attribute android:requestLegacyExternalStorage not found" in Console
"android:requestLegacyExternalStorage" attribute in AndroidManifest.xml grants full access to device's storage on Android 10 but requires you to update your Android SDK to at least SDK 29. If this isn't possible for you, you should open SimpleFileBrowser.aar with WinRAR or 7-Zip and then remove the "<application ... />" tag from AndroidManifest.xml.

- Can't show the file browser on Android, it says "java.lang.ClassNotFoundException: com.yasirkula.unity.FileBrowserPermissionReceiver" in Logcat
If you are sure that your plugin is up-to-date, then enable "Custom Proguard File" option from Player Settings and add the following line to that file: -keep class com.yasirkula.unity.* { *; }

- File browser doesn't show any files on Android 10+
File browser uses Storage Access Framework on these Android versions and users must first click the "Pick Folder" button in the quick links section

- File browser doesn't show any files on Unity 2021.3.x
Please see: https://github.com/yasirkula/UnitySimpleFileBrowser/issues/70

- RequestPermission returns Permission.Denied on Android
Declare the WRITE_EXTERNAL_STORAGE permission manually in your Plugins/Android/AndroidManifest.xml file as follows: <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" tools:node="replace"/>
You'll need to add the following attribute to the '<manifest ...>' element: xmlns:tools="http://schemas.android.com/tools"


### SCRIPTING API
Please see the online documentation for a more in-depth documentation of the Scripting API: https://github.com/yasirkula/UnitySimpleFileBrowser

NOTE: On Android Q (10) or later, it is impossible to work with File APIs. On these devices, SimpleFileBrowser uses Storage Access Framework (SAF) to browse the files. However, paths returned by SAF are not File API compatible. To simulate the behaviour of the File API on all devices (including SAF), you can check out the FileBrowserHelpers functions. For reference, here is an example SAF path: content://com.android.externalstorage.documents/tree/primary%3A/document/primary%3APictures

// Namespace
using SimpleFileBrowser;

public enum Permission { Denied = 0, Granted = 1, ShouldAsk = 2 };
public enum PickMode { Files = 0, Folders = 1, FilesAndFolders = 2 };

public delegate void OnSuccess( string[] paths );
public delegate void OnCancel();

// Changing the dialog's skin
public static UISkin Skin { get; set; }

// Showing dialog
bool ShowSaveDialog( OnSuccess onSuccess, OnCancel onCancel, PickMode pickMode, bool allowMultiSelection = false, string initialPath = null, string initialFilename = null, string title = "Save", string saveButtonText = "Save" );
bool ShowLoadDialog( OnSuccess onSuccess, OnCancel onCancel, PickMode pickMode, bool allowMultiSelection = false, string initialPath = null, string initialFilename = null, string title = "Load", string loadButtonText = "Select" );

IEnumerator WaitForSaveDialog( PickMode pickMode, bool allowMultiSelection = false, string initialPath = null, string initialFilename = null, string title = "Save", string saveButtonText = "Save" );
IEnumerator WaitForLoadDialog( PickMode pickMode, bool allowMultiSelection = false, string initialPath = null, string initialFilename = null, string title = "Load", string loadButtonText = "Select" );

// Force closing an open dialog
void HideDialog( bool invokeCancelCallback = false );

// Customizing the dialog
bool AddQuickLink( string name, string path, Sprite icon = null );
void ClearQuickLinks();

void SetExcludedExtensions( params string[] excludedExtensions );

// Filters should include the period (e.g. ".jpg" instead of "jpg")
void SetFilters( bool showAllFilesFilter, IEnumerable<string> filters );
void SetFilters( bool showAllFilesFilter, params string[] filters );
void SetFilters( bool showAllFilesFilter, IEnumerable<FileBrowser.Filter> filters );
void SetFilters( bool showAllFilesFilter, params FileBrowser.Filter[] filters );

bool SetDefaultFilter( string defaultFilter );

// Filtering displayed files/folders programmatically
delegate bool FileSystemEntryFilter( FileSystemEntry entry );
event FileSystemEntryFilter DisplayedEntriesFilter;

// Android runtime permissions
FileBrowser.Permission CheckPermission();
FileBrowser.Permission RequestPermission();

// File manipulation functions that work on all platforms (including Storage Access Framework (SAF) on Android 10+)
// These functions should be called with the paths returned by the FileBrowser functions only
bool FileBrowserHelpers.FileExists( string path );
bool FileBrowserHelpers.DirectoryExists( string path );
bool FileBrowserHelpers.IsDirectory( string path );
bool FileBrowserHelpers.IsPathDescendantOfAnother( string path, string parentFolderPath );
string FileBrowserHelpers.GetDirectoryName( string path );
FileSystemEntry[] FileBrowserHelpers.GetEntriesInDirectory( string path, bool extractOnlyLastSuffixFromExtensions ); // Returns all files and folders in a directory. If you want "File.tar.gz"s extension to be extracted as ".tar.gz" instead of ".gz", set 'extractOnlyLastSuffixFromExtensions' to false
string FileBrowserHelpers.CreateFileInDirectory( string directoryPath, string filename ); // Returns the created file's path
string FileBrowserHelpers.CreateFolderInDirectory( string directoryPath, string folderName ); // Returns the created folder's path
void FileBrowserHelpers.WriteBytesToFile( string targetPath, byte[] bytes );
void FileBrowserHelpers.WriteTextToFile( string targetPath, string text );
void FileBrowserHelpers.AppendBytesToFile( string targetPath, byte[] bytes );
void FileBrowserHelpers.AppendTextToFile( string targetPath, string text );
byte[] FileBrowserHelpers.ReadBytesFromFile( string sourcePath );
string FileBrowserHelpers.ReadTextFromFile( string sourcePath );
void FileBrowserHelpers.CopyFile( string sourcePath, string destinationPath );
void FileBrowserHelpers.CopyDirectory( string sourcePath, string destinationPath );
void FileBrowserHelpers.MoveFile( string sourcePath, string destinationPath );
void FileBrowserHelpers.MoveDirectory( string sourcePath, string destinationPath );
string FileBrowserHelpers.RenameFile( string path, string newName ); // Returns the new path of the file
string FileBrowserHelpers.RenameDirectory( string path, string newName ); // Returns the new path of the directory
void FileBrowserHelpers.DeleteFile( string path );
void FileBrowserHelpers.DeleteDirectory( string path );
string FileBrowserHelpers.GetFilename( string path );
long FileBrowserHelpers.GetFilesize( string path );
DateTime FileBrowserHelpers.GetLastModifiedDate( string path );