namespace Sunlighter.GraphicsTerminalLib
{
    public partial class TextInputArea : UserControl
    {
        public TextInputArea()
        {
            InitializeComponent();
        }

        public string InputText
        {
            get { return textBoxInput.Text; }
            set { textBoxInput.Text = value; }
        }

        public event EventHandler? SubmitClicked;

        private void buttonSubmit_Click(object sender, EventArgs e)
        {
            SubmitClicked?.Invoke(this, EventArgs.Empty);
        }

        private void textBoxInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SubmitClicked?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }
    }
}
