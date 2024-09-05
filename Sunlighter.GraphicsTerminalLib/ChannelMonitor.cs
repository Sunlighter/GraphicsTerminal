using Sunlighter.SimpleChannelLib;
using Sunlighter.OptionLib;
using System.ComponentModel;

namespace Sunlighter.GraphicsTerminalLib
{
    public partial class ChannelMonitor : Component
    {
        public ChannelMonitor()
        {
            InitializeComponent();
            cts = Option<CancellationTokenSource>.None;
        }

        public ChannelMonitor(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
            cts = Option<CancellationTokenSource>.None;
        }

        public ISynchronizeInvoke? SyncRoot { get; set; }

        private Option<CancellationTokenSource> cts;

        public void SetChannelReader<T>(ISimpleChannelReceiver<T> reader)
        {
            if (cts.HasValue)
            {
                cts.Value.Cancel();
                cts.Value.Dispose();
            }

            CancellationTokenSource ctsi = new CancellationTokenSource();
            cts = Option<CancellationTokenSource>.Some(ctsi);
            CancellationToken cToken = ctsi.Token;

            ulong readerId = reader.ID;

            reader.ReceiveWithCallback
            (
                rr => ReceiveCompleted(rr, reader, cToken),
                cToken
            );
        }

        public void ClearChannelReader()
        {
            if (cts.HasValue)
            {
                cts.Value.Cancel();
            }
        }

        private void ReceiveCompleted<T>(ReceiveResult<T> result, ISimpleChannelReceiver<T> reader, CancellationToken cToken)
        {
            if (result is ReceivedItem<T> item)
            {
                reader.ReceiveWithCallback
                (
                    rr => ReceiveCompleted(rr, reader, cToken),
                    cToken
                );

                SyncRootInvoke
                (
                    () =>
                    {
                        ItemReceived?.Invoke(this, new ItemReceivedEventArgs(reader.ID, item.Item));
                    }
                );
            }
            else if (result is ReceivedEof<T>)
            {
                SyncRootInvoke
                (
                    () =>
                    {
                        EofReceived?.Invoke(this, new EofReceivedEventArgs(reader.ID));

                        if (cts.HasValue)
                        {
                            cts.Value.Dispose();
                            cts = Option<CancellationTokenSource>.None;
                        }
                    }
                );
            }
            else
            {
                if (cts.HasValue)
                {
                    cts.Value.Dispose();
                    cts = Option<CancellationTokenSource>.None;
                }
                // it's a cancellation; ignore it
            }
        }

        private void SyncRootInvoke(Action a)
        {
            if (SyncRoot is null)
            {
                a();
            }
            else
            {
                SyncRoot.Invoke(a, null);
            }
        }

        public event ItemReceivedEventHandler? ItemReceived;

        public event EofReceivedEventHandler? EofReceived;
    }

    public delegate void ItemReceivedEventHandler(object sender, ItemReceivedEventArgs e);

    public sealed class ItemReceivedEventArgs : EventArgs
    {
        private readonly ulong channelId;
        private readonly object? item;

        public ItemReceivedEventArgs(ulong channelId, object? item)
        {
            this.channelId = channelId;
            this.item = item;
        }

        public ulong ChannelID => channelId;
        public object? Item => item;
    }

    public delegate void EofReceivedEventHandler(object sender, EofReceivedEventArgs e);

    public sealed class EofReceivedEventArgs : EventArgs
    {
        private readonly ulong channelId;

        public EofReceivedEventArgs(ulong channelId)
        {
            this.channelId = channelId;
        }

        public ulong ChannelID => channelId;
    }
}
