using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleFileBrowser
{
	public class FileBrowserMovement : MonoBehaviour
	{
		#region Variables
#pragma warning disable 0649
		private FileBrowser fileBrowser;
		private RectTransform canvasTR;
		private Camera canvasCam;

		[SerializeField]
		private RectTransform window;

		[SerializeField]
		private RecycledListView listView;
#pragma warning restore 0649

		private Vector2 initialTouchPos = Vector2.zero;
		private Vector2 initialAnchoredPos, initialSizeDelta;
		#endregion

		#region Initialization Functions
		public void Initialize( FileBrowser fileBrowser )
		{
			this.fileBrowser = fileBrowser;
			canvasTR = fileBrowser.GetComponent<RectTransform>();
		}
		#endregion

		#region Pointer Events
		public void OnDragStarted( BaseEventData data )
		{
			PointerEventData pointer = (PointerEventData) data;

			canvasCam = pointer.pressEventCamera;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( window, pointer.pressPosition, canvasCam, out initialTouchPos );
		}

		public void OnDrag( BaseEventData data )
		{
			PointerEventData pointer = (PointerEventData) data;

			Vector2 touchPos;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( window, pointer.position, canvasCam, out touchPos );
			window.anchoredPosition += touchPos - initialTouchPos;
		}

		public void OnEndDrag( BaseEventData data )
		{
			fileBrowser.EnsureWindowIsWithinBounds();
		}

		public void OnResizeStarted( BaseEventData data )
		{
			PointerEventData pointer = (PointerEventData) data;

			canvasCam = pointer.pressEventCamera;
			initialAnchoredPos = window.anchoredPosition;
			initialSizeDelta = window.sizeDelta;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( canvasTR, pointer.pressPosition, canvasCam, out initialTouchPos );
		}

		public void OnResize( BaseEventData data )
		{
			PointerEventData pointer = (PointerEventData) data;

			Vector2 touchPos;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( canvasTR, pointer.position, canvasCam, out touchPos );

			Vector2 delta = touchPos - initialTouchPos;
			Vector2 newSize = initialSizeDelta + new Vector2( delta.x, -delta.y );
			Vector2 canvasSize = canvasTR.sizeDelta;

			if( newSize.x < fileBrowser.minWidth ) newSize.x = fileBrowser.minWidth;
			if( newSize.y < fileBrowser.minHeight ) newSize.y = fileBrowser.minHeight;

			if( newSize.x > canvasSize.x ) newSize.x = canvasSize.x;
			if( newSize.y > canvasSize.y ) newSize.y = canvasSize.y;

			newSize.x = (int) newSize.x;
			newSize.y = (int) newSize.y;

			delta = newSize - initialSizeDelta;

			window.anchoredPosition = initialAnchoredPos + new Vector2( delta.x * 0.5f, delta.y * -0.5f );

			if( window.sizeDelta != newSize )
			{
				window.sizeDelta = newSize;
				fileBrowser.OnWindowDimensionsChanged( newSize );
			}

			listView.OnViewportDimensionsChanged();
		}

		public void OnEndResize( BaseEventData data )
		{
			fileBrowser.EnsureWindowIsWithinBounds();
		}
		#endregion
	}
}