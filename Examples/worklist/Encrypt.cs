using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.International.Converters.PinYinConverter;

namespace ZyWorkListScp
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
    //工具类
    public static class Utility
    {
        /// <summary>
        /// 获取汉字的拼音
        /// </summary>
        /// <param name="input">汉字字符串</param>
        /// <param name="IsUpper">大写则true</param>
        public static string  GetPy(string input,bool IsUpper=true)
        {
            StringBuilder result = new StringBuilder();
            char[] chs = input.ToCharArray();
            
            foreach (char ch in chs)
            {
                if (IsUpper)
                {
                    result.Append(GetPy(ch).ToUpper()+ " ");
                }
                else
                {
                    result.Append(GetPy(ch).ToLower() + " ");
                }
            }


            return result.ToString();
            
        }
        /// <summary>
        /// 获取单个汉字的拼音
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static string GetPy(char ch)
        {
            if (Microsoft.International.Converters.PinYinConverter.ChineseChar.IsValidChar(ch))
            {
                //得到第一个拼音
                string result = "";
                SortedDictionary<int, string> dict = new SortedDictionary<int, string>();
                ChineseChar chineseChar = new ChineseChar(ch);
                if (chineseChar.IsHomophone(ch))
                {


                    if (chineseChar.PinyinCount == 1)
                    {
                        return Regex.Replace(chineseChar.Pinyins[0], @"\d", "");
                    }
                    else
                    {
                        for (int i = 0; i < chineseChar.PinyinCount; i++)
                        {
                            string pinyin = chineseChar.Pinyins[i].ToString();
                            pinyin = Regex.Replace(pinyin, @"\d", "");

                            if (!dict.ContainsValue(pinyin))
                            {
                                dict.Add(i, pinyin);
                            }

                        }
                        int count = 0;
                        foreach (var item in dict.Values)
                        {
                            if (count == 1)
                            {
                                result = item.ToString();
                            }
                            if (dict.Values.Count == 1)
                            {
                                result = item.ToString();
                            }
                            count += 1;
                        }
                        return result;
                    }
                }
                else
                {
                    return Regex.Replace(chineseChar.Pinyins[0], @"\d", "");
                }
            }
            else
            {
                return ch.ToString();
            }
        }
    }
}
