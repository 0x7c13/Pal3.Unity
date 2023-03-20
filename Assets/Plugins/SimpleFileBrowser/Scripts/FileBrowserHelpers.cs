using System.IO;
using UnityEngine;

namespace SimpleFileBrowser
{
	public struct FileSystemEntry
	{
		public readonly string Path;
		public readonly string Name;
		public readonly string Extension;
		public readonly FileAttributes Attributes;

		public bool IsDirectory { get { return ( Attributes & FileAttributes.Directory ) == FileAttributes.Directory; } }

		public FileSystemEntry( string path, string name, string extension, bool isDirectory )
		{
			Path = path;
			Name = name;
			Extension = extension;
			Attributes = isDirectory ? FileAttributes.Directory : FileAttributes.Normal;
		}

		public FileSystemEntry( FileSystemInfo fileInfo, string extension )
		{
			Path = fileInfo.FullName;
			Name = fileInfo.Name;
			Extension = extension;
			Attributes = fileInfo.Attributes;
		}
	}

	public static class FileBrowserHelpers
	{
#if !UNITY_EDITOR && UNITY_ANDROID
		private static AndroidJavaClass m_ajc = null;
		public static AndroidJavaClass AJC
		{
			get
			{
				if( m_ajc == null )
					m_ajc = new AndroidJavaClass( "com.yasirkula.unity.FileBrowser" );

				return m_ajc;
			}
		}

		private static AndroidJavaObject m_context = null;
		public static AndroidJavaObject Context
		{
			get
			{
				if( m_context == null )
				{
					using( AndroidJavaObject unityClass = new AndroidJavaClass( "com.unity3d.player.UnityPlayer" ) )
					{
						m_context = unityClass.GetStatic<AndroidJavaObject>( "currentActivity" );
					}
				}

				return m_context;
			}
		}

		private static string m_temporaryFilePath = null;
		private static string TemporaryFilePath
		{
			get
			{
				if( m_temporaryFilePath == null )
				{
					m_temporaryFilePath = Path.Combine( Application.temporaryCachePath, "tmpFile" );
					Directory.CreateDirectory( Application.temporaryCachePath );
				}

				return m_temporaryFilePath;
			}
		}
		
		// On Android 10+, filesystem can be accessed via Storage Access Framework only
		private static bool? m_shouldUseSAF = null;
		public static bool ShouldUseSAF
		{
			get
			{
				if( m_shouldUseSAF == null )
					m_shouldUseSAF = AJC.CallStatic<bool>( "CheckSAF" );

				return m_shouldUseSAF.Value;
			}
		}

		public static bool ShouldUseSAFForPath( string path ) // true: path should be managed with AJC (native helper class for Storage Access Framework), false: path should be managed with System.IO
		{
			return ShouldUseSAF && ( string.IsNullOrEmpty( path ) || path[0] != '/' );
		}
#endif

		public static bool FileExists( string path )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( path ) )
				return AJC.CallStatic<bool>( "SAFEntryExists", Context, path, false );
#endif
			return File.Exists( path );
		}

		public static bool DirectoryExists( string path )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( path ) )
				return AJC.CallStatic<bool>( "SAFEntryExists", Context, path, true );
			else if( ShouldUseSAF ) // Directory.Exists returns true even for inaccessible directories on Android 10+, we need to check if the directory is accessible
			{
				try
				{
					Directory.GetFiles( path, "testtesttest" );
					return true;
				}
				catch
				{
					return false;
				}
			}
#endif
			return Directory.Exists( path );
		}

		public static bool IsDirectory( string path )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( path ) )
				return AJC.CallStatic<bool>( "SAFEntryDirectory", Context, path );
#endif
			if( Directory.Exists( path ) )
				return true;
			if( File.Exists( path ) )
				return false;

			string extension = Path.GetExtension( path );
			return extension == null || extension.Length <= 1; // extension includes '.'
		}

		public static bool IsPathDescendantOfAnother( string path, string parentFolderPath )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( path ) )
				return AJC.CallStatic<bool>( "IsSAFEntryChildOfAnother", Context, path, parentFolderPath );
#endif
			path = Path.GetFullPath( path ).Replace( '\\', '/' );
			parentFolderPath = Path.GetFullPath( parentFolderPath ).Replace( '\\', '/' );

			if( path == parentFolderPath )
				return false;

			if( parentFolderPath[parentFolderPath.Length - 1] != '/' )
				parentFolderPath += "/";

			return path != parentFolderPath && path.StartsWith( parentFolderPath, System.StringComparison.OrdinalIgnoreCase );
		}

		public static string GetDirectoryName( string path )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( path ) )
				return AJC.CallStatic<string>( "GetParentDirectory", Context, path );
#endif
			return Path.GetDirectoryName( path );
		}

		public static FileSystemEntry[] GetEntriesInDirectory( string path, bool extractOnlyLastSuffixFromExtensions )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( path ) )
			{
				string resultRaw = AJC.CallStatic<string>( "OpenSAFFolder", Context, path );
				int separatorIndex = resultRaw.IndexOf( "<>" );
				if( separatorIndex <= 0 )
				{
					Debug.LogError( "Entry count does not exist" );
					return null;
				}

				int entryCount = 0;
				for( int i = 0; i < separatorIndex; i++ )
				{
					char ch = resultRaw[i];
					if( ch < '0' && ch > '9' )
					{
						Debug.LogError( "Couldn't parse entry count" );
						return null;
					}
					
					entryCount = entryCount * 10 + ( ch - '0' );
				}

				if( entryCount <= 0 )
					return null;

				FileSystemEntry[] result = new FileSystemEntry[entryCount];
				for( int i = 0; i < entryCount; i++ )
				{
					separatorIndex += 2;
					if( separatorIndex >= resultRaw.Length )
					{
						Debug.LogError( "Couldn't fetch directory attribute" );
						return null;
					}

					bool isDirectory = resultRaw[separatorIndex] == 'd';

					separatorIndex++;
					int nextSeparatorIndex = resultRaw.IndexOf( "<>", separatorIndex );
					if( nextSeparatorIndex <= 0 )
					{
						Debug.LogError( "Entry name is empty" );
						return null;
					}

					string entryName = resultRaw.Substring( separatorIndex, nextSeparatorIndex - separatorIndex );

					separatorIndex = nextSeparatorIndex + 2;
					nextSeparatorIndex = resultRaw.IndexOf( "<>", separatorIndex );
					if( nextSeparatorIndex <= 0 )
					{
						Debug.LogError( "Entry rawUri is empty" );
						return null;
					}

					string rawUri = resultRaw.Substring( separatorIndex, nextSeparatorIndex - separatorIndex );

					separatorIndex = nextSeparatorIndex;

					result[i] = new FileSystemEntry( rawUri, entryName, isDirectory ? null : FileBrowser.GetExtensionFromFilename( entryName, extractOnlyLastSuffixFromExtensions ), isDirectory );
				}

				return result;
			}
#endif

			try
			{
				FileSystemInfo[] items = new DirectoryInfo( path ).GetFileSystemInfos();
				FileSystemEntry[] result = new FileSystemEntry[items.Length];
				int index = 0;
				for( int i = 0; i < items.Length; i++ )
				{
					try
					{
						result[index] = new FileSystemEntry( items[i], FileBrowser.GetExtensionFromFilename( items[i].Name, extractOnlyLastSuffixFromExtensions ) );
						index++;
					}
					catch( System.Exception e )
					{
						Debug.LogException( e );
					}
				}

				if( result.Length != index )
					System.Array.Resize( ref result, index );

				return result;
			}
			catch( System.UnauthorizedAccessException ) { }
			catch( System.Exception e )
			{
				Debug.LogException( e );
			}

			return null;
		}

		public static string CreateFileInDirectory( string directoryPath, string filename )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( directoryPath ) )
				return AJC.CallStatic<string>( "CreateSAFEntry", Context, directoryPath, false, filename );
#endif

			string path = Path.Combine( directoryPath, filename );
			using( File.Create( path ) ) { }
			return path;
		}

		public static string CreateFolderInDirectory( string directoryPath, string folderName )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( directoryPath ) )
				return AJC.CallStatic<string>( "CreateSAFEntry", Context, directoryPath, true, folderName );
#endif

			string path = Path.Combine( directoryPath, folderName );
			Directory.CreateDirectory( path );
			return path;
		}

		public static void WriteBytesToFile( string targetPath, byte[] bytes )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( targetPath ) )
			{
				File.WriteAllBytes( TemporaryFilePath, bytes );
				AJC.CallStatic( "WriteToSAFEntry", Context, targetPath, TemporaryFilePath, false );
				File.Delete( TemporaryFilePath );
				
				return;
			}
#endif
			File.WriteAllBytes( targetPath, bytes );
		}

		public static void WriteTextToFile( string targetPath, string text )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( targetPath ) )
			{
				File.WriteAllText( TemporaryFilePath, text );
				AJC.CallStatic( "WriteToSAFEntry", Context, targetPath, TemporaryFilePath, false );
				File.Delete( TemporaryFilePath );
				
				return;
			}
#endif
			File.WriteAllText( targetPath, text );
		}

		public static void AppendBytesToFile( string targetPath, byte[] bytes )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( targetPath ) )
			{
				File.WriteAllBytes( TemporaryFilePath, bytes );
				AJC.CallStatic( "WriteToSAFEntry", Context, targetPath, TemporaryFilePath, true );
				File.Delete( TemporaryFilePath );
				
				return;
			}
#endif
			using( var stream = new FileStream( targetPath, FileMode.Append, FileAccess.Write ) )
			{
				stream.Write( bytes, 0, bytes.Length );
			}
		}

		public static void AppendTextToFile( string targetPath, string text )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( targetPath ) )
			{
				File.WriteAllText( TemporaryFilePath, text );
				AJC.CallStatic( "WriteToSAFEntry", Context, targetPath, TemporaryFilePath, true );
				File.Delete( TemporaryFilePath );
				
				return;
			}
#endif
			File.AppendAllText( targetPath, text );
		}

		private static void AppendFileToFile( string targetPath, string sourceFileToAppend )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( targetPath ) )
			{
				AJC.CallStatic( "WriteToSAFEntry", Context, targetPath, sourceFileToAppend, true );
				return;
			}
#endif
			using( Stream input = File.OpenRead( sourceFileToAppend ) )
			using( Stream output = new FileStream( targetPath, FileMode.Append, FileAccess.Write ) )
			{
				byte[] buffer = new byte[4096];
				int bytesRead;
				while( ( bytesRead = input.Read( buffer, 0, buffer.Length ) ) > 0 )
					output.Write( buffer, 0, bytesRead );
			}
		}

		public static byte[] ReadBytesFromFile( string sourcePath )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( sourcePath ) )
			{
				AJC.CallStatic( "ReadFromSAFEntry", Context, sourcePath, TemporaryFilePath );
				byte[] result = File.ReadAllBytes( TemporaryFilePath );
				File.Delete( TemporaryFilePath );
				return result;
			}
#endif
			return File.ReadAllBytes( sourcePath );
		}

		public static string ReadTextFromFile( string sourcePath )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( sourcePath ) )
			{
				AJC.CallStatic( "ReadFromSAFEntry", Context, sourcePath, TemporaryFilePath );
				string result = File.ReadAllText( TemporaryFilePath );
				File.Delete( TemporaryFilePath );
				return result;
			}
#endif
			return File.ReadAllText( sourcePath );
		}

		public static void CopyFile( string sourcePath, string destinationPath )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAF ) // No need to use ShouldUseSAFForPath because both SAF paths and raw file paths are handled on the native-side
			{
				AJC.CallStatic( "CopyFile", Context, sourcePath, destinationPath, false );
				return;
			}
#endif
			File.Copy( sourcePath, destinationPath, true );
		}

		public static void CopyDirectory( string sourcePath, string destinationPath )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAF ) // No need to use ShouldUseSAFForPath because both SAF paths and raw directory paths are handled on the native-side
			{
				AJC.CallStatic( "CopyDirectory", Context, sourcePath, destinationPath, false );
				return;
			}
#endif
			CopyDirectoryRecursively( new DirectoryInfo( sourcePath ), destinationPath );
		}

		private static void CopyDirectoryRecursively( DirectoryInfo sourceDirectory, string destinationPath )
		{
			Directory.CreateDirectory( destinationPath );

			FileInfo[] files = sourceDirectory.GetFiles();
			for( int i = 0; i < files.Length; i++ )
				files[i].CopyTo( Path.Combine( destinationPath, files[i].Name ), true );

			DirectoryInfo[] subDirectories = sourceDirectory.GetDirectories();
			for( int i = 0; i < subDirectories.Length; i++ )
				CopyDirectoryRecursively( subDirectories[i], Path.Combine( destinationPath, subDirectories[i].Name ) );
		}

		public static void MoveFile( string sourcePath, string destinationPath )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAF ) // No need to use ShouldUseSAFForPath because both SAF paths and raw file paths are handled on the native-side
			{
				AJC.CallStatic( "CopyFile", Context, sourcePath, destinationPath, true );
				return;
			}
#endif
			File.Move( sourcePath, destinationPath );
		}

		public static void MoveDirectory( string sourcePath, string destinationPath )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAF ) // No need to use ShouldUseSAFForPath because both SAF paths and raw directory paths are handled on the native-side
			{
				AJC.CallStatic( "CopyDirectory", Context, sourcePath, destinationPath, true );
				return;
			}
#endif
			Directory.Move( sourcePath, destinationPath );
		}

		public static string RenameFile( string path, string newName )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( path ) )
				return AJC.CallStatic<string>( "RenameSAFEntry", Context, path, newName );
#endif
			string newPath = Path.Combine( Path.GetDirectoryName( path ), newName );
			File.Move( path, newPath );

			return newPath;
		}

		public static string RenameDirectory( string path, string newName )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( path ) )
				return AJC.CallStatic<string>( "RenameSAFEntry", Context, path, newName );
#endif
			string newPath = Path.Combine( new DirectoryInfo( path ).Parent.FullName, newName );
			Directory.Move( path, newPath );

			return newPath;
		}

		public static void DeleteFile( string path )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( path ) )
			{
				AJC.CallStatic<bool>( "DeleteSAFEntry", Context, path );
				return;
			}
#endif
			File.Delete( path );
		}

		public static void DeleteDirectory( string path )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( path ) )
			{
				AJC.CallStatic<bool>( "DeleteSAFEntry", Context, path );
				return;
			}
#endif
			Directory.Delete( path, true );
		}

		public static string GetFilename( string path )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( path ) )
				return AJC.CallStatic<string>( "SAFEntryName", Context, path );
#endif
			return Path.GetFileName( path );
		}

		public static long GetFilesize( string path )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			if( ShouldUseSAFForPath( path ) )
				return AJC.CallStatic<long>( "SAFEntrySize", Context, path );
#endif
			return new FileInfo( path ).Length;
		}

		public static System.DateTime GetLastModifiedDate( string path )
		{
#if !UNITY_EDITOR && UNITY_ANDROID
			// Credit: https://stackoverflow.com/a/28504416/2373034
			if( ShouldUseSAFForPath( path ) )
				return new System.DateTime( 1970, 1, 1, 0, 0, 0 ).AddMilliseconds( AJC.CallStatic<long>( "SAFEntryLastModified", Context, path ) );
#endif
			return new FileInfo( path ).LastWriteTime;
		}
	}
}