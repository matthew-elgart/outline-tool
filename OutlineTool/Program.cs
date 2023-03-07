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
			* threads
				* add beat to thread
					* refactor this like talking with michael
				* "drag" beat within thread
				* rename thread
				* delete beat (with confirmation)
				* add a new thread
					* specify color
				* "drag" threads around
				* switch between threads to view
				* delete thread (with confirmation)
			- chapters
				* add chapter
				* "drag" chapter around
				* delete chapter (with confirmation)
				* rename chapter
				- filter threads to display beats for? (stretch/not sure if worth)
			* assign beat to chapter
				* change "selecting new" to just be computed property
				* to start, consider not enforcing consistent order between threads/chapters
					- if it's valuable, could offer a warning when dragging beats in a thread, and then unassigning from chapter
					- and when moving between chapters, could offer a warning and then auto update thread order
			* toggle colors
		- cleanup frontend state, consider moving everything to functions
			* factor out cursor state
			* factor out state for displayed stuff?
			- consider what it would take to split out frontend rendering from frontend state
		* GetConfiguredRenderers can't be the best way to do that lol
		* scrolling for rendering?
		- make all those user actions look nice
			* decide on how to highlight and stuff
			- nano-esque "user input area" at the bottom?
				- maybe easy to build on thread/chapter situation
			- bug: when not rendering on a spot, nothing gets erased
		- consider giving display access to parent (in a partial class)
		- take a pass to rethink any keybinds
		* consider updating render only when things are added
			* may not need to bother with async at all
				* update: should definitely do this. Parallelism is causing ~a bug~ bugs
			* can track previous console dimensions, and only render when they change (or on input)
		- display errors better (maybe not)
	*/
	private static async Task Main(string[] args)
	{
		var filePath = args.SingleOrDefault();
		Story story;

		if (filePath == null)
		{
			Console.WriteLine("No save file was given (in order to load a pre-existing story, provide the save file as a console argument). Proceeding with a new story.");
			Console.WriteLine("What is the title of your story?");
			Console.Write("> ");

			var title = Console.ReadLine();
			if (title == string.Empty) { return; }
			story = new() { Name = title! };
		}
		else
		{
			try
			{
				story = LoadStory(filePath);
			}
			catch
			{
				Console.WriteLine($"Could not load file {filePath}. You'll probably want to git gud and try again.");
				return;
			}
		}

		Console.Clear();
		Console.CursorVisible = false;

		var tickRate = TimeSpan.FromMilliseconds(100);
		var frontEnd = new FrontEnd(story);
		var exit = false;

		var prevConsoleHeight = -1;
		var prevConsoleWidth = -1;

		do
		{
			var shouldRender = false;

			if (Console.KeyAvailable)
			{
				var input = Console.ReadKey(intercept: true);

				if (input.Key == ConsoleKey.Escape)
				{ 
					exit = true;
					return;
				}

				frontEnd.HandleInput(input);
				shouldRender = true;
			}
			else
			{
				var newHeight = Console.WindowHeight;
				var newWidth = Console.WindowWidth;
				if (newHeight != prevConsoleHeight || newWidth != prevConsoleWidth)
				{
					shouldRender = true;
					prevConsoleHeight = newHeight;
					prevConsoleWidth = newWidth;
				}
			}

			if (shouldRender) { frontEnd.Render(); }

			await Task.Delay(tickRate);
		} while (!exit);

		// so terminal prompt shows up at the bottom without any scrolling
		Console.SetCursorPosition(0, Console.WindowHeight - 3);
		Console.CursorVisible = true;
	}

	//private static void SaveStory(Story story)
	//{
		//var fileName = "test.json";

		//var options = new JsonSerializerOptions
		//{
			//ReferenceHandler = ReferenceHandler.Preserve
		//};
		//var serializedStory = JsonSerializer.Serialize(
			//story,
			//options: options
		//);

		//File.WriteAllText(fileName, serializedStory);
	//}

	private static Story LoadStory(string fileName)
	{
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