partial class FrontEnd
{
	// needs to be nested because it accesses private FrontEnd fields
	private class Cursor
	{
		public bool Visible { get; private set; }
		public ColumnType Column { get
		{
			if (!this.Visible) { return default; }
			var activeColumns = this._parent._activeColumns;
			return activeColumns.Length switch
			{
				0 => default,
				1 => activeColumns.Single(),
				2 => this._selectFromRightColumn
					? activeColumns[1]
					: activeColumns[0],
				_ => throw new InvalidOperationException($"There should be maximum 2 active columns, but got {activeColumns.Length}")
			};
		}}
		public int Index => !this.Visible ? -1 : this._index;
		private int _index;
		private bool _selectFromRightColumn;
		private FrontEnd _parent;

		public Cursor(FrontEnd parent) { this._parent = parent; }

		public void Up()
		{
			this.Visible = true;

			var newIndex = Math.Max(this.Index - 1, 0);
			this._index = newIndex;
		}

		public void Down()
		{
			this.Visible = true;

			var numElements = this._parent.GetCurrentElements().Count;
			var newIndex = Math.Min(this.Index + 1, numElements - 1);
			this._index = newIndex;
		}

		public void Left()
		{
			var switchedColumns = false;
			if (this._parent._activeColumns.Length == 2
				&& this._selectFromRightColumn)
			{
				this._selectFromRightColumn = false;
				switchedColumns = true;
			}

			// if the cursor was already visible and we didn't switch
			// columns, then we should leave it where it is;
			// otherwise we set it to the top
			if (!this.Visible || switchedColumns) { this._index = 0; }

			this.Visible = true;
		}

		public void Right()
		{
			var switchedColumns = false;
			if (this._parent._activeColumns.Length == 2
				&& !this._selectFromRightColumn)
			{
				this._selectFromRightColumn = true;
				switchedColumns = true;
			}

			// if the cursor was already visible and we didn't switch
			// columns, then we should leave it where it is;
			// otherwise we set it to the top
			if (!this.Visible || switchedColumns) { this._index = 0; }

			this.Visible = true;
		}

		public void Reset(bool resetColumn = false)
		{
			// set cursor invisible
			this.Visible = false;
			// selection should be cleared whenever we reset cursor
			this._parent._selection = null;
			// reset to -1 so that the up/down logic above will correctly
			// set index to 0 when they are first called
			this._index = -1;
			// it feels better to not mess with which column the cursor
			// is on after doing CRUD operations on the list elements.
			// But it also feels good to consistently set the cursor on
			// the left when changing which columns are visible.
			// Therefore we default to not changing, but optionally allow
			// column position to be reset as well.
			if (resetColumn) { this._selectFromRightColumn = false; }
		}
	}
}