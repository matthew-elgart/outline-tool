public class StoryThread
{
	public string Name { get; set; } = null!;

	public ConsoleColor? TextColor { get; set; }
	
	public List<StoryBeat> StoryBeats { get; set; } = null!;
}