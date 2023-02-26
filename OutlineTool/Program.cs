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
			- threads
				* add beat to thread
					* refactor this like talking with michael
				* "drag" beat within thread
				* rename thread
				+ delete beat (with confirmation)
				- add a new thread
				- switch between threads to view
				- delete thread (with confirmation)
			- chapters
				+ add chapter
				+ "drag" chapter around
				- filter threads to display beats for? (stretch/not sure if worth)
				+ delete chapter (with confirmation)
				+ rename chapter
			* assign beat to chapter
				* change "selecting new" to just be computed property
				* to start, consider not enforcing consistent order between threads/chapters
					- if it's valuable, could offer a warning when dragging beats in a thread, and then unassigning from chapter
					- and when moving between chapters, could offer a warning and then auto update thread order
			- toggle colors
		- display errors better
		- make all those user actions look nice
			- decide on how to highlight and stuff
			- nano-esque "user input area" at the bottom?
				- maybe easy to build on thread/chapter situation
			- bug: when not rendering on a spot, nothing gets erased
		- consider updating render only when things are added
			- may not need to bother with async at all
			- can track previous console dimensions, and only render when they change (or on input)
		- scrolling for rendering?
	*/
	private static async Task Main(string[] args)
	{
		Console.Clear();
		Console.CursorVisible = false;

		var tickRate = TimeSpan.FromMilliseconds(100);
		var story = StoryInfoProvider.Get();
		var frontEnd = new FrontEnd(story);
		var exit = false;

		var handlingInput = false;

		using var cts = new CancellationTokenSource();
		async Task MonitorKeyPresses()
		{
			while (!cts.Token.IsCancellationRequested)
			{
				if (Console.KeyAvailable)
				{
					handlingInput = true;
					var input = Console.ReadKey(intercept: true);

					if (input.Key == ConsoleKey.Escape)
					{ 
						exit = true;
						return;
					}
					
					frontEnd.HandleInput(input);
				}

				handlingInput = false;

				await Task.Delay(10);
			}
		}

		var monitorKeyPresses = MonitorKeyPresses();

		do
		{
			if (!handlingInput)
			{
				frontEnd.Render(story);
			}

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