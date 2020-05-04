using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace NICE_P16F8x
{
    /// <summary>
    /// class responsible for executing instructions
    /// </summary>
    public static class InstructionProcessor
    {
        public static void ADDWF(Data.Command com)
        {
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            //calculate result
            byte result = (byte)(Data.getRegisterW() + Data.getRegister(f));

            //FLAGS
            //set Carry flag if byte overflows
            if (result < Data.getRegisterW()) Data.setRegisterBit(Registers.STATUS, Flags.C, true);
            else Data.setRegisterBit(Registers.STATUS, Flags.C, false);
            
            //set DC flag if 4th low order bit overflows !!STILL IN QUESTION!!
            if(((Data.getRegisterW() & 8) == 8 || (Data.getRegister(f) & 8) == 8) && (result & 8) == 0) Data.setRegisterBit(Registers.STATUS, Flags.DC, true);
            else Data.setRegisterBit(Registers.STATUS, Flags.C, false);

            //set Z flag if result is zero
            CheckZFlag(result);

            //save to w register
            if (d == 0) Data.setRegisterW(result);
            //save to f address
            else if (d == 128) Data.setRegister(f, result);
        }

        #region HelperFunctions
        /// <summary>
        /// sets Z flag to 1 if result is zero
        /// </summary>
        /// <param name="result"></param>
        private static void CheckZFlag(byte result)
        {
            if (result == 0) Data.setRegisterBit(Registers.STATUS, Flags.Z, true);
            else Data.setRegisterBit(Registers.STATUS, Flags.Z, false);
        }
        #endregion

    }
}
