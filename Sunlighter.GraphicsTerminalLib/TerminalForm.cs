using Sunlighter.OptionLib;
using System.ComponentModel;

namespace Sunlighter.GraphicsTerminalLib
{
    internal partial class TerminalForm : Form
    {
        private FormArguments? formArguments;
        private bool formLoaded;
        private TerminalState terminalState;
        private bool pendingCloseRequest;
        private bool reallyClosing;

        public TerminalForm()
        {
            InitializeComponent();
            formArguments = null;
            formLoaded = false;
            terminalState = new IdleState(false);
            pendingCloseRequest = false;
            reallyClosing = false;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal FormArguments? FormArguments
        {
            get
            {
                return formArguments;
            }
            set
            {
                formArguments = value;
                if (formLoaded && formArguments is not null)
                {
                    channelMonitor1.SetChannelReader(formArguments.RequestReader);
                }
                else if (formLoaded)
                {
                    channelMonitor1.ClearChannelReader();
                }
            }
        }

        private void TerminalForm_Load(object sender, EventArgs e)
        {
            formLoaded = true;
            if (formArguments is not null)
            {
                channelMonitor1.SetChannelReader(formArguments.RequestReader);
            }
        }

        public void SetPane(Pane p)
        {
            bool textInputAreaPreviouslyVisible = textInputArea.Visible;

            terminalCanvas1.Visible = (p == Pane.Canvas || p == Pane.CanvasWithTextInput);
            textInputArea.Visible = (p == Pane.CanvasWithTextInput);
            bigTextDisplay1.Visible = (p == Pane.BigText);
            busyDisplay1.Visible = (p == Pane.Busy);

            switch (p)
            {
                case Pane.BigText:
                    bigTextDisplay1.Focus();
                    bigTextDisplay1.MoveCaretToEnd();
                    break;
                case Pane.CanvasWithTextInput:
                    if (!textInputAreaPreviouslyVisible)
                    {
                        textInputArea.Focus();
                    }
                    break;
                case Pane.Canvas:
                    terminalCanvas1.Focus();
                    break;
                case Pane.Busy:
                    busyDisplay1.Focus();
                    break;
            }
        }

        private void SetTerminalState(TerminalState e)
        {
            terminalState = e;

            if (e is IdleState ids)
            {
                if (ids.ShowText)
                {
                    SetPane(Pane.CanvasWithTextInput);
                }
                else
                {
                    SetPane(Pane.Canvas);
                }
            }
            else if (e is DesiredCanvasEvent dce)
            {
                if (dce.DesiresCanvasEvent(EventFlags.TextEntry))
                {
                    SetPane(Pane.CanvasWithTextInput);
                }
                else
                {
                    SetPane(Pane.Canvas);
                }
            }
            else if (e is DesiredTextEvent)
            {
                SetPane(Pane.BigText);
            }
            else if (e is BusyState b)
            {
                SetPane(Pane.Busy);
            }
        }

        private void channelMonitor1_ItemReceived(object sender, ItemReceivedEventArgs e)
        {
            if (e.Item is TerminalRequest tr)
            {
                System.Diagnostics.Debug.Assert(formArguments is not null);

                if (pendingCloseRequest)
                {
                    formArguments.EventWriter.Send(TE_UserCloseRequest.Value);
                    SetTerminalState(new IdleState(terminalState.DesiresCanvasEvent(EventFlags.TextEntry)));
                    pendingCloseRequest = false;
                }
                else
                {
                    if (tr is TR_GetEvent getEventRequest)
                    {
                        Bitmap b = new Bitmap(getEventRequest.DesiredSize.Width, getEventRequest.DesiredSize.Height);
                        try
                        {
                            using (Graphics g = Graphics.FromImage(b))
                            {
                                getEventRequest.Draw(g);
                            }
                        }
                        catch (Exception exc)
                        {
                            System.Diagnostics.Debug.WriteLine(exc);
                        }

                        terminalCanvas1.SetBitmap(b);

                        if ((getEventRequest.Flags & EventFlags.NewTextEntry) != EventFlags.None)
                        {
                            textInputArea.InputText = string.Empty;
                        }

                        SetTerminalState(new DesiredCanvasEvent(getEventRequest.Flags));
                    }
                    else if (tr is TR_GetBigText getTextRequest)
                    {
                        bigTextDisplay1.LabelText = getTextRequest.LabelText;
                        bigTextDisplay1.ContentText = getTextRequest.InitialContent;
                        bigTextDisplay1.ContentReadOnly = getTextRequest.IsReadOnly;
                        bigTextDisplay1.ButtonStyle = getTextRequest.Buttons;

                        SetTerminalState(DesiredTextEvent.Value);
                    }
                    else if (tr is TR_ShowBusyForm busy)
                    {
                        busyDisplay1.BusyDoing = busy.BusyDoing;
                        busyDisplay1.ProgressAmount = busy.ProgressAmount;
                        busyDisplay1.CancelVisible = busy.OptionalCts.HasValue;
                        busyDisplay1.CancelEnabled = true;

                        formArguments.EventWriter.Send(TE_BusyDisplayed.Value);

                        SetTerminalState(new BusyState(busy.OptionalCts));
                    }
                    else if (tr is TR_ShowDialog showDialog)
                    {
                        TE_DialogResult dr = showDialog.CallAndCreateResult(this);
                        formArguments.EventWriter.Send(dr);

                        // leave terminal state alone
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false, "Unknown type of terminal request");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "item not a TerminalRequest");
            }
        }

        private void channelMonitor1_EofReceived(object sender, EofReceivedEventArgs e)
        {
            if (formArguments is not null)
            {
                formArguments.EventWriter.SendEof();
            }

            reallyClosing = true;
            Close();
            formLoaded = false;
        }

        private void TerminalForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            channelMonitor1.ClearChannelReader();
        }

        private void textInputArea_SubmitClicked(object sender, EventArgs e)
        {
            if (formArguments is not null && terminalState.DesiresCanvasEvent(EventFlags.TextEntry))
            {
                formArguments.EventWriter.Send(new TE_TextEntry(textInputArea.InputText));
                textInputArea.InputText = string.Empty;
                SetTerminalState(new IdleState(false));
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (formArguments is not null && terminalState.DesiresCanvasEvent(EventFlags.TimerTick))
            {
                formArguments.EventWriter.Send(TE_TimerTick.Value);
                SetTerminalState(new IdleState(terminalState.DesiresCanvasEvent(EventFlags.TextEntry)));
            }
        }

        private void TerminalForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (reallyClosing) return;

            if (formArguments is not null)
            {
                e.Cancel = true;

                if (terminalState is not IdleState)
                {
                    formArguments.EventWriter.Send(TE_UserCloseRequest.Value);
                    SetTerminalState(new IdleState(terminalState.DesiresCanvasEvent(EventFlags.TextEntry)));
                }
                else
                {
                    pendingCloseRequest = true;
                }
            }
            else
            {
                Close();
            }
        }

        private void terminalCanvas1_CanvasMouseClick(object sender, CanvasMouseEventArgs e)
        {
            if (formArguments is not null && terminalState.DesiresCanvasEvent(EventFlags.MouseClick))
            {
                formArguments.EventWriter.Send(new TE_MouseClick(e.Location.X, e.Location.Y));
                SetTerminalState(new IdleState(terminalState.DesiresCanvasEvent(EventFlags.TextEntry)));
            }
        }

        private void terminalCanvas1_CanvasKeyDown(object sender, CanvasKeyEventArgs e)
        {
            if (formArguments is not null && terminalState.DesiresCanvasEvent(EventFlags.KeyDown))
            {
                formArguments.EventWriter.Send(new TE_KeyDown(e.KeyData));
                SetTerminalState(new IdleState(terminalState.DesiresCanvasEvent(EventFlags.TextEntry)));
            }
        }

        private void bigTextDisplay1_ButtonClicked(object sender, BigTextButtonClickedEventArgs e)
        {
            if (formArguments is not null && terminalState is DesiredTextEvent)
            {
                formArguments.EventWriter.Send(new TE_BigTextEntry(e.DialogResult, bigTextDisplay1.ContentText));
                SetTerminalState(new IdleState(false));
            }
        }

        private void busyDisplay1_CancelClicked(object sender, EventArgs e)
        {
            if (terminalState is BusyState bs)
            {
                bs.Cancel();
                busyDisplay1.CancelEnabled = false;
            }
        }
    }

    public enum Pane
    {
        Canvas,
        CanvasWithTextInput,
        BigText,
        Busy
    }

    public abstract class TerminalState
    {
        public abstract bool DesiresCanvasEvent(EventFlags e);
    }

    public sealed class IdleState : TerminalState
    {
        private readonly bool showText;

        public IdleState(bool showText)
        {
            this.showText = showText;
        }

        public bool ShowText => showText;

        public override bool DesiresCanvasEvent(EventFlags e)
        {
            return false;
        }
    }

    public sealed class DesiredCanvasEvent : TerminalState
    {
        private readonly EventFlags desiredEvents;

        public DesiredCanvasEvent(EventFlags desiredEvents)
        {
            this.desiredEvents = desiredEvents;
        }

        public override bool DesiresCanvasEvent(EventFlags e)
        {
            return (e & desiredEvents) != EventFlags.None;
        }
    }

    public sealed class DesiredTextEvent : TerminalState
    {
        private static readonly DesiredTextEvent value = new DesiredTextEvent();

        private DesiredTextEvent() { }

        public static DesiredTextEvent Value => value;

        public override bool DesiresCanvasEvent(EventFlags e)
        {
            return false;
        }
    }

    public sealed class BusyState : TerminalState
    {
        private Option<CancellationTokenSource> cts;

        public BusyState(Option<CancellationTokenSource> cts)
        {
            this.cts = cts;
        }

        public Option<CancellationTokenSource> OptionalCts => cts;

        public void Cancel()
        {
            if (cts.HasValue) cts.Value.Cancel();
        }

        public override bool DesiresCanvasEvent(EventFlags e)
        {
            return false;
        }
    }
}
