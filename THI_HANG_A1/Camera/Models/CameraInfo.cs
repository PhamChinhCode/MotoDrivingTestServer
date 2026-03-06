namespace THI_HANG_A1.Camera.Models
{
    public class CameraInfo
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string IPAddress { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Channel { get; set; } = 1;

        public override string ToString() => Name;
    }

}
