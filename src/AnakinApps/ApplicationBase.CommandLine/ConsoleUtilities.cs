using System;
using System.Linq;
using System.Reflection;
using System.Text;

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
        var message = FormatExceptionMessage(exception);
        var details = FormatExceptionDetails(exception);

        WriteApplicationFatalError(appName, message, details);
    }

    private static string FormatExceptionMessage(Exception ex)
    {
        return ex switch
        {
            TypeInitializationException tie =>
                $"{ex.GetType().Name} in type '{tie.TypeName}'",
            AggregateException ae =>
                $"{ex.GetType().Name} ({ae.InnerExceptions.Count} errors)",
            _ => $"{ex.GetType().Name}: {ex.Message}"
        };
    }

    private static string FormatExceptionDetails(Exception ex)
    {
        var sb = new StringBuilder();
        FormatExceptionChain(ex, sb, indent: 0);
        return sb.ToString();
    }

    private static void FormatExceptionChain(Exception ex, StringBuilder sb, int indent)
    {
        var prefix = new string(' ', indent * 2);

        switch (ex)
        {
            case AggregateException ae:
                sb.AppendLine($"{prefix}{ae.Message}");
                foreach (var inner in ae.InnerExceptions)
                {
                    sb.AppendLine($"{prefix}---> [{inner.GetType().Name}]");
                    FormatExceptionChain(inner, sb, indent + 1);
                }
                break;

            case ReflectionTypeLoadException rtle:
                sb.AppendLine($"{prefix}{rtle.Message}");
                foreach (var loader in rtle.LoaderExceptions?.Where(e => e != null) ?? [])
                {
                    sb.AppendLine($"{prefix}---> [{loader!.GetType().Name}] {loader.Message}");
                }
                sb.AppendLine();
                sb.AppendLine(rtle.StackTrace);
                break;

            default:
                if (ex.InnerException != null)
                {
                    sb.AppendLine($"{prefix}---> [{ex.InnerException.GetType().Name}]");
                    FormatExceptionChain(ex.InnerException, sb, indent + 1);
                }
                else
                {
                    sb.AppendLine(ex.StackTrace);
                }
                break;
        }
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

    public static T UserQuestionOnSameLine<T>(
        string question,
        ConsoleQuestionValueFactory<T> inputCorrect)
    {
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(question);
            Console.ResetColor();

            var input = ReadLineInline();

            if (!inputCorrect(input, out var result))
            {
                // Clear current line using carriage return + spaces + carriage return
                // // This works in all console contexts unlike Console.SetCursorPosition()
                Console.Write("\r" + new string(' ', question.Length + input.Length) + "\r");
                continue;
            }

            Console.WriteLine(); // Move to next line
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
                    // Backspace-space-backspace: move back, overwrite with space, move back again
                    // This character sequence works in all console contexts
                    Console.Write("\b \b");
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
}