using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace LiBondServer
{
    class MyHelper
    {
        // Hex字符串转字节数组Byte[]
        public static byte[] HexToByte(string hexString)
        {
            string newhexString = Regex.Replace(hexString, @"\s", "");          // 去除特殊字符，如空格、换行符等等

            byte[] returnBytes = new byte[newhexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
            {
                returnBytes[i] = Convert.ToByte(newhexString.Substring(i * 2, 2), 16);
            }
            return returnBytes;
        }

        // 字节数组转Hex字符串
        public static string ByteArray2HexStr(byte[] byteArray, int nDataLen, bool bSpace = true)
        {
            //int nDataLen = byteArray.Length;
            StringBuilder strMsg = new StringBuilder();
            for (int index = 0; index < nDataLen; index++)
            {
                if (index == 0)
                {
                    strMsg.AppendFormat("{0:X2}", byteArray[index]);
                }
                else
                {
                    if(bSpace)
                    {
                        strMsg.AppendFormat(" {0:X2}", byteArray[index]);       // 字节之间有空格
                    }
                    else
                    {
                        strMsg.AppendFormat("{0:X2}", byteArray[index]);        // 字节之间无空格
                    }
                }
            }

            return strMsg.ToString();
        }       // End of ByteArray2HexStr()

        // CRC 校验
        public static byte[] CRC16_C(byte[] data, int nDataLen)
        {
            byte CRC16Lo;
            byte CRC16Hi;                                   // CRC寄存器 
            byte CL; byte CH;                               // 多项式码&HA001 
            byte SaveHi; byte SaveLo;
            byte[] tmpData;
            int Flag;
            CRC16Lo = 0xFF;
            CRC16Hi = 0xFF;
            CL = 0x01;
            CH = 0xA0;
            tmpData = data;
            for (int i = 0; i < nDataLen; i++)
            {
                CRC16Lo = (byte)(CRC16Lo ^ tmpData[i]);     // 每一个数据与CRC寄存器进行异或 
                for (Flag = 0; Flag <= 7; Flag++)
                {
                    SaveHi = CRC16Hi;
                    SaveLo = CRC16Lo;
                    CRC16Hi = (byte)(CRC16Hi >> 1);         // 高位右移一位 
                    CRC16Lo = (byte)(CRC16Lo >> 1);         // 低位右移一位 
                    if ((SaveHi & 0x01) == 0x01)            // 如果高位字节最后一位为1 
                    {
                        CRC16Lo = (byte)(CRC16Lo | 0x80);   // 则低位字节右移后前面补1 
                    }                                       // 否则自动补0 
                    if ((SaveLo & 0x01) == 0x01)            // 如果LSB为1，则与多项式码进行异或 
                    {
                        CRC16Hi = (byte)(CRC16Hi ^ CH);
                        CRC16Lo = (byte)(CRC16Lo ^ CL);
                    }
                }
            }
            byte[] ReturnData = new byte[2];
            ReturnData[0] = CRC16Hi;                        // CRC高位 
            ReturnData[1] = CRC16Lo;                        // CRC低位 
            return ReturnData;
        }       // End of CRC16_C()

        // 十六进制字符串转浮点数格式
        public static float HexToFloat(string p_strRaw)
        {
            int len = p_strRaw.Length;
            byte[] TempArry = new byte[len / 2];
            for (int i = 0; i < len / 2; i++)
            {
                TempArry[i] = Convert.ToByte(p_strRaw.Substring(i * 2, 2), 16);
            }
            return BitConverter.ToSingle(TempArry, 0);
        }


    }   // End of class MyHelper
}
