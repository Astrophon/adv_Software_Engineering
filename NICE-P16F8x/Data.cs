using System;
using System.Collections.Generic;
using System.Dynamic;

namespace NICE_P16F8x
{
    public static class Data
    {
        #region Fields
        //normal
        private static List<Command> program = new List<Command>();
        private static byte[] register = new byte[256];
        private static int[] stack = new int[8];
        private static int stackPointer;
        private static byte w;
        private static int pc; // vor dem fetch obere bits auf 0 setzen um später overflow zu vermeiden
        private static int tmr0Precounter;
        private static int prescaler;
        private static decimal runtime, watchdog; //in microseconds
        private static int clockspeed = 4000000; //in Hz
        private static bool watchdogEnabled;

        //integrity
        private static bool programInitialized = false;
        #endregion

        #region Constants
        public static class Registers
        {
            //Bank 1
            public static readonly byte INDF = 0x00;
            public static readonly byte TMR0 = 0x01;
            public static readonly byte PCL = 0x02;
            public static readonly byte STATUS = 0x03;
            public static readonly byte FSR = 0x04;
            public static readonly byte PORTA = 0x05;
            public static readonly byte PORTB = 0x06;
            public static readonly byte EEDATA = 0x08;
            public static readonly byte EEADR = 0x09;
            public static readonly byte PCLATH = 0x0A;
            public static readonly byte INTCON = 0x0B;

            //Bank 2
            public static readonly byte PCL2 = 0x82;
            public static readonly byte OPTION = 0x81;
            public static readonly byte TRISA = 0x85;
            public static readonly byte TRISB = 0x86;
            public static readonly byte EECON1 = 0x88;
            public static readonly byte EECON2 = 0x88;
        }

        public static class Flags
        {
            public static class Status
            {
                public static readonly int C = 0;
                public static readonly int DC = 1;
                public static readonly int Z = 2;
                public static readonly int PD = 3;
                public static readonly int TO = 4;
                public static readonly int RP0 = 5;
                public static readonly int RP1 = 6;
                public static readonly int IRP = 7;
            }
            public static class Intcon
            {
                public static readonly int RBIF = 0;
                public static readonly int INTF = 1;
                public static readonly int T0IF = 2;
                public static readonly int RBIE = 3;
                public static readonly int INTE = 4;
                public static readonly int T0IE = 5;
                public static readonly int EEIE = 6;
                public static readonly int GIE = 7;
            }
            public static class Option
            {
                public static readonly int PS0 = 0;
                public static readonly int PS1 = 1;
                public static readonly int PS2 = 2;
                public static readonly int PSA = 3;
                public static readonly int T0SE = 4;
                public static readonly int T0CS = 5;
                public static readonly int INTEDG = 6;
                public static readonly int RBPU = 7;
            }
        }
        #endregion

        #region Structures
        public class Command
        {
            public Command(string hexData)
            {
                high = (byte)hexToInt(hexData.Substring(0, 2));
                low = (byte)hexToInt(hexData.Substring(2, 2));
            }
            public Command(byte high, byte low)
            {
                this.high = high;
                this.low = low;
            }

            private byte high; //0x30 - command
            private byte low;  //0x11 - data

            public byte getHighByte()
            {
                return high;
            }

            public byte getLowByte()
            {
                return low;
            }
        }

        public enum Instruction
        {
            ADDWF, ANDWF, CLRF, CLRW, COMF, DECF, DECFSZ, INCF, INCFSZ, IORWF, MOVF, MOVWF, NOP, RLF, RRF, SUBWF, SWAPF, XORWF,
            BCF, BSF, BTFSC, BTFSS,
            ADDLW, ANDLW, CALL, CLRWDT, GOTO, IORLW, MOVLW, RETFIE, RETLW, RETURN, SLEEP, SUBLW, XORLW,
            UNKNOWNCOMMAND
        }
        #endregion

        #region Access
        public static void setWatchdogEnabled(bool wdte)
        {
            watchdogEnabled = wdte;
        }
        public static int[] getStack()
        {
            return stack;
        }
        public static void setClockSpeed(int speed)
        {
            clockspeed = speed;
        }
        public static long getSingleExectionTime() //In Microseconds
        {
            return (4000000 / clockspeed);
        }
        public static void ProcessTMR0()
        {
            bool increment = true;
            if (getRegisterBit(Registers.OPTION, Flags.Option.PSA) == false) //Prescaler assigned to TMR0
            {
                tmr0Precounter++;
                byte psByte = BoolArrayToByte(new bool[] { getRegisterBit(Registers.OPTION, Flags.Option.PS0), getRegisterBit(Registers.OPTION, Flags.Option.PS1), getRegisterBit(Registers.OPTION, Flags.Option.PS2) });
                int prescaler = (int)(Math.Pow(2, psByte) + 1);
                if (tmr0Precounter < prescaler)
                {
                    increment = false;
                }
            }
            if (increment)
            {
                byte tmr0 = (byte)(getRegister(Registers.TMR0) + 1);
                setRegister(Registers.TMR0, tmr0);
                if (tmr0 == 0)
                {
                    setRegisterBit(Registers.INTCON, Flags.Intcon.T0IF, true);
                }
            }
        }
        public static void ProcessWDT()
        {
            if (watchdogEnabled)
            {
                long limit = 18000; // 18 milliseconds watchdog time without prescaler
                watchdog += getSingleExectionTime();
                if (getRegisterBit(Registers.OPTION, Flags.Option.PSA) == true) //Prescaler assigned to WDT
                {
                    byte psByte = BoolArrayToByte(new bool[] { getRegisterBit(Registers.OPTION, Flags.Option.PS0), getRegisterBit(Registers.OPTION, Flags.Option.PS1), getRegisterBit(Registers.OPTION, Flags.Option.PS2) });
                    limit *= (long)(Math.Pow(2, psByte));
                }
                if (watchdog >= limit) //Watchdog attacks!!
                {
                    WDTReset();
                }
            }
        }
        public static void increaseRuntime()
        {
            runtime += getSingleExectionTime();
        }
        public static void Reset()
        {
            pc = 0;
            w = 0;
            register = new byte[256];
            stack = new int[8];
            stackPointer = 0;
            tmr0Precounter = 0;
            runtime = 0;
            watchdog = 0;

            setRegister(Registers.STATUS, 0x18);    //0001 1000
            setRegister(Registers.OPTION, 0xFF);    //1111 1111
            setRegister(Registers.TRISA, 0x1F);     //0001 1111
            setRegister(Registers.TRISB, 0xFF);     //1111 1111
        }
        public static void WDTReset() //TODO
        {
            pc = 0;
            w = 0;
            setRegister(Registers.STATUS, (byte)(getRegister(Registers.STATUS) & 7 + 8));
            throw new NotImplementedException();
        }
        public static void pushStack()
        {
            stack[stackPointer] = pc;

            if (stackPointer == 7) stackPointer = 0;
            else stackPointer++;
        }

        public static int popStack()
        {
            if (stackPointer == 0) stackPointer = 7;
            else stackPointer--;

            return stack[stackPointer];
        }

        public static void IncPC()
        {
            pc++;
            SetPCLfromPC();
        }
        public static int getPC()
        {
            return pc;
        }
        public static void setPC(int newPc)
        {
            pc = newPc;
        }
        public static decimal getRuntime()
        {
            return runtime;
        }
        public static decimal getWatchdog()
        {
            return watchdog;
        }
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
        public static int getProgramLineCount()
        {
            return program.Count;
        }

        public static List<Command> getProgram()
        {
            return program;
        }
        public static bool isProgramInitialized()
        {
            return programInitialized;
        }
        public static byte getRegisterW()
        {
            return w;
        }

        public static void setRegisterW(byte val)
        {
            w = val;
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
            switch (address)
            {
                case 0x00:
                    register[Convert.ToInt16(0x80)] = data;
                    break;
                case 0x01: //TMR0
                    prescaler = 0; //Reset Prescaler
                    break;
                case 0x02:
                    register[Convert.ToInt16(0x82)] = data;
                    setPCFromBytes(getRegister(Registers.PCLATH), getRegister(Registers.PCL));
                    break;
                case 0x03:
                    register[Convert.ToInt16(0x83)] = data;
                    break;
                case 0x04:
                    register[Convert.ToInt16(0x84)] = data;
                    break;
                case 0x0A:
                    register[Convert.ToInt16(0x8A)] = data;
                    break;
                case 0x0B:
                    register[Convert.ToInt16(0x8B)] = data;
                    break;

                case 0x80:
                    register[Convert.ToInt16(0x00)] = data;
                    break;
                case 0x82:
                    register[Convert.ToInt16(0x02)] = data;
                    setPCFromBytes(getRegister(Registers.PCLATH), getRegister(Registers.PCL));
                    break;
                case 0x83:
                    register[Convert.ToInt16(0x03)] = data;
                    break;
                case 0x84:
                    register[Convert.ToInt16(0x04)] = data;
                    break;
                case 0x8A:
                    register[Convert.ToInt16(0x0A)] = data;
                    break;
                case 0x8B:
                    register[Convert.ToInt16(0x0B)] = data;
                    break;
            }
        }

        /// <summary>
        /// Sets a specific bit in the given register
        /// </summary>
        /// <param name="address"></param>
        /// <param name="bit"></param>
        /// <param name="value"></param>
        public static void setRegisterBit(byte address, int bit, bool value)
        {
            setRegister(address, setBit(register[address], bit, value));
        }
        /// <summary>
        /// Gets a specific bit in the given register
        /// </summary>
        /// <param name="address"></param>
        /// <param name="bit"></param>
        /// <param name="value"></param>
        public static bool getRegisterBit(byte address, int position)
        {
            return (1 == ((register[address] >> position) & 1));
        }
        #endregion

        #region HelperFunctions
        public static void SetPCLfromPC()
        {
            byte pcl = BitConverter.GetBytes(pc)[0];
            setRegister(Registers.PCL, pcl);
        }

        public static void setPCFromBytes(byte bHigh, byte bLow)
        {
            pc = BitConverter.ToUInt16(new byte[] { bLow, bHigh }, 0);
        }
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
        /// <summary>
        /// Converts a Byte to a bool[8]
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool[] ByteToBoolArray(byte b)
        {
            // prepare the return result
            bool[] result = new bool[8];

            // check each bit in the byte. if 1 set to true, if 0 set to false
            for (int i = 0; i < 8; i++)
                result[i] = (b & (1 << i)) == 0 ? false : true;
            return result;
        }
        /// <summary>
        /// Converts a bool[8] to a byte
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static byte BoolArrayToByte(bool[] source)
        {
            byte result = 0;
            // This assumes the array never contains more than 8 elements!
            int index = 8 - source.Length;

            // reverse the array
            Array.Reverse(source);
            // Loop through the array
            foreach (bool b in source)
            {
                // if the element is 'true' set the bit at that position
                if (b)
                    result |= (byte)(1 << (7 - index));
                index++;
            }
            return result;
        }
        public static string[] ByteToStringArray(byte b)
        {
            string[] result = new string[8];

            for (int i = 0; i < 8; i++)
                result[i] = (b & (1 << i)) == 0 ? "0" : "1";
            return result;
        }
        /// <summary>
        /// changes bit of given byte and returns the altered byte
        /// </summary>
        /// <param name="ofByte"></param>
        /// <param name="bit"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte setBit(byte ofByte, int bitIndex, bool value)
        {
            byte mask = (byte)(1 << bitIndex);
            if (value)
                return ofByte |= mask;
            else
                return ofByte &= (byte)~mask;
        }
        /// <summary>
        /// Finds the Instruction to the specific command and returns it as an Instruction enum
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static Instruction InstructionLookup(Command command)
        {
            //BYTE-ORIENTED FILE REGISTER OPERATIONS
            switch (command.getHighByte())
            {
                //bytes get compared as integers, so opcode conversion is needed
                case 7: return Instruction.ADDWF;
                case 5: return Instruction.ANDWF;
                case 1:
                    if ((command.getLowByte() & 128) == 0) return Instruction.CLRW;
                    else return Instruction.CLRF;
                case 9: return Instruction.COMF;
                case 3: return Instruction.DECF;
                case 11: return Instruction.DECFSZ;
                case 10: return Instruction.INCF;
                case 15: return Instruction.INCFSZ;
                case 4: return Instruction.IORWF;
                case 8: return Instruction.MOVF;
                case 13: return Instruction.RLF;
                case 12: return Instruction.RRF;
                case 2: return Instruction.SUBWF;
                case 14: return Instruction.SWAPF;
                case 6: return Instruction.XORWF;

                //LITERAL AND CONTROL OPERATIONS + NOP & MOVWF
                case 57: return Instruction.ANDLW;
                case 0:
                    if (command.getLowByte() == 100) return Instruction.CLRWDT;
                    else if (command.getLowByte() == 9) return Instruction.RETFIE;
                    else if (command.getLowByte() == 8) return Instruction.RETURN;
                    else if (command.getLowByte() == 99) return Instruction.SLEEP;
                    else if ((command.getLowByte() & 159) == 0) return Instruction.NOP;
                    else return Instruction.MOVWF;
                case 56: return Instruction.IORLW;
                case 58: return Instruction.XORLW;
            }
            //BIT-ORIENTED FILE REGISTER OPERATIONS
            switch (command.getHighByte() & 60)
            {
                case 16: return Instruction.BCF;
                case 20: return Instruction.BSF;
                case 24: return Instruction.BTFSC;
                case 28: return Instruction.BTFSS;

                //LITERAL AND CONTROL OPERATIONS
                case 48: return Instruction.MOVLW;
                case 52: return Instruction.RETLW;
            }
            //LITERAL AND CONTROL OPERATIONS
            switch (command.getHighByte() & 56)
            {
                case 32: return Instruction.CALL;
                case 40: return Instruction.GOTO;
            }
            switch (command.getHighByte() & 62)
            {
                case 62: return Instruction.ADDLW;
                case 60: return Instruction.SUBLW;
            }


            return Instruction.UNKNOWNCOMMAND;
        }

        /// <summary>
        /// Convert single hex char to integer
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static int hexLookup(char c)
        {
            return c switch
            {
                '0' => 0,
                '1' => 1,
                '2' => 2,
                '3' => 3,
                '4' => 4,
                '5' => 5,
                '6' => 6,
                '7' => 7,
                '8' => 8,
                '9' => 9,
                'a' => 10,
                'b' => 11,
                'c' => 12,
                'd' => 13,
                'e' => 14,
                'f' => 15,
                'A' => 10,
                'B' => 11,
                'C' => 12,
                'D' => 13,
                'E' => 14,
                'F' => 15,
                _ => -1,
            };
        }
        #endregion
    }
}