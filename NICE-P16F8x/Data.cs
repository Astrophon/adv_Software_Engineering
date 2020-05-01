using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace NICE_P16F8x
{
    public static class Data
    {
        #region Fields
        //normal
        private static List<Command> program = new List<Command>();
        private static byte[] register = new byte[256];

        //integrity
        private static bool programInitialized = false;

        #endregion

        #region Structures
        public class Command
        {
            public Command(string hexData)
            {
                high = (byte) hexToInt(hexData.Substring(0, 2));
                low = (byte) hexToInt(hexData.Substring(2, 2));
            }
            public Command(byte high, byte low)
            {
                this.high = high;
                this.low = low;
            }

            private byte high; //0x30 - command
            private byte low;  //0x11 - data
        }

        public enum Instruction
        {
            ADDWF, ANDWF, CLRF, CLRW, COMF, DECF, DECFSZ, INCF, INCFSZ, IORWF, MOVF, MOVWF, NOP, RLF, RRF, SUBWF, SWAPF, XORWF,
            BCF, BSF, BTFSC, BTFSS,
            ADDLW, ANDLW, CALL, CLRWDT, GOTO, IORLW, MOVLW, RETFIE, RETLW, RETURN, SLEEP, SUBLW, XORLW
        }
        #endregion

        #region Access
        public static void setWriteProgram(List<string> commands)
        {
            if (commands == null) throw new ArgumentNullException();

            program = new List<Command>();
            for (int i = 0; i < commands.Count; i++)
            {
                Command line = new Command(commands[i]);
                program.Add(line);
            }

            programInitialized = true;
        }

        public static Command getProgramLine(int at)
        {
            if (!programInitialized) return null;

            return program[at];
        }

        public static byte getRegister(byte address)
        {
            return register[Convert.ToInt16(address)];
        }
        public static byte[] getAllRegisters()
        {
            return register;
        }
        public static void setRegister(byte address, byte data)
        {
            register[Convert.ToInt16(address)] = data;
        }
        #endregion

        #region HelperFunctions
        /// <summary>
        /// Converts a 2 character string of hex numbers to an integer
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static int hexToInt(string hex)
        {
            if (hex.Length > 2) return -1;
            return 16 * hexLookup(hex[0]) + hexLookup(hex[1]);
        }

        public static Instruction CommandLookup(Command instruction)
        {
            Instruction inst = Instruction.NOP;
            throw new NotImplementedException();
            return inst;
        }

        public static Command InstructionLookup(Instruction inst)
        {
            Command com = new Command(0, 0);
            throw new NotImplementedException();
            return com;
        }

        /// <summary>
        /// Convert single hex char to integer
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static int hexLookup(char c)
        {
            switch (c)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'a': return 10;
                case 'b': return 11;
                case 'c': return 12;
                case 'd': return 13;
                case 'e': return 14;
                case 'f': return 15;

                case 'A': return 10;
                case 'B': return 11;
                case 'C': return 12;
                case 'D': return 13;
                case 'E': return 14;
                case 'F': return 15;
                default:  return -1;
            }
        }
        #endregion

    }
}