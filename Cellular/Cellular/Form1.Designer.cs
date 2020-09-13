namespace Cellular
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.rightWrapper = new System.Windows.Forms.Panel();
            this.radioPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.configButton = new System.Windows.Forms.Button();
            this.fastLabel = new System.Windows.Forms.Label();
            this.slowLabel = new System.Windows.Forms.Label();
            this.nextButton = new System.Windows.Forms.Button();
            this.speedBar = new System.Windows.Forms.TrackBar();
            this.playButton = new System.Windows.Forms.Button();
            this.leftWrapper = new System.Windows.Forms.Panel();
            this.contentPanel = new System.Windows.Forms.Panel();
            this.rightWrapper.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.speedBar)).BeginInit();
            this.leftWrapper.SuspendLayout();
            this.SuspendLayout();
            // 
            // rightWrapper
            // 
            this.rightWrapper.Controls.Add(this.radioPanel);
            this.rightWrapper.Controls.Add(this.configButton);
            this.rightWrapper.Controls.Add(this.fastLabel);
            this.rightWrapper.Controls.Add(this.slowLabel);
            this.rightWrapper.Controls.Add(this.nextButton);
            this.rightWrapper.Controls.Add(this.speedBar);
            this.rightWrapper.Controls.Add(this.playButton);
            this.rightWrapper.Dock = System.Windows.Forms.DockStyle.Right;
            this.rightWrapper.Location = new System.Drawing.Point(781, 0);
            this.rightWrapper.Name = "rightWrapper";
            this.rightWrapper.Size = new System.Drawing.Size(155, 629);
            this.rightWrapper.TabIndex = 0;
            // 
            // radioPanel
            // 
            this.radioPanel.AutoScroll = true;
            this.radioPanel.Location = new System.Drawing.Point(23, 209);
            this.radioPanel.Name = "radioPanel";
            this.radioPanel.Size = new System.Drawing.Size(109, 359);
            this.radioPanel.TabIndex = 0;
            // 
            // configButton
            // 
            this.configButton.Location = new System.Drawing.Point(30, 579);
            this.configButton.Name = "configButton";
            this.configButton.Size = new System.Drawing.Size(95, 40);
            this.configButton.TabIndex = 6;
            this.configButton.Text = "Load different config";
            this.configButton.UseVisualStyleBackColor = true;
            this.configButton.Click += new System.EventHandler(this.configButton_Click);
            // 
            // fastLabel
            // 
            this.fastLabel.AutoSize = true;
            this.fastLabel.Location = new System.Drawing.Point(101, 180);
            this.fastLabel.Name = "fastLabel";
            this.fastLabel.Size = new System.Drawing.Size(24, 13);
            this.fastLabel.TabIndex = 4;
            this.fastLabel.Text = "fast";
            // 
            // slowLabel
            // 
            this.slowLabel.AutoSize = true;
            this.slowLabel.Location = new System.Drawing.Point(27, 180);
            this.slowLabel.Name = "slowLabel";
            this.slowLabel.Size = new System.Drawing.Size(28, 13);
            this.slowLabel.TabIndex = 3;
            this.slowLabel.Text = "slow";
            // 
            // nextButton
            // 
            this.nextButton.Location = new System.Drawing.Point(30, 112);
            this.nextButton.Name = "nextButton";
            this.nextButton.Size = new System.Drawing.Size(95, 29);
            this.nextButton.TabIndex = 2;
            this.nextButton.Text = "next";
            this.nextButton.UseVisualStyleBackColor = true;
            this.nextButton.Click += new System.EventHandler(this.nextButton_Click);
            // 
            // speedBar
            // 
            this.speedBar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.speedBar.LargeChange = 1;
            this.speedBar.Location = new System.Drawing.Point(23, 151);
            this.speedBar.Maximum = 8;
            this.speedBar.Name = "speedBar";
            this.speedBar.Size = new System.Drawing.Size(109, 45);
            this.speedBar.TabIndex = 1;
            this.speedBar.Value = 4;
            // 
            // playButton
            // 
            this.playButton.Image = ((System.Drawing.Image)(resources.GetObject("playButton.Image")));
            this.playButton.Location = new System.Drawing.Point(40, 21);
            this.playButton.Name = "playButton";
            this.playButton.Size = new System.Drawing.Size(75, 75);
            this.playButton.TabIndex = 0;
            this.playButton.UseVisualStyleBackColor = true;
            this.playButton.Click += new System.EventHandler(this.playButton_Click);
            // 
            // leftWrapper
            // 
            this.leftWrapper.AutoScroll = true;
            this.leftWrapper.Controls.Add(this.contentPanel);
            this.leftWrapper.Dock = System.Windows.Forms.DockStyle.Left;
            this.leftWrapper.Location = new System.Drawing.Point(0, 0);
            this.leftWrapper.Name = "leftWrapper";
            this.leftWrapper.Size = new System.Drawing.Size(784, 629);
            this.leftWrapper.TabIndex = 1;
            // 
            // contentPanel
            // 
            this.contentPanel.Location = new System.Drawing.Point(0, 0);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Size = new System.Drawing.Size(0, 0);
            this.contentPanel.TabIndex = 0;
            this.contentPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.contentPanel_Paint);
            this.contentPanel.MouseClick += new System.Windows.Forms.MouseEventHandler(this.contentPanel_MouseClick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(936, 629);
            this.Controls.Add(this.leftWrapper);
            this.Controls.Add(this.rightWrapper);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "Form1";
            this.Text = "Cellular";
            this.Resize += new System.EventHandler(this.Form1_Resize);
            this.rightWrapper.ResumeLayout(false);
            this.rightWrapper.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.speedBar)).EndInit();
            this.leftWrapper.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel rightWrapper;
        private System.Windows.Forms.TrackBar speedBar;
        private System.Windows.Forms.Button playButton;
        private System.Windows.Forms.Panel leftWrapper;
        private System.Windows.Forms.Panel contentPanel;
        private System.Windows.Forms.Button configButton;
        private System.Windows.Forms.Label fastLabel;
        private System.Windows.Forms.Label slowLabel;
        private System.Windows.Forms.Button nextButton;
        private System.Windows.Forms.FlowLayoutPanel radioPanel;
    }
}

