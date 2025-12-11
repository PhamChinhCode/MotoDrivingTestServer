using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using THI_HANG_A1.Managers;
using THI_HANG_A1.Models;

namespace THI_HANG_A1.Helpers
{
    public static class BaiThiHelper
    {
        private static readonly Dictionary<byte, (int Id, string Name)> _map
                                = new Dictionary<byte, (int, string)>();
        private static bool _loaded = false;

        private static string cnn = THI_HANG_A1.Properties.Settings.Default.Conn;

        // GỌI 1 LẦN KHI CHẠY PHẦN MỀM
        public static void LoadBaiThi()
        {
            if (_loaded) return;

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                string sql = "SELECT StatusCode, ID, TenBai FROM BaiThi WHERE StatusCode = 193 OR StatusCode BETWEEN 196 AND 199;";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        byte status = Convert.ToByte(rd["StatusCode"]);
                        int id = Convert.ToInt32(rd["ID"]);
                        string name = rd["TenBai"].ToString();

                        if (!_map.ContainsKey(status))
                            _map.Add(status, (id, name));
                    }
                }
            }

            _loaded = true;
        }

        // TRA BÀI THI → KHÔNG BAO GIỜ TRUY VẤN DB
        public static int GetId(byte status)
        {
            return _map.TryGetValue(status, out var info) ? info.Id : 0;
        }

        public static string GetName(byte statusCode)
            => _map.TryGetValue(statusCode, out var info) ? info.Name : "Không xác định";

        public static string GetNameByBaiThiId(int baiThiId)
        {
            foreach (var kv in _map)
            {
                if (kv.Value.Id == baiThiId)
                    return kv.Value.Name;
            }

            return "Không xác định";
        }

        public static void CapNhatBaiThiHienTai(ThiSinhDangThi ts, byte status)
        {
            int baiThiId = GetId(status);

            if (baiThiId > 0)
                ts.BaiThiHienTaiID = baiThiId;
        }

        public static bool IsInValidContest1_4(byte st)
        {
            return st == ConstantKeys.STATUS_CONTEST1 ||
                   st == ConstantKeys.STATUS_CONTEST2 ||
                   st == ConstantKeys.STATUS_CONTEST3 ||
                   st == ConstantKeys.STATUS_CONTEST4;
        }


        public enum TrangThaiTS
        {
            None,
            DaCapXe,
            ChuanBi,
            DangThi,
            KhongDat,
            Dat
        }
        public static Dictionary<TrangThaiTS, Color> mapMau
            = new Dictionary<TrangThaiTS, Color>()
        {
            { TrangThaiTS.None,       Color.Transparent },
            { TrangThaiTS.DaCapXe,    Color.Silver },
            { TrangThaiTS.ChuanBi,    Color.Gold },
            { TrangThaiTS.DangThi,    Color.DeepSkyBlue },
            { TrangThaiTS.KhongDat,   Color.Red },
            { TrangThaiTS.Dat,        Color.LimeGreen },
        };
        public static TrangThaiTS ParseTrangThai(string s)
        {
            switch (s)
            {
                case "Đã cấp xe": return TrangThaiTS.DaCapXe;
                case "Chuẩn bị": return TrangThaiTS.ChuanBi;
                case "Đang thi": return TrangThaiTS.DangThi;
                case "Không đạt": return TrangThaiTS.KhongDat;
                case "Đạt": return TrangThaiTS.Dat;
                default: return TrangThaiTS.None;
            }
        }

    }

}
