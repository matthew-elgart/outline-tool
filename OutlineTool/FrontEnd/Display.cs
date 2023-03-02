public class Display
{
	public StoryThread? CurrentStoryThread { get; private set; }
	private bool _displayLeftColumn;
	private bool _displayChapters;

	public void ToggleLeftColumn()
	{
		this._displayLeftColumn = !this._displayLeftColumn;
		this.CurrentStoryThread = null;
	}

	public void SetCurrentStoryThread(StoryThread? storyThread)
	{
		this._displayLeftColumn = true;
		this.CurrentStoryThread = storyThread;
	}

	public void ToggleRightColumn() =>
		this._displayChapters = !this._displayChapters;

	public ColumnType[] CalculateActiveColumns() => new ColumnType?[]
		{
			this._displayLeftColumn
				? this.CurrentStoryThread != null
					? ColumnType.Beats
					: ColumnType.Threads
				: null,
			this._displayChapters ? ColumnType.Chapters : null
		}
		.Where(ct => ct != null)
		.Select(ct => ct!.Value)
		.ToArray();
}