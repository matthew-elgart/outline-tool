public class Chapter : IOrderedElement
{
	public string Name { get; set; } = null!;

	public int Order { get; set; }

	// associated to StoryBeat.Chapter
	public ICollection<StoryBeat> StoryBeats { get; set; } = new HashSet<StoryBeat>();

	public Chapter DeepCopyAndAssociateBeats(
		Dictionary<StoryBeat, StoryBeat> dictionary)
	{
		var chapterCopy = new Chapter()
		{
			Name = this.Name,
			Order = this.Order
		};

		var storyBeatsCopy = new HashSet<StoryBeat>();
		foreach (var storyBeat in this.StoryBeats)
		{
			var beatCopy = dictionary[storyBeat];
			storyBeatsCopy.Add(beatCopy);
			beatCopy.Chapter = chapterCopy;
		}

		chapterCopy.StoryBeats = storyBeatsCopy;
		return chapterCopy;
	}
}