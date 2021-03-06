using System;
using System.Collections.Generic;

namespace NICE_P16F8x
{
    public static class Data
    {
        #region Fields
        //Loaded Program code
        private static List<Command> program = new List<Command>();

        //Data Registers
        private static byte[] register = new byte[256];
        private static int[] stack = new int[8];
        private static int stackPointer;
        private static byte w;
        private static int pc;

        //Status Vars
        private static decimal runtime, watchdog; //in microseconds
        private static int clockspeed = 4000000; //in Hz
        private static bool sleeping;

        //Watchdog
        private static readonly int watchdogLimit = 18000; // 18 milliseconds watchdog time without prescaler
        private static bool watchdogEnabled;

        //Timer prescaler / Watchdog postscaler
        private static int prePostscalerRatio, prePostscaler;

        //Interrupt / Timer state saves
        private static byte RBIntLastState;
        private static byte RB0IntLastState;
        private static byte RA4TimerLastState;

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
        }
        #endregion

        #region Structures
        public class Command
        {
            public Command(string hexData)
            {
                high = (byte)HexToInt(hexData.Substring(0, 2));
                low = (byte)HexToInt(hexData.Substring(2, 2));
            }
            public Command(byte high, byte low)
            {
                this.high = high;
                this.low = low;
            }

            private readonly byte high; //0x30 - command
            private readonly byte low;  //0x11 - data

            public byte GetHighByte()
            {
                return high;
            }

            public byte GetLowByte()
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
        public static void ProcessTMR0()
        {
            byte RA4 = (byte)(GetRegister(Registers.PORTA) >> 4 & 0x01);
            if (GetRegisterBit(Registers.OPTION, Flags.Option.T0CS) == false || //Internal clock source
                GetRegisterBit(Registers.OPTION, Flags.Option.T0SE) && RA4 < RA4TimerLastState || //External clock source (RA4) selected and falling edge detected
                !GetRegisterBit(Registers.OPTION, Flags.Option.T0SE) && RA4 > RA4TimerLastState)  //External clock source (RA4) selected and rising edge detected
            {
                bool increment = true;
                if (GetRegisterBit(Registers.OPTION, Flags.Option.PSA) == false) //Prescaler assigned to TMR0
                {
                    prePostscaler++;
                    if (prePostscaler >= GetPrePostscalerRatio())
                    {
                        ResetPrePostScaler();
                        increment = true;
                    }
                    else increment = false;
                }
                if (increment)
                {
                    byte tmr0 = (byte)(GetRegister(Registers.TMR0) + 1); //Increment TMR0
                    register[Registers.TMR0] = tmr0; //Direct access to avoid prescaler reset
                    if (tmr0 == 0)
                    {
                        SetRegisterBit(Registers.INTCON, Flags.Intcon.T0IF, true);
                    }
                }
            }
            RA4TimerLastState = RA4;
        }

        public static void ProcessWDT()
        {
            if (watchdogEnabled)
            {
                watchdog += GetSingleExectionTime();
                int limit = watchdogLimit;
                if (GetRegisterBit(Registers.OPTION, Flags.Option.PSA) == true) //Postscaler assigned to WDT
                {
                    limit *= GetPrePostscalerRatio();
                }
                if (watchdog >= limit) //Watchdog attacks!!
                {
                    WDTReset();
                }
            }
        }
        public static void SetPrePostscalerRatio()
        {
            byte PSByte = (byte)(GetRegister(Registers.OPTION) & 7);

            if (GetRegisterBit(Registers.OPTION, Flags.Option.PSA) == false) //Prescaler assigned to TMR0
            {
                prePostscalerRatio = (int)Math.Pow(2, PSByte + 1);
            }
            else // Postscaler assigned to WDT
            {
                prePostscalerRatio = (int)Math.Pow(2, PSByte);
            }
        }
        public static void ResetPrePostScaler()
        {
            prePostscaler = 0;
        }
        public static void ProcessRBInterrupts()
        {
            //RB Interrupt
            byte RB = (byte)(GetRegister(Registers.PORTB) & 0xF0);
            if (((RBIntLastState ^ RB) & GetRegister(Registers.TRISB)) != 0)
            {
                SetRegisterBit(Registers.INTCON, Flags.Intcon.RBIF, true);
            }
            RBIntLastState = RB;

            //RB0 Interrupt depending on Flankenwechsel
            byte RB0 = (byte)(GetRegister(Registers.PORTB) & 0x01);
            if (GetRegisterBit(Registers.OPTION, Flags.Option.INTEDG) && RB0 > RB0IntLastState || !GetRegisterBit(Registers.OPTION, Flags.Option.INTEDG) && RB0 < RB0IntLastState)
            {
                SetRegisterBit(Registers.INTCON, Flags.Intcon.INTF, true);
            }

            RB0IntLastState = RB0;
        }
        public static bool CheckInterrupts()
        {
            if (GetRegisterBit(Registers.INTCON, Flags.Intcon.GIE))
            {
                if (GetRegisterBit(Registers.INTCON, Flags.Intcon.T0IE) && GetRegisterBit(Registers.INTCON, Flags.Intcon.T0IF) ||
                    GetRegisterBit(Registers.INTCON, Flags.Intcon.INTE) && GetRegisterBit(Registers.INTCON, Flags.Intcon.INTF) ||
                    GetRegisterBit(Registers.INTCON, Flags.Intcon.RBIE) && GetRegisterBit(Registers.INTCON, Flags.Intcon.RBIF))
                {
                    return true;
                }
            }
            return false;
        }
        public static void IncreaseRuntime()
        {
            runtime += GetSingleExectionTime();
        }

        public static void Reset()
        {
            sleeping = false;
            pc = 0;
            w = 0;
            register = new byte[256];
            stack = new int[8];
            stackPointer = 0;
            runtime = 0;
            watchdog = 0;

            SetRegister(Registers.STATUS, 0x18);    //0001 1000
            SetRegister(Registers.OPTION, 0xFF);    //1111 1111
            SetRegister(Registers.TRISA, 0x1F);     //0001 1111
            SetRegister(Registers.TRISB, 0xFF);     //1111 1111
            SetPrePostscalerRatio();
        }
        public static void WDTReset()
        {
            ResetWatchdog();
            if (IsSleeping())
            {
                SetRegisterBit(Registers.STATUS, Flags.Status.TO, false);
                SetRegisterBit(Registers.STATUS, Flags.Status.PD, false);
                IncPC();
                SetSleeping(false);
            }
            else
            {
                SetPC(0);
                SetRegisterW(0);
                SetRegister(Registers.STATUS, (byte)((GetRegister(Registers.STATUS) & 7) + 0x08));  //0000 1uuu
                SetRegister(Registers.OPTION, 0xFF);    //1111 1111
                SetRegister(Registers.PCLATH, 0x00);    //0000 0000

            }
        }
        public static void PushStack()
        {
            stack[stackPointer] = pc;

            if (stackPointer == 7) stackPointer = 0;
            else stackPointer++;
        }
        public static int PopStack()
        {
            if (stackPointer == 0) stackPointer = 7;
            else stackPointer--;

            return stack[stackPointer];
        }
        public static void IncPC()
        {
            if (pc < 1023) pc++;
            else pc = 0;
            SetPCLfromPC();
        }
        public static void SetWriteProgram(List<string> commands)
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
        public static byte GetRegister(byte address)
        {
            return address switch
            {
                0x00 => register[GetRegister(Registers.FSR)],
                _ => register[Convert.ToInt16(address)],
            };
        }
        public static void SetRegister(byte address, byte data)
        {
            //set actual register data
            register[Convert.ToInt16(address)] = data;

            //Mirroring, special functions
            switch (address)
            {
                case 0x00: //indirect (using FSR)
                    register[GetRegister(Registers.FSR)] = data;
                    break;
                case 0x01: //TMR0
                    ResetPrePostScaler(); //Reset Prescaler
                    InstructionProcessor.SkipCycle();
                    break;
                case 0x02:
                    register[Convert.ToInt16(0x82)] = data;
                    SetPCFromBytes(GetRegister(Registers.PCLATH), GetRegister(Registers.PCL));
                    break;
                case 0x03:
                    register[Convert.ToInt16(0x83)] = data;
                    break;
                case 0x04:
                    register[Convert.ToInt16(0x84)] = data;
                    break;
                case 0x05: //PORTA Latch TODO
                           //latchPortA = data;

                    break;
                case 0x06: //PORTB Latch TODO

                    break;
                case 0x0A:
                    register[Convert.ToInt16(0x8A)] = data;
                    break;
                case 0x0B:
                    register[Convert.ToInt16(0x8B)] = data;
                    break;

                case 0x80: //indirect (using FSR)
                    register[GetRegister(Registers.FSR)] = data;
                    break;
                case 0x81: //OPTION 
                    SetPrePostscalerRatio();
                    break;
                case 0x82:
                    register[Convert.ToInt16(0x02)] = data;
                    SetPCFromBytes(GetRegister(Registers.PCLATH), GetRegister(Registers.PCL));
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
        /// Sets a specific bit in the given register (absolute address)
        /// </summary>
        /// <param name="address"></param>
        /// <param name="bit"></param>
        /// <param name="value"></param>
        public static void SetRegisterBit(byte address, int bit, bool value)
        {
            SetRegister(address, SetBit(GetRegister(address), bit, value));
        }
        /// <summary>
        /// Gets a specific bit in the given register (absolute address)
        /// </summary>
        /// <param name="address"></param>
        /// <param name="bit"></param>
        /// <param name="value"></param>
        public static bool GetRegisterBit(byte address, int bit)
        {
            return (1 == ((GetRegister(address) >> bit) & 1));
        }

        public static void ToggleRegisterBit(byte address, int bit)
        {
            if (GetRegisterBit(address, bit))
                SetRegisterBit(address, bit, false);
            else SetRegisterBit(address, bit, true);

        }

        public static byte AddressResolution(byte address)
        {
            //Add 0x80 if Bank 1 selected
            if (GetRegisterBit(Registers.STATUS, Flags.Status.RP0))
            {
                return (byte)(address + 0x80);
            }
            if (address >= register.Length)
            {
                throw new IndexOutOfRangeException();
            }
            return address;
        }

        #endregion

        #region HelperFunctions
        public static void SetPCLfromPC()
        {
            byte pcl = BitConverter.GetBytes(pc)[0];
            register[Registers.PCL] = pcl;
            register[Registers.PCL2] = pcl;
        }

        public static void SetPCFromBytes(byte bHigh, byte bLow)
        {
            pc = BitConverter.ToUInt16(new byte[] { bLow, bHigh }, 0);
        }
        /// <summary>
        /// Converts a 2 character string of hex numbers to an integer
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static int HexToInt(string hex)
        {
            if (hex.Length > 2) return -1;
            return 16 * HexLookup(hex[0]) + HexLookup(hex[1]);
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
        public static byte SetBit(byte ofByte, int bitIndex, bool value)
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
            switch (command.GetHighByte())
            {
                //bytes get compared as integers, so opcode conversion is needed
                case 7: return Instruction.ADDWF;
                case 5: return Instruction.ANDWF;
                case 1:
                    if ((command.GetLowByte() & 128) == 0) return Instruction.CLRW;
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
                    if (command.GetLowByte() == 100) return Instruction.CLRWDT;
                    else if (command.GetLowByte() == 9) return Instruction.RETFIE;
                    else if (command.GetLowByte() == 8) return Instruction.RETURN;
                    else if (command.GetLowByte() == 99) return Instruction.SLEEP;
                    else if ((command.GetLowByte() & 159) == 0) return Instruction.NOP;
                    else return Instruction.MOVWF;
                case 56: return Instruction.IORLW;
                case 58: return Instruction.XORLW;
            }
            //BIT-ORIENTED FILE REGISTER OPERATIONS
            switch (command.GetHighByte() & 60)
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
            switch (command.GetHighByte() & 56)
            {
                case 32: return Instruction.CALL;
                case 40: return Instruction.GOTO;
            }
            switch (command.GetHighByte() & 62)
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
        public static int HexLookup(char c)
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

        #region Getter/Setter
        public static int[] GetStack()
        {
            return stack;
        }
        public static long GetSingleExectionTime() //In Microseconds
        {
            return (4000000 / clockspeed);
        }
        public static int GetPC()
        {
            return pc;
        }
        public static void SetPC(int newPc)
        {
            pc = newPc;
            SetPCLfromPC();
        }
        public static decimal GetRuntime()
        {
            return runtime;
        }
        public static decimal GetWatchdog()
        {
            return watchdog;
        }
        public static void ResetWatchdog()
        {
            watchdog = 0;
        }
        public static byte[] GetAllRegisters()
        {
            return register;
        }
        public static int GetProgramLineCount()
        {
            return program.Count;
        }
        public static List<Command> GetProgram()
        {
            return program;
        }
        public static bool IsProgramInitialized()
        {
            return programInitialized;
        }
        public static byte GetRegisterW()
        {
            return w;
        }
        public static void SetRegisterW(byte val)
        {
            w = val;
        }
        public static Command GetProgramLine(int index)
        {
            if (!programInitialized) return null;

            return program[index];
        }
        public static void SetClockSpeed(int speed)
        {
            clockspeed = speed;
        }
        public static void SetWatchdogEnabled(bool wdte)
        {
            watchdogEnabled = wdte;
        }
        public static int GetPrePostscalerRatio()
        {
            return prePostscalerRatio;
        }
        public static void SetSleeping(bool sleep)
        {
            sleeping = sleep;
        }
        public static bool IsSleeping()
        {
            return sleeping;
        }
        #endregion
    }
}