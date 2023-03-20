using UnityEngine;

namespace SimpleFileBrowser
{
	public class FileBrowserQuickLink : FileBrowserItem
	{
		#region Properties
		private string m_targetPath;
		public string TargetPath { get { return m_targetPath; } }
		#endregion

		#region Initialization Functions
		public void SetQuickLink( Sprite icon, string name, string targetPath )
		{
			SetFile( icon, name, true );

			m_targetPath = targetPath;
		}
		#endregion
	}
}