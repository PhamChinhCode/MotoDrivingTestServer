using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace THI_HANG_A1
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();

            this.Load += new EventHandler(this.Form3_Load);
            this.dgvDevices.CellClick += new DataGridViewCellEventHandler(this.dgvDevices_CellClick);

            if (this.Controls.ContainsKey("button4"))
                this.Controls["button4"].Click += new EventHandler(this.btnClear_Click);
            else if (this.Controls.ContainsKey("btnClear"))
                this.Controls["btnClear"].Click += new EventHandler(this.btnClear_Click);
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            try
            {
                THI_HANG_A1.MCDV2A1DataSetTableAdapters.DevicesTableAdapter adapter = new THI_HANG_A1.MCDV2A1DataSetTableAdapters.DevicesTableAdapter();
                THI_HANG_A1.MCDV2A1DataSet.DevicesDataTable table = new THI_HANG_A1.MCDV2A1DataSet.DevicesDataTable();

                adapter.Fill(table);
                table.DefaultView.Sort = "Name ASC";

                dgvDevices.DataSource = table;
                if (dgvDevices.Columns.Contains("ID")) dgvDevices.Columns["ID"].Visible = true;
                cboIPAddress.Items.Clear();
                foreach (DataRow row in table.Rows)
                {
                    string ip = row["IPAddress"].ToString();
                    if (!cboIPAddress.Items.Contains(ip)) cboIPAddress.Items.Add(ip);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        // --- SỰ KIỆN CLICK VÀO BẢNG ---
        private void dgvDevices_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                try
                {
                    DataGridViewRow row = this.dgvDevices.Rows[e.RowIndex];
                    txtDeviceID.Text = row.Cells["ID"].Value.ToString();

                    string ip = row.Cells["IPAddress"].Value.ToString();
                    if (cboIPAddress.Items.Contains(ip))
                        cboIPAddress.SelectedItem = ip;
                    else
                        cboIPAddress.Text = ip;

                    txtName.Text = row.Cells["Name"].Value.ToString();
                    txtType.Text = row.Cells["Type"].Value.ToString();

                    cboIPAddress.Enabled = true; 
                }
                catch { }
            }
        }


        private void btnAdd_Click(object sender, EventArgs e)
        {
            // Kiểm tra dữ liệu
            if (string.IsNullOrEmpty(txtType.Text) || string.IsNullOrEmpty(cboIPAddress.Text) || string.IsNullOrEmpty(txtName.Text))
            {
                MessageBox.Show("Vui lòng nhập đủ Type, IP và Tên");
                return;
            }

            try
            {
                THI_HANG_A1.MCDV2A1DataSetTableAdapters.DevicesTableAdapter adapter = new THI_HANG_A1.MCDV2A1DataSetTableAdapters.DevicesTableAdapter();

                adapter.Insert(txtType.Text.Trim(), cboIPAddress.Text.Trim(), txtName.Text.Trim());

                MessageBox.Show("Thêm thành công!");
                Form3_Load(null, null); 
                btnClear_Click(null, null);
            }
            catch (Exception ex) { MessageBox.Show("Lỗi Thêm: " + ex.Message); }
        }

        // --- NÚT SỬA 
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtDeviceID.Text))
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa trước.");
                return;
            }

            try
            {
                int idCanSua = int.Parse(txtDeviceID.Text);

                THI_HANG_A1.MCDV2A1DataSetTableAdapters.DevicesTableAdapter adapter = new THI_HANG_A1.MCDV2A1DataSetTableAdapters.DevicesTableAdapter();
                THI_HANG_A1.MCDV2A1DataSet.DevicesDataTable table = new THI_HANG_A1.MCDV2A1DataSet.DevicesDataTable();

                adapter.Fill(table);

                foreach (System.Data.DataRow row in table.Rows)
                {
                    if (Convert.ToInt32(row["ID"]) == idCanSua)
                    {
                        // Cập nhật thông tin mới
                        row["Type"] = txtType.Text.Trim();
                        row["Name"] = txtName.Text.Trim();             
                        row["IPAddress"] = cboIPAddress.Text.Trim();
                      

                        adapter.Update(table);

                        MessageBox.Show("Cập nhật thành công!");
                        Form3_Load(null, null);
                        btnClear_Click(null, null);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi Sửa: " + ex.Message);
            }
        }

        // --- NÚT XÓA 
        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtDeviceID.Text))
            {
                MessageBox.Show("Vui lòng chọn dòng cần xóa.");
                return;
            }

            if (MessageBox.Show("Bạn chắc chắn xóa?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.No) return;

            try
            {
                int idCanXoa = int.Parse(txtDeviceID.Text);

                THI_HANG_A1.MCDV2A1DataSetTableAdapters.DevicesTableAdapter adapter = new THI_HANG_A1.MCDV2A1DataSetTableAdapters.DevicesTableAdapter();
                THI_HANG_A1.MCDV2A1DataSet.DevicesDataTable table = new THI_HANG_A1.MCDV2A1DataSet.DevicesDataTable();

                // 1. Lấy dữ liệu về
                adapter.Fill(table);

                // 2. Tìm dòng và Xóa
                foreach (System.Data.DataRow row in table.Rows)
                {
                    if (Convert.ToInt32(row["ID"]) == idCanXoa)
                    {
                        row.Delete(); // Đánh dấu xóa
                        adapter.Update(table); // Cập nhật database

                        MessageBox.Show("Xóa thành công!");
                        Form3_Load(null, null);
                        btnClear_Click(null, null);
                        return;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi Xóa: " + ex.Message); }
        }

        // --- NÚT LÀM MỚI (CLEAR) ---
        private void btnClear_Click(object sender, EventArgs e)
        {
            txtDeviceID.Text = "";
            cboIPAddress.Text = "";
            cboIPAddress.SelectedIndex = -1;
            txtType.Text = "";
            txtName.Text = "";
            cboIPAddress.Enabled = true; // Mở khóa cho nhập IP mới
        }

        // --- CHỌN IP TỪ COMBOBOX ---
        private void cboIPAddress_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboIPAddress.SelectedIndex == -1) return;

            try
            {
                string selectedIP = cboIPAddress.Text;

                THI_HANG_A1.MCDV2A1DataSetTableAdapters.DevicesTableAdapter adapter = new THI_HANG_A1.MCDV2A1DataSetTableAdapters.DevicesTableAdapter();
                THI_HANG_A1.MCDV2A1DataSet.DevicesDataTable table = new THI_HANG_A1.MCDV2A1DataSet.DevicesDataTable();

                adapter.Fill(table);

                foreach (System.Data.DataRow row in table.Rows)
                {
                    if (row["IPAddress"].ToString() == selectedIP)
                    {
                        txtType.Text = row["Type"].ToString();
                        txtName.Text = row["Name"].ToString();
                        txtDeviceID.Text = row["ID"].ToString(); // Quan trọng: Phải lấy ID để còn Sửa/Xóa
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi chọn IP: " + ex.Message);
            }
        }
    }
}