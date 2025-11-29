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
        private static readonly Dictionary<byte, int> _map = new Dictionary<byte, int>();
        private static bool _isLoaded = false;

        private static string cnn = THI_HANG_A1.Properties.Settings.Default.Conn;

        // GỌI 1 LẦN KHI CHẠY PHẦN MỀM
        public static void LoadBaiThi()
        {
            if (_isLoaded) return;

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                string sql = "SELECT StatusCode, ID FROM BaiThi";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        byte st = Convert.ToByte(rd["StatusCode"]);
                        int id = Convert.ToInt32(rd["ID"]);

                        if (!_map.ContainsKey(st))
                            _map.Add(st, id);
                    }
                }
            }

            _isLoaded = true;
        }

        // TRA BÀI THI → KHÔNG BAO GIỜ TRUY VẤN DB
        public static int GetBaiThiId(byte status)
        {
            return _map.TryGetValue(status, out int id) ? id : 0;
        }

        public static void CapNhatBaiThiHienTai(ThiSinhDangThi ts, byte status)
        {
            int baiThiId = GetBaiThiId(status);

            if (baiThiId > 0)
                ts.BaiThiHienTaiID = baiThiId;
        }
    }

}
