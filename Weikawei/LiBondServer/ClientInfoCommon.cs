using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;


namespace LiBondServer
{
    class ClientInfoCommon
    {
        private static Mutex m_Locker;
        static List<ClientInfo> clientInfos;

        private static Mutex m_dataLocker;
        static List<MeterInfo1> m_listMsg;

        private static Mutex m_errorLocker;
        static long m_errCode;

        public static void InitInstance()
        {
            m_Locker     = new Mutex();

            clientInfos  = new List<ClientInfo>();

            m_dataLocker = new Mutex();

            m_listMsg = new List<MeterInfo1>();

            m_errorLocker = new Mutex();
        }

        /// <summary>
        /// 添加一个客户端
        /// </summary>
        /// <param name="clientInfo"></param>
        public static void AddClientInfo(ClientInfo clientInfo)
        {
            m_Locker.WaitOne();
            bool bExist = false;
            int nCount = clientInfos.Count;
            int index = 0;
            for (index = 0; index < nCount; index++)
            {
                if (clientInfos[index].Sim == clientInfo.Sim)
                {
                    bExist = true;
                    break;
                }
            }
            if (bExist)
            {
                clientInfos[index].CloseClient();
                clientInfos.RemoveAt(index);
            }

            clientInfos.Add(clientInfo);

            m_Locker.ReleaseMutex();
        }   // End of AddClientInfo

        /// <summary>
        /// 移除一个客户端
        /// </summary>
        /// <param name="clientInfo"></param>
        public static void RemoveClientInfo(ClientInfo clientInfo)
        {
            m_Locker.WaitOne();
            clientInfos.Remove(clientInfo);
            m_Locker.ReleaseMutex();
        }

        /// <summary>
        /// 移除所有客户端
        /// </summary>
        public static void RemoveAllClientInfo()
        {
            m_Locker.WaitOne();
            while (clientInfos.Count > 0)
            {
                clientInfos[0].CloseClient();
                clientInfos.RemoveAt(0);
            }
            m_Locker.ReleaseMutex();
        }

        public static int ReadClientCount()
        {
            int ret = 0;
            m_Locker.WaitOne();
            ret = clientInfos.Count;
            m_Locker.ReleaseMutex();
            return ret;
        }

        public static List<string> ReadClientList()
        {
            List<string> retListString = new List<string>();
            m_Locker.WaitOne();
            for(int i = 0; i < clientInfos.Count; i ++)
            {
                retListString.Add(clientInfos[i].Sim);
            }
            retListString.Sort();
            m_Locker.ReleaseMutex();

            return retListString;
        }

        public static List<ClientInfo> ReadClientInfoList()
        {
            List<ClientInfo> retListClientInfo = new List<ClientInfo>();
            m_Locker.WaitOne();
            for (int i = 0; i < clientInfos.Count; i++)
            {
                retListClientInfo.Add(clientInfos[i]);
            }
            m_Locker.ReleaseMutex();
            //contactsList = contactsList.OrderByDescending(i=>i.EndTime).ToList();   //降序
            List<ClientInfo> retListClientInfo1 = new List<ClientInfo>();
            retListClientInfo1 = retListClientInfo.OrderBy(ClientInfo => ClientInfo.Sim).ToList();      // 升序

            return retListClientInfo1;
        }

        /// <summary>
        /// 插入消息到消息队列中
        /// </summary>
        /// <param name="mMeterInfo1"></param>
        public static void AddMsg(MeterInfo1 mMeterInfo1)
        {
            m_dataLocker.WaitOne();
            m_listMsg.Add(mMeterInfo1);
            m_dataLocker.ReleaseMutex();
        }

        public static int ReadMsgCount()
        {
            int ret = 0;
            m_dataLocker.WaitOne();
            ret = m_listMsg.Count;
            m_dataLocker.ReleaseMutex();
            return ret;
        }

        public static int ReadMeterList(ref List<MeterInfo1> tm_listMsg)
        {
            int ret = 1;
            m_dataLocker.WaitOne();
            tm_listMsg = m_listMsg;
            m_dataLocker.ReleaseMutex();
            return ret;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mMeterInfo1"></param>
        /// <returns>     0: 消息需要被过滤掉        1：消息不过滤掉</returns>
        public static int MsgFilter(MeterInfo1 mMeterInfo1)
        {
            int ret = 1;

            m_dataLocker.WaitOne();

            for (int i = 0; i < m_listMsg.Count; i++)
            {
                if ((mMeterInfo1.GateId == m_listMsg[i].GateId) && (mMeterInfo1.MeterAddr == m_listMsg[i].MeterAddr))
                {
                    string strTemp = mMeterInfo1.MsgTime;
                    string strTemp1 = m_listMsg[i].MsgTime;
                    int cmp = string.Compare(strTemp, strTemp1);
                    if ((mMeterInfo1.RecvData == m_listMsg[i].RecvData) || (cmp == 0) || (cmp == -1))      // 消息体本身和缓存相同，或者消息的id比实际的缓存中的id小，则不插入数据库
                    {
                        ret = 0;            // 接收到的消息和缓存中的完全一样
                    }
                    else
                    {
                        Log.RecvLog(mMeterInfo1.GateId, "recv MsgFilter", "ret = 1 " + strTemp + strTemp1);
                        ret = 1;            // 接收到的数据和缓存中的数据不一样，需要上传
                    }
                    break;
                }
            }

            m_dataLocker.ReleaseMutex();

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mMeterInfo1"></param>
        /// <returns>     0: 消息需要被过滤掉        1：消息不过滤掉</returns>
        public static int MsgUpdate(MeterInfo1 mMeterInfo1)
        {
            int ret = 1;

            m_dataLocker.WaitOne();

            int cmpMeterId = 0;             // 标志表计消息是否已经在列表中了
            for (int i = 0; i < m_listMsg.Count; i++)
            {
                if ((mMeterInfo1.GateId == m_listMsg[i].GateId) && (mMeterInfo1.MeterAddr == m_listMsg[i].MeterAddr))
                {
                    cmpMeterId = 1;
                    
                    m_listMsg[i] = mMeterInfo1;       // 更新数据
                    break;
                }
            }

            if (cmpMeterId == 0)            // 列表中没有该表计的最近一条消息缓存，则增加一条消息进入列表中，按网关、表计id从小到大排序
            {
                int cmpGate, cmpMeter;

                int i = 0;
                for (i = 0; i < m_listMsg.Count; i++)
                {
                    cmpGate = string.Compare(mMeterInfo1.GateId, m_listMsg[i].GateId);

                    if (cmpGate == -1)
                    {
                        break;
                    }
                    else if (cmpGate == 0)
                    {
                        cmpMeter = string.Compare(mMeterInfo1.MeterAddr, m_listMsg[i].MeterAddr);

                        if(cmpMeter == -1)
                        {
                            break;
                        }
                    }
                }
                m_listMsg.Insert(i, mMeterInfo1);
            }

            m_dataLocker.ReleaseMutex();

            return ret;
        }

        public static void theout(object source, System.Timers.ElapsedEventArgs e)
        {
            //MeterInfo1 mMeterInfo1;
            //m_dataLocker.WaitOne();
            ////if(m_listMsg.Count != 0)
            ////{
            ////    mMeterInfo1 = m_listMsg[0];
            ////    m_listMsg.RemoveAt(0);
            ////    //MySqlConnector.InsertData(mMeterInfo1.InsertTime, mMeterInfo1.MsgTime, mMeterInfo1.GateId, mMeterInfo1.MeterAddr, mMeterInfo1.RecvData);
            ////}
            //m_dataLocker.ReleaseMutex();
        }

        public static void SetErrCode(long err)
        {
            m_errorLocker.WaitOne();

            m_errCode = m_errCode | err;

            m_errorLocker.ReleaseMutex();
        }

        public static void ResetErrCode(long err)
        {
            m_errorLocker.WaitOne();

            m_errCode = m_errCode & (~err);

            m_errorLocker.ReleaseMutex();
        }

        public static long ReadErrCode()
        {
            return m_errCode;
        }

    }
}
