﻿using ATMSim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATMSimTests.Fakes
{
    internal class FakeConsoleWriter : IConsoleWriter
    {
        public int resetColorCount = 0;
        public int changeForegroundColorCount = 0;
        public int changeBackgroundColorCount = 0;
        public int writeCount = 0;
        public int writeLineCount = 0;

        public string consoleText = "";
        public string consoleTextWithColorInfo = "";

        public ConsoleColor foregroundColor;

        public ConsoleColor backgroundColor;


        public ConsoleColor ForegroundColor
        {
            get => foregroundColor;
            set
            {
                foregroundColor = value;
                consoleTextWithColorInfo += "[FG=" + value.ToString() + "]";
                changeForegroundColorCount++;
            }
        }

        public ConsoleColor BackgroundColor
        {
            get => backgroundColor;
            set
            {
                backgroundColor = value;
                consoleTextWithColorInfo += "[BG=" + value.ToString() + "]";
                changeBackgroundColorCount++;
            }
        }


        public void WriteLine(double text)
        {
            consoleText += text.ToString() + "\n";
            consoleTextWithColorInfo += text.ToString() + "\n";
            writeLineCount++;
        }
        public void Write(double text)
        {
            consoleText += text.ToString();
            consoleTextWithColorInfo += text.ToString();
            writeCount++;
        }
        public void ResetColor()
        {
            resetColorCount++;
            consoleTextWithColorInfo += "[RESET]";
        }

        public void Write(string text)
        {
            consoleText += text;
            consoleTextWithColorInfo += text;
            writeCount++;
        }





        public void WriteLine(string text)
        {
            consoleText += text + "\n";
            consoleTextWithColorInfo += text + "\n";
            writeLineCount++;
        }


    }
}
