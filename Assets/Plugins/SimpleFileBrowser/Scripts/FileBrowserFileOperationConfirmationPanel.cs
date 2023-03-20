using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace SimpleFileBrowser
{
	public class FileBrowserFileOperationConfirmationPanel : MonoBehaviour
	{
		public enum OperationType { Delete = 0, Overwrite = 1 };

		public delegate void OnOperationConfirmed();

#pragma warning disable 0649
		[SerializeField]
		private Text[] titleLabels;

		[SerializeField]
		private GameObject[] targetItems;

		[SerializeField]
		private Image[] targetItemIcons;

		[SerializeField]
		private Text[] targetItemNames;

		[SerializeField]
		private GameObject targetItemsRest;

		[SerializeField]
		private Text targetItemsRestLabel;

		[SerializeField]
		private RectTransform yesButtonTransform;

		[SerializeField]
		private RectTransform noButtonTransform;

		[SerializeField]
		private float narrowScreenWidth = 380f;
#pragma warning restore 0649

		private OnOperationConfirmed onOperationConfirmed;

		internal void Show( FileBrowser fileBrowser, List<FileSystemEntry> items, OperationType operationType, OnOperationConfirmed onOperationConfirmed )
		{
			Show( fileBrowser, items, null, operationType, onOperationConfirmed );
		}

		internal void Show( FileBrowser fileBrowser, List<FileSystemEntry> items, List<int> selectedItemIndices, OperationType operationType, OnOperationConfirmed onOperationConfirmed )
		{
			this.onOperationConfirmed = onOperationConfirmed;

			int itemCount = ( selectedItemIndices != null ) ? selectedItemIndices.Count : items.Count;

			for( int i = 0; i < titleLabels.Length; i++ )
				titleLabels[i].gameObject.SetActive( (int) operationType == i );

			for( int i = 0; i < targetItems.Length; i++ )
				targetItems[i].SetActive( i < itemCount );

			for( int i = 0; i < targetItems.Length && i < itemCount; i++ )
			{
				FileSystemEntry item = items[( selectedItemIndices != null ) ? selectedItemIndices[i] : i];
				targetItemIcons[i].sprite = fileBrowser.GetIconForFileEntry( item );
				targetItemNames[i].text = item.Name;
			}

			if( itemCount > targetItems.Length )
			{
				targetItemsRestLabel.text = string.Concat( "...and ", ( itemCount - targetItems.Length ).ToString(), " other" );
				targetItemsRest.SetActive( true );
			}
			else
				targetItemsRest.SetActive( false );

			gameObject.SetActive( true );
		}

		// Handles responsive user interface
		internal void OnCanvasDimensionsChanged( Vector2 size )
		{
			if( size.x >= narrowScreenWidth )
			{
				yesButtonTransform.anchorMin = new Vector2( 0.5f, 0f );
				yesButtonTransform.anchorMax = new Vector2( 0.75f, 1f );
				noButtonTransform.anchorMin = new Vector2( 0.75f, 0f );
			}
			else
			{
				yesButtonTransform.anchorMin = Vector2.zero;
				yesButtonTransform.anchorMax = new Vector2( 0.5f, 1f );
				noButtonTransform.anchorMin = new Vector2( 0.5f, 0f );
			}
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
				if( Keyboard.current[Key.Enter].wasPressedThisFrame || Keyboard.current[Key.NumpadEnter].wasPressedThisFrame )
#else
				if( Input.GetKeyDown( KeyCode.Return ) || Input.GetKeyDown( KeyCode.KeypadEnter ) )
#endif
					YesButtonClicked();

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
				if( Keyboard.current[Key.Escape].wasPressedThisFrame )
#else
				if( Input.GetKeyDown( KeyCode.Escape ) )
#endif
					NoButtonClicked();
			}
		}
#endif

		internal void RefreshSkin( UISkin skin )
		{
			Image background = GetComponentInChildren<Image>();
			background.color = skin.PopupPanelsBackgroundColor;
			background.sprite = skin.PopupPanelsBackground;

			skin.ApplyTo( yesButtonTransform.GetComponent<Button>() );
			skin.ApplyTo( noButtonTransform.GetComponent<Button>() );

			for( int i = 0; i < titleLabels.Length; i++ )
				skin.ApplyTo( titleLabels[i], skin.PopupPanelsTextColor );

			skin.ApplyTo( targetItemsRestLabel, skin.PopupPanelsTextColor );

			for( int i = 0; i < targetItemNames.Length; i++ )
				skin.ApplyTo( targetItemNames[i], skin.PopupPanelsTextColor );

			for( int i = 0; i < targetItems.Length; i++ )
				targetItems[i].GetComponent<LayoutElement>().preferredHeight = skin.FileHeight;
		}

		public void YesButtonClicked()
		{
			gameObject.SetActive( false );

			if( onOperationConfirmed != null )
				onOperationConfirmed();
		}

		public void NoButtonClicked()
		{
			gameObject.SetActive( false );
			onOperationConfirmed = null;
		}
	}
}