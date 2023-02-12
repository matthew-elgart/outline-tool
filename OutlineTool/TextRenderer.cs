public class TextRenderer
{
	private const int whitespaceOnTop = 2;

	private int _width;
	private int _height;
	private List<ColoredString> Lines;

	public TextRenderer(int width, int height)
	{
		this._width = width;
		this._height = height;
		this.Lines = new();
		this.Reset();
	}

	/// <summary>
	/// Clears the renderer's text buffer, and optionally update its
	/// dimensions.
	/// </summary>
	public void Reset(int? width = null, int? height = null)
	{
		this._width = width ?? this._width;
		this._height = height ?? this._height;

		this.Lines.Clear();
		foreach (var i in Enumerable.Range(0, whitespaceOnTop))
		{
			this.Print();
		}
	}

	/// <summary>
	/// Writes the text to the next line in the specified color. If the text is
	/// longer than the renderer's width, it will wrap the text into multiple
	/// lines, preferring to do so on whitespace if possible.
	/// </summary>
	public void Print(
		string? text = null,
		int indentation = 0,
		ConsoleColor? color = null,
		bool hasWrapped = false)
	{
		var textLength = text?.Length ?? 0;

		// denote that we are wrapping text from the previous line by giving
		//  a bit extra indentation.
		// ^ just like that!
		var indentationWithWrap = hasWrapped ? indentation + 1 : indentation;

		// if we don't need to wrap, just proceed
		if (textLength + indentationWithWrap <= this._width)
		{
			this.AddToBuffer(text, indentationWithWrap, color);
			return;
		}

		// maximal substring we could put on this line
		var substring = text!.Substring(0, this._width - indentationWithWrap);
		var lastWhitespaceIndex = substring.LastIndexOf(' ');

		// if there's whitespace to break on, only print that far;
		// otherwise, print as much as we have room for
		var lengthToPrintOnCurrentLine = lastWhitespaceIndex >= 0
			? lastWhitespaceIndex
			: this._width - indentationWithWrap;
		this.AddToBuffer(
			text.Substring(0, lengthToPrintOnCurrentLine),
			indentationWithWrap,
			color
		);

		// now recursively print the carryover string. If we broke on whitespace,
		// we don't need to start the next line with whitespace, so just skip it;
		// otherwise, pick up right where we left off.
		var indexToStartNextLineWith = lastWhitespaceIndex >= 0
			? lengthToPrintOnCurrentLine + 1
			: lengthToPrintOnCurrentLine;
		this.Print(
			text.Substring(
				indexToStartNextLineWith,
				text.Length - indexToStartNextLineWith
			),
			indentation,
			color,
			hasWrapped: true
		);
		return;
	}

	private void AddToBuffer(
		string? text = null,
		int indentation = 0,
		ConsoleColor? color = null)
	{
		var textLength = text?.Length ?? 0;

		if (textLength + indentation > this._width)
		{
			throw new ArgumentException("Text exceeded render frame width");
		}

		var lineText = new char[this._width];
		for (var i = 0; i < this._width; i++)
		{
			if (i < indentation || i >= textLength + indentation)
			{
				lineText[i] = ' ';
			}
			else
			{
				lineText[i] = text![i - indentation];
			}
		}

		this.Lines.Add(new(lineText, color ?? ConsoleColor.Gray));
	}

	/// <summary>
	/// Writes the text in the buffer to the console. It is the caller's
	/// responsibility to call <see cref="Reset"/> afterwards to clear the
	/// buffer (and potentially readjust the Renderer's dimensions)
	/// </summary>
	public void RenderFrame()
	{
		// first, add blank lines to "render" - this is to overwrite any old text
		// with whitespace if the new frame is shorter
		var numBlankLinesToAdd = this._height - this.Lines.Count;
		if (numBlankLinesToAdd < 0)
		{
			throw new InvalidOperationException("Too many lines to render, and I don't know how to scroll!");
		}

		for (var i = 0; i < numBlankLinesToAdd; i++)
		{
			var blankLine = new char[this._width];
			for (var j = 0; j < this._width; j++)
			{
				blankLine[j] = ' ';
			}

			this.Lines.Add(new(blankLine));
		}

		// sanity check: can remove if I see this later
		if (this.Lines.Count != this._height)
		{
			throw new InvalidOperationException("shouldn't get here!");
		}

		// print!
		Console.SetCursorPosition(0,0);
		foreach (var line in this.Lines)
		{
			Console.ForegroundColor = line.Color;
			Console.Write(line.Text);
		}

		// thought it made sense to do this here, but put the responsibility
		// on the caller instead, so there was a chance to update dimensions
		// in between renders, without needing to reset twice
		// this.Reset();
	}

	private record ColoredString(
		char[] Text,
		ConsoleColor Color = ConsoleColor.Gray);
}