// fine if this becomes not static later
public static class StoryUpdateService
{
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
}

public interface IOrderedElementList
{
	Type GetElementType();
	void InsertNewElement(int index, string name);
	void UpdateElementOrder(int oldIndex, int newIndex);
	void DeleteElement(int index);
	int Count { get; }
	IOrderedElement this[int index] { get; }
}

public class OrderedElementList<T> : List<T>, IOrderedElementList
	where T : IOrderedElement, new()
{
	public Type GetElementType() => typeof(T);

	public void InsertNewElement(int index, string name)
	{
		if (index < 0 || index > this.Count)
		{
			throw new IndexOutOfRangeException($"Tried to create element \"{name}\" at index {index}, but it was out of range");
		}

		var element = new T();
		element.Name = name;
		this.Insert(index, element);

		this.RefreshElementOrders();
	}

	public void UpdateElementOrder(int oldIndex, int newIndex)
	{
		if (oldIndex < 0 || oldIndex >= this.Count)
		{
			throw new IndexOutOfRangeException($"Index {oldIndex} out of range for order update");
		}

		var element = this[oldIndex];

		if (newIndex < 0 || newIndex >= this.Count)
		{
			throw new IndexOutOfRangeException($"Index {newIndex} out of range when updating element {element.Name}");
		};

		this.Remove(element);
		this.Insert(newIndex, element);
		RefreshElementOrders();
	}

	public void DeleteElement(int index)
	{
		if (index < 0 || index >= this.Count)
		{
			throw new IndexOutOfRangeException($"Index {index} out of range for deletion");
		};

		var element = this[index];

		this.Remove(element);
		RefreshElementOrders();

		// storyBeats don't get removed from chapters for free; need
		// to do that ourselves
		if (element is StoryBeat storyBeat)
		{
			storyBeat.Chapter?.StoryBeats.Remove(storyBeat);
		}
		// similarly, chapters don't get removed from storyBeats for free
		else if (element is Chapter chapter)
		{
			foreach (var chapterBeat in chapter.StoryBeats)
			{
				chapterBeat.Chapter = null;
			}
		}
	}

	IOrderedElement IOrderedElementList.this[int index] => this[index];

	private void RefreshElementOrders()
	{
		// ensure that the Order property on the elements remains correct
		for (var i = 0; i < this.Count; i++)
		{
			this[i].Order = i;
		}
	}
}