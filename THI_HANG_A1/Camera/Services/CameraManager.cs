using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THI_HANG_A1.Camera.Models;

namespace THI_HANG_A1.Camera.Services
{
    public class CameraManager
    {
        private readonly Dictionary<int, HikvisionCameraService> _services = new Dictionary<int, HikvisionCameraService>();

        public HikvisionCameraService GetOrCreate(CameraInfo cam)
        {
            if (_services.ContainsKey(cam.Id))
                return _services[cam.Id];

            var service = new HikvisionCameraService();
            service.Init();
            service.Login(cam.IPAddress, 8000, cam.Username, cam.Password);

            _services[cam.Id] = service;
            return service;
        }

        public void StartPreview(CameraInfo cam, IntPtr hwnd)
        {
            // stop tất cả preview khác (chỉ xem 1 cam)
            StopAll();

            var service = GetOrCreate(cam);
            service.StartPreview(hwnd, cam.Channel);
        }

        public bool Reconnect(CameraInfo cam, IntPtr hwnd)
        {
            try
            {
                // stop & remove service cũ
                if (_services.TryGetValue(cam.Id, out var old))
                {
                    old.StopPreview();
                    old.Dispose();
                    _services.Remove(cam.Id);
                }

                // tạo service mới
                var service = GetOrCreate(cam);

                return service.StartPreview(hwnd, cam.Channel);
            }
            catch
            {
                return false;
            }
        }

        public void StopAll()
        {
            foreach (var s in _services.Values)
                s.Dispose();

            _services.Clear();
        }

        public void Remove(CameraInfo cam)
        {
            if (cam == null) return;

            if (_services.TryGetValue(cam.Id, out var service))
            {
                service.Dispose();
                _services.Remove(cam.Id);
            }
        }

        public Bitmap Capture(CameraInfo cam)
        {
            var service = GetOrCreate(cam);
            return service.CaptureFrame(cam.Channel);
        }

    }

}
