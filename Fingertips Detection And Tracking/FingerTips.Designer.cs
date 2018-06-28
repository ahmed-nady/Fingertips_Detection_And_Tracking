namespace Depth_Kinect.tries
{
    partial class hand_detection
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
            this.components = new System.ComponentModel.Container();
            this.StartButton = new System.Windows.Forms.Button();
            this.imageBoxSkin = new Emgu.CV.UI.ImageBox();
            this.CaptureImageBox = new Emgu.CV.UI.ImageBox();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxSkin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CaptureImageBox)).BeginInit();
            this.SuspendLayout();
            // 
            // StartButton
            // 
            this.StartButton.Location = new System.Drawing.Point(277, 394);
            this.StartButton.Name = "StartButton";
            this.StartButton.Size = new System.Drawing.Size(96, 33);
            this.StartButton.TabIndex = 7;
            this.StartButton.Text = "Start";
            this.StartButton.UseVisualStyleBackColor = true;
            this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // imageBoxSkin
            // 
            this.imageBoxSkin.Location = new System.Drawing.Point(347, 12);
            this.imageBoxSkin.Name = "imageBoxSkin";
            this.imageBoxSkin.Size = new System.Drawing.Size(325, 371);
            this.imageBoxSkin.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.imageBoxSkin.TabIndex = 6;
            this.imageBoxSkin.TabStop = false;
            // 
            // CaptureImageBox
            // 
            this.CaptureImageBox.Location = new System.Drawing.Point(16, 10);
            this.CaptureImageBox.Name = "CaptureImageBox";
            this.CaptureImageBox.Size = new System.Drawing.Size(325, 371);
            this.CaptureImageBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.CaptureImageBox.TabIndex = 5;
            this.CaptureImageBox.TabStop = false;
            // 
            // FingerTips
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 439);
            this.Controls.Add(this.StartButton);
            this.Controls.Add(this.imageBoxSkin);
            this.Controls.Add(this.CaptureImageBox);
            this.Name = "FingerTips";
            this.Text = "FingerTips";
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxSkin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CaptureImageBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button StartButton;
        private Emgu.CV.UI.ImageBox imageBoxSkin;
        private Emgu.CV.UI.ImageBox CaptureImageBox;
    }
}