namespace THI_HANG_A1.Models
{
    public class Xe
    {
        public string MaXe { get; set; }
        public bool DangRanh { get; set; }
        public string SBDThiSinhHienTai { get; set; }
        public int GiaiDoan { get; set; }

    }

    public enum TrangThaiXe
    {
        Ranh,      // chưa ai dùng / dùng xong
        SanSang,   // đã chuẩn bị cho 1 thí sinh
        DangThi    // đang thi
    }
}