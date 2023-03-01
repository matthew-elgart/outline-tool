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
	private bool _oneColumnVisible =>
		this._displayThread ^ this._displayStory;
	private ColumnType? _currentlySelectedColumnType =>
		this._selectedIndex == null ? null
			: this._oneColumnVisible ? this._displayThread
				? ColumnType.Thread
				: ColumnType.Story
			: this._selectFromRightColumn
				? ColumnType.Story
				: ColumnType.Thread;
	private Dictionary<ColumnType, string> _columnTypeNames = new()
	{
		{ ColumnType.Thread, "story beat" },
		{ ColumnType.Story, "chapter" },
		//{ ColumnType.Threads, "story thread" }
	};

	private int? _selectedIndex;
	private bool _selectFromRightColumn;
	private IOrderedElement? _selectedElement;
	private bool _selectingNewElement => this._selectedElement != null;

	private TextRenderer _threadRenderer = new();
	private TextRenderer _storyRenderer = new();

	public FrontEnd(Story story)
	{
		this._story = story;
	}

	public void Render(Story story)
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
				this._selectedIndex = null;
				if (this._currentStoryThread == this._story.Threads[0])
				{
					this._currentStoryThread = null;
					return;
				}

				this._currentStoryThread = this._story.Threads[0];
				break;
			case ConsoleKey.D2:
				this._selectedIndex = null;
				if (this._currentStoryThread == this._story.Threads[1])
				{
					this._currentStoryThread = null;
					return;
				}

				this._currentStoryThread = this._story.Threads[1];
				break;
			case ConsoleKey.D3:
			 	this._selectedIndex = null;
				this._selectFromRightColumn = false;
				this._displayStory = !this._displayStory;
				break;

			case ConsoleKey.DownArrow:
			case ConsoleKey.J:
				var currentColumn = this._currentlySelectedColumnType;
				if (currentColumn == null)
				{
					this._selectedIndex = 0;
					return;
				}

				var numItems = this.GetCurrentElements().Count;
				var newDownIndex = Math.Min(
					this._selectedIndex!.Value + 1,
					numItems - 1);
				this._selectedIndex = newDownIndex;
				break;
			case ConsoleKey.UpArrow:
			case ConsoleKey.K:
				if (this._selectedIndex == null)
				{
					this._selectedIndex = 0;
					return;
				}
				var newUpIndex = Math.Max(
					this._selectedIndex.Value - 1,
					0);
				this._selectedIndex = newUpIndex;
				break;
			case ConsoleKey.RightArrow:
			case ConsoleKey.L:
				if (this._selectedIndex == null)
				{
					this._selectedIndex = 0;
					return;
				}
				if (this._displayThread
					&& this._displayStory
					&& !this._selectFromRightColumn)
				{
					this._selectFromRightColumn = true;
					this._selectedIndex = 0;
				}
				break;
			case ConsoleKey.LeftArrow:
			case ConsoleKey.H:
				if (this._selectedIndex == null)
				{
					this._selectedIndex = 0;
					return;
				}
				if (this._displayThread
					&& this._displayStory
					&& this._selectFromRightColumn)
				{
					this._selectFromRightColumn = false;
					this._selectedIndex = 0;
				}
				break;

			case ConsoleKey.A:
			case ConsoleKey.I:
				if (this._selectingNewElement) { return; }
				if (this._selectedIndex == null) { return; }

				var thingToAdd = this._columnTypeNames
					[this._currentlySelectedColumnType!.Value];

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
					(false, false) => this._selectedIndex.Value,
					// lowercase a
					(true, false) => this._selectedIndex.Value + 1,
					// uppercase I
					(false, true) => 0,
					// uppercase A
					(true, true) => list.Count
				};

				list.InsertNewElement(index, name!);
				this._selectedIndex = null;
				break;
			case ConsoleKey.E:
				if (this._selectingNewElement) { return; }
				if (this._selectedIndex == null) { return; }

				var thingToEdit = this._columnTypeNames
					[this._currentlySelectedColumnType!.Value];

			 	Console.SetCursorPosition(0, 20);
				Console.Write($"New {thingToEdit} name?");
				Console.SetCursorPosition(0, 21);
				var newName = Console.ReadLine();
				if (newName == string.Empty) { return; }

				var element = this.GetCurrentElements()
					[this._selectedIndex.Value];
				element.Name = newName!;
				break;
			case ConsoleKey.D:
				if (this._selectingNewElement) { return; }
				if (this._selectedIndex == null) { return; }

				var confirmation = string.Empty;
				var thingToDelete = this._columnTypeNames
					[this._currentlySelectedColumnType!.Value];
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
						.DeleteElement(this._selectedIndex!.Value);
				}

				this._selectedIndex = null;
				break;

			case ConsoleKey.Enter:
			  	var columnType = this._currentlySelectedColumnType;
			 	if (columnType == null)
				{
					return;
				}

				var elements = this.GetCurrentElements();

				// if nothing is selected, then select where cursor is
				if (!this._selectingNewElement)
				{
					this._selectedElement = this.GetCurrentElements()
						[this._selectedIndex!.Value];
					return;
				}

				// if user has selected two elements in the same list,
				//then they are "dragging" them into a new order
				if (elements!.GetElementType() == this._selectedElement!.GetType())
				{
					elements.UpdateElementOrder(this._selectedElement, this._selectedIndex!.Value);
				}
				// special case: user selected a story beat and "dragged"
				// it to the story column to assign it to a chapter
				else if (this._selectedElement is StoryBeat storyBeat1
					&& columnType == ColumnType.Story)
				{
					StoryUpdateService.AssignStoryBeatToChapter(
						storyBeat1,
						this._story.Chapters[this._selectedIndex!
							.Value]);
				}

				this._selectedElement = null;
				this._selectedIndex = null;
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
				this._currentlySelectedColumnType == ColumnType.Thread
				&& beat.Order == this._selectedIndex;

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
				this._currentlySelectedColumnType == ColumnType.Story
				&& chapter.Order == this._selectedIndex;

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
		var columnType = this._currentlySelectedColumnType;
		if (columnType == null) { throw new ArgumentException("columnType can't be null when getting current elements"); }

		IOrderedElementList? result = columnType.Value switch
		{
			ColumnType.Thread => this._currentStoryThread?.StoryBeats,
			ColumnType.Story => this._story.Chapters,
			_ => throw new ArgumentOutOfRangeException(nameof(columnType))
		};

		if (result == null) { throw new ArgumentException("got a null element list for currently selected column type, which shouldn't be possible"); }
		return result;
	}

	private enum ColumnType
	{
		Thread,
		Story
	}
}