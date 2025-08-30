using System;
using System.IO;
using System.Text;

namespace AnakinRaW.ApplicationBase;

public sealed class HorizontalConsoleFrame : IDisposable
{
    private readonly bool _newLineAtEnd;
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;
    private readonly TextReader _originalIn;
    private readonly FrameTextReader _frameReader;
    private readonly RedrawingTextWriter _writer;
    private bool _isDisposed;

    public HorizontalConsoleFrame(char lineChar, int length, bool startWithNewLine, bool newLineAtEnd)
    {
        _newLineAtEnd = newLineAtEnd;
        _originalOut = Console.Out;
        _originalError = Console.Error;
        _originalIn = Console.In;

        if (startWithNewLine)
            Console.WriteLine();

        Console.WriteLine(new string(lineChar, length));
        Console.WriteLine(new string(lineChar, length));

        _writer = new RedrawingTextWriter(lineChar, length, _originalOut, _originalIn);
        _frameReader = new FrameTextReader(_writer);

        Console.SetOut(_writer);
        Console.SetError(_writer);
        Console.SetIn(_frameReader);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
        Console.SetIn(_originalIn);

        _writer.EnsureProperDispose();

        if (_newLineAtEnd)
            Console.WriteLine();

        _frameReader.Dispose();
    }

    private sealed class FrameTextReader(RedrawingTextWriter writer) : TextReader
    {
        public override string? ReadLine()
        {
            return writer.ReadLine();
        }

        public override int Read()
        {
            var line = ReadLine();
            return line?.Length > 0 ? line[0] : -1;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            var line = ReadLine();
            if (string.IsNullOrEmpty(line))
                return 0;

            var charsToCopy = Math.Min(count, line.Length);
            line.CopyTo(0, buffer, index, charsToCopy);
            return charsToCopy;
        }
    }

    private sealed class RedrawingTextWriter(char lineChar, int length, TextWriter originalOut, TextReader originalIn)
        : TextWriter
    {
        // ANSI Escape Sequences
        private static readonly string AnsiCursorUp1 = "\e[1A";        // Move cursor up 1 line
        private static readonly string AnsiCursorUp2 = "\e[2A";        // Move cursor up 2 lines
        private static readonly string AnsiCursorDown1 = "\e[1B";      // Move cursor down 1 line
        private static readonly string AnsiCursorToColumn0 = "\e[0G";   // Move cursor to column 0
        private static readonly string AnsiSaveCursor = "\e[s";         // Save cursor position
        private static readonly string AnsiRestoreCursor = "\e[u";      // Restore cursor position

        private bool _isOnContentLine;
        private bool _bottomLineDrawn;

        public override Encoding Encoding => originalOut.Encoding;

        internal string? ReadLine()
        {
            if (!_isOnContentLine)
            {
                // Move cursor up 1 line
                originalOut.Write(AnsiCursorUp1);

                // Clear the current line with spaces
                originalOut.Write(new string(' ', Math.Min(length, Console.WindowWidth - 1)));

                // Move cursor left to start of line
                originalOut.Write(GetAnsiMoveLeftSequence(Math.Min(length, Console.WindowWidth - 1)));

                originalOut.WriteLine();
                originalOut.WriteLine(new string(lineChar, length));

                // Move cursor up 2 lines
                originalOut.Write(AnsiCursorUp2);

                _isOnContentLine = true;
                _bottomLineDrawn = true;
            }

            Console.SetOut(originalOut);
            Console.SetError(originalOut);
            Console.SetIn(originalIn);

            string? input;
            try
            {
                input = Console.ReadLine();
            }
            finally
            {
                Console.SetOut(this);
                Console.SetError(this);
                Console.SetIn(new FrameTextReader(this));
            }

            originalOut.WriteLine(new string(lineChar, length));
            _isOnContentLine = false;
            _bottomLineDrawn = true;
            return input;
        }

        private void PrepareForContent()
        {
            if (!_isOnContentLine)
            {
                // Move up one line to overwrite the bottom border
                originalOut.Write(AnsiCursorUp1);

                // Clear the line with spaces
                originalOut.Write(new string(' ', Math.Min(length, Console.WindowWidth - 1)));

                // Move cursor back to start of line
                originalOut.Write(GetAnsiMoveLeftSequence(Math.Min(length, Console.WindowWidth - 1)));

                _isOnContentLine = true;
                _bottomLineDrawn = false;
            }
        }

        public void EnsureProperDispose()
        {
            if (_isOnContentLine && !_bottomLineDrawn)
            {
                originalOut.WriteLine();
                originalOut.WriteLine(new string(lineChar, length));
                _isOnContentLine = false;
                _bottomLineDrawn = true;
            }
        }

        public override void Write(char value)
        {
            PrepareForContent();
            originalOut.Write(value);

            if (value == '\n' || Console.CursorLeft >= Console.WindowWidth - 1) 
                RedrawBottomLine();
        }

        public override void Write(string? value)
        {
            if (string.IsNullOrEmpty(value)) return;

            PrepareForContent();
            originalOut.Write(value);

            if (!_bottomLineDrawn) 
                RedrawBottomLine();
        }

        private void RedrawBottomLine()
        {
            if (_isOnContentLine && !_bottomLineDrawn)
            {
                originalOut.Write(AnsiSaveCursor);      // Save cursor position
                originalOut.Write(AnsiCursorDown1);     // Move cursor down 1 line  
                originalOut.Write(AnsiCursorToColumn0); // Move cursor to column 0

                var currentColor = Console.ForegroundColor;
                Console.ResetColor();
                originalOut.Write(new string(lineChar, length));
                Console.ForegroundColor = currentColor;

                originalOut.Write(AnsiRestoreCursor);   // Restore cursor position
                _bottomLineDrawn = true;
            }
        }

        /// <summary>
        /// Gets ANSI escape sequence to move cursor left by specified positions.
        /// Caches common values to avoid repeated string allocations.
        /// </summary>
        private static string GetAnsiMoveLeftSequence(int positions)
        {
            // Cache common window widths to avoid repeated allocations
            return positions switch
            {
                40 => "\e[40D",
                50 => "\e[50D",
                80 => "\e[80D",
                120 => "\e[120D",
                _ => $"\e[{positions}D" // Fallback for uncommon widths
            };
        }

        public override void WriteLine()
        {
            PrepareForContent();
            originalOut.WriteLine();
            originalOut.WriteLine(new string(lineChar, length));
            _isOnContentLine = false;
            _bottomLineDrawn = true;
        }

        public override void WriteLine(string? value)
        {
            PrepareForContent();
            originalOut.WriteLine(value);
            originalOut.WriteLine(new string(lineChar, length));
            _isOnContentLine = false;
            _bottomLineDrawn = true;
        }

        public override void Flush()
        {
            originalOut.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) 
                EnsureProperDispose();
            base.Dispose(disposing);
        }
    }
}