namespace SAPAutoLogon
{
    partial class OptionDialog
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
        private void InitializeComponent()
        {
            this.groupBoxPath = new System.Windows.Forms.GroupBox();
            this.iNBCSPath = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.iSapGuiPath = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.groupBoxOptions = new System.Windows.Forms.GroupBox();
            this.isMaxGui = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.BtnCancel = new System.Windows.Forms.Button();
            this.btnDetectGui = new System.Windows.Forms.Button();
            this.btnDetectNbc = new System.Windows.Forms.Button();
            this.groupBoxPath.SuspendLayout();
            this.groupBoxOptions.SuspendLayout();
            this.SuspendLayout();
            //
            // groupBoxPath
            //
            this.groupBoxPath.Controls.Add(this.btnDetectNbc);
            this.groupBoxPath.Controls.Add(this.btnDetectGui);
            this.groupBoxPath.Controls.Add(this.iNBCSPath);
            this.groupBoxPath.Controls.Add(this.label9);
            this.groupBoxPath.Controls.Add(this.iSapGuiPath);
            this.groupBoxPath.Controls.Add(this.label8);
            this.groupBoxPath.Location = new System.Drawing.Point(12, 12);
            this.groupBoxPath.Name = "groupBoxPath";
            this.groupBoxPath.Size = new System.Drawing.Size(533, 73);
            this.groupBoxPath.TabIndex = 0;
            this.groupBoxPath.TabStop = false;
            this.groupBoxPath.Text = "System paths";
            //
            // iNBCSPath
            //
            this.iNBCSPath.CausesValidation = false;
            this.iNBCSPath.Location = new System.Drawing.Point(136, 41);
            this.iNBCSPath.Name = "iNBCSPath";
            this.iNBCSPath.Size = new System.Drawing.Size(300, 20);
            this.iNBCSPath.TabIndex = 4;
            //
            // label9
            //
            this.label9.AutoSize = true;
            this.label9.CausesValidation = false;
            this.label9.Location = new System.Drawing.Point(4, 44);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(126, 13);
            this.label9.TabIndex = 3;
            this.label9.Text = "SAP Business Client path";
            //
            // iSapGuiPath
            //
            this.iSapGuiPath.Location = new System.Drawing.Point(136, 17);
            this.iSapGuiPath.Name = "iSapGuiPath";
            this.iSapGuiPath.Size = new System.Drawing.Size(300, 20);
            this.iSapGuiPath.TabIndex = 1;
            //
            // btnDetectGui
            //
            this.btnDetectGui.Location = new System.Drawing.Point(442, 15);
            this.btnDetectGui.Name = "btnDetectGui";
            this.btnDetectGui.Size = new System.Drawing.Size(75, 23);
            this.btnDetectGui.TabIndex = 2;
            this.btnDetectGui.Text = "Detect";
            this.btnDetectGui.UseVisualStyleBackColor = true;
            this.btnDetectGui.Click += new System.EventHandler(this.OnBtnDetectGui_Click);
            //
            // btnDetectNbc
            //
            this.btnDetectNbc.Location = new System.Drawing.Point(442, 39);
            this.btnDetectNbc.Name = "btnDetectNbc";
            this.btnDetectNbc.Size = new System.Drawing.Size(75, 23);
            this.btnDetectNbc.TabIndex = 5;
            this.btnDetectNbc.Text = "Detect";
            this.btnDetectNbc.UseVisualStyleBackColor = true;
            this.btnDetectNbc.Click += new System.EventHandler(this.OnBtnDetectNbc_Click);
            //
            // label8
            //
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(59, 20);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(71, 13);
            this.label8.TabIndex = 0;
            this.label8.Text = "SAP Gui path";
            //
            // groupBoxOptions
            //
            this.groupBoxOptions.Controls.Add(this.isMaxGui);
            this.groupBoxOptions.Location = new System.Drawing.Point(12, 91);
            this.groupBoxOptions.Name = "groupBoxOptions";
            this.groupBoxOptions.Size = new System.Drawing.Size(533, 50);
            this.groupBoxOptions.TabIndex = 1;
            this.groupBoxOptions.TabStop = false;
            this.groupBoxOptions.Text = "Gui start options";
            //
            // isMaxGui
            //
            this.isMaxGui.AutoSize = true;
            this.isMaxGui.Location = new System.Drawing.Point(136, 19);
            this.isMaxGui.Name = "isMaxGui";
            this.isMaxGui.Size = new System.Drawing.Size(158, 17);
            this.isMaxGui.TabIndex = 0;
            this.isMaxGui.Text = "Run with maximized window";
            this.isMaxGui.UseVisualStyleBackColor = true;
            //
            // btnOK
            //
            this.btnOK.Image = global::SAPAutoLogon.Properties.Resources.okey_16;
            this.btnOK.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnOK.Location = new System.Drawing.Point(12, 153);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 23);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.OnBtnOK_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.Image = global::SAPAutoLogon.Properties.Resources.cancel_16;
            this.BtnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.BtnCancel.Location = new System.Drawing.Point(122, 153);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(100, 23);
            this.BtnCancel.TabIndex = 3;
            this.BtnCancel.Text = "Cancel";
            this.BtnCancel.UseVisualStyleBackColor = true;
            this.BtnCancel.Click += new System.EventHandler(this.OnBtnCancel_Click);
            //
            // OptionDialog
            //
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.BtnCancel;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(557, 188);
            this.Controls.Add(this.BtnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.groupBoxOptions);
            this.Controls.Add(this.groupBoxPath);
            this.Name = "OptionDialog";
            this.Text = "SAPAutoLogon Options";
            this.groupBoxPath.ResumeLayout(false);
            this.groupBoxPath.PerformLayout();
            this.groupBoxOptions.ResumeLayout(false);
            this.groupBoxOptions.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxPath;
        private System.Windows.Forms.TextBox iNBCSPath;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox iSapGuiPath;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.GroupBox groupBoxOptions;
        private System.Windows.Forms.CheckBox isMaxGui;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button BtnCancel;
        private System.Windows.Forms.Button btnDetectGui;
        private System.Windows.Forms.Button btnDetectNbc;
    }
}
