using System;
using System.Dynamic;
using System.Reflection;
using System.Windows;

namespace NICE_P16F8x
{
    /// <summary>
    /// class responsible for executing instructions
    /// </summary>
    public static class InstructionProcessor
    {
        #region BYTE-ORIENTED FILE REGISTER OPERATIONS
        public static void ADDWF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = BitwiseAdd(Data.getRegisterW(), Data.getRegister(f));

            DirectionalWrite(d, f, result);
        }
        public static void ANDWF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegisterW() & Data.getRegister(f));

            DirectionalWrite(d, f, result);
        }
        public static void CLRF(Data.Command com)
        {
            byte f = (byte)(com.getLowByte() & 127);
            Data.setRegister(f, 0);
            Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.Z, true);
        }
        public static void CLRW(Data.Command com)
        {
            Data.setRegisterW(0);
            Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.Z, true);
        }
        public static void COMF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegister(f) ^ Data.getRegister(f));
            CheckZFlag(result);

            DirectionalWrite(d, f, result);
        }
        public static void DECF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegister(f) - 1);
            CheckZFlag(result);

            DirectionalWrite(d, f, result);
        }
        public static void DECFSZ(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegister(f) - 1);

            if (result == 0)
            {
                Data.IncPC();
                SkipCycle();
            }

            DirectionalWrite(d, f, result);
        }
        public static void INCF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegister(f) + 1);
            CheckZFlag(result);

            DirectionalWrite(d, f, result);
        }

        public static void INCFSZ(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegister(f) + 1);

            if (result == 0)
            {
                Data.IncPC();
                SkipCycle();
            }

            DirectionalWrite(d, f, result);
        }
        public static void IORWF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegister(f) | Data.getRegisterW());
            CheckZFlag(result);

            DirectionalWrite(d, f, result);
        }
        public static void MOVF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            CheckZFlag(Data.getRegister(f));
            if (d == 128)
            {
                Data.setRegisterW(Data.getRegister(f));
            }
        }
        public static void MOVWF(Data.Command com)
        {
            byte f = (byte)(com.getLowByte() & 127);

            Data.setRegister(f, Data.getRegisterW());
        }
        public static void NOP(Data.Command com)
        {
            //NOP
        }
        public static void RLF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegister(f) << 1);

            if ((Data.getRegister(f) & 128) == 128) Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.C, true);
            else Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.C, false);

            DirectionalWrite(d, f, result);
        }
        public static void RRF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegister(f) >> 1);

            if ((Data.getRegister(f) & 1) == 1) Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.C, true);
            else Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.C, false);

            DirectionalWrite(d, f, result);
        }
        public static void SUBWF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte onecomp = (byte)(Data.getRegisterW() ^ Data.getRegisterW());
            byte twocomp = (byte)(onecomp + 1);

            byte result = BitwiseAdd(Data.getRegister(f), twocomp);

            DirectionalWrite(d, f, result);
        }
        public static void SWAPF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)((Data.getRegister(f) & 0x0F) << 4 | (Data.getRegister(f) & 0xF0) >> 4); ;

            DirectionalWrite(d, f, result);
        }
        public static void XORWF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegisterW() ^ Data.getRegister(f));
            CheckZFlag(result);

            DirectionalWrite(d, f, result);
        }
        #endregion

        #region BIT-ORIENTED FILE REGISTER OPERATIONS
        public static void BCF(Data.Command com)
        {
            int b1 = (com.getHighByte() & 3);
            int b = b1 + (((com.getLowByte() & 128) == 128) ? 4 : 0);
            byte f = (byte)(com.getLowByte() & 127);

            Data.setRegisterBit(f, b, false);
        }
        public static void BSF(Data.Command com)
        {
            int b1 = (com.getHighByte() & 3);
            int b = b1 + (((com.getLowByte() & 128) == 128) ? 4 : 0);
            byte f = (byte)(com.getLowByte() & 127);

            Data.setRegisterBit(f, b, true);
        }
        public static void BTFSC(Data.Command com)
        {
            int b1 = (com.getHighByte() & 3);
            int b = b1 + (((com.getLowByte() & 128) == 128) ? 4 : 0);
            byte f = (byte)(com.getLowByte() & 127);

            if (Data.getRegisterBit(f, b) == false)
            {
                Data.IncPC();
                SkipCycle();
            }
        }
        public static void BTFSS(Data.Command com)
        {
            int b1 = (com.getHighByte() & 3);
            int b = b1 + (((com.getLowByte() & 128) == 128) ? 4 : 0);
            byte f = (byte)(com.getLowByte() & 127);

            if (Data.getRegisterBit(f, b) == true)
            {
                Data.IncPC();
                SkipCycle();
            }
        }
        #endregion

        #region LITERAL AND CONTROL OPERATIONS
        public static void ADDLW(Data.Command com)
        {
            byte k = com.getLowByte();

            byte result = BitwiseAdd(Data.getRegisterW(), k);
            Data.setRegisterW(result);
        }
        public static void ANDLW(Data.Command com)
        {
            byte k = com.getLowByte();

            byte result = (byte)(Data.getRegisterW() & k);
            CheckZFlag(result);
            Data.setRegisterW(result);
        }
        public static void CALL(Data.Command com)
        {
            byte k1 = com.getLowByte();
            byte k2 = (byte)(com.getHighByte() & 7);

            byte merge = (byte)((Data.getRegister(Data.Registers.PCLATH) & 24) + k2); // geht evtl. nicht

            Data.pushStack();
            Data.setPCFromBytes(merge, k1);
            Data.SetPCLfromPC();
            SkipCycle();
        }
        public static void CLRWDT(Data.Command com)
        {
            //TODO
        }
        public static void GOTO(Data.Command com)
        {
            byte k1 = com.getLowByte();
            byte k2 = (byte)(com.getHighByte() & 7);

            byte merge = (byte)((Data.getRegister(Data.Registers.PCLATH) & 24) + k2); // geht evtl. nicht
            Data.setPCFromBytes(merge, k1);
            Data.SetPCLfromPC();
            SkipCycle();
        }
        public static void IORLW(Data.Command com)
        {
            byte k = com.getLowByte();

            byte result = (byte)(Data.getRegisterW() | k);
            CheckZFlag(result);
            Data.setRegisterW(result);
        }
        public static void MOVLW(Data.Command com)
        {
            byte k = com.getLowByte();

            Data.setRegisterW(k);
        }
        public static void RETFIE(Data.Command com)
        {
            Data.setPC(Data.popStack());
            Data.SetPCLfromPC();
            Data.setRegisterBit(Data.Registers.INTCON, Data.Flags.Intcon.GIE, true); //Re-enable Global-Interrupt-Bit
            SkipCycle();
        }
        public static void RETLW(Data.Command com)
        {
            byte k = com.getLowByte();

            Data.setRegisterW(k);
            Data.setPC(Data.popStack());
            Data.SetPCLfromPC();
            SkipCycle();
        }
        public static void RETURN(Data.Command com)
        {
            Data.setPC(Data.popStack());
            Data.SetPCLfromPC();
            SkipCycle();
        }
        //public static void SLEEP(Data.Command com)
        public static void SUBLW(Data.Command com)
        {
            byte k = com.getLowByte();

            byte twocomp = (byte)(~Data.getRegisterW() + 1);

            byte result = BitwiseAdd(twocomp, k);
            Data.setRegisterW(result);
        }
        public static void XORLW(Data.Command com)
        {
            byte k = com.getLowByte();

            byte result = (byte)(Data.getRegisterW() ^ k);
            CheckZFlag(result);
            Data.setRegisterW(result);
        }
        #endregion

        #region HelperFunctions
        /// <summary>
        /// sets Z flag to 1 if result is zero
        /// </summary>
        /// <param name="result"></param>
        private static void CheckZFlag(byte result)
        {
            if (result == 0) Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.Z, true);
            else Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.Z, false);
        }
        /// <summary>
        /// Bitwise add of two bytes, also sets Z, C and DC flag
        /// </summary>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        private static byte BitwiseAdd(byte b1, byte b2)
        {
            //calculate result
            byte result = (byte)(b1 + b2);

            //FLAGS
            //set Carry flag if byte overflows
            if (result < b1) Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.C, true);
            else Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.C, false);

            //set DC flag if 4th low order bit overflows
            if (((b1 & 8) == 8 || (b2 & 8) == 8) && (result & 8) == 0)
                Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.DC, true);
            else
                Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.DC, false);

            //set Z flag if result is zero
            CheckZFlag(result);

            return result;
        }
        /// <summary>
        /// Write result according to d bit
        /// </summary>
        /// <param name="d"></param>
        /// <param name="f"></param>
        /// <param name="result"></param>
        private static void DirectionalWrite(byte d, byte f, byte result)
        {
            //save to w register
            if (d == 0) Data.setRegisterW(result);
            //save to f address
            else if (d == 128) Data.setRegister(f, result);
        }

        private static void SkipCycle()
        {
            Data.ProcessTMR0();
            Data.ProcessWDT();
            Data.increaseRuntime();
        }

        private static void CheckInterrupts()
        {
            if (Data.getRegisterBit(Data.Registers.INTCON, Data.Flags.Intcon.GIE))
            {
                if (Data.getRegisterBit(Data.Registers.INTCON, Data.Flags.Intcon.T0IE) && Data.getRegisterBit(Data.Registers.INTCON, Data.Flags.Intcon.T0IF))
                {
                    CallInterrupt();
                }
            } //TODO
        }

        private static void CallInterrupt()
        {
            Data.setPC(0x04); //Fixed interrupt routine address
            Data.setRegisterBit(Data.Registers.INTCON, Data.Flags.Intcon.GIE, false); //Disable Global-Interrupt-Bit
        }
        #endregion

        #region Access
        public static void Execute(Data.Instruction instruction, Data.Command com)
        {
            MethodInfo theMethod = typeof(InstructionProcessor).GetMethod(instruction.ToString());
            theMethod.Invoke(null, new object[] { com });
        }

        public static void PCStep()
        {
            if (Data.isProgramInitialized())
            {
                if(Data.getPC() < Data.getProgram().Count)
                {
                    Data.Command com = Data.getProgram()[Data.getPC()];
                    Data.IncPC();
                    
                    InstructionProcessor.Execute(Data.InstructionLookup(com), com);
                } else //PC has left program area
                {
                    Data.IncPC();
                    MessageBox.Show("PC has left program area!\nPlease avoid this behavior by ending the code in an infinite loop.", "Out of bounds" , MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }

                Data.ProcessTMR0();
                Data.ProcessWDT();
                Data.increaseRuntime();
                CheckInterrupts();
            }
        }
        #endregion
    }
}