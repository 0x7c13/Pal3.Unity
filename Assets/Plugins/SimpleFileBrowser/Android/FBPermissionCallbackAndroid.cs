#if UNITY_EDITOR || UNITY_ANDROID
using System.Threading;
using UnityEngine;

namespace SimpleFileBrowser
{
	public class FBPermissionCallbackAndroid : AndroidJavaProxy
	{
		private object threadLock;
		public int Result { get; private set; }

		public FBPermissionCallbackAndroid( object threadLock ) : base( "com.yasirkula.unity.FileBrowserPermissionReceiver" )
		{
			Result = -1;
			this.threadLock = threadLock;
		}

		public void OnPermissionResult( int result )
		{
			Result = result;

			lock( threadLock )
			{
				Monitor.Pulse( threadLock );
			}
		}
	}
}
#endif