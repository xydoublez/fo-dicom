using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data;
namespace ZYStoreScp
{
    public class TableIdentity
    {
        public decimal GetPKNum(string tablename)
        {
            string connectionString =Encrypt.Decrypt(ConfigurationManager.AppSettings["ZYPACSDB"].ToString(), "ihepass");

            SqlConnection conn = new SqlConnection(connectionString);

            SqlCommand cmd = new SqlCommand("up_get_table_key", conn);
            cmd.CommandTimeout = 0;
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter table = cmd.Parameters.Add("@table_name", SqlDbType.VarChar, 50);
            table.Direction = ParameterDirection.Input;
            SqlParameter pkNum = cmd.Parameters.Add("@key_value", SqlDbType.Decimal, 9);
            pkNum.Direction = ParameterDirection.Output;

            table.Value = tablename;
            decimal retValue;
            retValue = 0;
            conn.Open();
            try
            {
                SqlDataReader rdr = cmd.ExecuteReader();

                retValue = (decimal)pkNum.Value;

                rdr.Close();
            }
            catch
            {

            }
            finally
            {
                conn.Close();
            }


            return retValue;
        }
    }
}
