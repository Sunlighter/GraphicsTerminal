using Sunlighter.OptionLib;
using System.Collections.Immutable;

namespace Sunlighter.GraphicsTerminalLib
{
    public partial class BigTextDisplay : UserControl
    {
        private MessageBoxButtons buttons;

        public BigTextDisplay()
        {
            InitializeComponent();
        }

        public string LabelText
        {
            get { return label1.Text; }
            set { label1.Text = value ?? string.Empty; }
        }

        public string ContentText
        {
            get { return textBox1.Text; }
            set { textBox1.Text = value ?? string.Empty; }
        }

        public bool ContentReadOnly
        {
            get { return textBox1.ReadOnly; }
            set { textBox1.ReadOnly = value; }
        }

        public void MoveCaretToEnd()
        {
            textBox1.SelectionLength = 0;
            textBox1.SelectionStart = textBox1.Text.Length;
        }

        public MessageBoxButtons ButtonStyle
        {
            get { return buttons; }
            set
            {
                buttons = value;
                SetButtons(buttons);
            }
        }

        public event BigTextButtonClickedHandler? ButtonClicked;

        private void button0_Click(object sender, EventArgs e)
        {
            TryFireEvent(bi => bi.Button0);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TryFireEvent(bi => bi.Button1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            TryFireEvent(bi => bi.Button2);
        }

        private void SetButtons(MessageBoxButtons buttons)
        {
            if (ButtonInfo.Dictionary.TryGetValue(buttons, out ButtonInfo? bi))
            {
                SetButtons(bi);
            }
            else
            {
                SetButtons(ButtonInfo.Dictionary[MessageBoxButtons.OK]);
            }
        }

        private void TryFireEvent(Func<ButtonInfo, Option<DialogResult>> getButton)
        {
            if (ButtonInfo.Dictionary.TryGetValue(buttons, out ButtonInfo? bi))
            {
                Option<DialogResult> odr = getButton(bi);
                if (odr.HasValue)
                {
                    ButtonClicked?.Invoke(this, new BigTextButtonClickedEventArgs(odr.Value));
                }
            }
        }

        private void SetButtons(ButtonInfo bi)
        {
            SetButton(bi.Button0, button0);
            SetButton(bi.Button1, button1);
            SetButton(bi.Button2, button2);
        }

        private static void SetButton(Option<DialogResult> function, Button b)
        {
            if (function.HasValue)
            {
                b.Visible = true;
                switch(function.Value)
                {
                    case DialogResult.OK:
                        b.Text = "OK"; break;
                    case DialogResult.Cancel:
                        b.Text = "Cancel"; break;
                    case DialogResult.Yes:
                        b.Text = "Yes"; break;
                    case DialogResult.No:
                        b.Text = "No"; break;
                    case DialogResult.Abort:
                        b.Text = "Abort"; break;
                    case DialogResult.Retry:
                        b.Text = "Retry"; break;
                    case DialogResult.TryAgain:
                        b.Text = "Try Again"; break;
                    case DialogResult.Ignore:
                        b.Text = "Ignore"; break;
                    case DialogResult.Continue:
                        b.Text = "Continue"; break;
                    default:
                        b.Visible = false; break;
                }
            }
            else
            {
                b.Visible = false;
            }
        }
    }

    internal sealed class ButtonInfo
    {
        private readonly Option<DialogResult> button0;
        private readonly Option<DialogResult> button1;
        private readonly Option<DialogResult> button2;

        public ButtonInfo
        (
            Option<DialogResult> button0,
            Option<DialogResult> button1,
            Option<DialogResult> button2
        )
        {
            this.button0 = button0;
            this.button1 = button1;
            this.button2 = button2;
        }

        public Option<DialogResult> Button0 => button0;
        public Option<DialogResult> Button1 => button1;
        public Option<DialogResult> Button2 => button2;

        private static readonly Lazy<ImmutableSortedDictionary<MessageBoxButtons, ButtonInfo>> dict =
            new Lazy<ImmutableSortedDictionary<MessageBoxButtons, ButtonInfo>>(GetDict, LazyThreadSafetyMode.ExecutionAndPublication);

        private static ImmutableSortedDictionary<MessageBoxButtons, ButtonInfo> GetDict()
        {
            ImmutableSortedDictionary<MessageBoxButtons, ButtonInfo>.Builder results =
                ImmutableSortedDictionary<MessageBoxButtons, ButtonInfo>.Empty.ToBuilder();

            results.Add(MessageBoxButtons.OK, new ButtonInfo(Option<DialogResult>.None, Option<DialogResult>.None, Option<DialogResult>.Some(DialogResult.OK)));
            results.Add(MessageBoxButtons.OKCancel, new ButtonInfo(Option<DialogResult>.None, Option<DialogResult>.Some(DialogResult.OK), Option<DialogResult>.Some(DialogResult.Cancel)));
            results.Add(MessageBoxButtons.YesNoCancel, new ButtonInfo(Option<DialogResult>.Some(DialogResult.Yes), Option<DialogResult>.Some(DialogResult.No), Option<DialogResult>.Some(DialogResult.Cancel)));
            results.Add(MessageBoxButtons.YesNo, new ButtonInfo(Option<DialogResult>.None, Option<DialogResult>.Some(DialogResult.Yes), Option<DialogResult>.Some(DialogResult.No)));
            results.Add(MessageBoxButtons.RetryCancel, new ButtonInfo(Option<DialogResult>.None, Option<DialogResult>.Some(DialogResult.Retry), Option<DialogResult>.Some(DialogResult.Cancel)));
            results.Add(MessageBoxButtons.AbortRetryIgnore, new ButtonInfo(Option<DialogResult>.Some(DialogResult.Abort), Option<DialogResult>.Some(DialogResult.Retry), Option<DialogResult>.Some(DialogResult.Ignore)));
            results.Add(MessageBoxButtons.CancelTryContinue, new ButtonInfo(Option<DialogResult>.Some(DialogResult.Cancel), Option<DialogResult>.Some(DialogResult.TryAgain), Option<DialogResult>.Some(DialogResult.Continue)));

            return results.ToImmutable();
        }

        public static ImmutableSortedDictionary<MessageBoxButtons, ButtonInfo> Dictionary => dict.Value;
    }

    public delegate void BigTextButtonClickedHandler(object sender, BigTextButtonClickedEventArgs e);

    public sealed class BigTextButtonClickedEventArgs : EventArgs
    {
        private readonly DialogResult result;

        public BigTextButtonClickedEventArgs(DialogResult result)
        {
            this.result = result;
        }

        public DialogResult DialogResult => result;
    }
}
