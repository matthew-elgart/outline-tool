public class Story
{
	public string Name { get; set; } = null!;

	// associated to Chapter.Order
	public OrderedElementList<Chapter> Chapters { get; set; } = new();

	public OrderedElementList<StoryThread> Threads { get; set; } = new();

	public string? SaveFileLocation { get; set; } = null!;
}