using System;
using System.IO;
using System.Text;

namespace AnakinRaW.ApplicationBase;

public sealed class HorizontalConsoleFrame : IDisposable
{
    private readonly bool _newLineAtEnd;
    private bool _isDisposed;

    public TextWriter Writer { get; }

    public HorizontalConsoleFrame(char lineChar, int length, bool startWithNewLine, bool newLineAtEnd)
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

    public string? ReadLine()
    {
        var writer = (RedrawingTextWriter)Writer;
        return writer.ReadLine();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        ((RedrawingTextWriter)Writer).EnsureProperDispose();

        if (_newLineAtEnd)
            Console.WriteLine();
    }

    private sealed class RedrawingTextWriter(char lineChar, int length) : TextWriter
    {
        public override Encoding Encoding => Console.Out.Encoding;
        private bool _isOnContentLine;

        public string? ReadLine()
        {
            if (!_isOnContentLine)
            {
                // Move up to overwrite the bottom line and prepare content line
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine(new string(' ', Math.Min(length, Console.WindowWidth - 1)));
                // Draw the bottom line immediately
                Console.WriteLine(new string(lineChar, length));
                // Move cursor back to the content line (2 lines up)
                Console.SetCursorPosition(0, Console.CursorTop - 2);
                _isOnContentLine = true;
            }

            // Now the user can see the bottom line while typing
            string? input = Console.ReadLine();

            // After ReadLine, we need to position cursor AFTER the bottom line
            // so that the next content operation can work correctly
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine(new string(lineChar, length)); // Write bottom line and move cursor to next line

            _isOnContentLine = false;
            return input;
        }

        private void PrepareForContent()
        {
            if (!_isOnContentLine)
            {
                // Move up to overwrite the bottom line
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                // Clear the line and prepare for content
                Console.Write(new string(' ', Math.Min(length, Console.WindowWidth - 1)));
                Console.SetCursorPosition(0, Console.CursorTop);
                _isOnContentLine = true;
            }
        }


        public void EnsureProperDispose()
        {
            if (_isOnContentLine)
            {
                // Move cursor to after the bottom line
                Console.SetCursorPosition(0, Console.CursorTop + 1);
                Console.WriteLine(new string(lineChar, length));
                _isOnContentLine = false;
            }
        }

        public override void Write(char value)
        {
            PrepareForContent();
            Console.Write(value);

            // Only redraw bottom line when we reach end of line or buffer is flushed
            if (value == '\n' || Console.CursorLeft >= Console.WindowWidth - 1)
            {
                RedrawBottomLine();
            }
        }

        public override void Write(string? value)
        {
            if (string.IsNullOrEmpty(value)) return;

            PrepareForContent();
            Console.Write(value);
            RedrawBottomLine();
        }

        private void RedrawBottomLine()
        {
            if (_isOnContentLine)
            {
                var currentLeft = Console.CursorLeft;
                var currentTop = Console.CursorTop;

                // Move to next line and draw border
                Console.SetCursorPosition(0, currentTop + 1);

                var currentColor = Console.ForegroundColor;
                Console.ResetColor();
               
                Console.Write(new string(lineChar, length));
                
                Console.ForegroundColor = currentColor;
                // Return to content position
                Console.SetCursorPosition(currentLeft, currentTop);
            }
        }

        public override void WriteLine()
        {
            PrepareForContent();
            Console.WriteLine();
            Console.WriteLine(new string(lineChar, length));
            _isOnContentLine = false;
        }

        public override void WriteLine(string? value)
        {
            PrepareForContent();
            Console.WriteLine(value);
            Console.WriteLine(new string(lineChar, length));
            _isOnContentLine = false;
        }
    }
}