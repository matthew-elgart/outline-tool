using System.Collections;

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
			throw new ArgumentException($"Element not found in the provided list; cannot update order for Element \"{element.Name}\"");
		}
		if (index < 0 || index >= elements.Count)
		{
			throw new IndexOutOfRangeException($"Index {index} out of range for updating element {element.Name}");
		};

		elements.Remove(element);
		elements.Insert(index, element);
		RefreshElementOrders(elements);
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

		elements.Insert(index, newElement);
		RefreshElementOrders(elements);
	}

	public static void DeleteElement<T>(
		T element,
		IList<T> elements)
		where T : IOrderedElement
	{
		if (!elements.Contains(element))
		{
			throw new ArgumentException($"Element not found in the provided list; cannot delete Element \"{element.Name}\"");
		}

		elements.Remove(element);
		RefreshElementOrders(elements);

		// storyBeats don't get removed from chapters for free; need
		// to do that ourselves
		if (element is StoryBeat storyBeat)
		{
			storyBeat.Chapter?.StoryBeats.Remove(storyBeat);
		}
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

	public static void RefreshElementOrders<T>(IList<T> elements)
		where T : IOrderedElement
	{
		// ensure that the Order property on the elements remains correct
		for (var i = 0; i < elements.Count; i++)
		{
			elements[i].Order = i;
		}
	}
}

public interface IOrderedElementList : IList
{
	void InsertNewElement(int index, string name);
}

public class OrderedElementList<T> : List<T>, IOrderedElementList
	where T : IOrderedElement, new()
{
	public void InsertNewElement(int index, string name)
	{
		if (index < 0 || index > this.Count)
		{
			throw new IndexOutOfRangeException($"Tried to create element \"{name}\" at index {index}, but it was out of range");
		}

		var element = new T();
		element.Name = name;
		this.Insert(index, element);

		StoryUpdateService.RefreshElementOrders(this);
	}
}