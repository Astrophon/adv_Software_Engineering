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
            com.getLowByte();
            byte d = (byte)(com.getLowByte() & 128);
            byte f = (byte)(com.getLowByte() & 127);

            //save to w register
            if(d == 0) Data.setRegisterW((byte)(Data.getRegisterW() + Data.getRegister(f)));
            //save to f address
            else if(d == 128) Data.setRegister(f, (byte)(Data.getRegisterW() + Data.getRegister(f)));

            Data.setRegisterBit(Data.STATUS, 0, true);
        }
    }
}
