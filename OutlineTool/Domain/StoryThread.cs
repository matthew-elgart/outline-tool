public class StoryThread
{
	public string Name { get; set; } = null!;

	public ConsoleColor? TextColor { get; set; }
	
	public OrderedElementList<StoryBeat> StoryBeats { get; set; } = null!;
}