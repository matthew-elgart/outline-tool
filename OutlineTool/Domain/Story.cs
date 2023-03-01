public class Story
{
	public string Name { get; set; } = null!;

	// associated to Chapter.Order
	public OrderedElementList<Chapter> Chapters { get; set; } = null!;

	public OrderedElementList<StoryThread> Threads { get; set; } = null!;
}