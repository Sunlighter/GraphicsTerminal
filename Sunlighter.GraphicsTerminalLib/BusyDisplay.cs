using Sunlighter.OptionLib;
using System.ComponentModel;

namespace Sunlighter.GraphicsTerminalLib
{
    public partial class BusyDisplay : UserControl
    {
        public BusyDisplay()
        {
            InitializeComponent();
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string BusyDoing
        {
            get { return label.Text; }
            set { label.Text = value ?? string.Empty; }
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool CancelVisible
        {
            get { return buttonCancel.Visible; }
            set { buttonCancel.Visible = value; }
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool CancelEnabled
        {
            get { return buttonCancel.Enabled; }
            set { buttonCancel.Enabled = value; }
        }

        public event EventHandler? CancelClicked;

        [DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
        public Option<double> ProgressAmount
        {
            get
            {
                if (progressBar.Style == ProgressBarStyle.Marquee)
                {
                    return Option<double>.None;
                }
                else
                {
                    return Option<double>.Some(progressBar.Value / 1000.0);
                }
            }
            set
            {
                if (value.HasValue)
                {
                    progressBar.Style = ProgressBarStyle.Continuous;
                    progressBar.Value = (int)Math.Round(Math.Min(1000.0, Math.Max(0.0, value.Value * 1000.0)));
                }
                else
                {
                    progressBar.Style = ProgressBarStyle.Marquee;
                }
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            CancelClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
