using System;
using System.IO;

namespace AnakinRaW.ApplicationBase;

public static class ConsoleUtilities
{
    private const char DefaultLineChar = '─';
    private const int DefaultLineLength = 20;

    public delegate bool ConsoleQuestionValueFactory<T>(string input, out T value);

    public static void WriteHorizontalLine(char lineChar = DefaultLineChar, int length = DefaultLineLength)
    {
        var line = new string(lineChar, length);
        Console.WriteLine(line);
    }

    public static void WriteLineRight(string message, int lineLength)
    {
        if (message.Length >= lineLength)
            Console.WriteLine(message);
        else
            Console.WriteLine(new string(' ', lineLength - message.Length) + message);
    }

    public static IDisposable HorizontalLineSeparatedBlock(
        char lineChar = DefaultLineChar,
        int length = DefaultLineLength,
        bool startWithNewLine = false,
        bool newLineAtEnd = false)
    {
        return new InHorizontalLineBlock(lineChar, length, startWithNewLine, newLineAtEnd);
    }

    public static HorizontalConsoleFrame CreateHorizontalFrame(
        char lineChar = DefaultLineChar,
        int length = DefaultLineLength,
        bool startWithNewLine = false,
        bool newLineAtEnd = false)
    {
        return new HorizontalConsoleFrame(lineChar, length, startWithNewLine, newLineAtEnd);
    }

    private class InHorizontalLineBlock : IDisposable
    {
        private readonly char _lineChar;
        private readonly int _length;
        private readonly bool _nlEnd;

        public InHorizontalLineBlock(char lineChar = '─', int length = 20, bool nlStart = false, bool nlEnd = false)
        {
            _lineChar = lineChar;
            _length = length;
            _nlEnd = nlEnd;
            if (nlStart)
                Console.WriteLine();
            WriteHorizontalLine(lineChar, length);
        }

        public void Dispose()
        {
            WriteHorizontalLine(_lineChar, _length);
            if (_nlEnd)
                Console.WriteLine();
        }
    }

    public static void WriteApplicationFatalError(string appName, string? errorMessage = null, string? detailedError = null)
    {
        using (HorizontalLineSeparatedBlock('*', startWithNewLine: true, newLineAtEnd: true))
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($" {appName} Failure! ");
            Console.ResetColor();
        }
        Console.WriteLine("The application encountered an unexpected error and will terminate now!");

        Console.WriteLine();

        try
        {
            if (!string.IsNullOrEmpty(errorMessage)) 
                Console.WriteLine($"Error: {errorMessage}");

            if (!string.IsNullOrEmpty(detailedError))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(detailedError);
            }
        }
        finally
        {
            Console.ResetColor();
            Console.WriteLine();
        }
    }

    public static void WriteApplicationFatalError(string appName, Exception exception)
    {
        WriteApplicationFatalError(appName, exception.Message, exception.StackTrace);
    }

    public static bool UserYesNoQuestion(string question, char yes = 'Y', char no = 'n', HorizontalConsoleFrame? frame = null)
    {
        var questionText = $"{question} [{yes}/{no}] ";
        return UserQuestionOnSameLine(questionText, (string input, out bool result) =>
        {
            result = false;
            if (input.Length != 1)
                return false;

            var answer = char.ToUpperInvariant(input[0]);

            if (answer.Equals(char.ToUpperInvariant(yes)))
            {
                result = true;
                return true;
            }

            if (answer.Equals(char.ToUpperInvariant(no)))
            {
                result = false;
                return true;
            }

            result = false;
            return false;
        }, frame);
    }

    public static T UserQuestionOnSameLine<T>(
        string question, 
        ConsoleQuestionValueFactory<T> inputCorrect,
        HorizontalConsoleFrame? frame = null)
    {
        return UserQuestionOnSameLineInternal(question, inputCorrect, frame);
    }

    private static T UserQuestionOnSameLineInternal<T>(
        string question, 
        ConsoleQuestionValueFactory<T> inputCorrect, 
        HorizontalConsoleFrame? frame)
    {
        var writer = frame?.Writer ?? Console.Out;
        
        while (true)
        {
            var promptLeft = 0;
            var promptTop = Console.CursorTop;

            // Write question
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            if (frame != null)
                writer.Write(question);
            else
            {
                Console.SetCursorPosition(promptLeft, promptTop);
                writer.Write(question);
                Console.SetCursorPosition(promptLeft + question.Length, promptTop);
            }
            Console.ResetColor();

            var input = ReadLineInline(writer);

            if (!inputCorrect(input, out var result))
            {
                // Clear and retry
                if (frame != null)
                {
                    var currentTop = Console.CursorTop;
                    Console.SetCursorPosition(0, currentTop);
                    writer.Write(new string(' ', Console.WindowWidth - 1));
                    Console.SetCursorPosition(0, currentTop);
                }
                else
                {
                    Console.SetCursorPosition(0, promptTop);
                    writer.Write(new string(' ', Console.WindowWidth - 1));
                }
                continue;
            }

            // Success
            writer.WriteLine();
            return result;
        }
    }
    
    private static string ReadLineInline(TextWriter writer)
    {
        var input = "";
        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
                break;

            if (key.Key == ConsoleKey.Backspace)
            {
                if (input.Length > 0)
                {
                    input = input.Substring(0, input.Length - 1);
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    writer.Write(' ');
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                input += key.KeyChar;
                writer.Write(key.KeyChar);
            }
        }

        return input;
    }
}