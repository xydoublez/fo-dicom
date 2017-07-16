using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
namespace ZYStoreScp
{
    public class Log
    {
        private bool enableLog = false; //是否开启错误日志
        public Log(string error)
        {
            if (ConfigurationManager.AppSettings["LOG"].ToString() == "1")
            {
                enableLog = true;
            }
            if (enableLog)
            {
                if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\WorkListLOG.log"))
                {
                    File.Create(AppDomain.CurrentDomain.BaseDirectory + "\\WorkListLOG.log");
                }
                FileStream fs = File.OpenWrite(AppDomain.CurrentDomain.BaseDirectory + "\\WorkListLOG.log");
                byte[] file = System.Text.Encoding.UTF8.GetBytes(error);
                fs.Write(file, 0, file.Length);
                fs.Close();
               

            }

        }
    }
}
