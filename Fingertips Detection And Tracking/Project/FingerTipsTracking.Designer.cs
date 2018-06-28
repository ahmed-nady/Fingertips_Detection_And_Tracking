namespace FingerTipsDetectionTracking.Project
{
    partial class FingerTipsTracking
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
            this.CaptureImageBox = new Emgu.CV.UI.ImageBox();
            this.FingerTipsTrackingBox = new Emgu.CV.UI.ImageBox();
            this.StartButton = new System.Windows.Forms.Button();
            this.fingerTipsTrajectoryBox = new Emgu.CV.UI.ImageBox();
            ((System.ComponentModel.ISupportInitialize)(this.CaptureImageBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.FingerTipsTrackingBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fingerTipsTrajectoryBox)).BeginInit();
            this.SuspendLayout();
            // 
            // CaptureImageBox
            // 
            this.CaptureImageBox.Location = new System.Drawing.Point(13, 13);
            this.CaptureImageBox.Name = "CaptureImageBox";
            this.CaptureImageBox.Size = new System.Drawing.Size(325, 371);
            this.CaptureImageBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.CaptureImageBox.TabIndex = 2;
            this.CaptureImageBox.TabStop = false;
            this.CaptureImageBox.Paint += new System.Windows.Forms.PaintEventHandler(this.CaptureImageBox_Paint);
            this.CaptureImageBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.CaptureImageBox_MouseDown);
            this.CaptureImageBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.CaptureImageBox_MouseMove);
            this.CaptureImageBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.CaptureImageBox_MouseUp);
            // 
            // FingerTipsTrackingBox
            // 
            this.FingerTipsTrackingBox.Location = new System.Drawing.Point(344, 15);
            this.FingerTipsTrackingBox.Name = "FingerTipsTrackingBox";
            this.FingerTipsTrackingBox.Size = new System.Drawing.Size(325, 371);
            this.FingerTipsTrackingBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.FingerTipsTrackingBox.TabIndex = 3;
            this.FingerTipsTrackingBox.TabStop = false;
            // 
            // StartButton
            // 
            this.StartButton.Location = new System.Drawing.Point(317, 392);
            this.StartButton.Name = "StartButton";
            this.StartButton.Size = new System.Drawing.Size(96, 33);
            this.StartButton.TabIndex = 4;
            this.StartButton.Text = "Start";
            this.StartButton.UseVisualStyleBackColor = true;
            this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // fingerTipsTrajectoryBox
            // 
            this.fingerTipsTrajectoryBox.Location = new System.Drawing.Point(675, 15);
            this.fingerTipsTrajectoryBox.Name = "fingerTipsTrajectoryBox";
            this.fingerTipsTrajectoryBox.Size = new System.Drawing.Size(325, 371);
            this.fingerTipsTrajectoryBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.fingerTipsTrajectoryBox.TabIndex = 5;
            this.fingerTipsTrajectoryBox.TabStop = false;
            // 
            // FingerTipsTracking
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1003, 428);
            this.Controls.Add(this.fingerTipsTrajectoryBox);
            this.Controls.Add(this.StartButton);
            this.Controls.Add(this.FingerTipsTrackingBox);
            this.Controls.Add(this.CaptureImageBox);
            this.Name = "FingerTipsTracking";
            this.Text = "FingerTipsTracking";
            ((System.ComponentModel.ISupportInitialize)(this.CaptureImageBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.FingerTipsTrackingBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fingerTipsTrajectoryBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Emgu.CV.UI.ImageBox CaptureImageBox;
        private Emgu.CV.UI.ImageBox FingerTipsTrackingBox;
        private System.Windows.Forms.Button StartButton;
        private Emgu.CV.UI.ImageBox fingerTipsTrajectoryBox;
    }
}