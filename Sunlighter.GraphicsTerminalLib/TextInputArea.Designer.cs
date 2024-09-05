namespace Sunlighter.GraphicsTerminalLib
{
    partial class TextInputArea
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
            textBoxInput = new TextBox();
            buttonSubmit = new Button();
            SuspendLayout();
            // 
            // textBoxInput
            // 
            textBoxInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxInput.Location = new Point(9, 9);
            textBoxInput.Name = "textBoxInput";
            textBoxInput.Size = new Size(494, 23);
            textBoxInput.TabIndex = 0;
            textBoxInput.KeyDown += textBoxInput_KeyDown;
            // 
            // buttonSubmit
            // 
            buttonSubmit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonSubmit.Location = new Point(509, 9);
            buttonSubmit.Name = "buttonSubmit";
            buttonSubmit.Size = new Size(75, 23);
            buttonSubmit.TabIndex = 1;
            buttonSubmit.Text = "Submit";
            buttonSubmit.UseVisualStyleBackColor = true;
            buttonSubmit.Click += buttonSubmit_Click;
            // 
            // TextInputArea
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(buttonSubmit);
            Controls.Add(textBoxInput);
            Name = "TextInputArea";
            Padding = new Padding(6);
            Size = new Size(593, 42);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBoxInput;
        private Button buttonSubmit;
    }
}
