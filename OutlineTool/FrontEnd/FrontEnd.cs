using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

public partial class FrontEnd
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
	private bool _enableColors;

	private Cursor _cursor;
	private (ColumnType Column, int Index)? _selection;
	private bool _selectingNewElement => this._selection != null;

	// arguably we don't need these, since we configure the renderers
	// anew each cycle (so we could instead *create* and configure them).
	// But I liked the idea of not needing to constantly allocate and
	// deallocate renderers, so I'm keeping them around
	private TextRenderer _leftRenderer = new();
	private TextRenderer _rightRenderer = new();
	private TextRenderer _topRenderer = new();
	private TextRenderer _bottomRenderer = new();

	public FrontEnd(Story story)
	{
		this._story = story;
		this._cursor = new(this);
	}

	public void HandleInput(ConsoleKeyInfo input)
	{
		switch (input.Key)
		{
			case ConsoleKey.D1:
				this._cursor.Reset(resetColumn: true);
				this._display.ToggleLeftColumn();
				this._activeColumns =
					this._display.CalculateActiveColumns();
				break;
			case ConsoleKey.D2:
				this._cursor.Reset(resetColumn: true);
				this._display.ToggleRightColumn();
				this._activeColumns =
					this._display.CalculateActiveColumns();
				break;
			case ConsoleKey.C
			when input.Modifiers != ConsoleModifiers.Shift:
				if (!this._cursor.Visible) { return; }
				if (this._cursor.Column != ColumnType.Threads) { return; }

				var clickElements = this.GetCurrentElements();
				if (clickElements.Count == 0) { return; }

				this._display.SetCurrentStoryThread(
					this._story.Threads[this._cursor.Index]);
				this._activeColumns =
					this._display.CalculateActiveColumns();
				this._cursor.Reset();
				break;
			case ConsoleKey.C
			when input.Modifiers == ConsoleModifiers.Shift:
				if (!this._activeColumns.Contains(ColumnType.Beats))
				{
					return;
				}

				this._display.SetCurrentStoryThread(null);
				this._activeColumns =
					this._display.CalculateActiveColumns();
				this._cursor.Reset();
				break;
			case ConsoleKey.N:
				if (!this._activeColumns.Contains(ColumnType.Beats))
				{
					return;
				}

				var nextIndex = input.Modifiers == ConsoleModifiers.Shift
					? Math.Max(
						this._display.CurrentStoryThread!.Order - 1,
						0)
					: Math.Min(
						this._display.CurrentStoryThread!.Order + 1,
						this._story.Threads.Count - 1);
				this._display.SetCurrentStoryThread(
					this._story.Threads[nextIndex]);
				this._activeColumns =
					this._display.CalculateActiveColumns();
				this._cursor.Reset();
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

				var name = GetInputFromUser($"New {thingToAdd} name?");
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

				var newName = GetInputFromUser($"New {thingToEdit} name?");
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
					Console.SetCursorPosition(1, Console.WindowHeight - 3);
					Console.WriteLine(
						new string(' ',
						Console.WindowWidth - 2));
					confirmation = GetInputFromUser($"DELETE this {thingToDelete} (y/n)");
				} while (!new[] { "y", "n" }.Contains(confirmation));

				if (confirmation == "y")
				{
					deleteElements.DeleteElement(this._cursor.Index);
				}

				this._cursor.Reset();
				break;

			case ConsoleKey.Enter:
			case ConsoleKey.Spacebar:
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

			case ConsoleKey.T:
				this._enableColors = !this._enableColors;
				break;
		}
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

	private static string GetInputFromUser(string prompt)
	{
		Console.SetCursorPosition(0, Console.WindowHeight - 5);
		var panel = new Panel(prompt);
		panel.Height = 4;
		panel.Width = Console.WindowWidth;
		AnsiConsole.Write(panel);

		Console.SetCursorPosition(2, Console.WindowHeight - 3);
		AnsiConsole.Write("> ");
		return Console.ReadLine()!;
	}

#region rendering
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

		this._topRenderer.Reset(0, 0, Console.WindowWidth, 2);
		this._bottomRenderer.Reset(
			xPosition: 0,
			yPosition: Console.WindowHeight - 4,
			width: Console.WindowWidth,
			height: 4);
		this._topRenderer.RenderFrame();
		this._bottomRenderer.RenderFrame();
	}

	private void RenderStoryThreads(Story story, TextRenderer renderer)
	{
		renderer.Print(story.Name, isHeader: true);
		renderer.Print(new string('-', story.Name.Length), isHeader: true);

		foreach (var thread in story.Threads)
		{
			var stringToPrint = thread.Name;
			var highlightText =
				this._cursor.Column == ColumnType.Threads
				&& thread.Order == this._cursor.Index
				&& this._selection?.Column != ColumnType.Threads;
			var color = this._enableColors
				? thread.TextColor
				: ConsoleColor.Gray;
			var shouldPutArrowAboveThisOne =
				this.ShouldPutArrowAboveElement(
					ColumnType.Threads,
					thread.Order);

			renderer.Print(
				// want to count the first bit of whitespace as part of the header
				isHeader: thread.Order == 0,
				arrow: shouldPutArrowAboveThisOne);
			renderer.Print(
				stringToPrint,
				indentation: 2,
				color: color,
				highlighted: highlightText);
			renderer.Print(
				new string('-', stringToPrint.Length),
				indentation: 2,
				color: color,
				highlighted: highlightText);

			if (this.ShouldAddArrowBelowElement(
				ColumnType.Threads,
				thread.Order))
			{
				renderer.Print(arrow: true);
			}
		}
	}

	private void RenderStoryBeats(
		StoryThread thread,
		TextRenderer renderer)
	{
		var color = this._enableColors
			? thread.TextColor
			: ConsoleColor.Gray;

		renderer.Print(thread.Name, color: color, isHeader: true);
		renderer.Print(
			new string('-', thread.Name.Length),
			color: color,
			isHeader: true);

		foreach (var beat in thread.StoryBeats)
		{
			var highlightText =
				this._cursor.Column == ColumnType.Beats
				&& beat.Order == this._cursor.Index
				&& this._selection?.Column != ColumnType.Beats;
			var shouldPutArrowAboveThisOne =
				this.ShouldPutArrowAboveElement(
					ColumnType.Beats,
					beat.Order);

			renderer.Print(
				// want to count the first bit of whitespace as part of
				// the header
				isHeader: beat.Order == 0,
				arrow: shouldPutArrowAboveThisOne);
			renderer.Print(
				$"{beat.Name}{(beat.Chapter != null ? $" (Chapter {beat.Chapter.Order + 1})" : "")}",
				indentation: 2,
				highlighted: highlightText);

			if (this.ShouldAddArrowBelowElement(
				ColumnType.Beats,
				beat.Order))
			{
				renderer.Print(arrow: true);
			}
		}
	}

	private void RenderChapters(Story story, TextRenderer renderer)
	{
		renderer.Print(story.Name, isHeader: true);
		renderer.Print(new string('-', story.Name.Length), isHeader: true);

		foreach (var chapter in story.Chapters)
		{
			var stringToPrint = $"{chapter.Order + 1}. {chapter.Name}";
			var highlightText =
				this._cursor.Column == ColumnType.Chapters
				&& chapter.Order == this._cursor.Index
				&& this._selection?.Column != ColumnType.Chapters;
			var shouldPutArrowAboveThisOne =
				this.ShouldPutArrowAboveElement(
					ColumnType.Chapters,
					chapter.Order);

			renderer.Print(
				// want to count the first bit of whitespace as part of
				// the header
				isHeader: chapter.Order == 0,
				arrow: shouldPutArrowAboveThisOne);
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
					color: this._enableColors
						? thread.TextColor
						: ConsoleColor.Gray,
					highlighted: highlightText);
			}

			if (this.ShouldAddArrowBelowElement(
				ColumnType.Chapters,
				chapter.Order))
			{
				renderer.Print(arrow: true);
			}
		}
	}

	private bool ShouldPutArrowAboveElement(ColumnType column, int order)
	{
		var swapping =
			this._cursor.Column == column
			&& this._selection?.Column == column;

		if (!swapping) { return false; }

		var shouldPutArrowAboveThis =
			order == this._cursor.Index
			&& order <= this._selection!.Value.Index;
		var shouldPutArrowBelowPrevious =
			order - 1 == this._cursor.Index
			&& order - 1 > this._selection!.Value.Index;

		return shouldPutArrowAboveThis || shouldPutArrowBelowPrevious;
	}

	private bool ShouldAddArrowBelowElement(ColumnType column, int order)
	{
		var swapping =
			this._cursor.Column == column
			&& this._selection?.Column == column;

		// need to short-circuit on this because GetCurrentElements
		// fails if there's no cursor
		if (!swapping) { return false; }

		var elements = this.GetCurrentElements();
		return order == elements.Count - 1
			&& order == this._cursor.Index
			&& order > this._selection!.Value.Index;
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
#endregion
}