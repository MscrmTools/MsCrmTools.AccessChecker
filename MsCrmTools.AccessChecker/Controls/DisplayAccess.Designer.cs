
namespace MsCrmTools.AccessChecker
{
    partial class DisplayAccess
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.image = new System.Windows.Forms.PictureBox();
            this.linkRelated = new System.Windows.Forms.LinkLabel();
            this.flowLabel = new System.Windows.Forms.FlowLayoutPanel();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.lblTitle = new GrowLabel();
            ((System.ComponentModel.ISupportInitialize)(this.image)).BeginInit();
            this.flowLabel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // image
            // 
            this.image.Dock = System.Windows.Forms.DockStyle.Top;
            this.image.Location = new System.Drawing.Point(0, 0);
            this.image.Name = "image";
            this.image.Size = new System.Drawing.Size(25, 24);
            this.image.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.image.TabIndex = 0;
            this.image.TabStop = false;
            // 
            // linkRelated
            // 
            this.linkRelated.AutoSize = true;
            this.linkRelated.Location = new System.Drawing.Point(3, 13);
            this.linkRelated.Name = "linkRelated";
            this.linkRelated.Size = new System.Drawing.Size(55, 13);
            this.linkRelated.TabIndex = 2;
            this.linkRelated.TabStop = true;
            this.linkRelated.Text = "linkLabel1";
            this.linkRelated.Visible = false;
            this.linkRelated.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkRelated_LinkClicked);
            // 
            // flowLabel
            // 
            this.flowLabel.Controls.Add(this.lblTitle);
            this.flowLabel.Controls.Add(this.linkRelated);
            this.flowLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLabel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLabel.Location = new System.Drawing.Point(0, 0);
            this.flowLabel.Name = "flowLabel";
            this.flowLabel.Size = new System.Drawing.Size(121, 35);
            this.flowLabel.TabIndex = 4;
            // 
            // splitMain
            // 
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitMain.Location = new System.Drawing.Point(0, 0);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.image);
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.flowLabel);
            this.splitMain.Size = new System.Drawing.Size(150, 35);
            this.splitMain.SplitterDistance = 25;
            this.splitMain.TabIndex = 5;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(3, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(62, 13);
            this.lblTitle.TabIndex = 3;
            this.lblTitle.Text = "growLabel1";
            // 
            // DisplayAccess
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.splitMain);
            this.MinimumSize = new System.Drawing.Size(150, 35);
            this.Name = "DisplayAccess";
            this.Size = new System.Drawing.Size(150, 35);
            ((System.ComponentModel.ISupportInitialize)(this.image)).EndInit();
            this.flowLabel.ResumeLayout(false);
            this.flowLabel.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel1.PerformLayout();
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox image;
        private System.Windows.Forms.LinkLabel linkRelated;
        private GrowLabel lblTitle;
        private System.Windows.Forms.FlowLayoutPanel flowLabel;
        private System.Windows.Forms.SplitContainer splitMain;
    }
}
