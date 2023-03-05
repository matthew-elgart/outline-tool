using Spectre.Console;

public class TextRenderer
{
	// coordinates of the top left cell
	private int _xPosition;
	private int _yPosition;

	private int _width;
	private int _height;

	private int _previousWindowTop = 0;
	private int _headerSize = 0;

	private List<ColoredString> Lines = new();

	/// <summary>
	/// Clears the renderer's text buffer, and optionally update its
	/// position/dimensions.
	/// </summary>
	public void Reset(
		int? xPosition,
		int? yPosition,
		int? width = null,
		int? height = null)
	{
		this._xPosition = xPosition ?? this._xPosition;
		this._yPosition = yPosition ?? this._yPosition;
		this._width = width ?? this._width;
		this._height = height ?? this._height;

		this._headerSize = 0;

		this.Lines.Clear();
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
		bool highlighted = false,
		bool isHeader = false,
		bool arrow = false,
		bool hasWrapped = false)
	{
		// there needs to be space for the arrow :)
		if (arrow && indentation == 0 && text != null)
		{
			throw new ArgumentException("Can't put an arrow if there's no indentation");
		}

		// allow lines to be specified as "header" lines that are not
		// subject to scrolling. The header can be whatever size the
		// consumer wants - but all header lines must be contiguous
		// at the top
		if (isHeader)
		{
			if (this._headerSize != this.Lines.Count)
			{
				throw new ArgumentException("All header lines must be together at the top of the renderer");
			}

			this._headerSize++;
		}

		var textLength = text?.Length ?? 0;

		// denote that we are wrapping text from the previous line by giving
		//  a bit extra indentation.
		// ^ just like that!
		var indentationWithWrap = hasWrapped ? indentation + 1 : indentation;

		// if we don't need to wrap, just proceed
		if (textLength + indentationWithWrap <= this._width)
		{
			this.AddToBuffer(
				text,
				indentationWithWrap,
				color,
				highlighted,
				arrow);
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
			color,
			highlighted,
			arrow
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
			highlighted,
			isHeader,
			// explicitly false so arrow only shows up on top line
			arrow: false,
			hasWrapped: true
		);
		return;
	}

	private void AddToBuffer(
		string? text,
		int indentation,
		ConsoleColor? color,
		bool highlighted,
		bool arrow)
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

		this.Lines.Add(new(
			lineText,
			color ?? ConsoleColor.Gray,
			highlighted,
			arrow));
	}

	/// <summary>
	/// Writes the text in the buffer to the console. It is the caller's
	/// responsibility to call <see cref="Reset"/> afterwards to clear the
	/// buffer (and potentially readjust the Renderer's dimensions)
	/// </summary>
	public void RenderFrame()
	{
		if (this._headerSize > this._height)
		{
			throw new InvalidOperationException("Too many header lines that can't be scrolled");
		}

		// first, add blank lines to "render" - this is to overwrite any old text
		// with whitespace if the new frame is shorter
		this.AddBlankLinesIfNecessary();

		// print!
		var currentY = this._yPosition;

		// first, print the header lines (which will never scroll)
		for (var i = 0; i < this._headerSize; i++)
		{
			this.PrintLine(this.Lines[i], currentY);
			currentY++;
		}

		// then, determine what scrolling window to render, and print
		// those lines
		int windowTop = this.GetScrollingWindowTop();
		for (var i = windowTop;
			i < windowTop + this._height - this._headerSize;
			i++)
		{
			this.PrintLine(this.Lines[i], currentY);
			currentY++;
		}

		// save off window top for next render cycle
		this._previousWindowTop = windowTop;

		// thought it made sense to do this here, but put the responsibility
		// on the caller instead, so there was a chance to update dimensions
		// in between renders, without needing to reset twice
		// this.Reset();
	}

	private void AddBlankLinesIfNecessary()
	{
		var numBlankLinesToAdd = this._height - this.Lines.Count;
		if (numBlankLinesToAdd < 0) { return; }

		for (var i = 0; i < numBlankLinesToAdd; i++)
		{
			var blankLine = new char[this._width];
			for (var j = 0; j < this._width; j++)
			{
				blankLine[j] = ' ';
			}

			this.Lines.Add(new(blankLine, ConsoleColor.Gray));
		}
	}

	private int GetScrollingWindowTop()
	{
		var linesCount = this.Lines.Count;
		if (linesCount < this._height)
		{
			throw new InvalidOperationException($"There are fewer lines ({linesCount}) than the height of the text renderer ({this._height}), which shouldn't be possible");
		}

		if (linesCount == this._height) { return this._headerSize; }

		// the top of the scrolling window should always be below the
		// header. Therefore, if the header has grown, we move our window down to compensate
		var previousWindowTop = Math.Max(this._previousWindowTop, this._headerSize);
		var windowSize = this._height - this._headerSize;

		// if there are more lines than the renderer can display at once,
		// we need to scroll. Use highlighted lines to determine where to
		// scroll to. The algorithm:
		// - if there's no highlighted text, maintain the same window as before
		// - if the previous window already contains the highlighted text, don't move it
		// - if the top of the highlighted text is higher than the window, scroll up to accommodate it
		// - if the bottom of the highlighted text is lower than the window, scroll down to accommodate it
		var highlightedIndexes =
			Enumerable.Range(this._headerSize, linesCount - this._headerSize)
			.Where(i => this.Lines[i].Highlighted || this.Lines[i].Arrow)
			.ToList();
		if (!highlightedIndexes.Any()) { return previousWindowTop; }

		var minHighlightedIndex = highlightedIndexes.Min();
		var maxHighlightedIndex = highlightedIndexes.Max();

		// scrolling looks a little nicer if we leave some padding, so
		// do most of our logic against the padded min/max values (with
		// one exception below)
		const int Padding = 2;
		var paddedMinHighlightedIndex = Math.Max(
			minHighlightedIndex - Padding,
			this._headerSize);
		var paddedMaxHighlightedIndex = Math.Min(
			maxHighlightedIndex + Padding,
			linesCount - 1);

		// "or equals" for the case where highlighted text spans more
		// than the window can show. Without the equals check, the window
		// flips between showing the top and bottom of the highlighted
		// text on each frame. We arbitrarily choose to show the top of
		// the highlighted text in this case, and include the equals
		// check here
		if (paddedMinHighlightedIndex <= previousWindowTop)
		{
			// the exception to always using the padded values. If the
			// highlighted block is just on the edge of being too big,
			// it may fit within the window, but not when we pad. In
			// these cases, use the unpadded values so we can fit
			// everything into view
			var numHighlightedLines = maxHighlightedIndex
				- minHighlightedIndex
				+ 1;
			var numPaddedHighlightedLines = paddedMaxHighlightedIndex
				- paddedMinHighlightedIndex
				+ 1;
			if (numPaddedHighlightedLines > windowSize
				&& numHighlightedLines <= windowSize)
			{
				return maxHighlightedIndex;
			}

			return Math.Max(paddedMinHighlightedIndex, this._headerSize);
		}

		if (paddedMaxHighlightedIndex >= previousWindowTop + windowSize)
		{
			var difference = paddedMaxHighlightedIndex
				- previousWindowTop
				- windowSize
				// +1 to account for indexing. We display up to *but not
				// including* windowTop + windowSize
				+ 1;
			return previousWindowTop + difference;
		}

		return previousWindowTop;
	}

	private void PrintLine(ColoredString line, int currentY)
	{
		Console.SetCursorPosition(this._xPosition, currentY);

		var toPrint = new string(line.Text);
		if (line.Arrow)
		{
			toPrint = $"[bold white]>[/]{toPrint.Substring(1).EscapeMarkup()}";
		}

		var styles = Color.FromConsoleColor(line.Color).ToMarkup();
		if (line.Highlighted) { styles += " invert"; }

		AnsiConsole.Markup($"[{styles}]{toPrint}[/]");

		Console.ResetColor();
	}

	private record ColoredString(
		char[] Text,
		ConsoleColor Color,
		bool Highlighted = false,
		bool Arrow = false);
}