public class Chapter : IOrderedElement
{
	public string Name { get; set; } = null!;

	public int Order { get; set; }

	// associated to StoryBeat.Chapter
	public ICollection<StoryBeat> StoryBeats { get; set; } = new HashSet<StoryBeat>();
}