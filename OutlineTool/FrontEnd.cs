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

	public void HandleInput(ConsoleKey input)
	{
		switch (input)
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
				if (this._selectingNewStoryBeat) { return; }
				this._selectedStoryBeatIndex = null;
			 	Console.SetCursorPosition(0, 20);
				Console.Write("New story beat name?");
				Console.SetCursorPosition(0, 21);
				var x = Console.ReadLine();
				this._currentStoryThread!.StoryBeats.Add(new()
				{
					Name = x!,
					Order = this._currentStoryThread.StoryBeats.Count
				});
				break;
			case ConsoleKey.Enter:
				if (this._currentStoryThread == null
					|| this._selectedStoryBeatIndex == null)
				{
					return;
				}
				if (this._selectingNewStoryBeat)
				{
					this._currentStoryThread
						.StoryBeats
						.Remove(this._storyBeatToMove!);

					this._currentStoryThread
						.StoryBeats.
						Insert(
							this._selectedStoryBeatIndex.Value,
							this._storyBeatToMove!);
					
					for (var i = this._selectedStoryBeatIndex.Value;
						i < this._currentStoryThread.StoryBeats.Count;
						i++)
					{
						this._currentStoryThread.StoryBeats[i].Order = i;
					}

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