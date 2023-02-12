using System.Text.Json;
using System.Text.Json.Serialization;

public class Program
{
	/* todos:
		* set up entities
		* create test data
		* render thread
		* render full story
		* accept input to switch between story and each thread
		* rename test data to be comprehensible
		* fix console flickering
		* parameterize rendering by width (and height?)
		* add bidirectional navigation properties
		* word wrap
			* clean up that code...
			* investigate why the two spaces on top aren't showing up
			* consider just using a 2D array for render buffer
				(couldn't see much gain, and it feels more intuitive to add to this.Lines than update a pointer)
		* saving/loading (while making sure to handle navigation props)
			- factor to its own service to share options
			- let user input name? maybe not yet
		- user actions (while handling navigation props)
			- add chapter
			- add beat to thread
		- consider updating render only when things are added
		- scrolling for rendering?
	*/
	private static async Task Main(string[] args)
	{
		Console.Clear();
		Console.CursorVisible = false;

		var tickRate = TimeSpan.FromMilliseconds(100);
		var story = StoryInfoProvider.Get();
		var whatToRender = 0;
		var exit = false;

		using var cts = new CancellationTokenSource();
		async Task MonitorKeyPresses()
		{
			while (!cts.Token.IsCancellationRequested)
			{
				if (Console.KeyAvailable)
				{
					var key = Console.ReadKey(intercept: true).Key;

					if (key == ConsoleKey.Escape)
					{ 
						exit = true;
						return;
					}
					
					whatToRender = key switch
					{
						ConsoleKey.D1 => 0,
						ConsoleKey.D2 => 1,
						ConsoleKey.D3 => 2,
						ConsoleKey.S => 3,
						ConsoleKey.L => 4,
						_ => -1
					};
				}

				await Task.Delay(10);
			}
		}

		var monitorKeyPresses = MonitorKeyPresses();

		var renderer = new TextRenderer(
			Console.WindowWidth,
			// a -1 may be necessary, since terminal seems to autoscroll when fully writing out the final line, which isn't what we want
			// changed to =4 to account for the 3 extra lines of the bash prompt (more important for development so we don't scroll the screen as we iterate)
			Console.WindowHeight - 4);

		do
		{
			renderer.Reset(Console.WindowWidth, Console.WindowHeight - 4);

			switch (whatToRender)
			{
				case 0:
					RenderStoryThread(story.Threads[0], renderer);
					break;
				case 1:
					RenderStoryThread(story.Threads[1], renderer);
					break;
				case 2:
					RenderStory(story, renderer);
					break;
				case 3:
					SaveStory(story);
					whatToRender = -1;
					break;
				case 4:
					story = LoadStory();
					whatToRender = -1;
					break;
			}

			renderer.RenderFrame();

			await Task.Delay(tickRate);
		} while (!exit);

		cts.Cancel();
		await monitorKeyPresses;
		Console.CursorVisible = true;
	}

	private static void RenderStoryThread(
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
				$"{beat.Name}{(beat.Chapter != null ? $" (Chapter {beat.Chapter.Order})" : "")}",
				indentation: 2);
		}
	}

	private static void RenderStory(Story story, TextRenderer renderer)
	{
		renderer.Print(story.Name);
		renderer.Print(new string('-', story.Name.Length));

		foreach (var chapter in story.Chapters)
		{
			var stringToPrint = $"{chapter.Order}. {chapter.Name}";

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

	private static void SaveStory(Story story)
	{
		var fileName = "test.json";

		var options = new JsonSerializerOptions
		{
			ReferenceHandler = ReferenceHandler.Preserve
		};
		var serializedStory = JsonSerializer.Serialize(
			story,
			options: options
		);

		File.WriteAllText(fileName, serializedStory);
	}

	private static Story LoadStory()
	{
		var fileName = "test.json";

		var options = new JsonSerializerOptions
		{
			ReferenceHandler = ReferenceHandler.Preserve
		};

		var serializedString = File.ReadAllText(fileName);
		var story = JsonSerializer.Deserialize<Story>(serializedString, options);

		if (story == null)
		{
			throw new InvalidOperationException("Failed to load story!");
		}

		return story;
	}
}