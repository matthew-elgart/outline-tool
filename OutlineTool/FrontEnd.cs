using System.Text.Json;
using System.Text.Json.Serialization;

public class FrontEnd
{
	private Story _story;
	private StoryThread? _currentStoryThread;
	private bool _displayStory;
	private int? _selectedStoryBeatIndex;
	private bool _selectingNewStoryBeat;
	private StoryBeat? _storyBeatToMove;

	public FrontEnd(Story story)
	{
		this._story = story;
	}

	public void Render(
		Story story,
		TextRenderer renderer
	)
	{
		if (this._displayStory)
		{
			RenderStory(this._story, renderer);
		}

		if (this._currentStoryThread != null)
		{
			this.RenderStoryThread(this._currentStoryThread, renderer);
		}

		renderer.RenderFrame();
	}

	public void HandleInput(ConsoleKeyInfo input)
	{
		switch (input.Key)
		{
			case ConsoleKey.D1:
				this._currentStoryThread = this._story.Threads[0];
				this._displayStory = false;
				this._selectedStoryBeatIndex = null;
				break;
			case ConsoleKey.D2:
				this._currentStoryThread = this._story.Threads[1];
				this._displayStory = false;
				this._selectedStoryBeatIndex = null;
				break;
			case ConsoleKey.D3:
				this._currentStoryThread = null;
				this._displayStory = true;
				break;

			case ConsoleKey.DownArrow:
			case ConsoleKey.J:
				if (this._currentStoryThread == null) { return; }
				if (this._selectedStoryBeatIndex == null)
				{
					this._selectedStoryBeatIndex = 0;
					return;
				}
				var newDownIndex = Math.Min(
					this._selectedStoryBeatIndex.Value + 1,
					this._currentStoryThread.StoryBeats.Count - 1);
				this._selectedStoryBeatIndex = newDownIndex;
				break;
			case ConsoleKey.UpArrow:
			case ConsoleKey.K:
				if (this._currentStoryThread == null) { return; }
				if (this._selectedStoryBeatIndex == null)
				{
					this._selectedStoryBeatIndex = 0;
					return;
				}
				var newUpIndex = Math.Max(
					this._selectedStoryBeatIndex.Value - 1,
					0);
				this._selectedStoryBeatIndex = newUpIndex;
				break;

			case ConsoleKey.N:
				if (this._currentStoryThread == null) { return; }
				if (this._selectingNewStoryBeat) { return; }

			 	Console.SetCursorPosition(0, 20);
				Console.Write("New story beat name?");
				Console.SetCursorPosition(0, 21);
				var name = Console.ReadLine();
				if (name == string.Empty) { return; }

				int index;
				if (this._selectedStoryBeatIndex == null)
				{
					index = input.Modifiers == ConsoleModifiers.Shift
						? 0
						: this._currentStoryThread.StoryBeats.Count;
				}
				else
				{
					index = input.Modifiers == ConsoleModifiers.Shift
						? this._selectedStoryBeatIndex.Value
						: this._selectedStoryBeatIndex.Value + 1;
				}

				StoryUpdateService.AddStoryBeat(
					index,
					name!,
					this._currentStoryThread
				);
				break;
			case ConsoleKey.E:
				if (this._currentStoryThread == null) { return; }
				if (this._selectingNewStoryBeat) { return; }
				if (this._selectedStoryBeatIndex == null) { return; }

			 	Console.SetCursorPosition(0, 20);
				Console.Write("New story beat name?");
				Console.SetCursorPosition(0, 21);
				var newName = Console.ReadLine();
				if (newName == string.Empty) { return; }

				StoryUpdateService.RenameStoryBeat(
					this._selectedStoryBeatIndex.Value,
					newName!,
					this._currentStoryThread
				);
				break;

			case ConsoleKey.Enter:
				if (this._currentStoryThread == null
					|| this._selectedStoryBeatIndex == null)
				{
					return;
				}
				if (this._selectingNewStoryBeat)
				{
					StoryUpdateService.UpdateStoryBeatOrder(
						this._storyBeatToMove!,
						this._selectedStoryBeatIndex.Value
					);

					this._storyBeatToMove = null;
					this._selectedStoryBeatIndex = null;
					this._selectingNewStoryBeat = false;
					return;
				}

				this._storyBeatToMove = this._currentStoryThread
					.StoryBeats[this._selectedStoryBeatIndex.Value];
				this._selectingNewStoryBeat = true;
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
			renderer.Print();
			renderer.Print(
				$"{beat.Name}{(beat.Chapter != null ? $" (Chapter {beat.Chapter.Order + 1})" : "")}",
				indentation: 2,
				highlighted: beat.Order == this._selectedStoryBeatIndex);
		}
	}

	private static void RenderStory(Story story, TextRenderer renderer)
	{
		renderer.Print(story.Name);
		renderer.Print(new string('-', story.Name.Length));

		foreach (var chapter in story.Chapters)
		{
			var stringToPrint = $"{chapter.Order + 1}. {chapter.Name}";

			renderer.Print();
			renderer.Print(stringToPrint, indentation: 2);
			renderer.Print(new string('-', stringToPrint.Length), indentation: 2);

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
}