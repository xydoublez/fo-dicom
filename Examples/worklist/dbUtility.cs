using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
namespace ZyWorkListScp
{
    public class dbUtility
    {
        private string _connectionString = Encrypt.Decrypt(System.Configuration.ConfigurationManager.AppSettings["ZYRISDB"], "ihepass");
        public  DataSet GetRegInfo(string strWhere)
        {
            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;
                    if (string.IsNullOrEmpty(strWhere))
                    {
                        cmd.CommandText = "select top 100 * from ZYRISDB.DBO.VIEW_REGLIST where PROCESS_STATUS_ID in (0,1) ";
                    }
                    else
                    {
                        cmd.CommandText = "select top 100 * from ZYRISDB.DBO.VIEW_REGLIST where PROCESS_STATUS_ID in (0,1) " + strWhere;
                    }
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(ds, "reginfo");

                    return ds;
                }
                catch(Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }
    }
}
