﻿using System;
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

            byte result = BitwiseAdd(Data.getRegisterW(), Data.getRegister(Data.AddressResolution(f)));

            DirectionalWrite(d, f, result);
        }
        public static void ANDWF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegisterW() & Data.getRegister(Data.AddressResolution(f)));

            DirectionalWrite(d, f, result);
        }
        public static void CLRF(Data.Command com)
        {
            byte f = (byte)(com.getLowByte() & 127);
            Data.setRegister(Data.AddressResolution(f), 0);
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

            byte result = (byte)~Data.getRegister(Data.AddressResolution(f));
            CheckZFlag(result);

            DirectionalWrite(d, f, result);
        }
        public static void DECF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegister(Data.AddressResolution(f)) - 1);
            CheckZFlag(result);

            DirectionalWrite(d, f, result);
        }
        public static void DECFSZ(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegister(Data.AddressResolution(f)) - 1);

            if (result == 0)
            {
                Data.IncPC();
                Data.SkipCycle();
            }

            DirectionalWrite(d, f, result);
        }
        public static void INCF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegister(Data.AddressResolution(f)) + 1);
            CheckZFlag(result);

            DirectionalWrite(d, f, result);
        }

        public static void INCFSZ(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegister(Data.AddressResolution(f)) + 1);

            if (result == 0)
            {
                Data.IncPC();
                Data.SkipCycle();
            }

            DirectionalWrite(d, f, result);
        }
        public static void IORWF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegister(Data.AddressResolution(f)) | Data.getRegisterW());
            CheckZFlag(result);

            DirectionalWrite(d, f, result);
        }
        public static void MOVF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            CheckZFlag(Data.getRegister(Data.AddressResolution(f)));
            if (d != 128)
            {
                Data.setRegisterW(Data.getRegister(Data.AddressResolution(f)));
            }
        }
        public static void MOVWF(Data.Command com)
        {
            byte f = (byte)(com.getLowByte() & 127);

            Data.setRegister(Data.AddressResolution(f), Data.getRegisterW());
        }
        public static void NOP(Data.Command com)
        {
            //NOP
        }
        public static void RLF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegister(Data.AddressResolution(f)) << 1);

            //Add carry bit if flag is set
            if (Data.getRegisterBit(Data.Registers.STATUS, Data.Flags.Status.C)) result++;

            //Set carry flag for current calculation
            if ((Data.getRegister(Data.AddressResolution(f)) & 128) == 128) Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.C, true);
            else Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.C, false);

            DirectionalWrite(d, f, result);
        }
        public static void RRF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegister(Data.AddressResolution(f)) >> 1);

            //Add carry bit if flag is set
            if (Data.getRegisterBit(Data.Registers.STATUS, Data.Flags.Status.C)) result += 128;

            //Set carry flag for current calculation
            if ((Data.getRegister(Data.AddressResolution(f)) & 1) == 1) Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.C, true);
            else Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.C, false);

            DirectionalWrite(d, f, result);
        }
        public static void SUBWF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = BitwiseSubstract(Data.getRegister(Data.AddressResolution(f)), Data.getRegisterW());

            DirectionalWrite(d, f, result);
        }
        public static void SWAPF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)((Data.getRegister(Data.AddressResolution(f)) & 0x0F) << 4 | (Data.getRegister(Data.AddressResolution(f)) & 0xF0) >> 4); ;

            DirectionalWrite(d, f, result);
        }
        public static void XORWF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            byte result = (byte)(Data.getRegisterW() ^ Data.getRegister(Data.AddressResolution(f)));
            CheckZFlag(result);

            DirectionalWrite(d, f, result);
        }
        #endregion

        #region BIT-ORIENTED FILE REGISTER OPERATIONS
        public static void BCF(Data.Command com)
        {
            int b1 = (com.getHighByte() & 3) << 1;
            int b = b1 + (((com.getLowByte() & 128) == 128) ? 1 : 0);
            byte f = (byte)(com.getLowByte() & 127);

            Data.setRegisterBit(Data.AddressResolution(f), b, false);
        }
        public static void BSF(Data.Command com)
        {
            int b1 = (com.getHighByte() & 3) << 1;
            int b = b1 + (((com.getLowByte() & 128) == 128) ? 1 : 0);
            byte f = (byte)(com.getLowByte() & 127);

            Data.setRegisterBit(Data.AddressResolution(f), b, true);
        }
        public static void BTFSC(Data.Command com)
        {
            int b1 = (com.getHighByte() & 3) << 1;
            int b = b1 + (((com.getLowByte() & 128) == 128) ? 1 : 0);
            byte f = (byte)(com.getLowByte() & 127);

            if (Data.getRegisterBit(Data.AddressResolution(f), b) == false)
            {
                Data.IncPC();
                Data.SkipCycle();
            }
        }
        public static void BTFSS(Data.Command com)
        {
            int b1 = (com.getHighByte() & 3) << 1;
            int b = b1 + (((com.getLowByte() & 128) == 128) ? 1 : 0);
            byte f = (byte)(com.getLowByte() & 127);

            if (Data.getRegisterBit(Data.AddressResolution(f), b) == true)
            {
                Data.IncPC();
                Data.SkipCycle();
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

            byte merge = (byte)((Data.getRegister(Data.Registers.PCLATH) & 24) + k2);

            Data.pushStack();
            Data.setPCFromBytes(merge, k1);
            Data.SetPCLfromPC();
            Data.SkipCycle();
        }
        public static void CLRWDT(Data.Command com)
        {
            Data.resetWatchdog();
            Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.TO, true);
            Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.PD, false);
            Data.SetPrePostscaler(); //Reset Postscaler if assigned to WDT
        }
        public static void GOTO(Data.Command com)
        {
            byte k1 = com.getLowByte();
            byte k2 = (byte)(com.getHighByte() & 7);

            byte merge = (byte)((Data.getRegister(Data.Registers.PCLATH) & 24) + k2);
            Data.setPCFromBytes(merge, k1);
            Data.SetPCLfromPC();
            Data.SkipCycle();
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
            Data.SkipCycle();
        }
        public static void RETLW(Data.Command com)
        {
            byte k = com.getLowByte();

            Data.setRegisterW(k);
            Data.setPC(Data.popStack());
            Data.SetPCLfromPC();
            Data.SkipCycle();
        }
        public static void RETURN(Data.Command com)
        {
            Data.setPC(Data.popStack());
            Data.SetPCLfromPC();
            Data.SkipCycle();
        }
        public static void SLEEP(Data.Command com)
        {
            CLRWDT(null);
            Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.TO, true);
            Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.PD, false);
            Data.setSleeping(true);
            Data.setPC(Data.getPC() - 1);
        }
        public static void SUBLW(Data.Command com)
        {
            byte k = com.getLowByte();

            byte result = BitwiseSubstract(k, Data.getRegisterW());
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
            if (result < b1 || result < b2) Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.C, true);
            else Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.C, false);

            //set DC flag if 4th low order bit overflows
            //if (((b1 & 8) == 8 || (b2 & 8) == 8) && (result & 8) == 0)
            if ((((b1 & 15) + (b2 & 15)) & 16) == 16)
                Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.DC, true);
            else
                Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.DC, false);

            //set Z flag if result is zero
            CheckZFlag(result);

            return result;
        }
        private static byte BitwiseSubstract(byte b1, byte b2)
        {
            //calculate two's complement
            b2 = (byte)(~b2 + 1);

            //calculate result
            byte result = (byte)(b1 + b2);

            //FLAGS
            //set Carry flag if byte overflows OR if either b1 or b2 is zero
            if (result < b1 || result < b2 || b1 == 0 || b2 == 0) Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.C, true);
            else Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.C, false);

            //set DC flag if 4th low order bit overflows OR if either b1 or b2 is zero
            if (((((b1 & 15) + (b2 & 15)) & 16) == 16) || b1 == 0 || b2 == 0)
                Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.DC, true);
            else
                Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.DC, false);

            //set Z flag if result is zero
            CheckZFlag(result);

            return result;
        }
        /// <summary>
        /// Write result according to d bit (relative address)
        /// </summary>
        /// <param name="d"></param>
        /// <param name="f"></param>
        /// <param name="result"></param>
        private static void DirectionalWrite(byte d, byte f, byte result)
        {
            //save to w register
            if (d == 0) Data.setRegisterW(result);
            //save to f address
            else if (d == 128) Data.setRegister(Data.AddressResolution(f), result);
        }

        private static void CallInterrupt()
        {
            Data.pushStack();
            Data.setPC(0x04); //Fixed interrupt routine address
            Data.setRegisterBit(Data.Registers.INTCON, Data.Flags.Intcon.GIE, false); //Disable Global-Interrupt-Bit

            if (Data.isSleeping())
            {
                Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.TO, true);
                Data.setRegisterBit(Data.Registers.STATUS, Data.Flags.Status.PD, false);
                Data.IncPC();
                Data.setSleeping(false);
            }
        }
        #endregion

        #region Access
        public static void Execute(Data.Instruction instruction, Data.Command com)
        {
            MethodInfo theMethod = typeof(InstructionProcessor).GetMethod(instruction.ToString());
            theMethod.Invoke(null, new object[] { com });
        }
        /// <summary>
        /// Step function. Executes current command of loaded program and increases PC
        /// </summary>
        /// <returns>false if within program bounds, true if PC left program bounds</returns>
        public static void PCStep()
        {
            if (Data.isProgramInitialized())
            {
                if (!Data.isSleeping())
                {
                    if (Data.getPC() < Data.getProgram().Count)
                    {
                        Data.Command com = Data.getProgram()[Data.getPC()];
                        Data.IncPC();
                        InstructionProcessor.Execute(Data.InstructionLookup(com), com);
                    }
                    else //PC has left program area
                    {
                        Data.IncPC();
                    }
                    Data.ProcessTMR0();
                }
                Data.ProcessWDT();
                Data.increaseRuntime();
                Data.ProcessRBInterrupts();
                if (Data.CheckInterrupts())
                {
                    CallInterrupt();
                }

            }
        }
        #endregion
    }
}