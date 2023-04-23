using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATMSim
{
    public interface IConsoleWriter
    {
        public void Write(string text);
        public void Write(double text);
        public void WriteLine(string text);
        public void WriteLine(double text);
        public void ResetColor();

        public ConsoleColor ForegroundColor { get; set; }
        public ConsoleColor BackgroundColor { get; set; }
    }

    public class ConsoleWriter : IConsoleWriter
    {

        public ConsoleColor ForegroundColor { get => Console.ForegroundColor; set => Console.ForegroundColor = value; }
        public ConsoleColor BackgroundColor { get => Console.BackgroundColor; set => Console.BackgroundColor = value; }

        public void ResetColor()
        {
            Console.ResetColor();
        }

        public void Write(string text)
        {
            Console.Write(text);
        }

        public void Write(double text)
        {
            Console.Write(text);
        }

        public void WriteLine(string text)
        {
            Console.WriteLine(text);
        }

        public void WriteLine(double text)
        {
            Console.WriteLine(text);
        }
    }
}
