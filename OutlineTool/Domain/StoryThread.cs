public class StoryThread : IOrderedElement
{
	public string Name { get; set; } = null!;

	public int Order { get; set; }

	public ConsoleColor? TextColor { get; set; }
	
	public OrderedElementList<StoryBeat> StoryBeats { get; set; } = new();

	public StoryThread DeepCopy(Dictionary<StoryBeat, StoryBeat> dictionary)
	{
		var storyBeatsCopy = new OrderedElementList<StoryBeat>();
		foreach (var storyBeat in this.StoryBeats)
		{
			var copy = storyBeat.DeepCopyWithoutChapter();
			// We shouldn't see a new one, so .Add() to throw if we do
			dictionary.Add(storyBeat, copy);
			storyBeatsCopy.Add(copy);
		}

		return new()
		{
			Name = this.Name,
			Order = this.Order,
			TextColor = this.TextColor,
			StoryBeats = storyBeatsCopy
		};
	}
}