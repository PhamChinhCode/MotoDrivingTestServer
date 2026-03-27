using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THI_HANG_A1.Camera.Models;

namespace THI_HANG_A1.Repositories
{
    public class CameraRepository
    {
        private readonly string _conn = THI_HANG_A1.Properties.Settings.Default.Conn;


        public List<CameraInfo> GetAll()
        {
            var list = new List<CameraInfo>();

            var conn = new SqlConnection(_conn);
            conn.Open();

            var cmd = new SqlCommand("SELECT ID, Type, IPAddress, Name, Username, Password FROM Devices where Type = 'IP'", conn);

            var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new CameraInfo
                {
                    Id = (int)rd["ID"],
                    Type = rd["Type"].ToString(),
                    IPAddress = rd["IPAddress"].ToString(),
                    Name = rd["Name"].ToString(),
                    Username = rd["Username"].ToString(),
                    Password = rd["Password"].ToString()
                });
            }
            return list;
        }

        public int Add(CameraInfo cam)
        {
            var conn = new SqlConnection(_conn);
            conn.Open();

            var cmd = new SqlCommand(@"
            INSERT INTO Devices(Type, IPAddress, Name, Username, Password)
            OUTPUT INSERTED.ID
            VALUES(@Type,@IP,@Name,@User,@Pass)", conn);

            cmd.Parameters.AddWithValue("@Type", cam.Type);
            cmd.Parameters.AddWithValue("@IP", cam.IPAddress);
            cmd.Parameters.AddWithValue("@Name", cam.Name);
            cmd.Parameters.AddWithValue("@User", cam.Username);
            cmd.Parameters.AddWithValue("@Pass", cam.Password);

            return (int)cmd.ExecuteScalar();
        }

        public void Update(CameraInfo cam)
        {
            var conn = new SqlConnection(_conn);
            conn.Open();

            var cmd = new SqlCommand(@"
                UPDATE Devices SET
                    Type = @Type,
                    IPAddress = @IP,
                    Name = @Name,
                    Username = @User,
                    Password = @Pass
                WHERE ID = @ID", conn);

            cmd.Parameters.AddWithValue("@Type", cam.Type ?? "IP");
            cmd.Parameters.AddWithValue("@IP", cam.IPAddress);
            cmd.Parameters.AddWithValue("@Name", cam.Name);
            cmd.Parameters.AddWithValue("@User", cam.Username);
            cmd.Parameters.AddWithValue("@Pass", cam.Password);
            cmd.Parameters.AddWithValue("@ID", cam.Id);

            cmd.ExecuteNonQuery();
        }


        public void Delete(int id)
        {
            var conn = new SqlConnection(_conn);
            conn.Open();

            var cmd = new SqlCommand(
                "DELETE FROM Devices WHERE ID=@ID", conn);
            cmd.Parameters.AddWithValue("@ID", id);

            cmd.ExecuteNonQuery();
        }
    }
}
