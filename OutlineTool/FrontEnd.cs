using System.Text.Json;
using System.Text.Json.Serialization;

public class FrontEnd
{
	// a -1 may be necessary, since terminal seems to autoscroll when fully writing out the final line, which isn't what we want
	// changed to -4 to account for the 3 extra lines of the bash prompt (more important for development so we don't scroll the screen as we iterate)
	// changed to -6 to allow two lines of whitespace at the top, just for style
	private const int Padding = 6;

	private Story _story;
	private Display _display = new();
	// could be computed on every access - but since it's a couple
	// layers of checks, keeping this field as a "cache" and updating it
	// when the columns change
	private ColumnType[] _activeColumns = new ColumnType[0];
	private Dictionary<ColumnType, string> _columnTypeNames = new()
	{
		{ ColumnType.Beats, "story beat" },
		{ ColumnType.Chapters, "chapter" },
		{ ColumnType.Threads, "story thread" }
	};

	private Cursor _cursor;
	private (ColumnType Column, int Index)? _selection;
	private bool _selectingNewElement => this._selection != null;

	// arguably we don't need these, since we configure the renderers
	// anew each cycle (so we could instead *create* and configure them).
	// But I liked the idea of not needing to constantly allocate and
	// deallocate renderers, so I'm keeping them around
	private TextRenderer _leftRenderer = new();
	private TextRenderer _rightRenderer = new();

	public FrontEnd(Story story)
	{
		this._story = story;
		this._cursor = new(this);
	}

	public void Render()
	{
		var renderers =
			this.GetConfiguredRenderers(this._activeColumns.Length);
		if (this._activeColumns.Length != renderers.Length)
		{
			throw new InvalidOperationException($"Number of columns ({this._activeColumns.Length}) was different from the number of renderers ({renderers.Length})");
		}

		for (var i = 0; i < this._activeColumns.Length; i++)
		{
			var renderer = renderers[i];
			switch (this._activeColumns[i])
			{
				case ColumnType.Beats:
					this.RenderStoryBeats(
						this._display.CurrentStoryThread!,
						renderer);
					break;
				case ColumnType.Chapters:
					this.RenderChapters(this._story, renderer);
					break;
				case ColumnType.Threads:
					this.RenderStoryThreads(this._story, renderer);
					break;
			}

			renderer.RenderFrame();
		}
	}

	public void HandleInput(ConsoleKeyInfo input)
	{
		switch (input.Key)
		{
			case ConsoleKey.D1:
				this._cursor.Reset(resetColumn: true);
				this._display
					.SetCurrentStoryThread(this._story.Threads[0]);
				this._activeColumns =
					this._display.CalculateActiveColumns();
				break;
			case ConsoleKey.D2:
				this._cursor.Reset(resetColumn: true);
				this._display
					.SetCurrentStoryThread(this._story.Threads[1]);
				this._activeColumns =
					this._display.CalculateActiveColumns();
				break;
			case ConsoleKey.D3:
				this._cursor.Reset(resetColumn: true);
				this._display.ToggleRightColumn();
				this._activeColumns =
					this._display.CalculateActiveColumns();
				break;
			case ConsoleKey.D4:
				this._cursor.Reset(resetColumn: true);
				this._display.ToggleLeftColumn();
				this._activeColumns =
					this._display.CalculateActiveColumns();
				break;

			case ConsoleKey.DownArrow:
			case ConsoleKey.J:
				if (this._activeColumns.Length == 0)
				{
					return;
				}
			 	this._cursor.Down();
				break;
			case ConsoleKey.UpArrow:
			case ConsoleKey.K:
				if (this._activeColumns.Length == 0)
				{
					return;
				}
				this._cursor.Up();
				break;
			case ConsoleKey.RightArrow:
			case ConsoleKey.L:
				if (this._activeColumns.Length == 0)
				{
					return;
				}
				this._cursor.Right();
				break;
			case ConsoleKey.LeftArrow:
			case ConsoleKey.H:
				if (this._activeColumns.Length == 0)
				{
					return;
				}
				this._cursor.Left();
				break;

			case ConsoleKey.A:
			case ConsoleKey.I:
				if (this._selectingNewElement) { return; }
				if (!this._cursor.Visible) { return; }

				var thingToAdd = this._columnTypeNames
					[this._cursor.Column];

			 	Console.SetCursorPosition(0, 20);
				Console.Write($"New {thingToAdd} name?");
				Console.SetCursorPosition(0, 21);
				var name = Console.ReadLine();
				if (name == string.Empty) { return; }

				var list = this.GetCurrentElements();

				var append = input.Key == ConsoleKey.A;
				var shift = input.Modifiers == ConsoleModifiers.Shift;

				var index = list.Count == 0
					// always insert to 0 when list is empty
					? 0
					: (append, shift) switch
					{
						// lowercase i
						(false, false) => this._cursor.Index,
						// lowercase a
						(true, false) => this._cursor.Index + 1,
						// uppercase I
						(false, true) => 0,
						// uppercase A
						(true, true) => list.Count
					};

				list.InsertNewElement(index, name!);
				this._cursor.Reset();
				break;
			case ConsoleKey.E:
				if (this._selectingNewElement) { return; }
				if (!this._cursor.Visible) { return; }

				var editElements = this.GetCurrentElements();
				if (editElements.Count == 0) { return; }

				var thingToEdit = this._columnTypeNames
					[this._cursor.Column];

			 	Console.SetCursorPosition(0, 20);
				Console.Write($"New {thingToEdit} name?");
				Console.SetCursorPosition(0, 21);
				var newName = Console.ReadLine();
				if (newName == string.Empty) { return; }

				var element = editElements[this._cursor.Index];
				element.Name = newName!;
				break;
			case ConsoleKey.D:
				if (this._selectingNewElement) { return; }
				if (!this._cursor.Visible) { return; }

				var deleteElements = this.GetCurrentElements();
				if (deleteElements.Count == 0) { return; }

				var confirmation = string.Empty;
				var thingToDelete = this._columnTypeNames
					[this._cursor.Column];
				do
				{
					Console.SetCursorPosition(0, 20);
					Console.WriteLine(new string(' ', Console.WindowWidth));
					Console.SetCursorPosition(0, 20);
					Console.Write($"DELETE this {thingToDelete}? (y/n) ");
					confirmation = Console.ReadLine();
				} while (!new[] { "y", "n" }.Contains(confirmation));

				if (confirmation == "y")
				{
					deleteElements.DeleteElement(this._cursor.Index);
				}

				this._cursor.Reset();
				break;

			case ConsoleKey.Enter:
				if (!this._cursor.Visible) { return; }

				var elements = this.GetCurrentElements();
				if (elements.Count == 0) { return; }

				// if nothing is selected, then select where cursor is
				if (!this._selectingNewElement)
				{
					this._selection = (this._cursor.Column, this._cursor.Index);
					return;
				}

				var selection = this._selection!.Value;

				// if user has selected two elements in the same list,
				//then they are "dragging" them into a new order
				if (this._cursor.Column == selection.Column)
				{
					elements.UpdateElementOrder(
						selection.Index,
						this._cursor.Index);
				}
				// special case: user selected a story beat and "dragged"
				// it to the story column to assign it to a chapter
				else if (selection.Column == ColumnType.Beats
					&& this._cursor.Column == ColumnType.Chapters)
				{
					var storyBeat = this._display
						.CurrentStoryThread!
						.StoryBeats[selection.Index];
					StoryUpdateService.AssignStoryBeatToChapter(
						storyBeat,
						this._story.Chapters[this._cursor.Index]);
				}

				this._selection = null;
				this._cursor.Reset();
				break;
		}
	}

	private void RenderStoryThreads(Story story, TextRenderer renderer)
	{
		renderer.Print(story.Name);
		renderer.Print(new string('-', story.Name.Length));

		foreach (var storyThread in story.Threads)
		{
			var stringToPrint = storyThread.Name;
			var highlightText =
				this._cursor.Column == ColumnType.Threads
				&& storyThread.Order == this._cursor.Index;

			renderer.Print();
			renderer.Print(
				stringToPrint,
				indentation: 2,
				color: storyThread.TextColor,
				highlighted: highlightText);
			renderer.Print(
				new string('-', stringToPrint.Length),
				indentation: 2,
				color: storyThread.TextColor,
				highlighted: highlightText);
		}
	}

	private void RenderStoryBeats(
		StoryThread thread,
		TextRenderer renderer)
	{
		renderer.Print(thread.Name, color:thread.TextColor);
		renderer.Print(
			new string('-', thread.Name.Length),
			color: thread.TextColor);

		foreach (var beat in thread.StoryBeats)
		{
			var highlightText =
				this._cursor.Column == ColumnType.Beats
				&& beat.Order == this._cursor.Index;

			renderer.Print();
			renderer.Print(
				$"{beat.Name}{(beat.Chapter != null ? $" (Chapter {beat.Chapter.Order + 1})" : "")}",
				indentation: 2,
				highlighted: highlightText);
		}
	}

	private void RenderChapters(Story story, TextRenderer renderer)
	{
		renderer.Print(story.Name);
		renderer.Print(new string('-', story.Name.Length));

		foreach (var chapter in story.Chapters)
		{
			var stringToPrint = $"{chapter.Order + 1}. {chapter.Name}";
			var highlightText =
				this._cursor.Column == ColumnType.Chapters
				&& chapter.Order == this._cursor.Index;

			renderer.Print();
			renderer.Print(
				stringToPrint,
				indentation: 2,
				highlighted: highlightText);
			renderer.Print(
				new string('-', stringToPrint.Length),
				indentation: 2,
				highlighted: highlightText);

			foreach (var beat in chapter.StoryBeats)
			{
				var thread = story.Threads
					.Single(t => t.StoryBeats.Contains(beat));
				
				renderer.Print(
					beat.Name,
					indentation: 4,
					color: thread.TextColor);
			}
		}
	}

	private TextRenderer[] GetConfiguredRenderers(
		int numColumns)
	{
		if (numColumns == 0)
		{
			return new TextRenderer[0];
		}

		if (numColumns == 2)
		{
			var renderWidth = Console.WindowWidth / 2;

			this._leftRenderer.Reset(
				xPosition: 0,
				yPosition: 2,
				width: renderWidth,
				height: Console.WindowHeight - Padding);
			this._rightRenderer.Reset(
				xPosition: renderWidth,
				yPosition: 2,
				width: renderWidth,
				height: Console.WindowHeight - Padding);

			return new[] { this._leftRenderer, this._rightRenderer };
		}

		// arbitrarily choose the left as the one to use when only one column
		this._leftRenderer.Reset(
			xPosition: 0,
			yPosition: 2,
			width: Console.WindowWidth,
			height: Console.WindowHeight - Padding);

		return new[] { this._leftRenderer };
	}

	// returns the list of elements that correspond to the cursor's current location
	private IOrderedElementList GetCurrentElements()
	{
		if (!this._cursor.Visible) { throw new ArgumentException("cursor must be visible when getting current elements"); }

		IOrderedElementList? result = this._cursor.Column switch
		{
			ColumnType.Beats => this._display
				.CurrentStoryThread!
				.StoryBeats,
			ColumnType.Chapters => this._story.Chapters,
			ColumnType.Threads => this._story.Threads,
			_ => throw new ArgumentOutOfRangeException(nameof(this._cursor.Column))
		};

		if (result == null) { throw new ArgumentException("got a null element list for currently selected column type, which shouldn't be possible"); }
		return result;
	}

	private enum ColumnType
	{
		Beats,
		Chapters,
		Threads
	}

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

	private class Display
	{
		public StoryThread? CurrentStoryThread { get; private set; }
		private bool _displayLeftColumn;
		private bool _displayChapters;

		public void ToggleLeftColumn()
		{
			this._displayLeftColumn = !this._displayLeftColumn;
			this.CurrentStoryThread = null;
		}

		public void SetCurrentStoryThread(StoryThread storyThread)
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
}