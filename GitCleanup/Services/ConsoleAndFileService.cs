using System.Text;

namespace GitCleanup.Services
{
    public class ConsoleAndFileService
    {
        private static TextWriter _current;

        private class OutputWriter : TextWriter
        {
            public override Encoding Encoding => _current.Encoding;

            public override void WriteLine(string value)
            {
                _current.WriteLine(value);
                File.AppendAllLines("Output.txt", new [] {$"{DateTime.Now}|{value}",});
            }
        }

        public static void Init()
        {
            _current = Console.Out;
            Console.SetOut(new OutputWriter());
        }
    }
}