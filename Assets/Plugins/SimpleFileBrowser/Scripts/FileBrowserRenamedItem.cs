using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace SimpleFileBrowser
{
	public class FileBrowserRenamedItem : MonoBehaviour
	{
		public delegate void OnRenameCompleted( string filename );

#pragma warning disable 0649
		[SerializeField]
		private Image background;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private InputField nameInputField;
		public InputField InputField { get { return nameInputField; } }
#pragma warning restore 0649

		private OnRenameCompleted onRenameCompleted;

		private RectTransform m_transform;
		public RectTransform TransformComponent
		{
			get
			{
				if( m_transform == null )
					m_transform = (RectTransform) transform;

				return m_transform;
			}
		}

		public void Show( string initialFilename, Color backgroundColor, Sprite icon, OnRenameCompleted onRenameCompleted )
		{
			background.color = backgroundColor;
			this.icon.sprite = icon;
			this.onRenameCompleted = onRenameCompleted;

			transform.SetAsLastSibling();
			gameObject.SetActive( true );

			nameInputField.text = initialFilename;
			nameInputField.ActivateInputField();
		}

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA || UNITY_WSA_10_0
		private void LateUpdate()
		{
			// Don't allow scrolling with mouse wheel while renaming a file or creating a folder
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			if( Mouse.current != null && Mouse.current.scroll.ReadValue().y != 0f )
#else
			if( Input.mouseScrollDelta.y != 0f )
#endif
				nameInputField.DeactivateInputField();
		}
#endif

		public void OnInputFieldEndEdit( string filename )
		{
			gameObject.SetActive( false );

			// If we don't deselect the InputField manually, FileBrowser's keyboard shortcuts
			// no longer work until user clicks on a UI element and thus, deselects the InputField
			if( EventSystem.current && !EventSystem.current.alreadySelecting && EventSystem.current.currentSelectedGameObject == nameInputField.gameObject )
				EventSystem.current.SetSelectedGameObject( null );

			if( onRenameCompleted != null )
				onRenameCompleted( filename );
		}
	}
}