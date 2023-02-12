public class Story
{
	public string Name { get; set; } = null!;

	// associated to Chapter.Order
	public List<Chapter> Chapters { get; set; } = null!;

	public List<StoryThread> Threads { get; set; } = null!;
}