using Sunlighter.OptionLib;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
            terminalState = new IdleState(Pane.Busy);
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
            SetPane(Pane.Busy);
        }

        bool settingPane = false;

        public void SetPane(Pane p)
        {
            settingPane = true;

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

            settingPane = false;
        }

        private void SetTerminalState(TerminalState e)
        {
            terminalState = e;

            if (e is IdleState ids)
            {
                SetPane(ids.Pane);
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
            else if (e is BusyState)
            {
                SetPane(Pane.Busy);
            }
        }

        private void channelMonitor1_ItemReceived(object sender, ItemReceivedEventArgs e)
        {
            if (e.Item is TerminalRequest tr)
            {
                System.Diagnostics.Debug.Assert(formArguments is not null);

                if (tr is TR_GetEvent getEventRequest)
                {
                    SetTerminalState(new DesiredCanvasEvent(getEventRequest.Flags));

                    Bitmap b = getEventRequest.BitmapOption.CreateBitmap(terminalCanvas1.RemoveBitmap(), terminalCanvas1.ClientSize);

                    terminalCanvas1.ResizeRedraw2 = (getEventRequest.Flags & EventFlags.SizeChanged) == 0;
                    terminalCanvas1.SetBitmap(b);

                    if ((getEventRequest.Flags & EventFlags.NewTextEntry) != EventFlags.None)
                    {
                        textInputArea.InputText = string.Empty;
                    }
                }
                else if (tr is TR_GetBigText getTextRequest)
                {
                    SetTerminalState(new DesiredTextEvent(getTextRequest.ContentReturn));

                    bigTextDisplay1.LabelText = getTextRequest.LabelText;
                    bigTextDisplay1.ContentText = getTextRequest.InitialContent;
                    bigTextDisplay1.ContentReadOnly = getTextRequest.IsReadOnly;
                    bigTextDisplay1.ButtonStyle = getTextRequest.Buttons;
                }
                else if (tr is TR_ShowBusyForm busy)
                {
                    SetTerminalState(new BusyState(busy.OptionalCts));

                    busyDisplay1.BusyDoing = busy.BusyDoing;
                    busyDisplay1.ProgressAmount = busy.ProgressAmount;
                    busyDisplay1.CancelVisible = busy.OptionalCts.HasValue;
                    busyDisplay1.CancelEnabled = true;

                    formArguments.EventWriter.Send(TE_BusyDisplayed.Value);
                }
                else if (tr is TR_ShowDialog showDialog)
                {
                    TE_DialogResult dr = showDialog.CallAndCreateResult(this);
                    formArguments.EventWriter.Send(dr);

                    System.Diagnostics.Debug.Assert(terminalState is IdleState);
                    // leave terminal state alone
                }
                else if (tr is TR_CheckPendingClose)
                {
                    if (pendingCloseRequest)
                    {
                        formArguments.EventWriter.Send(TE_UserCloseRequest.Value);
                        pendingCloseRequest = false;
                    }
                    else
                    {
                        formArguments.EventWriter.Send(TE_Nothing.Value);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, "Unknown type of terminal request");
                }

                if (pendingCloseRequest && terminalState is not IdleState)
                {
                    if (terminalState is BusyState bs)
                    {
                        if (bs.OptionalCts.HasValue)
                        {
                            bs.Cancel();
                            busyDisplay1.CancelEnabled = false;
                            pendingCloseRequest = false;
                        }
                    }
                    else
                    {
                        if (terminalState is DesiredTextEvent dte)
                        {
                            dte.SetContentReturn(bigTextDisplay1.ContentText);
                        }

                        formArguments.EventWriter.Send(TE_UserCloseRequest.Value);
                        SetTerminalState(terminalState.GetIdleState());
                        pendingCloseRequest = false;
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
                SetTerminalState(terminalState.GetIdleState());
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (formArguments is not null && terminalState.DesiresCanvasEvent(EventFlags.TimerTick))
            {
                formArguments.EventWriter.Send(TE_TimerTick.Value);
                SetTerminalState(terminalState.GetIdleState());
            }
        }

        private void TerminalForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (reallyClosing) return;

            if (formArguments is not null)
            {
                e.Cancel = true;

                if (terminalState is BusyState bs)
                {
                    if (bs.OptionalCts.HasValue)
                    {
                        bs.Cancel();
                        busyDisplay1.CancelEnabled = false;
                    }
                    else
                    {
                        pendingCloseRequest = true;
                    }
                }
                else if (terminalState is not IdleState)
                {
                    if (terminalState is DesiredTextEvent dte)
                    {
                        dte.SetContentReturn(bigTextDisplay1.ContentText);
                    }

                    formArguments.EventWriter.Send(TE_UserCloseRequest.Value);
                    SetTerminalState(terminalState.GetIdleState());
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
                SetTerminalState(terminalState.GetIdleState());
            }
        }

        private void terminalCanvas1_CanvasKeyDown(object sender, CanvasKeyEventArgs e)
        {
            if (formArguments is not null && terminalState.DesiresCanvasEvent(EventFlags.KeyDown))
            {
                formArguments.EventWriter.Send(new TE_KeyDown(e.KeyData));
                SetTerminalState(terminalState.GetIdleState());
            }
        }

        private void bigTextDisplay1_ButtonClicked(object sender, BigTextButtonClickedEventArgs e)
        {
            if (formArguments is not null && terminalState is DesiredTextEvent dte)
            {
                dte.SetContentReturn(bigTextDisplay1.ContentText);
                formArguments.EventWriter.Send(new TE_BigTextEntry(e.DialogResult, bigTextDisplay1.ContentText));
                SetTerminalState(terminalState.GetIdleState());
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

        private void terminalCanvas1_ClientSizeChanged(object sender, EventArgs e)
        {
            if (settingPane) return;

            if (formArguments is not null && terminalState.DesiresCanvasEvent(EventFlags.SizeChanged))
            {
                formArguments.EventWriter.Send(new TE_SizeChanged(terminalCanvas1.ClientSize));
                SetTerminalState(terminalState.GetIdleState());
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

        public abstract TerminalState GetIdleState();
    }

    public sealed class IdleState : TerminalState
    {
        private readonly Pane pane;

        public IdleState(Pane pane)
        {
            this.pane = pane;
        }

        public Pane Pane => pane;

        public override bool DesiresCanvasEvent(EventFlags e)
        {
            return false;
        }

        public override TerminalState GetIdleState()
        {
            return this;
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

        public override TerminalState GetIdleState()
        {
            return new IdleState(DesiresCanvasEvent(EventFlags.TextEntry) ? Pane.CanvasWithTextInput : Pane.Canvas);
        }
    }

    public sealed class DesiredTextEvent : TerminalState
    {
        private readonly Option<StrongBox<string>> contentReturn;

        public DesiredTextEvent(Option<StrongBox<string>> contentReturn)
        {
            this.contentReturn = contentReturn;
        }

        public void SetContentReturn(string content)
        {
            if (contentReturn.HasValue)
            {
                contentReturn.Value.Value = content;
            }
        }

        public override bool DesiresCanvasEvent(EventFlags e)
        {
            return false;
        }

        public override TerminalState GetIdleState()
        {
            return new IdleState(Pane.BigText);
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

        public override TerminalState GetIdleState()
        {
            return new IdleState(Pane.Busy);
        }
    }
}
