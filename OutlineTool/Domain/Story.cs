public class Story
{
	public string Name { get; set; } = null!;

	// associated to Chapter.Order
	public OrderedElementList<Chapter> Chapters { get; set; } = new();

	public OrderedElementList<StoryThread> Threads { get; set; } = new();

	public string? SaveFileLocation { get; set; } = null!;

	public Story DeepCopy(ref StoryThread? storyThread)
	{
		var dictionary = new Dictionary<StoryBeat, StoryBeat>();

		var threadsCopy = new OrderedElementList<StoryThread>();
		foreach (var thread in this.Threads)
		{
			var threadCopy = thread.DeepCopy(dictionary);
			threadsCopy.Add(threadCopy);

			if (storyThread == thread) { storyThread = threadCopy; }
		}

		var chaptersCopy = new OrderedElementList<Chapter>();
		foreach (var chapter in this.Chapters)
		{
			chaptersCopy.Add(
				chapter.DeepCopyAndAssociateBeats(dictionary));
		}

		return new()
		{
			Name = this.Name,
			Chapters = chaptersCopy,
			Threads = threadsCopy
		};
	}
}