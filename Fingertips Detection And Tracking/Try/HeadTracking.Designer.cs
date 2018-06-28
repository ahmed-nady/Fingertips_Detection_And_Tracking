namespace FingerTipsDetectionTracking.Try
{
    partial class HeadTracking
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
            this.imageBoxOpticalFlow = new Emgu.CV.UI.ImageBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.buttonInitializeTracking = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxOpticalFlow)).BeginInit();
            this.SuspendLayout();
            // 
            // imageBoxOpticalFlow
            // 
            this.imageBoxOpticalFlow.Location = new System.Drawing.Point(13, 13);
            this.imageBoxOpticalFlow.Name = "imageBoxOpticalFlow";
            this.imageBoxOpticalFlow.Size = new System.Drawing.Size(501, 387);
            this.imageBoxOpticalFlow.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.imageBoxOpticalFlow.TabIndex = 2;
            this.imageBoxOpticalFlow.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(609, 66);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "label1";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(612, 97);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(35, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "label2";
            // 
            // buttonInitializeTracking
            // 
            this.buttonInitializeTracking.Location = new System.Drawing.Point(593, 29);
            this.buttonInitializeTracking.Name = "buttonInitializeTracking";
            this.buttonInitializeTracking.Size = new System.Drawing.Size(75, 23);
            this.buttonInitializeTracking.TabIndex = 5;
            this.buttonInitializeTracking.Text = "InitializeTracking";
            this.buttonInitializeTracking.UseVisualStyleBackColor = true;
            this.buttonInitializeTracking.Click += new System.EventHandler(this.buttonInitializeTracking_Click);
            // 
            // HeadTracking
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(694, 412);
            this.Controls.Add(this.buttonInitializeTracking);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.imageBoxOpticalFlow);
            this.Name = "HeadTracking";
            this.Text = "HeadTracking";
            this.Load += new System.EventHandler(this.HeadTracking_Load);
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxOpticalFlow)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Emgu.CV.UI.ImageBox imageBoxOpticalFlow;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button buttonInitializeTracking;
    }
}