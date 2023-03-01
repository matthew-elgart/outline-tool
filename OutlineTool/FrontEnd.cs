using System.Text.Json;
using System.Text.Json.Serialization;

public class FrontEnd
{
	// a -1 may be necessary, since terminal seems to autoscroll when fully writing out the final line, which isn't what we want
	// changed to -4 to account for the 3 extra lines of the bash prompt (more important for development so we don't scroll the screen as we iterate)
	// changed to -6 to allow two lines of whitespace at the top, just for style
	private const int Padding = 6;

	private Story _story;
	private StoryThread? _currentStoryThread;
	private bool _displayThread => this._currentStoryThread != null;
	private bool _displayStory;
	private Dictionary<ColumnType, string> _columnTypeNames = new()
	{
		{ ColumnType.Thread, "story beat" },
		{ ColumnType.Story, "chapter" },
		//{ ColumnType.Threads, "story thread" }
	};

	private Cursor _cursor;
	private IOrderedElement? _selectedElement;
	private bool _selectingNewElement => this._selectedElement != null;

	private TextRenderer _threadRenderer = new();
	private TextRenderer _storyRenderer = new();

	public FrontEnd(Story story)
	{
		this._story = story;
		this._cursor = new(this);
	}

	public void Render()
	{
		var (threadRenderer, storyRenderer) = this.GetConfiguredRenderers();

		if (this._displayStory)
		{
			RenderStory(this._story, this._storyRenderer);
		}

		if (this._displayThread)
		{
			this.RenderStoryThread(
				this._currentStoryThread!,
				this._threadRenderer);
		}

		threadRenderer?.RenderFrame();
		storyRenderer?.RenderFrame();
	}

	public void HandleInput(ConsoleKeyInfo input)
	{
		switch (input.Key)
		{
			case ConsoleKey.D1:
				this._cursor.Visible = false;
				if (this._currentStoryThread == this._story.Threads[0])
				{
					this._currentStoryThread = null;
					return;
				}

				this._currentStoryThread = this._story.Threads[0];
				break;
			case ConsoleKey.D2:
				this._cursor.Visible = false;
				if (this._currentStoryThread == this._story.Threads[1])
				{
					this._currentStoryThread = null;
					return;
				}

				this._currentStoryThread = this._story.Threads[1];
				break;
			case ConsoleKey.D3:
				this._cursor.Visible = false;
				this._displayStory = !this._displayStory;
				break;

			case ConsoleKey.DownArrow:
			case ConsoleKey.J:
			 	this._cursor.Down();
				break;
			case ConsoleKey.UpArrow:
			case ConsoleKey.K:
				this._cursor.Up();
				break;
			case ConsoleKey.RightArrow:
			case ConsoleKey.L:
				this._cursor.Right();
				break;
			case ConsoleKey.LeftArrow:
			case ConsoleKey.H:
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

				var index = (append, shift) switch
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
				this._cursor.Visible = false;
				break;
			case ConsoleKey.E:
				if (this._selectingNewElement) { return; }
				if (!this._cursor.Visible) { return; }

				var thingToEdit = this._columnTypeNames
					[this._cursor.Column];

			 	Console.SetCursorPosition(0, 20);
				Console.Write($"New {thingToEdit} name?");
				Console.SetCursorPosition(0, 21);
				var newName = Console.ReadLine();
				if (newName == string.Empty) { return; }

				var element = this.GetCurrentElements()
					[this._cursor.Index];
				element.Name = newName!;
				break;
			case ConsoleKey.D:
				if (this._selectingNewElement) { return; }
				if (!this._cursor.Visible) { return; }

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
					this.GetCurrentElements()
						.DeleteElement(this._cursor.Index);
				}

				this._cursor.Visible = false;
				break;

			case ConsoleKey.Enter:
				if (!this._cursor.Visible) { return; }

				var elements = this.GetCurrentElements();

				// if nothing is selected, then select where cursor is
				if (!this._selectingNewElement)
				{
					this._selectedElement = this.GetCurrentElements()
						[this._cursor.Index];
					return;
				}

				// if user has selected two elements in the same list,
				//then they are "dragging" them into a new order
				if (elements!.GetElementType() == this._selectedElement!.GetType())
				{
					elements.UpdateElementOrder(this._selectedElement, this._cursor.Index);
				}
				// special case: user selected a story beat and "dragged"
				// it to the story column to assign it to a chapter
				else if (this._selectedElement is StoryBeat storyBeat1
					&& this._cursor.Column == ColumnType.Story)
				{
					StoryUpdateService.AssignStoryBeatToChapter(
						storyBeat1,
						this._story.Chapters[this._cursor.Index]);
				}

				this._selectedElement = null;
				this._cursor.Visible = false;
				break;
		}
	}

	private void RenderStoryThread(
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
				this._cursor.Column == ColumnType.Thread
				&& beat.Order == this._cursor.Index;

			renderer.Print();
			renderer.Print(
				$"{beat.Name}{(beat.Chapter != null ? $" (Chapter {beat.Chapter.Order + 1})" : "")}",
				indentation: 2,
				highlighted: highlightText);
		}
	}

	private void RenderStory(Story story, TextRenderer renderer)
	{
		renderer.Print(story.Name);
		renderer.Print(new string('-', story.Name.Length));

		foreach (var chapter in story.Chapters)
		{
			var stringToPrint = $"{chapter.Order + 1}. {chapter.Name}";
			var highlightText =
				this._cursor.Column == ColumnType.Story
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

	private (TextRenderer?, TextRenderer?) GetConfiguredRenderers()
	{
		if (this._currentStoryThread == null && !this._displayStory)
		{
			return (null, null);
		}

		var threadRenderer = this._threadRenderer;
		var storyRenderer = this._storyRenderer;
		//TextRenderer? threadRenderer = null;
		//TextRenderer? storyRenderer = null;

		if (this._displayThread && this._displayStory)
		{
			var renderWidth = Console.WindowWidth / 2;
			threadRenderer = this._threadRenderer;
			storyRenderer = this._storyRenderer;

			threadRenderer.Reset(
				xPosition: 0,
				yPosition: 2,
				width: renderWidth,
				height: Console.WindowHeight - Padding);
			storyRenderer.Reset(
				xPosition: renderWidth,
				yPosition: 2,
				width: renderWidth,
				height: Console.WindowHeight - Padding);

			return (threadRenderer, storyRenderer);
		}

		if (this._displayThread)
		{
			threadRenderer.Reset(
				xPosition: 0,
				yPosition: 2,
				width: Console.WindowWidth,
				height: Console.WindowHeight - Padding);
			storyRenderer = null;
		}
		else
		{
			storyRenderer.Reset(
				xPosition: 0,
				yPosition: 2,
				width: Console.WindowWidth,
				height: Console.WindowHeight - Padding);
			threadRenderer = null;
		}

		//var rendererToDisplay = this._displayThread
			//? threadRenderer
			//: storyRenderer;
		//rendererToDisplay.Reset(
			//xPosition: 0,
			//yPosition: 2,
			//width: Console.WindowWidth,
			//height: Console.WindowHeight - 6);

		//var rendererToHide = this._displayThread
			//? storyRenderer
			//: threadRenderer;

		return (threadRenderer, storyRenderer);
	}

	// returns the list of elements that correspond to the cursor's current location
	private IOrderedElementList GetCurrentElements()
	{
		if (!this._cursor.Visible) { throw new ArgumentException("cursor must be visible when getting current elements"); }

		IOrderedElementList? result = this._cursor.Column switch
		{
			ColumnType.Thread => this._currentStoryThread?.StoryBeats,
			ColumnType.Story => this._story.Chapters,
			_ => throw new ArgumentOutOfRangeException(nameof(this._cursor.Column))
		};

		if (result == null) { throw new ArgumentException("got a null element list for currently selected column type, which shouldn't be possible"); }
		return result;
	}

	private enum ColumnType
	{
		Thread,
		Story
	}

	private class Cursor
	{
		public bool Visible { get; set; }
		public ColumnType Column =>
			!this.Visible
				? default
				: this._oneColumnVisible
					? this._parent._displayThread
						? ColumnType.Thread
						: ColumnType.Story
					: this._selectFromRightColumn
						? ColumnType.Story
						: ColumnType.Thread;
		public int Index => !this.Visible ? -1 : this._index;
		private int _index;
		private bool _oneColumnVisible =>
			this._parent._displayThread ^ this._parent._displayStory;
		private bool _selectFromRightColumn;
		private FrontEnd _parent;

		public Cursor(FrontEnd parent) { this._parent = parent; }
		public void Up()
		{
			if (!this.Visible)
			{
				this.Reset();
				return;
			}

			var newIndex = Math.Max(this.Index - 1, 0);
			this._index = newIndex;
		}
		public void Down()
		{
			if (!this.Visible)
			{
				this.Reset();
				return;
			}

			var numElements = this._parent.GetCurrentElements().Count;
			var newIndex = Math.Min(this.Index + 1, numElements - 1);
			this._index = newIndex;
		}
		public void Left()
		{
			if (!this.Visible)
			{
				this.Reset();
				return;
			}

			if (this._parent._displayThread
				&& this._parent._displayStory
				&& this._selectFromRightColumn)
			{
				this._selectFromRightColumn = false;
				this._index = 0;
			}
		}
		public void Right()
		{
			if (!this.Visible)
			{
				this.Reset();
				return;
			}

			if (this._parent._displayThread
				&& this._parent._displayStory
				&& !this._selectFromRightColumn)
			{
				this._selectFromRightColumn = true;
				this._index = 0;
			}
		}

		public void Reset()
		{
			this.Visible = true;
			this._index = 0;
			this._selectFromRightColumn = false;
		}
	}
}