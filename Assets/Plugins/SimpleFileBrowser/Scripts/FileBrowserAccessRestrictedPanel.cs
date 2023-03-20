using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace SimpleFileBrowser
{
	public class FileBrowserAccessRestrictedPanel : MonoBehaviour
	{
#pragma warning disable 0649
		[SerializeField]
		private Text messageLabel;

		[SerializeField]
		private Button okButton;
#pragma warning restore 0649

		internal void Show()
		{
			gameObject.SetActive( true );
		}

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA || UNITY_WSA_10_0
		private void LateUpdate()
		{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			if( Keyboard.current != null )
#endif
			{
				// Handle keyboard shortcuts
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
				if( Keyboard.current[Key.Enter].wasPressedThisFrame || Keyboard.current[Key.NumpadEnter].wasPressedThisFrame || Keyboard.current[Key.Escape].wasPressedThisFrame )
#else
				if( Input.GetKeyDown( KeyCode.Return ) || Input.GetKeyDown( KeyCode.KeypadEnter ) || Input.GetKeyDown( KeyCode.Escape ) )
#endif
					OKButtonClicked();
			}
		}
#endif

		internal void RefreshSkin( UISkin skin )
		{
			Image background = GetComponentInChildren<Image>();
			background.color = skin.PopupPanelsBackgroundColor;
			background.sprite = skin.PopupPanelsBackground;

			skin.ApplyTo( okButton );
			skin.ApplyTo( messageLabel, skin.PopupPanelsTextColor );
		}

		public void OKButtonClicked()
		{
			gameObject.SetActive( false );
		}
	}
}