using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiBondServer
{
    class TcpClientProtocol
    {
        // 2017-11-07 Write by AlexWang
        public static void ParseData(ref ClientInfo clientInfo, byte[] buffer, int nDataLen)
        {
            switch (buffer[7])
            {
                case 0x01:
                    ParseIdentity(ref clientInfo, buffer, nDataLen);
                    break;

                case 0x02:
                    ParseRealDataProtocal(ref clientInfo, buffer, nDataLen);
                    break;
            }
        }

        // 回复底层硬件设备
        public static void ReplyClient(ref ClientInfo clientInfo, byte[] buffer, int nDataLen)
        {
            byte[] sendData = { 0x10, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            sendData[3] = buffer[3];
            sendData[4] = buffer[4];
            sendData[5] = buffer[5];
            sendData[6] = buffer[6];
            sendData[8] = buffer[8];

            sendData[9]  = buffer[9];
            sendData[10] = buffer[10];
            sendData[11] = buffer[11];
            sendData[12] = buffer[12];
            sendData[13] = buffer[13];
            sendData[14] = buffer[14];
            sendData[15] = buffer[15];

            byte[] tx2 = MyHelper.CRC16_C(sendData, 16);
            sendData[16] = tx2[1];
            sendData[17] = tx2[0];

            try
            {
                int sndLen = clientInfo.Client.Send(sendData);
                Log.RecvLog(clientInfo.Sim, "ack ", MyHelper.ByteArray2HexStr(sendData, sndLen));
            }
            catch (Exception ex)
            {
                Log.RecvLog("Error", "Error", ex.Message);
            }
        }       // End of ReplyClient()

        //2017-11-07 Write by AlexWang 身份识别
        public static void ParseIdentity(ref ClientInfo clientInfo, byte[] buffer, int nDataLen)
        {
            if (nDataLen < 11)
            {
                return;
            }
            bool bVerified = clientInfo.Verified;

            if (bVerified)
            {
            }
            else
            {
                clientInfo.Sim = GetSimCard(buffer, 3);
                clientInfo.Verified = true;
                clientInfo.NeedVerify = false;
                ClientInfoCommon.AddClientInfo(clientInfo);
            }

            Log.RecvLog(clientInfo.Sim, "recv", MyHelper.ByteArray2HexStr(buffer, nDataLen));

            byte[] sendData = {0x10, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00,  
                               0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};

            sendData[3] = buffer[3];
            sendData[4] = buffer[4];
            sendData[5] = buffer[5];
            sendData[6] = buffer[6];
            sendData[7] = buffer[7];
            sendData[8] = buffer[8];

            DateTime time = DateTime.Now;
            int nYear = time.Year;
            int nMon = time.Month;
            int nDay = time.Day;
            int nHour = time.Hour;
            int nMin = time.Minute;
            int nSec = time.Second;

            sendData[9] = (byte)(nYear & 0xFF);
            sendData[10] = (byte)((nYear >> 8) & 0xFF);
            sendData[11] = (byte)nMon;
            sendData[12] = (byte)nDay;
            sendData[13] = (byte)nHour;
            sendData[14] = (byte)nMin;
            sendData[15] = (byte)nSec;

            byte[] tx2 = MyHelper.CRC16_C(sendData, 16);
            sendData[16] = tx2[1];
            sendData[17] = tx2[0];

            int sndLen = clientInfo.Client.Send(sendData);

            string strSndMsg = MyHelper.ByteArray2HexStr(sendData, sndLen);
            Log.RecvLog(clientInfo.Sim, "ack identity ", strSndMsg.ToString());                                  // 写接收日志
        }

        // 2018-09-11 解析接收到的数据报文
        public static int ParseRealDataProtocal(ref ClientInfo clientInfo, byte[] buffer, int nDataLen)
        {
            if (nDataLen <= 18)
            {
                return 0;
            }
            bool bVerified = clientInfo.Verified;

            if (bVerified)
            {
            }
            else
            {
                clientInfo.Sim = GetSimCard(buffer, 3);
                clientInfo.Verified = true;
                clientInfo.NeedVerify = false;
                ClientInfoCommon.AddClientInfo(clientInfo);
            }

            //Log.RecvLog(clientInfo.Sim, "recv", buffer, nDataLen);                                  // 写接收日志
            
            // 三相多功能表
            MeterInfo1 mMeterInfo1 = new MeterInfo1();
            mMeterInfo1.InsertTime = DateTime.Now.ToString();
            mMeterInfo1.MsgTime = GetMsgTime(ref buffer, 9, 7);
            mMeterInfo1.GateId = GetSimCard(buffer, 3);
            mMeterInfo1.MeterAddr = GetMeterId(buffer);
            mMeterInfo1.RecvData = MyHelper.ByteArray2HexStr(buffer, nDataLen);

            // ClientInfoCommon.AddMsg(mMeterInfo1);
            Log.RecvLog(clientInfo.Sim, "recv MsgTime:" + mMeterInfo1.MsgTime + " meterId:" + mMeterInfo1.MeterAddr, MyHelper.ByteArray2HexStr(buffer, nDataLen));
            ReplyClient(ref clientInfo, buffer, nDataLen);
            ClientInfoCommon.MsgUpdate(mMeterInfo1);        // 更新数据到内存缓存
          
            return 0;
        }       // End of ParseRealDataProtocal()

        // 解析报文中的时间
        public static bool GetDateTime(ref byte[] buffer, ref StringBuilder strTime, int nPos, int nLen)
        {
            bool bRet = true;

            int nYear = (buffer[nPos + 1] << 8) | buffer[nPos];
            int nMon  = buffer[nPos + 2];
            int nDate = buffer[nPos + 3];
            int nHour = buffer[nPos + 4];
            int nMin  = buffer[nPos + 5];
            int nSec  = buffer[nPos + 6];

            strTime.AppendFormat("{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}", nYear, nMon, nDate, nHour, nMin, nSec);

            return bRet;
        }

        // 获取报文中的日期时间
        public static string GetMsgTime(ref byte[] buffer, int nPos, int nLen)
        {
            //string ret = "";
            StringBuilder strTime;
            strTime = new StringBuilder();

            int nYear = (buffer[nPos + 1] << 8) | buffer[nPos];
            int nMon  = buffer[nPos + 2];
            int nDate = buffer[nPos + 3];
            int nHour = buffer[nPos + 4];
            int nMin  = buffer[nPos + 5];
            int nSec  = buffer[nPos + 6];

            strTime.AppendFormat("{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}", nYear, nMon, nDate, nHour, nMin, nSec);

            return strTime.ToString();
        }

        // 解析报文中的sim卡
        public static string GetSimCard(byte[] recvPacket, int nPos, bool littleEndian = true)
        {
            StringBuilder strSimCard = new StringBuilder();

            if (littleEndian)
            {
                for (int index = 3; index >= 0; index--)
                {
                    strSimCard.AppendFormat("{0:X2}", recvPacket[index + nPos]);
                }
            }
            else
            {
                for (int index = 0; index <= 3; index++)
                {
                    strSimCard.AppendFormat("{0:X2}", recvPacket[index + nPos]);
                }
            }
            return Convert.ToInt32(strSimCard.ToString(), 16).ToString();     // write by wang
        }

        // 获取报文中的表计地址
        public static string GetMeterId(byte[] recvPacket, int nPos = 8)
        {
            StringBuilder strMeterId = new StringBuilder();

            strMeterId.AppendFormat("{0:D3}", recvPacket[nPos]);

            return strMeterId.ToString();
        }

    }       // End of class TcpClientProtocol
}
