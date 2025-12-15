using DocumentFormat.OpenXml.Office.SpreadSheetML.Y2023.DataSourceVersioning;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using THI_HANG_A1.Forms;
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

        public static void RecoverStaleSessions(int timeoutSeconds)
        {
            var sessions = LoadStaleSessions(timeoutSeconds);

            foreach (var s in sessions)
            {
                InsertSystemAbortError(
                    s.SBD,
                    s.SessionID,
                    s.Ten,
                    s.Xe,
                    LyDoKetThuc.LoiKyThuat
                );
            }
            if (sessions.Count > 0)
            {
                MessageBox.Show("finish all session");
                AutoFinishStaleSessions(timeoutSeconds);
            }
        }
        private static List<SessionInfo> LoadStaleSessions(int timeoutSeconds)
        {
            var list = new List<SessionInfo>();

            string sql = @"
                SELECT 
                    S.ID,
                    S.SBD,
                    S.DeviceID,
                    TS.HoDem + ' ' + TS.Ten AS TenThiSinh
                FROM Sessions S
                JOIN ThiSinhSH TS ON TS.SBD = S.SBD
                WHERE 
                    S.IsFinish = 0
                    AND S.LastUpdate IS NOT NULL
                    AND S.LastUpdate < DATEADD(SECOND, -@Timeout, GETDATE())
            ";

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Timeout", timeoutSeconds);

                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            list.Add(new SessionInfo
                            {
                                SessionID = rd.GetInt32(0),
                                SBD = rd.GetInt64(1),
                                Xe = rd.GetString(2),
                                Ten = rd.GetString(3)
                            });
                        }
                    }
                }
            }
            return list;
        }


        public static void AutoFinishStaleSessions(int timeoutSeconds)
        {
            string sql = @"
                UPDATE Sessions
                SET 
                    IsFinish = 1,
                    IsAborted = 1,
                    AbortReason = N'Ứng dụng bị dừng đột ngột',
                    LyDoKetThuc = @LyDo,
                    EndTime = ISNULL(EndTime, LastUpdate),
                    LastUpdate = GETDATE()
                WHERE 
                    IsFinish = 0
                    AND LastUpdate IS NOT NULL
                    AND LastUpdate < DATEADD(SECOND, -@Timeout, GETDATE())
            ";

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Timeout", timeoutSeconds);
                    cmd.Parameters.AddWithValue("@LyDo", (int)LyDoKetThuc.LoiKyThuat);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private static void InsertSystemAbortError(
            long sbd,
            int sessionId,
            string ten,
            string xe,
            LyDoKetThuc lyDo)
        {
            string sql = @"
                INSERT INTO ChiTietLoi
                (
                    SoBaoDanh,
                    SessionID,
                    Ten,
                    Xe,
                    ThoiGian,
                    SuKien,
                    DiemTru,
                    ChiTiet,
                    FaultID,
                    BaiThiID,
                    ImagePath
                )
                VALUES
                (
                    @SBD,
                    @SessionID,
                    @Ten,
                    @Xe,
                    GETDATE(),
                    @SuKien,
                    0,
                    @ChiTiet,
                    NULL,
                    NULL,
                    NULL
                );
            ";

            string suKien = "Bài thi bị gián đoạn";
            string chiTiet = lyDo == LyDoKetThuc.LoiKyThuat
                ? "Lỗi kỹ thuật / mất kết nối – hệ thống tự động kết thúc"
                : "Hệ thống tự động kết thúc bài thi";

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@SBD", SqlDbType.BigInt).Value = sbd;
                    cmd.Parameters.Add("@SessionID", SqlDbType.Int).Value = sessionId;
                    cmd.Parameters.Add("@Ten", SqlDbType.NVarChar, 100).Value =
                        (object)ten ?? DBNull.Value;
                    cmd.Parameters.Add("@Xe", SqlDbType.NVarChar, 20).Value =
                        (object)xe ?? DBNull.Value;
                    cmd.Parameters.Add("@SuKien", SqlDbType.NVarChar, 100).Value = suKien;
                    cmd.Parameters.Add("@ChiTiet", SqlDbType.NVarChar, 255).Value =
                        (object)chiTiet ?? DBNull.Value;

                    cmd.ExecuteNonQuery();
                }
            }
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
        public static string GetMoTaTrangThai(byte status)
        {
            switch (status)
            {
                case ConstantKeys.STATUS_TESTING:
                    return "đang chuẩn bị thi";

                case ConstantKeys.STATUS_CONTEST1:
                    return "bài số 8";

                case ConstantKeys.STATUS_CONTEST2:
                    return "bài đường thẳng";

                case ConstantKeys.STATUS_CONTEST3:
                    return "bài ziczac";

                case ConstantKeys.STATUS_CONTEST4:
                    return "bài gồ ghề";

                default:
                    return "không xác định";
            }
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
