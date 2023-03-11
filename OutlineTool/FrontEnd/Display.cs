partial class FrontEnd
{
	// needs to be nested because it accesses private FrontEnd fields
	private class Display
	{
		public StoryThread? CurrentStoryThread { get; private set; }
		private bool _displayLeftColumn;
		private bool _displayChapters;
		private FrontEnd _parent;

		public Display(FrontEnd parent) { this._parent = parent; }

		public void ToggleLeftColumn()
		{
			this._displayLeftColumn = !this._displayLeftColumn;
			this.CurrentStoryThread = null;
			this.UpdateParentActiveColumns();
		}

		public void SetCurrentStoryThread(StoryThread? storyThread)
		{
			this._displayLeftColumn = true;
			this.CurrentStoryThread = storyThread;
			this.UpdateParentActiveColumns();
		}

		public void ToggleRightColumn()
		{
			this._displayChapters = !this._displayChapters;
			this.UpdateParentActiveColumns();
		}

		private void UpdateParentActiveColumns() => this._parent._activeColumns =
			new ColumnType?[]
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

		public Display DeepCopy(StoryThread? threadCopy)
		{
			var result = new Display(this._parent);
			if (this._displayLeftColumn) { result.SetCurrentStoryThread(threadCopy); }
			if (this._displayChapters) { result.ToggleRightColumn(); }

			return result;
		}
	}
}