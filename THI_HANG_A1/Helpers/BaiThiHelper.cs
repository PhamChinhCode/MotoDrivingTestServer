using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THI_HANG_A1.Models;

namespace THI_HANG_A1.Helpers
{
    public static class BaiThiHelper
    {
        private static string cnn = THI_HANG_A1.Properties.Settings.Default.Conn;
        public static int GetBaiThiIdFromStatus(byte status)
        {
            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();

                string sql = "SELECT ID FROM BaiThi WHERE StatusCode = @st";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@st", status);

                    var result = cmd.ExecuteScalar();

                    return (result == null) ? 0 : Convert.ToInt32(result);
                }
            }
        }

        public static void CapNhatBaiThiHienTai(ThiSinhDangThi ts, byte status)
        {
            int baiThiId = GetBaiThiIdFromStatus(status);

            if (baiThiId > 0)
                ts.BaiThiHienTaiID = baiThiId;
        }
    }

}
