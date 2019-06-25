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


namespace LiBondServer
{
    public partial class Form1 : Form
    {
        TcpListener m_tcpListener;
        private string m_strIp;
        private int   m_nPort;
        private Mutex m_lockPacket;

        public Form1()
        {
            ClientInfoCommon.InitInstance();
            

            InitializeComponent();
            ListViewInit();
            ListView2Init();
            this.label2.Text = DateTime.Now.ToString();

            System.Timers.Timer t = new System.Timers.Timer(1000);            // 实例化Timer类，设置间隔时间为500毫秒；
            t.Elapsed += new System.Timers.ElapsedEventHandler(ClientInfoCommon.theout);     // 到达时间的时候执行事件；
            t.AutoReset = true;                                             // 设置是执行一次（false）还是一直执行(true)；
            t.Enabled = true;                                               // 是否执行System.Timers.Timer.Elapsed事件；
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            m_lockPacket = new Mutex();

            string strErr  = "";
            string strLog = "OFF";
            string strPath = "";         

            try
            {
                strLog = ConfigurationManager.AppSettings["Log"].ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            try
            {
                strPath = ConfigurationManager.AppSettings["LogPath"].ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            

            if (Log.Init(strLog, strPath, ref strErr))
            {
                
            }
            else
            {
                this.Close();
                return;
            }


            //获取监听端口
            m_nPort = Convert.ToInt32(ConfigurationManager.AppSettings["Port"]);
            m_strIp = ConfigurationManager.AppSettings["IP"].ToString();

            //设置通讯socket
            IPAddress ip = IPAddress.Parse(m_strIp);
            try
            {
                m_tcpListener = new TcpListener(ip, m_nPort);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            //启动服务
            Thread startThread = new Thread(StartServer);
            startThread.IsBackground = true;
            startThread.Start();
        }

        // 启动服务
        private void StartServer(object obj)
        {
            // 开监听端口
            try
            {
                m_tcpListener.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                System.Environment.Exit(0);
            }

            while (true)
            {
                try
                {
                    // 接收连接请求
                    TcpClient tcpClient = m_tcpListener.AcceptTcpClient();
                    Thread clientThread = new Thread(ClientServer);
                    clientThread.IsBackground = true;
                    clientThread.Start(tcpClient);
                }
                catch (Exception ex)
                {
                    //Log.ErrLog(ex.Message);
                    Log.RecvLog("Error", "Error", ex.Message);
                    break;
                }
            }
        }

        private int RecvPacket(Socket client, Byte[] recvPacket, ref int nDataLen)
        {
            // 0接收到完整报文 1客户端断开或异常 2报文不正确 3超时
            int nLeft = 11;
            int nRead = 0;
            int nTotal = 0;
            int nErr = 0;

            while (true)
            {
                try
                {
                    nRead = client.Receive(recvPacket, nTotal, nLeft, SocketFlags.Partial);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        nErr = 3;
                        break;
                    }
                    else if (ex.SocketErrorCode == SocketError.SocketError)
                    {
                        //Log.ErrLog(string.Format("客户端Socket异常", ex.Message));
                        Log.RecvLog("Error", "客户端Socket异常", ex.Message);
                    }
                }
                nTotal += nRead;
                if (nRead == 0 || nRead == -1)
                {
                    nErr = 1;   //1表示客户端断开或异常
                    break;
                }
                else
                {
                    if (nTotal > 2 && nDataLen == 0)
                    {
                        int hi = recvPacket[1] << 8;
                        int lo = recvPacket[0];
                        nDataLen = (hi | lo) + 2;
                    }
                    if (nDataLen == nTotal)
                    {

                        byte[] tx2 = MyHelper.CRC16_C(recvPacket, nDataLen - 2);

                        if (recvPacket[nDataLen - 2] == tx2[1] && recvPacket[nDataLen - 1] == tx2[0])       // crc校验
                        {
                            nErr = 0;
                        }
                        else
                        {
                            nErr = 2;
                        }
                        break;
                    }
                    else
                    {
                        if (nDataLen == 0)
                        {
                            nLeft = 11 - nTotal;
                        }
                        else
                        {
                            nLeft = nDataLen - nTotal;
                        }
                    }
                }

            }
            return nErr;
        }

        //客户端处理服务
        private void ClientServer(object obj)
        {
            TcpClient tcpClient = (TcpClient)obj;
            tcpClient.ReceiveTimeout = 1000;
            bool bVerify = false;

            ClientInfo clientInfo = new ClientInfo(tcpClient);

            Byte[] recvPacket = new Byte[1024];
            int nErr = 0;
            int nDataLen = 0;

            while (true)
            {
                try
                {
                    Array.Clear(recvPacket, 0, 1024);
                    nDataLen = 0;
                    nErr = RecvPacket(clientInfo.Client, recvPacket, ref nDataLen);

                    if (nDataLen > 0)
                    {
                        try
                        {
                            
                        }
                        catch (Exception ex)
                        {
                            //Log.ErrLog(string.Format("解析报文出错, {0}", ex.Message));
                            Log.RecvLog("Error", "解析报文出错", ex.Message);
                        }
                    }
                    if (0 == nErr)
                    {
                        try
                        {
                            TcpClientProtocol.ParseData(ref clientInfo, recvPacket, nDataLen);
                        }
                        catch (Exception ex)
                        {
                            //Log.ErrLog(string.Format("解析报文出错, {0}", MyHelper.ByteArray2HexStr(recvPacket, nDataLen)));
                            //Log.ErrLog(ex.Message);
                            Log.RecvLog("Error", "解析报文出错", MyHelper.ByteArray2HexStr(recvPacket, nDataLen));
                            Log.RecvLog("Error", "Error", ex.Message);
                        }
                        clientInfo.RecvTime = DateTime.Now;
                    }
                    else if (1 == nErr)
                    {
                        clientInfo.CloseClient();
                        break;
                    }
                    else if (2 == nErr)
                    {

                    }
                    else if (3 == nErr)
                    {
                        // 超时处理
                        if (clientInfo.Verified)
                        {
                            if (clientInfo.RecvTime.AddMinutes(15) < DateTime.Now)
                            {
                                String str = string.Format("{0}时连接上来的{1}客户端已超时断开", clientInfo.ConnectTime.ToString("yyyy-MM-dd HH:mm:ss"), clientInfo.Client.RemoteEndPoint.ToString()) ;
                                Log.RecvLog("Error", "Error", str);
                                clientInfo.CloseClient();
                                break;
                            }
                            else
                            { }
                        }
                        else
                        {
                            if (clientInfo.VerifiedTimeout())
                            {
                                // 未经身份验证
                                if (bVerify)
                                {
                                    String str = string.Format("{0}时连接上来的{1}客户端失去心跳响应", clientInfo.ConnectTime.ToString("yyyy-MM-dd HH:mm:ss"), clientInfo.Sim);
                                    Log.RecvLog("Error", "Error", str);
                                }
                                else
                                {
                                    String str = string.Format("{0}时连接上来的{1}是非授权客户端", clientInfo.ConnectTime.ToString("yyyy-MM-dd HH:mm:ss"), clientInfo.Client.RemoteEndPoint.ToString());
                                    Log.RecvLog("Error", "Error", str);
                                }
                                clientInfo.CloseClient();
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    string strErr = ex.Message;
                    if (clientInfo != null)
                    {
                        if (clientInfo.Client != null)
                        {
                            String str = string.Format("{0}时连接上来的{1}客户端异常断开,{2}", clientInfo.ConnectTime.ToString("yyyy-MM-dd HH:mm:ss"), clientInfo.Client.RemoteEndPoint.ToString(), strErr);
                            Log.RecvLog("Error", "Error", str);
                        }
                        else
                        {
                            String str = string.Format("{0}时连接上来的{1}客户端异常断开,{2}", clientInfo.ConnectTime.ToString("yyyy-MM-dd HH:mm:ss"), clientInfo.Sim, strErr);
                            Log.RecvLog("Error", "Error", ex.Message);
                        }
                        clientInfo.CloseClient();
                    }
                    else
                    {
                        
                    }
                    break;
                }
            }

            ClientInfoCommon.RemoveClientInfo(clientInfo);
        }

        //停止服务
        private void StopServer()
        {
            m_tcpListener.Stop();

            ClientInfoCommon.RemoveAllClientInfo();

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopServer();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            labelClientCount.Text = ClientInfoCommon.ReadClientCount().ToString();

            //ListViewUpdateData(this.listView1, ClientInfoCommon.ReadClientList());
            //List<MeterInfo1> tmpMeterInfo1List = new List<MeterInfo1>();
            //ClientInfoCommon.ReadMeterList(ref tmpMeterInfo1List);
            //ListView2UpdateData(this.listView2, tmpMeterInfo1List);

            label4.Text = ClientInfoCommon.ReadMsgCount().ToString();           // 更新内部缓存数量
        }

        /// <summary>
        /// 初始化listView控件
        /// </summary>
        public void ListViewInit()
        {
            //this.listView1.BeginUpdate();                                           // 数据更新，UI暂时挂起，直到EndUpdate绘制控件，可以有效避免闪烁并大大提高加载速度 
            this.listView1.Columns.Add("ID", 50, HorizontalAlignment.Left);          // 一步添加
            this.listView1.Columns.Add("Client", 120, HorizontalAlignment.Left);      // 一步添加
            this.listView1.Columns.Add("Ip", 150, HorizontalAlignment.Left);
            this.listView1.Columns.Add("Port", 80, HorizontalAlignment.Left);
            //this.listView1.EndUpdate();                                             // 结束数据处理，UI界面一次性绘制。
        }

        public void ListViewUpdateData(ListView listView1, List<string> mSimList)
        {

            listView1.BeginUpdate();  //数据更新，UI暂时挂起，直到EndUpdate绘制控件，可以有效避免闪烁并大大提高加载速度 

            try
            {
                listView1.Clear();
                ListViewInit();

                for (int i = 0; i < mSimList.Count; i++)  //添加10行数据 
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = (i + 1).ToString();
                    lvi.SubItems.Add(mSimList[i]);
                    this.listView1.Items.Add(lvi);
                }

                //隔行显示不同的颜色
                for (int k = 0; k < listView1.Items.Count; k++)
                {
                    if (k % 2 == 0)
                    {
                        listView1.Items[k].BackColor = Color.DarkGray;
                    }
                }
            }
            catch
            {
 
            }

            listView1.EndUpdate();  //结束数据处理，UI界面一次性绘制。 
        }

        public void ListViewUpdateData(ListView listView1, List<ClientInfo> mClientInfo)
        {

            listView1.BeginUpdate();  //数据更新，UI暂时挂起，直到EndUpdate绘制控件，可以有效避免闪烁并大大提高加载速度 

            try
            {
                listView1.Clear();
                ListViewInit();

                for (int i = 0; i < mClientInfo.Count; i++)  //添加10行数据 
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = (i + 1).ToString();
                    lvi.SubItems.Add(mClientInfo[i].Sim);
                    lvi.SubItems.Add(mClientInfo[i].StrIp);
                    lvi.SubItems.Add(Convert.ToString(mClientInfo[i].intPort));
                    this.listView1.Items.Add(lvi);
                }

                //隔行显示不同的颜色
                for (int k = 0; k < listView1.Items.Count; k++)
                {
                    if (k % 2 == 0)
                    {
                        listView1.Items[k].BackColor = Color.DarkGray;
                    }
                }
            }
            catch
            {

            }

            listView1.EndUpdate();  //结束数据处理，UI界面一次性绘制。 
        }

        /// <summary>
        /// 初始化listView控件
        /// </summary>
        public void ListView2Init()
        {
            //this.listView2.BeginUpdate();                                           // 数据更新，UI暂时挂起，直到EndUpdate绘制控件，可以有效避免闪烁并大大提高加载速度 
            this.listView2.Columns.Add("ID", 50, HorizontalAlignment.Left);           // 一步添加
            this.listView2.Columns.Add("gateId",  120, HorizontalAlignment.Left);      // 一步添加
            this.listView2.Columns.Add("meterId", 70, HorizontalAlignment.Left);      // 一步添加
            this.listView2.Columns.Add("msgTime", 250, HorizontalAlignment.Left);     // 一步添加
            //this.listView2.EndUpdate();                                             // 结束数据处理，UI界面一次性绘制。
        }

        public void ListView2UpdateData(ListView listViewtmp, List<MeterInfo1> mMeterInfo)
        {

            listViewtmp.BeginUpdate();  //数据更新，UI暂时挂起，直到EndUpdate绘制控件，可以有效避免闪烁并大大提高加载速度 

            try
            {
                listViewtmp.Clear();
                ListView2Init();

                for (int i = 0; i < mMeterInfo.Count; i++)  //添加10行数据 
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = (i + 1).ToString();
                    lvi.SubItems.Add(mMeterInfo[i].GateId);
                    lvi.SubItems.Add(mMeterInfo[i].MeterAddr);
                    lvi.SubItems.Add(mMeterInfo[i].MsgTime);
                    listViewtmp.Items.Add(lvi);
                }

                //隔行显示不同的颜色
                for (int k = 0; k < listView2.Items.Count; k++)
                {
                    if (k % 2 == 0)
                    {
                        listViewtmp.Items[k].BackColor = Color.DarkGray;
                    }
                }
            }
            catch
            {

            }

            listViewtmp.EndUpdate();  //结束数据处理，UI界面一次性绘制。 
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            //ListViewUpdateData(this.listView1, ClientInfoCommon.ReadClientList());
            ListViewUpdateData(this.listView1, ClientInfoCommon.ReadClientInfoList());
            List<MeterInfo1> tmpMeterInfo1List = new List<MeterInfo1>();
            ClientInfoCommon.ReadMeterList(ref tmpMeterInfo1List);
            ListView2UpdateData(this.listView2, tmpMeterInfo1List);
        }

    }       // End of public partial class Form1 : Form

}

