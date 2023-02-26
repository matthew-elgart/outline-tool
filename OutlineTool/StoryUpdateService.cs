// fine if this becomes not static later
public static class StoryUpdateService
{
	public static void UpdateElementOrder<T>(
		T element,
		int index,
		IList<T> elements)
		where T : IOrderedElement
	{
		if (!elements.Contains(element))
		{
			throw new ArgumentException("Element not found in the provided list; cannot update Element order");
		}
		if (index < 0 || index >= elements.Count)
		{
			throw new IndexOutOfRangeException($"Index {index} out of range for updating element {element.Name}");
		};

		elements.Remove(element);
		InsertBeatInternal(index, element, elements);
	}

	public static void AddElement<T>(
		int index,
		string name,
		List<T> elements)
		where T : IOrderedElement, new()
	{
		if (index < 0 || index > elements.Count)
		{
			throw new IndexOutOfRangeException($"Tried to create Element \"{name}\" at index {index}, but it was out of range");
		}

		var newElement = new T { Name = name };

		InsertBeatInternal(index, newElement, elements);
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

	private static void InsertBeatInternal<T>(
		int index,
		T element,
		IList<T> elements)
		where T : IOrderedElement
	{
		elements.Insert(index, element);

		// ensure that the Order property on the elements remains correct
		for (var i = 0; i < elements.Count; i++)
		{
			elements[i].Order = i;
		}
	}
}