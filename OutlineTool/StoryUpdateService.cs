// fine if this becomes not static later
public static class StoryUpdateService
{
	public static void UpdateStoryBeatOrder(StoryBeat storyBeat, int index)
	{
		var storyThread = storyBeat.StoryThread;
		var storyBeats = storyThread.StoryBeats;

		if (!storyBeats.Contains(storyBeat))
		{
			throw new ArgumentException("Something has gone horribly wrong! StoryBeat is not in the list for its associated StoryThread");
		}
		if (index < 0 || index >= storyBeats.Count)
		{
			throw new IndexOutOfRangeException($"Index {index} out of range for story thread {storyThread.Name}");
		};

		storyBeats.Remove(storyBeat);
		InsertBeatInternal(index, storyBeat, storyThread);
	}

	public static void AddStoryBeat(
		int index,
		string name,
		StoryThread storyThread)
	{
		if (index < 0 || index > storyThread.StoryBeats.Count)
		{
			throw new IndexOutOfRangeException($"Index {index} out of range for story thread {storyThread}");
		}

		var newStoryBeat = new StoryBeat
		{
			Name = name,
			StoryThread = storyThread
		};

		InsertBeatInternal(index, newStoryBeat, storyThread);
	}

	public static void RenameStoryBeat(
		int index,
		string newName,
		StoryThread storyThread)
	{
		if (index < 0 || index >= storyThread.StoryBeats.Count)
		{
			throw new IndexOutOfRangeException($"Index {index} out of range for story thread {storyThread}");
		}

		storyThread.StoryBeats[index].Name = newName;
	}

	public static void AssignStoryBeatToChapter(
		StoryBeat storyBeat,
		Chapter chapter)
	{
		var existingChapter = storyBeat.Chapter;
		if (existingChapter != null
			&& !existingChapter.StoryBeats.Contains(storyBeat))
		{
			throw new ArgumentException("Something has gone horribly wrong! StoryBeat is not in the list for its associated Chapter");
		}

		existingChapter?.StoryBeats.Remove(storyBeat);
		storyBeat.Chapter = chapter;
		chapter.StoryBeats.Add(storyBeat);
	}

	private static void InsertBeatInternal(
		int index,
		StoryBeat storyBeat,
		StoryThread storyThread)
	{
		var storyBeats = storyThread.StoryBeats;
		storyBeats.Insert(index, storyBeat);

		// ensure that the Order property on the StoryBeats remains correct
		for (var i = 0; i < storyBeats.Count; i++)
		{
			storyBeats[i].Order = i;
		}
	}
}