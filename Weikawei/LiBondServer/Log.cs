using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LiBondServer
{
    public class Log
    {
        private static object m_lockRcv = new object();
        private static string m_strPath = "";               // 2016-4-18 Update by AlexWang     
        private static bool m_log = false;

        /// <summary>
        /// 初始化日志类
        /// 2016-4-18 Update by AlexWang 
        /// </summary>
        /// <param name="strPath">文件路径</param>
        /// <param name="strErr">出错信息</param>
        /// <returns>成功返回true，失败返回false</returns>
        public static bool Init(string strLog, string strPath, ref string strErr)
        {
            m_strPath = strPath;
            try
            {
                if (strLog.Contains("ON") || strLog.Contains("On") || strLog.Contains("on"))
                {
                    m_log = true;
                }

                if (Directory.Exists(m_strPath) == true)
                {
                    return true;
                }
                else
                {
                    Directory.CreateDirectory(m_strPath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                strErr = ex.Message;
                return false;
            }

        }

        private static void Write(string filePath, string note, string strMsg)
        {
            try
            {
                FileStream fs = new FileStream(filePath, FileMode.Append);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") +  note + " " + strMsg);
                sw.Close();
                fs.Close();
            }
            catch (System.Exception ex)
            {
                string strInfo = ex.Message;
            }
        }

        public static void RecvLog(string fileName, string note, string strMsg)
        {
            if(!m_log)
            {
                return;
            }

            string strFilePath = m_strPath + "\\Log-" + fileName + DateTime.Now.ToString("-yyyy-MM-dd") + ".txt";
            lock (m_lockRcv)
            {
                Write(strFilePath, note, strMsg);
            }
        }

    }
}
