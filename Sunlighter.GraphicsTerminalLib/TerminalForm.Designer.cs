namespace Sunlighter.GraphicsTerminalLib
{
    partial class TerminalForm
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
            components = new System.ComponentModel.Container();
            channelMonitor1 = new ChannelMonitor(components);
            textInputArea = new TextInputArea();
            timer1 = new System.Windows.Forms.Timer(components);
            terminalCanvas1 = new TerminalCanvas();
            bigTextDisplay1 = new BigTextDisplay();
            busyDisplay1 = new BusyDisplay();
            SuspendLayout();
            // 
            // channelMonitor1
            // 
            channelMonitor1.SyncRoot = this;
            channelMonitor1.ItemReceived += channelMonitor1_ItemReceived;
            channelMonitor1.EofReceived += channelMonitor1_EofReceived;
            // 
            // textInputArea
            // 
            textInputArea.Dock = DockStyle.Bottom;
            textInputArea.InputText = "";
            textInputArea.Location = new Point(0, 408);
            textInputArea.Name = "textInputArea";
            textInputArea.Padding = new Padding(6);
            textInputArea.Size = new Size(800, 42);
            textInputArea.TabIndex = 0;
            textInputArea.SubmitClicked += textInputArea_SubmitClicked;
            // 
            // timer1
            // 
            timer1.Enabled = true;
            timer1.Interval = 250;
            timer1.Tick += timer1_Tick;
            // 
            // terminalCanvas1
            // 
            terminalCanvas1.Dock = DockStyle.Fill;
            terminalCanvas1.Location = new Point(0, 0);
            terminalCanvas1.Name = "terminalCanvas1";
            terminalCanvas1.Size = new Size(800, 408);
            terminalCanvas1.TabIndex = 1;
            terminalCanvas1.Text = "terminalCanvas1";
            terminalCanvas1.CanvasMouseClick += terminalCanvas1_CanvasMouseClick;
            terminalCanvas1.CanvasKeyDown += terminalCanvas1_CanvasKeyDown;
            terminalCanvas1.ClientSizeChanged += terminalCanvas1_ClientSizeChanged;
            // 
            // bigTextDisplay1
            // 
            bigTextDisplay1.ButtonStyle = MessageBoxButtons.OK;
            bigTextDisplay1.ContentReadOnly = false;
            bigTextDisplay1.ContentText = "";
            bigTextDisplay1.Dock = DockStyle.Fill;
            bigTextDisplay1.LabelText = "Text Here!";
            bigTextDisplay1.Location = new Point(0, 0);
            bigTextDisplay1.Name = "bigTextDisplay1";
            bigTextDisplay1.Padding = new Padding(6);
            bigTextDisplay1.Size = new Size(800, 408);
            bigTextDisplay1.TabIndex = 2;
            bigTextDisplay1.ButtonClicked += bigTextDisplay1_ButtonClicked;
            // 
            // busyDisplay1
            // 
            busyDisplay1.BusyDoing = "Working...";
            busyDisplay1.CancelEnabled = true;
            busyDisplay1.CancelVisible = true;
            busyDisplay1.Dock = DockStyle.Fill;
            busyDisplay1.Location = new Point(0, 0);
            busyDisplay1.Name = "busyDisplay1";
            busyDisplay1.Padding = new Padding(6);
            busyDisplay1.Size = new Size(800, 408);
            busyDisplay1.TabIndex = 3;
            busyDisplay1.CancelClicked += busyDisplay1_CancelClicked;
            // 
            // TerminalForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(terminalCanvas1);
            Controls.Add(busyDisplay1);
            Controls.Add(bigTextDisplay1);
            Controls.Add(textInputArea);
            Name = "TerminalForm";
            Text = "TerminalForm";
            FormClosing += TerminalForm_FormClosing;
            FormClosed += TerminalForm_FormClosed;
            Load += TerminalForm_Load;
            ResumeLayout(false);
        }

        #endregion

        private ChannelMonitor channelMonitor1;
        private TextInputArea textInputArea;
        private System.Windows.Forms.Timer timer1;
        private TerminalCanvas terminalCanvas1;
        private BigTextDisplay bigTextDisplay1;
        private BusyDisplay busyDisplay1;
    }
}