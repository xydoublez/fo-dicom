using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ZYCGETScp
{
    public static class Encrypt
    {
        #region ========加密========
        public static string GetMD5String(string strData)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider myMD5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] bytBuf = System.Text.Encoding.Unicode.GetBytes(strData);
            byte[] md5 = myMD5.ComputeHash(bytBuf, 0, bytBuf.Length);
            return ByteToHex(md5);
        }
        public static string ByteToHex(byte[] bytBuf)
        {
            System.Text.StringBuilder myStr = new System.Text.StringBuilder();
            for (int iCount = 0; iCount < bytBuf.Length; iCount++)
            {
                myStr.Append(bytBuf[iCount].ToString("X2"));
            }
            return myStr.ToString();
        }
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="Text">要加密的文本</param>
        /// <returns></returns>
        public static string GetEncrypt(string Text)
        {
            return GetEncrypt(Text, "zyhis*!is!~no.1");
        }
        /// <summary> 
        /// 加密
        /// </summary> 
        /// <param name="Text">要加密的原文</param> 
        /// <param name="sKey">使用的密钥</param> 
        /// <returns></returns> 
        public static string GetEncrypt(string Text, string sKey)
        {

            if (sKey.Length < 14)
            {
                sKey += "zyhis~!@isNo1zyhis";
            }
            string sv = sKey.Substring(6, 8);
            sKey = sKey.Substring(0, 8);
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            byte[] inputByteArray;
            inputByteArray = Encoding.UTF8.GetBytes(Text);
            des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
            des.IV = ASCIIEncoding.ASCII.GetBytes(sv);
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            StringBuilder ret = new StringBuilder();
            foreach (byte b in ms.ToArray())
            {
                ret.AppendFormat("{0:X2}", b);
            }
            return ret.ToString();
        }

        #endregion

        #region ========解密========


        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="Text">要解密的文本</param>
        /// <returns></returns>
        public static string Decrypt(string Text)
        {
            return Decrypt(Text, "zyhis*!is!~no.1");
        }
        /// <summary> 
        /// 解密数据 
        /// </summary> 
        /// <param name="Text">要解密的密文</param> 
        /// <param name="sKey">使用的密钥</param> 
        /// <returns></returns> 
        public static string Decrypt(string Text, string sKey)
        {
            if (sKey.Length < 14)
            {
                sKey += "zyhis~!@isNo1zyhis";
            }
            string sv = sKey.Substring(6, 8);
            sKey = sKey.Substring(0, 8);

            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            int len;
            len = Text.Length / 2;
            byte[] inputByteArray = new byte[len];
            int x, i;
            for (x = 0; x < len; x++)
            {
                i = Convert.ToInt32(Text.Substring(x * 2, 2), 16);
                inputByteArray[x] = (byte)i;
            }
            des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
            des.IV = ASCIIEncoding.ASCII.GetBytes(sv);
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        #endregion


    }
    public class test
    {
        public bool testMethod(string info)
        {
            return true;
        }
        public bool test2(string name)
        {
            return false;
            
        }
    }
    public class DataBase
    {
        static string constr = Encrypt.Decrypt(System.Configuration.ConfigurationManager.AppSettings["ZYPACSDB"], "ihepass");
        public static  DataSet GetImage(string studySopinstanceUid)
        {
            DataSet ds = new DataSet();
            string sql = @"  SELECT  A.PATIENT_ID  patientId, A.STUDY_IDENTITY  studyIdentity,A.STUDY_ID studyId, B.SERIES_IDENTITY  seriesIdentity, A.STUDY_INSTANCE_UID  studyInstanceUid, B.SERIES_INSTANCE_UID  seriesInstanceUid, C.SOPINSTANCE_UID  sopinstanceUid,
                            C.REFERENCE_FILE
                            referenceFile, A.MODALITY  modality,B.SERIES_NUMBER seriesNumber,C.INSTANCE_NUMBER instanceNumber,C.IMAGE_IDENTITY imageIdentity,A.STUDY_DATE studyDate,A.STUDY_TIME studyTime,A.FLAG_ARCHIVE flagArchive,B.IMAGE_VISIT_LEVEL imageVisitLevel,A.HOSPITAL_ID hospitalId,
                            C.FILEM_PRINTED  filemPrinted,B.SERIES_DESCRIPTION seriesDescription
                            FROM         PACS_CONSULTATION_STUDY A  INNER JOIN PACS_CONSULTATION_SERIES B  ON A.STUDY_IDENTITY = B.STUDY_IDENTITY
                            INNER JOIN PACS_CONSULTATION_IMAGE C  ON B.SERIES_IDENTITY = C.SERIES_IDENTITY
                            WHERE  A.STUDY_INSTANCE_UID=:studyInstanceUid order by A.STUDY_IDENTITY,B.SERIES_NUMBER,B.SERIES_INSTANCE_UID,C.INSTANCE_NUMBER";



            using (OracleConnection conn = new OracleConnection(constr))
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("studySopinstanceUid", studySopinstanceUid));
                    using (OracleDataAdapter adp = new OracleDataAdapter(cmd))
                    {
                        adp.Fill(ds, "ds");
                    }
                }
            }
            return ds;
         
        }
    }
    public class Log
    {
        public static void Add(string oper,string msg)
        {
            try
            {
                var dir = AppDomain.CurrentDomain.BaseDirectory + "\\log";
                var log = dir + "\\" + System.DateTime.Now.ToString("yyyyMMdd") + "log.txt";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                if (File.Exists(log))
                {
                    var file = new FileInfo(log);
                    if (file.Length >= 1024 * 1024 * 10) //100M
                    {
                        file.Delete();
                    }
                }
                File.AppendAllText(log, "\r\n时间:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "操作：" + oper + "\r\n内容：" + msg);
            }
            catch
            {

            }
        }
    }
}
