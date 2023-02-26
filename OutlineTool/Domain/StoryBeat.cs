public class StoryBeat : IOrderedElement
{
	public string Name { get; set; } = null!;

	// associated to Chapter.StoryBeats
	public Chapter? Chapter { get; set; }

	public int Order { get; set; }
}