using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NICE_P16F8x
{
    public static class Data
    {
        #region Fields
        //normal
        private static List<Line> program = new List<Line>();
        private static byte[] register = new byte[256];

        //integrity
        private static bool programInitialized = false;
        #endregion

        #region Structures
        public class Line
        {
            public Line(string hexData)
            {
                high = Convert.ToByte(hexData.Substring(0, 1));
                low = Convert.ToByte(hexData.Substring(2, 3));
            }
            public Line(byte high, byte low)
            {
                this.high = high;
                this.low = low;
            }

            private byte high;
            private byte low;
        }
        #endregion

        #region Access
        public static void setWriteProgram(List<string> commands)
        {
            if (commands == null) throw new ArgumentNullException();

            program = new List<Line>();
            for (int i = 0; i < commands.Count; i++)
            {
                Line line = new Line(commands[i]);
                program[i] = line;
            }

            programInitialized = true;
        }

        public static Line getProgramLine(int at)
        {
            if (!programInitialized) return null;

            return program[at];
        }

        public static byte getRegister(byte address)
        {
            return register[Convert.ToInt16(address)];
        }
        public static void setRegister(byte address, byte data)
        {
            register[Convert.ToInt16(address)] = data;
        }
        #endregion
    }
}