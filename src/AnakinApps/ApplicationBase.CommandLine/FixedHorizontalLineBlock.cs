using System;
using System.IO;
using System.Text;

namespace AnakinRaW.ApplicationBase;

public sealed class FixedHorizontalLineBlock : IDisposable
{
    private readonly bool _newLineAtEnd;
    private bool _isDisposed;
    public TextWriter Writer { get; }

    public FixedHorizontalLineBlock(char lineChar, int length, bool startWithNewLine, bool newLineAtEnd)
    {
        _newLineAtEnd = newLineAtEnd;

        if (startWithNewLine)
            Console.WriteLine();

        Console.WriteLine(new string(lineChar, length));
        Console.WriteLine(new string(lineChar, length));

        Writer = new RedrawingTextWriter(lineChar, length);
    }

    public void Write(string? value) => Writer.Write(value);
    public void WriteLine(string? value) => Writer.WriteLine(value);
    public void WriteLine() => Writer.WriteLine();

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        // Ensure we're properly positioned after the bottom line
        ((RedrawingTextWriter)Writer).EnsureProperDispose();

        if (_newLineAtEnd)
            Console.WriteLine();
    }

    private sealed class RedrawingTextWriter(char lineChar, int length) : TextWriter
    {
        public override Encoding Encoding => Console.Out.Encoding;
        private bool _isOnContentLine;

        public void EnsureProperDispose()
        {
            if (_isOnContentLine)
            {
                // Move cursor to the line after the bottom line (+2: +1 to bottom line, +1 to after bottom line)
                Console.SetCursorPosition(0, Console.CursorTop + 2);
                _isOnContentLine = false;
            }
        }

        public override void Write(char value)
        {
            if (!_isOnContentLine)
            {
                // Remove bottom line and position for content
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine(new string(' ', length));
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                _isOnContentLine = true;
            }

            Console.Write(value);

            // Redraw bottom line (save current position, draw line, restore position)
            var currentLeft = Console.CursorLeft;
            Console.WriteLine(); // move to next line
            Console.WriteLine(new string(lineChar, length));
            Console.SetCursorPosition(currentLeft, Console.CursorTop - 2); // back to content line
        }

        public override void Write(string? value)
        {
            if (string.IsNullOrEmpty(value)) return;

            foreach (char c in value)
            {
                Write(c);
            }
        }

        public override void WriteLine()
        {
            if (!_isOnContentLine)
            {
                // Remove bottom line and position for content
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine(new string(' ', length));
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            }

            Console.WriteLine();
            Console.WriteLine(new string(lineChar, length));
            _isOnContentLine = false;
        }

        public override void WriteLine(string? value)
        {
            if (!_isOnContentLine)
            {
                // Remove bottom line and position for content
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine(new string(' ', length));
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            }

            Console.WriteLine(value);
            Console.WriteLine(new string(lineChar, length));
            _isOnContentLine = false;
        }
    }
}