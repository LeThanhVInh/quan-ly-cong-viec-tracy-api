using System;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Summary description for Connect
/// </summary>
public static class Connect
{
    //public static string ConnectionString = @"Data Source=27.0.15.8,1433;Initial Catalog=db_QLHeThongCongTy_TracyNguyen;User ID=login123; Password=123123123;Connect Timeout=10";
    public static string ConnectionString = @"Data Source=103.77.167.237,1433;Initial Catalog=db_QLPhanCongCongViec_TracyNguyen;User ID=VAS; Password=TamVinh136;Connect Timeout=10";
    public static SqlConnection ConnectSQL()
    {
        string s = ConnectionString;
        SqlConnection conn = new SqlConnection(s);
        return conn;
    }
    public static DataTable GetTable(string strCommandText)
    {
        SqlConnection Conn = ConnectSQL();
        try
        {
            if (Conn.State == ConnectionState.Closed)
                Conn.Open();
            SqlCommand cmd = new SqlCommand(strCommandText, Conn);
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 120;//2 minutes
            SqlDataAdapter ad = new SqlDataAdapter();
            ad.SelectCommand = cmd;
            DataSet ds = new DataSet();
            ad.Fill(ds, "table1");
            ad.Dispose();
            if (Conn.State == ConnectionState.Open)
                Conn.Close();
            return ds.Tables[0];
        }
        catch
        {
            if (Conn.State == ConnectionState.Open)
                Conn.Close();
            return null;
        }
    }
    public static DataTable GetTable(string strCommandText, string[] parameterNames, object[] parameterValues)
    {
        SqlConnection Conn = ConnectSQL();
        try
        {
            if (Conn.State == ConnectionState.Closed)
                Conn.Open();
            SqlCommand cmd = new SqlCommand(strCommandText, Conn);
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 120;//2 minutes
            cmd.Parameters.Clear();
            for (int i = 0; i < parameterNames.Length; i++)
            {
                SqlParameter s = new SqlParameter(parameterNames[i], parameterValues[i]);
                cmd.Parameters.Add(s);
            }
            SqlDataAdapter ad = new SqlDataAdapter();
            ad.SelectCommand = cmd;
            DataSet ds = new DataSet();
            ad.Fill(ds, "table1");
            ad.Dispose();
            if (Conn.State == ConnectionState.Open)
                Conn.Close();
            return ds.Tables[0];
        }
        catch
        {
            if (Conn.State == ConnectionState.Open)
                Conn.Close();
            return null;
        }
    }
    public static object FirstResulfExec(string strCommandText)
    {
        object value;
        SqlConnection Conn = ConnectSQL();
        try
        {
            SqlCommand Cmd = new SqlCommand(strCommandText, Conn);
            Cmd.CommandType = CommandType.Text;
            Cmd.Parameters.Clear();
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
            }
            value = Cmd.ExecuteScalar();
            if (Conn.State == ConnectionState.Open)
                Conn.Close();
            return value;
        }
        catch (Exception)
        {
            if (Conn.State == ConnectionState.Open)
                Conn.Close();
            return null;
        }
    }
    public static object FirstResulfExec(string strCommandText, string[] arrParameterName, object[] arrParameterValue)
    {
        object value = null;
        SqlConnection Conn = ConnectSQL();
        try
        {
            if (Conn.State == ConnectionState.Closed)
                Conn.Open();
            SqlCommand Cmd = new SqlCommand(strCommandText, Conn);
            Cmd.CommandType = CommandType.Text;
            Cmd.Parameters.Clear();
            for (int i = 0; i < arrParameterName.Length; i++)
            {
                SqlParameter s = new SqlParameter(arrParameterName[i], arrParameterValue[i]);
                Cmd.Parameters.Add(s);
            }
            value = Cmd.ExecuteScalar();
            if (Conn.State == ConnectionState.Open)
                Conn.Close();
            return value;
        }
        catch (Exception)
        {
            if (Conn.State == ConnectionState.Open)
                Conn.Close();
            return null;
        }
    }
    public static bool Exec(string strCommandText)
    {
        SqlConnection Conn = ConnectSQL();
        bool flag = false;
        try
        {
            SqlCommand Cmd = new SqlCommand(strCommandText, Conn);
            Cmd.CommandType = CommandType.Text;
            Cmd.Parameters.Clear();
            if (Conn.State == ConnectionState.Closed)
            {
                Conn.Open();
            }
            Cmd.ExecuteNonQuery();
            if (Conn.State == ConnectionState.Open)
                Conn.Close();
            flag = true;
        }
        catch (Exception)
        {
            if (Conn.State == ConnectionState.Open)
                Conn.Close();
            flag = false;
        }

        return flag;
    }
    public static bool Exec(string strCommandText, string[] arrParameterName, object[] arrParameterValue)
    {
        SqlConnection Conn = ConnectSQL();
        bool result = false;
        try
        {
            if (Conn.State == ConnectionState.Closed)
                Conn.Open();
            SqlCommand Cmd = new SqlCommand(strCommandText, Conn);
            Cmd.CommandType = CommandType.Text;
            Cmd.Parameters.Clear();
            for (int i = 0; i < arrParameterName.Length; i++)
            {
                SqlParameter s = new SqlParameter(arrParameterName[i], arrParameterValue[i]);
                Cmd.Parameters.Add(s);
            }
            Cmd.ExecuteNonQuery();
            if (Conn.State == ConnectionState.Open)
                Conn.Close();
            result = true;
        }
        catch (Exception)
        {
            if (Conn.State == ConnectionState.Open)
                Conn.Close();
        }

        return result;
    }
    public static bool Exec(string strCommandText, string[] arrParameterName, object[] arrParameterValue, SqlDbType[] types)
    {
        SqlConnection Conn = ConnectSQL();
        bool result = false;
        try
        {
            if (Conn.State == ConnectionState.Closed)
                Conn.Open();
            SqlCommand Cmd = new SqlCommand(strCommandText, Conn);
            Cmd.CommandType = CommandType.Text;
            Cmd.Parameters.Clear();
            for (int i = 0; i < arrParameterName.Length; i++)
            {
                SqlParameter s = new SqlParameter(arrParameterName[i], arrParameterValue[i]);
                s.SqlDbType = types[i];
                Cmd.Parameters.Add(s);
            }
            Cmd.ExecuteNonQuery();
            if (Conn.State == ConnectionState.Open)
                Conn.Close();
            result = true;
        }
        catch (Exception)
        {
            if (Conn.State == ConnectionState.Open)
                Conn.Close();
        }
        return result;
    }

    public static object getField(string table, string getField, string paraField, object valueParaField)
    {
        object result = null;
        string sql = "select " + getField + " from " + table + " where " + paraField + "=@valueParaField";
        string[] paraName = new string[1] { "@valueParaField" };
        object[] paraValue = new object[1] { valueParaField };
        try
        {
            DataTable tb = Connect.GetTable(sql, paraName, paraValue);
            if (tb.Rows.Count > 0)
                result = tb.Rows[0][0];
        }
        catch
        { }
        return result;
    }
    public static object getFieldHasTail(string table, string getField, string paraField, object valueParaField, string tail)
    {
        object result = null;
        string sql = "select " + getField + " from " + table + " where " + paraField + " =@valueParaField " + tail;
        string[] paraName = new string[1] { "@valueParaField" };
        object[] paraValue = new object[1] { valueParaField };
        try
        {
            DataTable tb = Connect.GetTable(sql, paraName, paraValue);
            if (tb.Rows.Count > 0)
                result = tb.Rows[0][0];
        }
        catch
        { }
        return result;
    }
}
