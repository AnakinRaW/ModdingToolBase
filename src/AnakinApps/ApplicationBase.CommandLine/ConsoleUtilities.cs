using System;

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

    public static bool UserYesNoQuestion(string question, char yes = 'Y', char no = 'n')
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
        });
    }

    public static T UserQuestionOnSameLine<T>(string question, ConsoleQuestionValueFactory<T> inputCorrect)
    {
        while (true)
        {
            var promptLeft = 0;
            var promptTop = Console.CursorTop;

            Console.SetCursorPosition(promptLeft, promptTop);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(question);
            Console.ResetColor();
            Console.SetCursorPosition(promptLeft + question.Length, promptTop);

            var input = ReadLineInline();

            if (!inputCorrect(input, out var result))
            {
                Console.SetCursorPosition(0, promptTop);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                continue;
            }

            Console.WriteLine();
            return result;
        }
    }

    private static string ReadLineInline()
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
                    Console.Write(' ');
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                input += key.KeyChar;
                Console.Write(key.KeyChar);
            }
        }

        return input;
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
}