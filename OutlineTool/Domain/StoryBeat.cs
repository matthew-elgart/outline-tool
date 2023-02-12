public class StoryBeat
{
	public string Name { get; set; } = null!;

	// associated to Chapter.StoryBeats
	public Chapter? Chapter { get; set; }

	// associated to StoryThread.StoryBeats
	public int Order { get; set; }

	// associated to StoryThread.StoryBeats
	public StoryThread StoryThread { get; set; } = null!;
}