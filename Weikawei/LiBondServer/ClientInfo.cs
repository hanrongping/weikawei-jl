using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Reflection;

namespace LiBondServer
{
    // 2018-09-12 add
    public class MeterInfo1
    {
        private DateTime m_lastCommunicateTime;             // 最近一次通讯时间
        private string m_insertTime;                        // 数据插入时间
        private string m_msgTime;                           // 报文中的时间
        private string m_gateId;                            // 网关地址
        private string m_MeterAddr;                         // 表地址
        private string m_recvData;                          // 接收到的数据

        public DateTime LastCommunicateTime
        {
            set { m_lastCommunicateTime = value; }
            get { return m_lastCommunicateTime; }
        }

        public string InsertTime
        {
            set { m_insertTime = value; }
            get { return m_insertTime; }
        }

        public string MsgTime
        {
            set { m_msgTime = value; }
            get { return m_msgTime; }
        }

        public string GateId
        {
            set { m_gateId = value; }
            get { return m_gateId; }
        }

        public string MeterAddr
        {
            set { m_MeterAddr = value; }
            get { return m_MeterAddr; }
        }

        public string RecvData
        {
            set { m_recvData = value; }
            get { return m_recvData; }
        }
    }
    public class ClientInfo
    {
        private string m_strSim;
        private bool m_bVerified;
        private bool m_bNeedVerify;
        private DateTime m_ConnectTime;
        private DateTime m_VerifyTime;
        private TcpClient m_tcpClient;
        private bool m_bModify;
        private DateTime m_recvTime;
        private string m_strIp;
        private int m_intPort;
      
      
        public ClientInfo(TcpClient tcpClient)
        {
            m_strSim      = "";
            m_bVerified   = false;
            m_bNeedVerify = true;
            m_ConnectTime = DateTime.Now;
            m_VerifyTime  = DateTime.Now;
            m_recvTime    = DateTime.Now;
            m_tcpClient   = tcpClient;
            m_bModify     = false;
            m_strIp       = GetRemoteIP(tcpClient);
            m_intPort     = GetRemotePort(tcpClient);
        }

        public string StrIp
        {
            get { return m_strIp; }
        }

        public int intPort
        {
            get { return m_intPort; }
        }

        public string Sim
        {
            set { m_strSim = value; }
            get { return m_strSim; }
        }

        public bool Verified
        {
            set { m_bVerified = value; }
            get { return m_bVerified; }
        }

        public bool NeedVerify
        {
            set { m_bNeedVerify = value; }
            get { return m_bNeedVerify; }
        }

        public bool Modify
        {
            set { m_bModify = value; }
            get { return m_bModify; }
        }

        public TcpClient MTcpClient
        {
            set { m_tcpClient = value; }
            get { return m_tcpClient; }
        }

        public Socket Client
        {
            get { return m_tcpClient.Client; }
        }


        public DateTime ConnectTime
        {
            get { return m_ConnectTime; }
        }

        public DateTime VerifyTime
        {
            set { m_VerifyTime = value; }
            get { return m_VerifyTime; }
        }

        public DateTime RecvTime
        {
            set { m_recvTime = value; }
            get { return m_recvTime; }
        }

        public bool VerifiedTimeout()
        {
            if (true == m_bNeedVerify && m_VerifyTime.AddSeconds(30) <= DateTime.Now)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }

        public void CloseClient()
        {
            try
            {
                m_tcpClient.Close();
            }
            catch (Exception ex)
            {
                Log.RecvLog("Error", "Error", ex.Message);
            }
        }

        public Socket GetSocket(TcpClient cln)
        {
            //PropertyInfo pi = cln.GetType().GetProperty("Client", BindingFlags.NonPublic | BindingFlags.Instance);
            //Socket sock = (Socket)pi.GetValue(cln, null);
            Socket sock = cln.Client;
            return sock;
        }

        // 获取ip
        public string GetRemoteIP(TcpClient cln)
        {
            string ip = GetSocket(cln).RemoteEndPoint.ToString().Split(':')[0];
            return ip;
        }

        // 获取端口
        public int GetRemotePort(TcpClient cln)
        {
            string temp = GetSocket(cln).RemoteEndPoint.ToString().Split(':')[1];
            int port = Convert.ToInt32(temp);
            return port;
        }
    }
}
