namespace THI_HANG_A1.Forms
{
    partial class FormKetThucSession
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        /// 
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.GroupBox grpLyDo;
        private System.Windows.Forms.RadioButton rbCanBoDung;
        private System.Windows.Forms.RadioButton rbLoiKyThuat;
        private System.Windows.Forms.RadioButton rbBoThi;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.grpLyDo = new System.Windows.Forms.GroupBox();
            this.rbCanBoDung = new System.Windows.Forms.RadioButton();
            this.rbLoiKyThuat = new System.Windows.Forms.RadioButton();
            this.rbBoThi = new System.Windows.Forms.RadioButton();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.grpLyDo.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(380, 45);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "KẾT THÚC BÀI THI";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // grpLyDo
            // 
            this.grpLyDo.Controls.Add(this.rbCanBoDung);
            this.grpLyDo.Controls.Add(this.rbLoiKyThuat);
            this.grpLyDo.Controls.Add(this.rbBoThi);
            this.grpLyDo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.grpLyDo.Location = new System.Drawing.Point(20, 55);
            this.grpLyDo.Name = "grpLyDo";
            this.grpLyDo.Size = new System.Drawing.Size(340, 113);
            this.grpLyDo.TabIndex = 1;
            this.grpLyDo.TabStop = false;
            this.grpLyDo.Text = "Chọn lý do kết thúc";
            // 
            // rbCanBoDung
            // 
            this.rbCanBoDung.Checked = true;
            this.rbCanBoDung.Location = new System.Drawing.Point(15, 26);
            this.rbCanBoDung.Name = "rbCanBoDung";
            this.rbCanBoDung.Size = new System.Drawing.Size(300, 24);
            this.rbCanBoDung.TabIndex = 1;
            this.rbCanBoDung.TabStop = true;
            this.rbCanBoDung.Text = "Cán bộ dừng";
            // 
            // rbLoiKyThuat
            // 
            this.rbLoiKyThuat.Location = new System.Drawing.Point(15, 51);
            this.rbLoiKyThuat.Name = "rbLoiKyThuat";
            this.rbLoiKyThuat.Size = new System.Drawing.Size(300, 24);
            this.rbLoiKyThuat.TabIndex = 2;
            this.rbLoiKyThuat.Text = "Lỗi kỹ thuật";
            // 
            // rbBoThi
            // 
            this.rbBoThi.Location = new System.Drawing.Point(15, 76);
            this.rbBoThi.Name = "rbBoThi";
            this.rbBoThi.Size = new System.Drawing.Size(300, 24);
            this.rbBoThi.TabIndex = 3;
            this.rbBoThi.Text = "Thí sinh bỏ thi";
            // 
            // btnOK
            // 
            this.btnOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOK.ForeColor = System.Drawing.Color.White;
            this.btnOK.Location = new System.Drawing.Point(160, 185);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 32);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "XÁC NHẬN";
            this.btnOK.UseVisualStyleBackColor = false;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(270, 185);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(90, 32);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "HỦY";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // FormKetThucSession
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(380, 241);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.grpLyDo);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormKetThucSession";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Lý do kết thúc bài thi";
            this.grpLyDo.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        #endregion
    }
}