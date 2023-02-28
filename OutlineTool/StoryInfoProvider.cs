public static class StoryInfoProvider
{
	public static Story Get()
	{
		var chapter1 = new Chapter { Name = "Introduction", Order = 0 };
		var chapter2 = new Chapter { Name = "Cooking Montage", Order = 1 };
		var chapter3 = new Chapter { Name = "Baseball Championship Game", Order = 2 };
		var chapter4 = new Chapter { Name = "Cooking Competition", Order = 3 };

		var story = new Story
		{
			Name = "Eddie's Million Dollar Cook-Off",
			Chapters = new()
			{
				chapter1,
				chapter2,
				chapter3,
				chapter4
			}
		};

		var thread1 = new StoryThread
		{
			Name = "Eddie improves at cooking",
			TextColor = ConsoleColor.Red
		};
		var thread2 = new StoryThread
		{
			Name = "Eddie's relationship with his dad",
			TextColor = ConsoleColor.DarkGreen,
		};

		var beat11 = new StoryBeat { Name = "Eddie learns about the Bobby Flay competition. And then does some other stuff, and then eats pancakes! And the pancakes are delicious, but you already know that. I don't think anyone needs to be convinced about how staggeringly incredible pancakes are.", Chapter = chapter1, Order = 0 };
		var beat12 = new StoryBeat { Name = "Eddie cooks a bunch of things in a montage", Chapter = chapter2, Order = 1 };
		var beat13 = new StoryBeat { Name = "Eddie wins the tournament", Chapter = chapter4, Order = 2 };
		thread1.StoryBeats = new() { beat11, beat12, beat13 };

		var beat21 = new StoryBeat { Name = "Eddie's dad disapproves of the Bobby Flay competition", Chapter = chapter1, Order = 0 };
		var beat22 = new StoryBeat { Name = "Eddie leaves the baseball game to go to the cooking competition", Chapter = chapter3, Order = 1 };
		var beat23 = new StoryBeat { Name = "Eddie's dad helps him crack eggs at the competition", Chapter = chapter4, Order = 2 };
		thread2.StoryBeats = new() { beat21, beat22, beat23 };

		chapter1.StoryBeats = new HashSet<StoryBeat>
		{
			thread1.StoryBeats[0],
			thread2.StoryBeats[0]
		};
		chapter2.StoryBeats = new HashSet<StoryBeat>
		{
			thread1.StoryBeats[1],
		};
		chapter3.StoryBeats = new HashSet<StoryBeat>
		{
			thread2.StoryBeats[1]
		};
		chapter4.StoryBeats = new HashSet<StoryBeat>
		{
			thread1.StoryBeats[2],
			thread2.StoryBeats[2]
		};

		story.Threads = new() { thread1, thread2 };
		
		return story;
	}
}