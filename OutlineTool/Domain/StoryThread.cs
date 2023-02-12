public class StoryThread
{
	public string Name { get; set; } = null!;

	public ConsoleColor? TextColor { get; set; }
	
	// associated to StoryBeat.Order
	public List<StoryBeat> StoryBeats { get; set; } = null!;
}