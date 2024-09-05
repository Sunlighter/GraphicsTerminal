using Sunlighter.OptionLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sunlighter.GraphicsTerminalLib
{
    public partial class BusyDisplay : UserControl
    {
        public BusyDisplay()
        {
            InitializeComponent();
        }

        public string BusyDoing
        {
            get { return label.Text; }
            set { label.Text = value ?? string.Empty; }
        }

        public bool CancelVisible
        {
            get { return buttonCancel.Visible; }
            set { buttonCancel.Visible = value; }
        }

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
