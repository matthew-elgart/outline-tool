public class UndoRedoStack<T>
{
	private T[] _items = null!;
	private int _current = 0;
	private int _top = 0;
	private int _bottom = 0;

	public UndoRedoStack(T startingItem, int capacity)
	{
		this._items = new T[capacity];
		this._items[0] = startingItem;
	}

	public void AddToHistory(T item)
	{
		this._current = this.Increment(this._current);
		this._top = this._current;
		if (this._current == this._bottom)
		{
			this._bottom = this.Increment(this._bottom);
		}

		this._items[this._current] = item;
	}

	public T Undo()
	{
		if (this._current != this._bottom)
		{
			this._current = this.Decrement(this._current);
		}

		return this._items[this._current];
	}

	public T Redo()
	{
		if (this._current != this._top)
		{
			this._current = this.Increment(this._current);
		}

		return this._items[this._current];
	}

	private int Increment(int i) => (i + 1) % this._items.Length;
	private int Decrement(int i) =>
		(this._items.Length + i - 1) % this._items.Length;
}