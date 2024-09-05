﻿using Sunlighter.SimpleChannelLib;
using Sunlighter.OptionLib;
using System.Xml.Schema;
using System.Reflection;

namespace Sunlighter.GraphicsTerminalLib
{
    public sealed class GraphicsTerminal : IAsyncDisposable
    {
        private readonly string title;
        private readonly ISimpleChannelSender<TerminalRequest> requestWriter;
        private readonly ISimpleChannelReceiver<TerminalEvent> eventReader;
        private readonly Thread windowThread;

        public GraphicsTerminal(string title)
        {
            this.title = title;

            SenderReceiverPair<TerminalRequest> requestChannel = SimpleChannel.GetSenderReceiverPair<TerminalRequest>();

            SenderReceiverPair<TerminalEvent> eventChannel = SimpleChannel.GetSenderReceiverPair<TerminalEvent>();

            requestWriter = requestChannel.Sender;
            eventReader = eventChannel.Receiver;

            FormArguments fa = new FormArguments(title, requestChannel.Receiver, eventChannel.Sender);

            windowThread = new Thread(new ParameterizedThreadStart(RunWindowThread));
            windowThread.SetApartmentState(ApartmentState.STA);
            windowThread.Start(fa);
        }

        private static void RunWindowThread(object? args)
        {
            if (args is FormArguments fa)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetHighDpiMode(HighDpiMode.DpiUnawareGdiScaled);

                using TerminalForm tf = new TerminalForm();
                tf.Text = fa.Title;
                tf.FormArguments = fa;

                Application.Run(tf);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Incorrect argument for WindowThread");
                throw new ArgumentException("FormArgument expected");
            }
        }

        public async Task<TerminalEvent> GetEventAsync
        (
            Size desiredSize,
            Action<Graphics> draw,
            EventFlags flags
        )
        {
            requestWriter.Send(new TR_GetEvent(flags, desiredSize, draw));
            ReceiveResult<TerminalEvent> te = await eventReader.ReceiveAsync(CancellationToken.None);
            return ((ReceivedItem<TerminalEvent>)te).Item;
        }

        public async Task<TerminalEvent> GetBigTextAsync
        (
            string labelText,
            bool isReadOnly,
            string content,
            MessageBoxButtons buttons
        )
        {
            requestWriter.Send(new TR_GetBigText(labelText, isReadOnly, content, buttons));
            ReceiveResult<TerminalEvent> te = await eventReader.ReceiveAsync(CancellationToken.None);
            return ((ReceivedItem<TerminalEvent>)te).Item;
        }

        public async Task ShowBusyForm
        (
            string busyDoing,
            Option<double> progressAmount,
            Option<CancellationTokenSource> cts
        )
        {
            requestWriter.Send(new TR_ShowBusyForm(busyDoing, progressAmount, cts));
            ReceiveResult<TerminalEvent> te = await eventReader.ReceiveAsync(CancellationToken.None);
            System.Diagnostics.Debug.Assert(te is ReceivedItem<TerminalEvent> ri && ri.Item is TE_BusyDisplayed);
        }

        public async Task<T> ShowDialog<T>
        (
            Func<IWin32Window, T> dialogFunc
        )
        {
            requestWriter.Send(new TR_ShowDialog<T>(dialogFunc));
            ReceiveResult<TerminalEvent> te = await eventReader.ReceiveAsync(CancellationToken.None);
            if (te is ReceivedItem<TerminalEvent> ri && ri.Item is TE_DialogResult<T> dialogResultEvent)
            {
                return dialogResultEvent.Value;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "Unexpected result from ShowDialog");
                throw new InvalidOperationException("Internal error: Unexpected result from ShowDialog");
            }
        }

        public async ValueTask DisposeAsync()
        {
            requestWriter.SendEof();
            while(true)
            {
                ReceiveResult<TerminalEvent> toss = await eventReader.ReceiveAsync(CancellationToken.None);
                if (toss is ReceivedEof<TerminalEvent>)
                {
                    break;
                }

                System.Diagnostics.Debug.Assert(false, "There were events waiting in the queue when DisposeAsync was called");
            }

            windowThread.Join(); // beginning to wish for a JoinAsync...
        }
    }

    [Flags]
    public enum EventFlags
    {
        None = 0,
        TimerTick = 1,
        MouseClick = 2,
        TextEntry = 4,
        NewTextEntry = 8,
        KeyDown = 16,
    }

    public abstract class TerminalEvent
    {

    }

    public sealed class TE_TimerTick : TerminalEvent
    {
        private static readonly TE_TimerTick value = new TE_TimerTick();

        private TE_TimerTick() { }

        public static TE_TimerTick Value => value;
    }

    public sealed class TE_UserCloseRequest : TerminalEvent
    {
        private static readonly TE_UserCloseRequest value = new TE_UserCloseRequest();

        private TE_UserCloseRequest() { }

        public static TE_UserCloseRequest Value => value;
    }

    public sealed class TE_MouseClick : TerminalEvent
    {
        private readonly double x;
        private readonly double y;

        public TE_MouseClick(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public double X => x;
        public double Y => y;
    }

    public sealed class TE_KeyDown : TerminalEvent
    {
        private readonly Keys keyData;

        public TE_KeyDown(Keys keyData)
        {
            this.keyData = keyData;
        }

        public Keys KeyData => keyData;
    }

    public sealed class TE_TextEntry : TerminalEvent
    {
        private readonly string text;

        public TE_TextEntry(string text)
        {
            this.text = text;
        }

        public string Text => text;
    }

    public sealed class TE_BigTextEntry : TerminalEvent
    {
        private readonly DialogResult result;
        private readonly string text;

        public TE_BigTextEntry(DialogResult result, string text)
        {
            this.result = result;
            this.text = text;
        }

        public DialogResult DialogResult => result;

        public string Text => text;
    }

    public sealed class TE_BusyDisplayed : TerminalEvent
    {
        private static readonly TE_BusyDisplayed value = new TE_BusyDisplayed();

        private TE_BusyDisplayed() { }

        public static TE_BusyDisplayed Value => value;
    }

    public abstract class TE_DialogResult : TerminalEvent
    {

    }

    public sealed class TE_DialogResult<T> : TE_DialogResult
    {
        private readonly T value;

        public TE_DialogResult(T value)
        {
            this.value = value;
        }

        public T Value => value;
    }

    internal abstract class TerminalRequest
    {

    }

    internal sealed class TR_GetEvent : TerminalRequest
    {
        private readonly EventFlags flags;
        private readonly Size desiredSize;
        private readonly Action<Graphics> draw;

        public TR_GetEvent
        (
            EventFlags flags,
            Size desiredSize,
            Action<Graphics> draw
        )
        {
            this.flags = flags;
            this.desiredSize = desiredSize;
            this.draw = draw;
        }

        public EventFlags Flags => flags;

        public Size DesiredSize => desiredSize;

        public Action<Graphics> DrawAction => draw;

        public void Draw(Graphics g) => draw(g);
    }

    internal sealed class TR_GetBigText : TerminalRequest
    {
        private readonly string labelText;
        private readonly bool isReadOnly;
        private readonly string initialContent;
        private readonly MessageBoxButtons buttons;

        public TR_GetBigText
        (
            string labelText,
            bool isReadOnly,
            string initialContent,
            MessageBoxButtons buttons
        )
        {
            this.labelText = labelText;
            this.isReadOnly = isReadOnly;
            this.initialContent = initialContent;
            this.buttons = buttons;
        }

        public string LabelText => labelText;
        public bool IsReadOnly => isReadOnly;
        public string InitialContent => initialContent;
        public MessageBoxButtons Buttons => buttons;
    }

    internal sealed class TR_ShowBusyForm : TerminalRequest
    {
        private readonly string busyDoing;
        private readonly Option<double> progressAmount;
        private readonly Option<CancellationTokenSource> cts;

        public TR_ShowBusyForm(string busyDoing, Option<double> progressAmount, Option<CancellationTokenSource> cts)
        {
            this.busyDoing = busyDoing;
            this.progressAmount = progressAmount;
            this.cts = cts;
        }

        public string BusyDoing => busyDoing;

        public Option<double> ProgressAmount => progressAmount;

        public Option<CancellationTokenSource> OptionalCts => cts;
    }

    internal abstract class TR_ShowDialog : TerminalRequest
    {
        public abstract TE_DialogResult CallAndCreateResult(IWin32Window parent);
    }

    internal sealed class TR_ShowDialog<T> : TR_ShowDialog
    {
        private readonly Func<IWin32Window, T> dialogFunc;

        public TR_ShowDialog
        (
            Func<IWin32Window, T> dialogFunc
        )
        {
            this.dialogFunc = dialogFunc;
        }

        public override TE_DialogResult CallAndCreateResult(IWin32Window parent)
        {
            T result = dialogFunc(parent);
            return new TE_DialogResult<T>(result);
        }
    }

    internal sealed class FormArguments
    {
        private readonly string title;
        private readonly ISimpleChannelReceiver<TerminalRequest> requestReader;
        private readonly ISimpleChannelSender<TerminalEvent> eventWriter;

        public FormArguments
        (
            string title,
            ISimpleChannelReceiver<TerminalRequest> requestReader,
            ISimpleChannelSender<TerminalEvent> eventWriter
        )
        {
            this.title = title;
            this.requestReader = requestReader;
            this.eventWriter = eventWriter;
        }

        public string Title => title;
        public ISimpleChannelReceiver<TerminalRequest> RequestReader => requestReader;
        public ISimpleChannelSender<TerminalEvent> EventWriter => eventWriter;
    }
}
