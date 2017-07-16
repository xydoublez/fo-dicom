using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace ZYStoreScp
{
    /// <summary>
    /// DICOM文件数据库操作类
    /// </summary>
    public class dbUtility
    {
        private string connectionString = "";
        string strConfigModality = "";
        string strHospitalId = "-1";
        string strServiceMode = "0";
        string strModality = "";
        public dbUtility(string strHospitalId,string strConfigModality)
        {
            this.strHospitalId = strHospitalId;
            this.strConfigModality = strConfigModality;
            this.connectionString = Encrypt.Decrypt(System.Configuration.ConfigurationManager.AppSettings["ZYPACSDB"], "ihepass");
        }
        //检查是否存在记录
        private decimal CheckInformation(string strTableName, Hashtable htCheckFields, string strReturnField)
        {
            SqlConnection conn = new SqlConnection(connectionString);
            try
            {
                //1、打开数据库连接
                conn.Open();

                //2、构建查询字符串
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT ");
                sb.Append(strReturnField);
                sb.Append(" FROM ");
                sb.Append(strTableName);
                sb.Append(" WHERE ");
                
                foreach (DictionaryEntry de in htCheckFields)
                {
                    sb.Append(de.Key);
                    sb.Append(" = ");
                    sb.Append("@");
                    sb.Append(de.Key);
                    sb.Append(" AND ");
                }

                //3、创建命令的参数；
                List<SqlParameter> list_parameters = new List<SqlParameter>();
                foreach (DictionaryEntry de in htCheckFields)
                {
                    list_parameters.Add(new SqlParameter("@" + de.Key, de.Value));
                }

                //4、声明命令对象，并加入参数；

                SqlCommand cmd = new SqlCommand(sb.ToString().Substring(0, sb.ToString().Length - 5), conn);
                cmd.CommandTimeout = 0;
                foreach (SqlParameter param in list_parameters)
                {
                    cmd.Parameters.Add(param);
                }


                //5、获取数据流
                decimal decResult = 0;
                SqlDataReader datareader = cmd.ExecuteReader();
                if (datareader.Read())
                {
                    decResult = (decimal)datareader[0];
                }
                else
                {
                    decResult = 0;
                }

                //6、
                datareader.Close();

                return decResult;
            }
            catch (Exception e)
            {
                new Log("CheckInformation_" + strTableName + ":" + e.Message);
                throw e;
            }
            finally
            {
                conn.Close();
            }
            //

        }

        //添加到Patient表
        private decimal AddPatient(Hashtable ht)
        {

            SqlConnection conn = new SqlConnection(connectionString);


            try
            {
                //0、打开数据连接
                conn.Open();


                //1、构建SQL语句
                StringBuilder sb = new StringBuilder();
                sb.Append("INSERT INTO [PATIENT](PATIENT_IDENTITY,PATIENT_ID,PATIENT_NAME,PATIENT_BIRTHDATE,PATIENT_SEX,PATIENT_COMMENTS,OTHER_PATIENT_ID,OTHER_PATIENT_NAME)");
                sb.Append(" Values (@PATIENT_IDENTITY,@PATIENT_ID,@PATIENT_NAME,@PATIENT_BIRTHDATE,@PATIENT_SEX,@PATIENT_COMMENTS,@OTHER_PATIENT_ID,@OTHER_PATIENT_NAME)");

                //2、声明command对象
                SqlCommand cmd = new SqlCommand(sb.ToString(), conn);
                cmd.CommandTimeout = 0;

                //3、构建参数列表
                decimal dec_new_patient_id;
                TableIdentity tab = new TableIdentity();
                dec_new_patient_id = tab.GetPKNum("PATIENT");

                List<SqlParameter> list_parameters = new List<SqlParameter>();
                list_parameters.Add(new SqlParameter("@PATIENT_IDENTITY", dec_new_patient_id));
                list_parameters.Add(new SqlParameter("@PATIENT_ID", ht["PATIENT_ID"]));
                list_parameters.Add(new SqlParameter("@PATIENT_NAME", ht["PATIENT_NAME"]));
                list_parameters.Add(new SqlParameter("@PATIENT_BIRTHDATE", ht["PATIENT_BIRTHDATE"]));
                list_parameters.Add(new SqlParameter("@PATIENT_SEX", ht["PATIENT_SEX"]));
                list_parameters.Add(new SqlParameter("@PATIENT_COMMENTS", ht["PATIENT_COMMENTS"]));
                list_parameters.Add(new SqlParameter("@OTHER_PATIENT_ID", ht["OTHER_PATIENT_ID"]));
                list_parameters.Add(new SqlParameter("@OTHER_PATIENT_NAME", ht["OTHER_PATIENT_NAME"]));


                //4、遍历SqlParameter，添加到cmd.Parameters

                foreach (SqlParameter param in list_parameters)
                {
                    if ((param.Direction == ParameterDirection.InputOutput || param.Direction == ParameterDirection.Input) &&
                    (param.Value == null))
                    {
                        param.Value = DBNull.Value;
                    }
                    cmd.Parameters.Add(param);
                }

                //5、执行查询语句
                cmd.ExecuteNonQuery();

                //6、


                return dec_new_patient_id;
            }
            catch (Exception e)
            {
                new Log("AddPatient:" + e.Message);
                throw e;
            }
            finally
            {
                conn.Close();
            }
        }

        //添加Study表
        private decimal AddStudy(Hashtable ht)
        {
            SqlConnection conn = new SqlConnection(connectionString);

            try
            {
                //0、打开数据连接
                conn.Open();


                //1、构建SQL语句
                StringBuilder sb = new StringBuilder();
                sb.Append("INSERT INTO [STUDY](STUDY_IDENTITY,STUDY_INSTANCE_UID,STUDY_ID,PATIENT_ID,ACCESSION_NUMBER,STUDY_DATE,STUDY_TIME,STUDY_DESCRIPTION,PATIENT_AGE,REFERRING_PHYSICIAN_NAME,ADDITIONAL_PATIENT_HISTORY,STATION_NAME,MODALITY,HOSPITAL_ID)");
                sb.Append(" Values (@STUDY_IDENTITY,@STUDY_INSTANCE_UID,@STUDY_ID,@PATIENT_ID,@ACCESSION_NUMBER,@STUDY_DATE,@STUDY_TIME,@STUDY_DESCRIPTION,@PATIENT_AGE,@REFERRING_PHYSICIAN_NAME,@ADDITIONAL_PATIENT_HISTORY,@STATION_NAME,@MODALITY,@HOSPITAL_ID)");

                //2、声明command对象
                SqlCommand cmd = new SqlCommand(sb.ToString(), conn);
                cmd.CommandTimeout = 0;
                //3、构建参数列表
                decimal dec_new_study_id;
                TableIdentity tab = new TableIdentity();
                dec_new_study_id = tab.GetPKNum("STUDY");

                List<SqlParameter> list_parameters = new List<SqlParameter>();
                list_parameters.Add(new SqlParameter("@STUDY_IDENTITY", dec_new_study_id));
                list_parameters.Add(new SqlParameter("@STUDY_INSTANCE_UID", ht["STUDY_INSTANCE_UID"]));
                list_parameters.Add(new SqlParameter("@STUDY_ID", ht["STUDY_ID"]));
                list_parameters.Add(new SqlParameter("@PATIENT_ID", ht["PATIENT_ID"]));
                list_parameters.Add(new SqlParameter("@ACCESSION_NUMBER", ht["ACCESSION_NUMBER"]));
                list_parameters.Add(new SqlParameter("@STUDY_DATE", ht["STUDY_DATE"]));
                list_parameters.Add(new SqlParameter("@STUDY_TIME", ht["STUDY_TIME"]));
                list_parameters.Add(new SqlParameter("@STUDY_DESCRIPTION", ht["STUDY_DESCRIPTION"]));
                list_parameters.Add(new SqlParameter("@PATIENT_AGE", ht["PATIENT_AGE"]));
                list_parameters.Add(new SqlParameter("@REFERRING_PHYSICIAN_NAME", ht["REFERRING_PHYSICIAN_NAME"]));
                list_parameters.Add(new SqlParameter("@ADDITIONAL_PATIENT_HISTORY", ht["ADDITIONAL_PATIENT_HISTORY"]));
                list_parameters.Add(new SqlParameter("@STATION_NAME", ht["STATION_NAME"]));
                list_parameters.Add(new SqlParameter("@MODALITY", strModality));
                list_parameters.Add(new SqlParameter("@HOSPITAL_ID", strHospitalId));
                //4、遍历SqlParameter，添加到cmd.Parameters

                foreach (SqlParameter param in list_parameters)
                {
                    if ((param.Direction == ParameterDirection.InputOutput || param.Direction == ParameterDirection.Input) &&
                      (param.Value == null))
                    {
                        param.Value = DBNull.Value;
                    }
                    cmd.Parameters.Add(param);
                }

                //5、执行查询语句
                cmd.ExecuteNonQuery();

                //6、


                return dec_new_study_id;
            }
            catch (Exception e)
            {
                new Log("AddStudy:" + e.Message);
                throw e;
            }
            finally
            {
                conn.Close();
            }
        }

        //添加系列表
        private decimal AddSeries(Hashtable ht, decimal dec_study_identity)
        {
            SqlConnection conn = new SqlConnection(connectionString);

            try
            {
                //0、打开数据连接
                conn.Open();



                //1、构建SQL语句
                StringBuilder sb = new StringBuilder();
                sb.Append("INSERT INTO [SERIES](SERIES_IDENTITY,STUDY_IDENTITY,SERIES_INSTANCE_UID,SERIES_NUMBER,SERIES_DATE,SERIES_TIME,MODALITY,SERIES_DESCRIPTION,BODY_PART_EXAMINED,OPERATORS_NAME,PROTOCOL_NAME,PATIENT_ID)");
                sb.Append(" Values (@SERIES_IDENTITY,@STUDY_IDENTITY,@SERIES_INSTANCE_UID,@SERIES_NUMBER,@SERIES_DATE,@SERIES_TIME,@MODALITY,@SERIES_DESCRIPTION,@BODY_PART_EXAMINED,@OPERATORS_NAME,@PROTOCOL_NAME,@PATIENT_ID)");

                //2、声明command对象
                SqlCommand cmd = new SqlCommand(sb.ToString(), conn);
                cmd.CommandTimeout = 0;
                //3、构建参数列表
                decimal dec_new_series_id;
                TableIdentity tab = new TableIdentity();
                dec_new_series_id = tab.GetPKNum("SERIES");


                List<SqlParameter> list_parameters = new List<SqlParameter>();
                list_parameters.Add(new SqlParameter("@SERIES_IDENTITY", dec_new_series_id));
                list_parameters.Add(new SqlParameter("@STUDY_IDENTITY", dec_study_identity));
                list_parameters.Add(new SqlParameter("@SERIES_INSTANCE_UID", ht["SERIES_INSTANCE_UID"]));
                list_parameters.Add(new SqlParameter("@SERIES_NUMBER", ht["SERIES_NUMBER"]));
                list_parameters.Add(new SqlParameter("@SERIES_DATE", ht["SERIES_DATE"]));
                list_parameters.Add(new SqlParameter("@SERIES_TIME", ht["SERIES_TIME"]));
                list_parameters.Add(new SqlParameter("@MODALITY", strModality));
                list_parameters.Add(new SqlParameter("@SERIES_DESCRIPTION", ht["SERIES_DESCRIPTION"]));
                list_parameters.Add(new SqlParameter("@BODY_PART_EXAMINED", ht["BODY_PART_EXAMINED"]));
                list_parameters.Add(new SqlParameter("@OPERATORS_NAME", ht["OPERATORS_NAME"]));
                list_parameters.Add(new SqlParameter("@PROTOCOL_NAME", ht["PROTOCOL_NAME"]));
                list_parameters.Add(new SqlParameter("@PATIENT_ID", ht["PATIENT_ID"]));

                //4、遍历SqlParameter，添加到cmd.Parameters

                foreach (SqlParameter param in list_parameters)
                {
                    if ((param.Direction == ParameterDirection.InputOutput || param.Direction == ParameterDirection.Input) &&
                      (param.Value == null))
                    {
                        param.Value = DBNull.Value;
                    }
                    cmd.Parameters.Add(param);
                }

                //5、执行查询语句
                cmd.ExecuteNonQuery();


                //6、


                return dec_new_series_id;
            }
            catch (Exception e)
            {
                new Log("AddSeries:" + e.Message);
                throw e;
            }
            finally
            {
                conn.Close();
            }

        }

        //添加图像表
        private decimal AddImage(Hashtable ht, decimal dec_series_identity)
        {
            SqlConnection conn = new SqlConnection(connectionString);


            try
            {
                //0、打开数据连接
                conn.Open();

                //1、构建SQL语句
                StringBuilder sb = new StringBuilder();
                sb.Append("INSERT INTO [IMAGE_");
                sb.Append(strModality);
                sb.Append("] (IMAGE_IDENTITY,SERIES_IDENTITY,SOPINSTANCE_UID,SOPCLASS_UID,INSTANCE_NUMBER,ACQUISITION_NUMBER,ACQUISITION_DATE,ACQUISITION_TIME,REFERENCE_FILE,PATIENT_ID)");
                sb.Append("  Values (@IMAGE_IDENTITY,@SERIES_IDENTITY,@SOPINSTANCE_UID,@SOPCLASS_UID,@INSTANCE_NUMBER,@ACQUISITION_NUMBER,@ACQUISITION_DATE,@ACQUISITION_TIME,@REFERENCE_FILE,@PATIENT_ID)");

                //2、声明command对象
                SqlCommand cmd = new SqlCommand(sb.ToString(), conn);
                cmd.CommandTimeout = 0;
                //3、构建参数列表
                decimal dec_new_image_id;
                TableIdentity tab = new TableIdentity();
                dec_new_image_id = tab.GetPKNum("IMAGE_" + strModality);

                List<SqlParameter> list_parameters = new List<SqlParameter>();
                list_parameters.Add(new SqlParameter("@IMAGE_IDENTITY", dec_new_image_id));
                list_parameters.Add(new SqlParameter("@SERIES_IDENTITY", dec_series_identity));
                list_parameters.Add(new SqlParameter("@SOPINSTANCE_UID", ht["SOPINSTANCE_UID"]));
                list_parameters.Add(new SqlParameter("@SOPCLASS_UID", ht["SOPCLASS_UID"]));
                list_parameters.Add(new SqlParameter("@INSTANCE_NUMBER", ht["INSTANCE_NUMBER"]));
                list_parameters.Add(new SqlParameter("@ACQUISITION_NUMBER", ht["ACQUISITION_NUMBER"]));
                list_parameters.Add(new SqlParameter("@ACQUISITION_DATE", ht["ACQUISITION_DATE"]));
                list_parameters.Add(new SqlParameter("@ACQUISITION_TIME", ht["ACQUISITION_TIME"]));
                list_parameters.Add(new SqlParameter("@REFERENCE_FILE", ht["REFERENCE_FILE"]));
                list_parameters.Add(new SqlParameter("@PATIENT_ID", ht["PATIENT_ID"]));

                //4、遍历SqlParameter，添加到cmd.Parameters

                foreach (SqlParameter param in list_parameters)
                {
                    if ((param.Direction == ParameterDirection.InputOutput || param.Direction == ParameterDirection.Input) &&
                      (param.Value == null))
                    {
                        param.Value = DBNull.Value;
                    }
                    cmd.Parameters.Add(param);
                }

                //5、执行查询语句
                cmd.ExecuteNonQuery();

                //6、


                return dec_new_image_id;
            }
            catch (Exception e)
            {
                new Log("AddImage:" + e.Message);
                throw e;

            }
            finally
            {
                conn.Close();
            }
        }

        //更新图像数量；
        private int UpdateImageCount(decimal dec_image_identity)
        {
            SqlConnection conn = new SqlConnection(connectionString);
            int count = 0;
            try
            {
                //0、打开数据连接
                conn.Open();

                //1、构建SQL语句
                StringBuilder sb = new StringBuilder();

                if (strServiceMode == "0")
                {
                    sb.Append("UPDATE ZYRISDB.dbo.PATIENT_REGISTRATION SET IMAGE_COUNT=(IMAGE_COUNT+1) WHERE REGISTRATION_ID = ");
                    sb.Append("(");
                    sb.Append("SELECT  C.REGISTRATION_ID  FROM ZYPACSDB.dbo.[IMAGE_");
                    sb.Append(strModality);

                    sb.Append("] AS A LEFT JOIN ZYPACSDB.dbo.SERIES AS B ON A.SERIES_IDENTITY = B.SERIES_IDENTITY ");
                    sb.Append(" LEFT JOIN ZYPACSDB.dbo.STUDY  AS C ON B.STUDY_IDENTITY =  C.STUDY_IDENTITY ");
                    sb.Append(" WHERE A.IMAGE_IDENTITY = ");
                    sb.Append(dec_image_identity.ToString());
                    sb.Append(")");
                }
                else
                {
                    sb.Append("UPDATE REGISTER SET IMAGE_COUNT=(IMAGE_COUNT+1) WHERE REG_ID = ");
                    sb.Append("(");
                    sb.Append("SELECT  C.REGISTRATION_ID  FROM [IMAGE_");
                    sb.Append(strModality);

                    sb.Append("] AS A LEFT JOIN [SERIES] AS B ON A.SERIES_IDENTITY = B.SERIES_IDENTITY ");
                    sb.Append("       LEFT JOIN [STUDY]  AS C ON B.STUDY_IDENTITY =  C.STUDY_IDENTITY  ");
                    sb.Append(" WHERE A.IMAGE_IDENTITY = ");
                    sb.Append(dec_image_identity.ToString());
                    sb.Append(")");
                }


                //2、声明command对象
                SqlCommand cmd = new SqlCommand(sb.ToString(), conn);
                cmd.CommandTimeout = 0;
                //执行查询语句
                count = cmd.ExecuteNonQuery();

            }
            catch (System.Exception e)
            {
                new Log("UpdateImageCount:" + e.Message);
                throw e;
            }
            finally
            {
                conn.Close();
            }
            return count;

        }
        /// <summary>
        /// 更新数据库
        /// </summary>
        /// <param name="ht"></param>
        public void UpdateDb(Hashtable ht)
        {
            //Log("单文件储存结束");
            /*************************************************/
            //开始数据库操作

            decimal dec_patient_identity;
            decimal dec_study_identity;
            decimal dec_series_identity;
            decimal dec_image_identity;

            dec_patient_identity = 0;
            dec_study_identity = 0;
            dec_series_identity = 0;
            dec_image_identity = 0;

            Hashtable htInfomation = new Hashtable();
            //取出Modality的别名
            strModality = (string)ht["MODALITY"];

            if (strConfigModality != "")
            {
                strModality = strConfigModality;
            }
            //                 foreach (DictionaryEntry de in htModality)
            //                 {
            //                     if ((string)de.Key == strModality)
            //                     {
            //                         if ((string)de.Value != "")
            //                         {
            //                             strModality = (string)de.Value;
            //                             break;
            //                         }
            //                         
            //                     }
            //                 }
            //

            try
            {
                //0、Patient;
                // 2012-11-06 注释掉插入PATIENT级别,以RIS中的PATIENT为准。
                /* 
                htInfomation.Add("PATIENT_ID", ht["PATIENT_ID"]);
                dec_patient_identity = CheckInformation("PATIENT", htInfomation, "PATIENT_IDENTITY");
                if (dec_patient_identity == 0)
                {
                    dec_patient_identity = AddPatient(ht);
                }
                */

                htInfomation.Clear();

                //1、Study
                htInfomation.Add("STUDY_INSTANCE_UID", ht["STUDY_INSTANCE_UID"]);
                htInfomation.Add("PATIENT_ID", ht["PATIENT_ID"]);
                htInfomation.Add("MODALITY", strModality);
                htInfomation.Add("FLAG_INVALID", false);
                htInfomation.Add("HOSPITAL_ID", strHospitalId);
                //
                dec_study_identity = CheckInformation("STUDY", htInfomation, "STUDY_IDENTITY");
                if (dec_study_identity == 0)
                {
                    dec_study_identity = AddStudy(ht);
                }
                htInfomation.Clear();

                //2、Series
                htInfomation.Add("SERIES_INSTANCE_UID", ht["SERIES_INSTANCE_UID"]);
                htInfomation.Add("PATIENT_ID", ht["PATIENT_ID"]);
                htInfomation.Add("FLAG_INVALID", false);
                dec_series_identity = CheckInformation("SERIES", htInfomation, "SERIES_IDENTITY");
                if (dec_series_identity == 0)
                {
                    dec_series_identity = AddSeries(ht, dec_study_identity);
                }
                htInfomation.Clear();

                //4、Image
                htInfomation.Add("SOPINSTANCE_UID", ht["SOPINSTANCE_UID"]);
                htInfomation.Add("PATIENT_ID", ht["PATIENT_ID"]);
                htInfomation.Add("FLAG_STATUS", 0);

                dec_image_identity = CheckInformation("IMAGE_" + strModality, htInfomation, "IMAGE_IDENTITY");
                if (dec_image_identity == 0)
                {
                    dec_image_identity = AddImage(ht, dec_series_identity);
                    if (dec_image_identity > 0)
                    {
                        int i = UpdateImageCount(dec_image_identity);
                    }

                }
                htInfomation.Clear();

                //5、
                ht.Clear();
                //
                /*************************************************/
            }
            catch (Exception e)
            {
                new Log("执行出错！错误信息：" + e.Message);
            }
        }
    }
}
