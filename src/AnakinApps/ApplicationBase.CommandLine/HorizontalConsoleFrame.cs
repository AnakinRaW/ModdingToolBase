using System;
using System.IO;
using System.Text;
#if NETSTANDARD2_0
using System.Linq;
#endif

namespace AnakinRaW.ApplicationBase
{
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

        private sealed class RedrawingTextWriter(
            char lineChar,
            int length,
            TextWriter originalOut,
            TextReader originalIn)
            : TextWriter
        {
            // ANSI Escape Sequences
            private static readonly string AnsiCursorUp1 = "\e[1A";        // Move cursor up 1 line
            private static readonly string AnsiCursorDown1 = "\e[1B";      // Move cursor down 1 line
            private static readonly string AnsiCursorToColumn0 = "\e[0G";   // Move cursor to column 0
            private static readonly string AnsiSaveCursor = "\e[s";         // Save cursor position
            private static readonly string AnsiRestoreCursor = "\e[u";      // Restore cursor position
            private static readonly string AnsiClearLine = "\e[2K";         // Clear entire line

            private bool _isOnContentLine;
            private bool _hasContentOnCurrentLine;

            public override Encoding Encoding => originalOut.Encoding;

            private void WriteBottomLineWithDefaultColor()
            {
                var foregroundColor = Console.ForegroundColor;
                var backgroundColor = Console.BackgroundColor;

                Console.ResetColor();
                originalOut.Write(new string(lineChar, length));

                Console.ForegroundColor = foregroundColor;
                Console.BackgroundColor = backgroundColor;
            }

            private void WriteBottomLineNewLineWithDefaultColor()
            {
                var foregroundColor = Console.ForegroundColor;
                var backgroundColor = Console.BackgroundColor;

                Console.ResetColor();
                originalOut.WriteLine(new string(lineChar, length));

                Console.ForegroundColor = foregroundColor;
                Console.BackgroundColor = backgroundColor;
            }

            internal string? ReadLine()
            {
                // Don't move cursor position for input - keep it where it is
                EnsureBottomLineForInput();

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

                // After input, redraw the bottom line
                WriteBottomLineNewLineWithDefaultColor();
                _isOnContentLine = false;
                _hasContentOnCurrentLine = false;

                // Return null if Console.ReadLine() returned null (EOF or redirected input)
                return input;
            }

            private void EnsureBottomLineForInput()
            {
                if (_isOnContentLine && _hasContentOnCurrentLine)
                {
                    // We have content on the current line, just ensure bottom line is below
                    // Don't move the cursor - keep it where it is for inline input
                    originalOut.Write(AnsiSaveCursor);
                    originalOut.Write(AnsiCursorDown1);
                    originalOut.Write(AnsiCursorToColumn0);
                    WriteBottomLineWithDefaultColor();
                    originalOut.Write(AnsiRestoreCursor);
                }
                else if (_isOnContentLine)
                {
                    // No content on current line, add bottom border
                    WriteBottomLineNewLineWithDefaultColor();
                    originalOut.Write(AnsiCursorUp1);
                    originalOut.Write(AnsiCursorToColumn0);
                }
                else
                {
                    // Not on content line, just ensure bottom border is there
                    WriteBottomLineNewLineWithDefaultColor();
                    originalOut.Write(AnsiCursorUp1);
                    originalOut.Write(AnsiCursorToColumn0);
                }
            }

            private void PrepareForContent()
            {
                if (!_isOnContentLine)
                {
                    // Move up one line to overwrite the bottom border
                    originalOut.Write(AnsiCursorUp1);
                    originalOut.Write(AnsiCursorToColumn0);
                    originalOut.Write(AnsiClearLine);
                    _isOnContentLine = true;
                    _hasContentOnCurrentLine = false;
                }
            }

            public void EnsureProperDispose()
            {
                if (_isOnContentLine)
                {
                    if (_hasContentOnCurrentLine)
                    {
                        originalOut.WriteLine();
                    }
                    WriteBottomLineNewLineWithDefaultColor();
                    _isOnContentLine = false;
                    _hasContentOnCurrentLine = false;
                }
            }

            public override void Write(char value)
            {
                PrepareForContent();
                originalOut.Write(value);
                _hasContentOnCurrentLine = true;

                // If we wrote a newline, we need to handle the bottom line specially
                if (value == '\n')
                {
                    WriteBottomLineNewLineWithDefaultColor();
                    _isOnContentLine = false;
                    _hasContentOnCurrentLine = false;
                }
                else
                {
                    // For regular characters, ensure bottom line is visible
                    RedrawBottomLineInPlace();
                }
            }

            public override void Write(string value)
            {
                if (string.IsNullOrEmpty(value)) return;

                PrepareForContent();
                originalOut.Write(value);
                _hasContentOnCurrentLine = true;

                // Check if the string contains newlines
                if (value.Contains('\n'))
                {
                    WriteBottomLineNewLineWithDefaultColor();
                    _isOnContentLine = false;
                    _hasContentOnCurrentLine = false;
                }
                else
                {
                    // For text that doesn't end with newline, ensure bottom line is visible
                    RedrawBottomLineInPlace();
                }
            }

            private void RedrawBottomLineInPlace()
            {
                if (_isOnContentLine)
                {
                    originalOut.Write(AnsiSaveCursor);
                    originalOut.Write(AnsiCursorDown1);
                    originalOut.Write(AnsiCursorToColumn0);
                    WriteBottomLineWithDefaultColor();
                    originalOut.Write(AnsiRestoreCursor);
                }
            }

            public override void WriteLine()
            {
                PrepareForContent();
                originalOut.WriteLine();
                WriteBottomLineNewLineWithDefaultColor();
                _isOnContentLine = false;
                _hasContentOnCurrentLine = false;
            }

            public override void WriteLine(string value)
            {
                PrepareForContent();
                originalOut.WriteLine(value);
                WriteBottomLineNewLineWithDefaultColor();
                _isOnContentLine = false;
                _hasContentOnCurrentLine = false;
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
}