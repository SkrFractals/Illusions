namespace Illusions
{
    partial class MyForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			components = new System.ComponentModel.Container();
			screen = new PictureBox();
			timer = new System.Windows.Forms.Timer(components);
			((System.ComponentModel.ISupportInitialize)screen).BeginInit();
			SuspendLayout();
			// 
			// screen
			// 
			screen.Location = new Point(12, 12);
			screen.Name = "screen";
			screen.Size = new Size(1920, 1080);
			screen.TabIndex = 0;
			screen.TabStop = false;
			// 
			// timer
			// 
			timer.Enabled = true;
			timer.Interval = 50;
			timer.Tick += timer_Tick;
			// 
			// Form1
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(1945, 1100);
			Controls.Add(screen);
			Name = "Form1";
			Text = "Form1";
			((System.ComponentModel.ISupportInitialize)screen).EndInit();
			ResumeLayout(false);
		}

		#endregion

		private PictureBox screen;
		private System.Windows.Forms.Timer timer;
	}
}
