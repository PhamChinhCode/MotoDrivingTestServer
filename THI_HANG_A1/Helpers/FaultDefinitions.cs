using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using THI_HANG_A1.Managers;
using THI_HANG_A1.Properties;

namespace THI_HANG_A1.Helpers
{
    public class Fault
    {
        public int Id { get; set; }          // ID trong bảng Faults
        public int ErrorId { get; set; }     // ErrorId từ ESP
        public int DiemTru { get; set; }     // Điểm trừ
        public string MoTa { get; set; }     // Mô tả lỗi
    }
    public static class FaultDefinitions
    {
        // id, errorId, diemTru, baiThiId, moTa
        public static Dictionary<string, (int id, int errorId, int diemTru, int baiThiId, string moTa)> FaultMap
            = new Dictionary<string, (int, int, int, int, string)>();

        public static Dictionary<int, Fault> FaultByErrorId = new Dictionary<int, Fault>();

        private static readonly string cnn = Settings.Default.Conn;

        public static void LoadFaults()
        {
            FaultMap.Clear();
            FaultByErrorId.Clear();

            FaultMap["Chuẩn bị"] = (0, 0, 0, 0, "Chuẩn bị thi");
            FaultMap["Bắt đầu"] = (0, 0, 0, 0, "Bắt đầu bài thi");

            string sql = "SELECT ID, FullName, Subtraction, ErrorId FROM Faults WHERE ErrorId IS NOT NULL";

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        var fault = new Fault
                        {
                            Id = Convert.ToInt32(rd["ID"]),
                            ErrorId = Convert.ToInt32(rd["ErrorId"]),
                            DiemTru = Convert.ToInt32(rd["Subtraction"]),
                            MoTa = rd["FullName"].ToString()
                        };

                        FaultByErrorId[fault.ErrorId] = fault;
                    }
                }
            }
        }

        public static Fault GetFaultByErrorId(int errorId)
        {
            return FaultByErrorId.TryGetValue(errorId, out var fault)
                ? fault
                : null;
        }

        public static readonly Dictionary<string, byte> FaultUIMap = new Dictionary<string, byte>
        {
            { "Chống chân", ConstantKeys.ERROR_CHAM_CHAN },
            { "Đổ xe", ConstantKeys.ERROR_DO_XE },
            { "Ngoài hình", ConstantKeys.ERROR_DI_RA_NGOAI }
        };
        private static readonly Dictionary<int, byte> ErrorIdToKeyMap =
            new Dictionary<int, byte>
            {
                { 0xE1, ConstantKeys.ERROR_DE_VACH_XP },
                { 0xE2, ConstantKeys.ERROR_DE_VACH_CNV },
                { 0xE3, ConstantKeys.ERROR_CHAM_CHAN },
                { 0xE4, ConstantKeys.ERROR_QUA_TG_THI },
                { 0xE5, ConstantKeys.ERROR_DI_SAI_DUONG },
                { 0xE6, ConstantKeys.ERROR_DO_XE },
                { 0xE7, ConstantKeys.ERROR_DI_RA_NGOAI },
                { 0xE8, ConstantKeys.ERROR_TAT_MAY },
                { 0xE9, ConstantKeys.ERROR_KHONG_DOI_MU },
                { 0xEA, ConstantKeys.ERROR_KHONG_XI_NHAN_VAO },
                { 0xEB, ConstantKeys.ERROR_QUA_THOI_GIAN_XP }
            };

        public static byte GetErrorKeyByErrorId(int errorId)
        {
            return ErrorIdToKeyMap.TryGetValue(errorId, out var key)
                ? key
                : ConstantKeys.ERROR_KEY;
        }
    }
}
