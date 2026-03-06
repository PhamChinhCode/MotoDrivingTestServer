using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using THI_HANG_A1.Camera.Models;
using THI_HANG_A1.Camera.Services;
using THI_HANG_A1.Repositories;

namespace THI_HANG_A1.Forms
{
    public partial class fQuanLyCamera : Form
    {

        // init camera


        private List<CameraInfo> _cameraList;
        private CameraManager _cameraManager = new CameraManager();
        private CameraInfo _camera;

        private CameraRepository _repo = new CameraRepository();
        public fQuanLyCamera()
        {
            InitializeComponent();
        }

        private void LoadCameraList()
        {
            //_cameraList = new List<CameraInfo>
            //{
            //    new CameraInfo
            //    {
            //        Id = 1,
            //        Name = "Cổng trước",
            //        IPAddress = "169.254.195.160",
            //        Username = "admin",
            //        Password = "Duansathach@",
            //        Channel = 1
            //    }
            //};

            _cameraList = _repo.GetAll();

            dgvCameras.DataSource = _cameraList;

            if (_cameraList.Count != 0)
            {
                _camera = _cameraList[0];
                OpenSelectedCam(_camera);
            }
        }
        private void InitCameraHeader()
        {
            dgvCameras.AutoGenerateColumns = false;
            dgvCameras.Columns.Clear();

            dgvCameras.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCameras.MultiSelect = false;
            dgvCameras.ReadOnly = true;
            dgvCameras.AllowUserToAddRows = false;

            dgvCameras.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Tên",
                DataPropertyName = "Name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 40
            });

            dgvCameras.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "IP",
                DataPropertyName = "IPAddress",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 30
            });

            dgvCameras.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Username",
                DataPropertyName = "Username",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 15
            });
        }


        private void fQuanLyCamera_FormClosing(object sender, FormClosingEventArgs e)
        {
            _cameraManager.StopAll();
        }

        private void fQuanLyCamera_Load(object sender, EventArgs e)
        {
            InitCameraHeader();
            LoadCameraList();
        }

        private void dgvCameras_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var cam = dgvCameras.Rows[e.RowIndex].DataBoundItem as CameraInfo;
            if (cam == null) return;
            OpenSelectedCam(cam);
        }
        private void OpenSelectedCam(CameraInfo cam)
        {
            if (cam == null) return;
            _camera = cam;

            // Start preview
            if (!pnlPreview.IsHandleCreated)
            {
                pnlPreview.CreateControl();
            }
            _cameraManager.StartPreview(cam, pnlPreview.Handle);

            txtName.Text = _camera.Name;
            txtIp.Text = _camera.IPAddress;
            txtUser.Text = _camera.Username;
            txtPass.Text = _camera.Password;
        }

        private async void btnSaveConfig_Click(object sender, EventArgs e)
        {
            if (_camera == null)
            {
                MessageBox.Show("Chưa chọn camera");
                return;
            }

            // 1️⃣ Validate đơn giản
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin");
                return;
            }

            // 3️⃣ Update model
            _camera.Name = txtName.Text.Trim();

            _repo.Update(_camera);

            // 4️⃣ Refresh DataGridView
            dgvCameras.Refresh();

            MessageBox.Show("Đã lưu cấu hình camera", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnReconnect_Click(object sender, EventArgs e)
        {
            if (_camera == null)
            {
                MessageBox.Show("Chưa chọn camera");
                return;
            }

            try
            {
                // 1️⃣ Update lại thông tin từ UI
                _camera.Name = txtName.Text.Trim();
                _camera.IPAddress = txtIp.Text.Trim();
                _camera.Username = txtUser.Text.Trim();
                _camera.Password = txtPass.Text.Trim();

                // 2️⃣ đảm bảo panel có handle
                if (!pnlPreview.IsHandleCreated)
                    pnlPreview.CreateControl();

                // 3️⃣ reconnect qua manager
                bool ok = _cameraManager.Reconnect(_camera, pnlPreview.Handle);

                if (!ok)
                {
                    MessageBox.Show("Reconnect thất bại");
                    return;
                }

                MessageBox.Show("Reconnect thành công", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Reconnect lỗi: " + ex.Message);
            }
        }

        private void btnAddCam_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtIp.Text) || string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Vui lòng nhập IP và Tên camera");
                return;
            }

            var cam = new CameraInfo
            {
                Type = "IP",
                IPAddress = txtIp.Text.Trim(),
                Name = txtName.Text.Trim(),
                Username = txtUser.Text.Trim(),
                Password = txtPass.Text.Trim()
            };

            cam.Id = _repo.Add(cam);

            _cameraList.Add(cam);
            dgvCameras.Refresh();

            MessageBox.Show("Đã thêm camera");
        }
        private void btnDeleteCam_Click(object sender, EventArgs e)
        {
            if (_camera == null)
            {
                MessageBox.Show("Chưa chọn camera");
                return;
            }

            if (MessageBox.Show(
                $"Xóa camera '{_camera.Name}' ?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                // 1️⃣ Stop + remove camera khỏi manager
                _cameraManager.Remove(_camera);

                // 2️⃣ Xóa trong DB
                _repo.Delete(_camera.Id);

                // 3️⃣ Xóa khỏi list + refresh grid
                _cameraList.Remove(_camera);
                dgvCameras.Refresh();

                // 4️⃣ Clear UI
                _camera = null;

                txtName.Clear();
                txtIp.Clear();
                txtUser.Clear();
                txtPass.Clear();

                MessageBox.Show("Đã xóa camera", "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xóa camera: " + ex.Message);
            }
        }

        private void btnCapture_Click(object sender, EventArgs e)
        {
            if (_camera == null) return;

            try
            {
                using (var bmp = _cameraManager.Capture(_camera))
                {
                    // clone ra 1 bản an toàn
                    using (var saveBmp = new Bitmap(bmp))
                    {
                        string folder = @"D:\CameraCaptures";
                        if (!Directory.Exists(folder))
                            Directory.CreateDirectory(folder);

                        string file = Path.Combine(
                            folder,
                            $"{_camera.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg"
                        );

                        saveBmp.Save(file, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }

                }

                MessageBox.Show("Capture thành công", "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


    }
}
