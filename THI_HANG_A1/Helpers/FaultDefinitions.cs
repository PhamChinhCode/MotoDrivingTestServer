using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using THI_HANG_A1.Properties;

namespace THI_HANG_A1.Helpers
{
    public static class FaultDefinitions
    {
        // id, errorId, diemTru, baiThiId, moTa
        public static Dictionary<string, (int id, int errorId, int diemTru, int baiThiId, string moTa)> FaultMap
            = new Dictionary<string, (int, int, int, int, string)>();

        public static Dictionary<int, (int id, int errorId, int diemTru, string moTa)> FaultByErrorId
            = new Dictionary<int, (int, int, int, string)>();

        private static readonly string cnn = Settings.Default.Conn;

        public static void LoadFaults()
        {
            FaultMap.Clear();

            // --- Lỗi mặc định ---
            FaultMap["Chuẩn bị"] = (0, 0, 0, 0, "Chuẩn bị thi");
            FaultMap["Bắt đầu"] = (0, 0, 0, 0, "Bắt đầu bài thi");

            string sql = "SELECT ID, FullName, Subtraction, ErrorId FROM Faults";

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        int id = Convert.ToInt32(rd["ID"]);
                        string fullName = rd["FullName"].ToString();
                        int subtraction = Convert.ToInt32(rd["Subtraction"]);
                        int errorId = Convert.ToInt32(rd["ErrorId"]);

                        // Map theo tên lỗi
                        FaultMap[fullName] = (id, errorId, subtraction, 0, fullName);

                        // Map theo ErrorId để đọc từ ESP32
                        FaultByErrorId[errorId] = (id, errorId, subtraction, fullName);
                    }
                }
            }
        }

        public static readonly Dictionary<string, string> FaultUIMap = new Dictionary<string, string>
        {
            { "Chống chân", "Chạm chân xuống đất" },
            { "Đổ xe", "Đổ xe" },
            { "Ngoài hình", "Đi ra ngoài" }
        };
    }
}
