namespace WeatherGUI
{
    partial class Form1
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
        private void InitializeComponent()
        {
            button1 = new Button();
            monthCalendar1 = new MonthCalendar();
            txtJsonData = new TextBox();
            btnCopyToClipboard = new Button();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(44, 26);
            button1.Name = "button1";
            button1.Size = new Size(715, 94);
            button1.TabIndex = 0;
            button1.Text = "Check Today's Weather";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // monthCalendar1
            // 
            monthCalendar1.Location = new Point(283, 213);
            monthCalendar1.Name = "monthCalendar1";
            monthCalendar1.TabIndex = 1;
            // 
            // txtJsonData
            // 
            txtJsonData.Location = new Point(44, 414);
            txtJsonData.Name = "txtJsonData";
            txtJsonData.Size = new Size(583, 23);
            txtJsonData.TabIndex = 2;
            // 
            // btnCopyToClipboard
            // 
            btnCopyToClipboard.Location = new Point(645, 414);
            btnCopyToClipboard.Name = "btnCopyToClipboard";
            btnCopyToClipboard.Size = new Size(114, 23);
            btnCopyToClipboard.TabIndex = 3;
            btnCopyToClipboard.Text = "Copy To Clipboard";
            btnCopyToClipboard.UseVisualStyleBackColor = true;
            btnCopyToClipboard.Click += btnCopyToClipboard_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnCopyToClipboard);
            Controls.Add(txtJsonData);
            Controls.Add(monthCalendar1);
            Controls.Add(button1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private MonthCalendar monthCalendar1;
        private TextBox txtJsonData;
        private Button btnCopyToClipboard;
    }
}