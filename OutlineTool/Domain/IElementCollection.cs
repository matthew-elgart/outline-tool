public interface IElementCollection<T> where T : IOrderedElement
{
	public List<T> Elements { get; set; }
}