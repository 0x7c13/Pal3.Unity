namespace SimpleFileBrowser
{
	public delegate void OnItemClickedHandler( ListItem item );

	public interface IListViewAdapter
	{
		OnItemClickedHandler OnItemClicked { get; set; }

		int Count { get; }
		float ItemHeight { get; }

		ListItem CreateItem();

		void SetItemContent( ListItem item );
	}
}