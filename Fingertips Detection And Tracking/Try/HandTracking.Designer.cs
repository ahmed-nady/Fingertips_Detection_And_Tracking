namespace FingerTipsDetectionTracking.Try
{
    partial class HandTracking
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
            this.initializeTracking = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxOpticalFlow)).BeginInit();
            this.SuspendLayout();
            // 
            // imageBoxOpticalFlow
            // 
            this.imageBoxOpticalFlow.Location = new System.Drawing.Point(13, 13);
            this.imageBoxOpticalFlow.Name = "imageBoxOpticalFlow";
            this.imageBoxOpticalFlow.Size = new System.Drawing.Size(527, 446);
            this.imageBoxOpticalFlow.TabIndex = 2;
            this.imageBoxOpticalFlow.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(645, 61);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "label1";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(562, 107);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(35, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "label2";
            // 
            // initializeTracking
            // 
            this.initializeTracking.Location = new System.Drawing.Point(585, 13);
            this.initializeTracking.Name = "initializeTracking";
            this.initializeTracking.Size = new System.Drawing.Size(108, 23);
            this.initializeTracking.TabIndex = 5;
            this.initializeTracking.Text = "initializeTracking";
            this.initializeTracking.UseVisualStyleBackColor = true;
            this.initializeTracking.Click += new System.EventHandler(this.buttonInitializeTracking_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(627, 158);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 6;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // HandTracking
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(732, 471);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.initializeTracking);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.imageBoxOpticalFlow);
            this.Name = "HandTracking";
            this.Text = "HandTracking";
            ((System.ComponentModel.ISupportInitialize)(this.imageBoxOpticalFlow)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Emgu.CV.UI.ImageBox imageBoxOpticalFlow;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button initializeTracking;
        private System.Windows.Forms.Button button1;
    }
}