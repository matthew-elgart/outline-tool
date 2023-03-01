public class StoryThread : IOrderedElement
{
	public string Name { get; set; } = null!;

	public int Order { get; set; }

	public ConsoleColor? TextColor { get; set; }
	
	public OrderedElementList<StoryBeat> StoryBeats { get; set; } = new();
}