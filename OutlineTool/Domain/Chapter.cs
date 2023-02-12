public class Chapter
{
	public string Name { get; set; } = null!;

	// associated to order in Story.Chapters
	public int Order { get; set; }

	// associated to StoryBeat.Chapter
	public ICollection<StoryBeat> StoryBeats { get; set; } = null!;
}