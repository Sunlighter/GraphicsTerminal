using Sunlighter.OptionLib;
using System.Collections.Immutable;

namespace Sunlighter.SimpleChannelLib
{
    public interface IWithID
    {
        ulong ID { get; }
    }

    public interface ISimpleChannelSender<T> : IWithID
    {
        void Send(T value);
        void SendEof();
    }

    public interface ISimpleChannelReceiver<T> : IWithID
    {
        void ReceiveWithCallback(Action<ReceiveResult<T>> callback, CancellationToken cToken);
    }

    public abstract class ReceiveResult<T>
    {

    }

    public sealed class ReceivedItem<T> : ReceiveResult<T>
    {
        private readonly T item;

        public ReceivedItem(T item)
        {
            this.item = item;
        }

        public T Item => item;
    }

    public sealed class ReceivedEof<T> : ReceiveResult<T>
    {
        private static readonly ReceivedEof<T> value = new ReceivedEof<T>();

        private ReceivedEof() {  }

        public static ReceivedEof<T> Value => value;
    }

    public sealed class ReceiveCancelled<T> : ReceiveResult<T>
    {
        private static readonly ReceiveCancelled<T> value = new ReceiveCancelled<T>();

        private ReceiveCancelled() { }

        public static ReceiveCancelled<T> Value => value;
    }

    public static class SimpleChannel
    {
        public static SenderReceiverPair<T> GetSenderReceiverPair<T>()
        {
            ChannelImpl<T> impl = new ChannelImpl<T>();

            return new SenderReceiverPair<T>(impl.Sender, impl.Receiver);
        }

        public static Task<ReceiveResult<T>> ReceiveAsync<T>(this ISimpleChannelReceiver<T> receiver, CancellationToken cToken)
        {
            TaskCompletionSource<ReceiveResult<T>> tcs = new TaskCompletionSource<ReceiveResult<T>>();
            receiver.ReceiveWithCallback
            (
                tcs.SetResult,
                cToken
            );
            return tcs.Task;
        }
    }

    internal class ClassWithId
    {
        private static readonly object staticSyncRoot = new object();
        private static ulong nextId = 0uL;

        protected static ulong GetId()
        {
            lock (staticSyncRoot)
            {
                ulong id = nextId;
                ++nextId;
                return id;
            }
        }
    }

    internal sealed class ChannelImpl<T> : ClassWithId
    {
        private readonly ulong id;
        private readonly object syncRoot;
        private ImmutableList<T> queue;
        private bool eofSent;
        private ulong nextWaitingReceiverId;
        private ImmutableSortedDictionary<ulong, ReceiverCallbackInfo> activeWaitingReceivers;
        private ImmutableList<ReceiverCallbackInfo> waitingReceivers;
        private SenderImpl sender;
        private ReceiverImpl receiver;

        private sealed class ReceiverCallbackInfo
        {
            public required Action<ReceiveResult<T>> callback;
            public required ulong id;
            public required Option<CancellationTokenRegistration> ctr;
            //public required bool isCancelled;
        }

        public ChannelImpl()
        {
            id = GetId();
            syncRoot = new object();
            queue = ImmutableList<T>.Empty;
            eofSent = false;
            nextWaitingReceiverId = 0uL;
            activeWaitingReceivers = ImmutableSortedDictionary<ulong, ReceiverCallbackInfo>.Empty;
            waitingReceivers = [];
            sender = new SenderImpl(this);
            receiver = new ReceiverImpl(this);
        }

        public ISimpleChannelSender<T> Sender => sender;

        public ISimpleChannelReceiver<T> Receiver => receiver;

        private void AddItem(T item)
        {
            queue = queue.Insert(0, item);
        }

        private void TryRemoveItem
        (
            Action<T> withItem,
            Action noItemEof,
            Action noItemWait
        )
        {
            //Console.WriteLine($"{this.id} TryRemoveItem");

            if (queue.IsEmpty)
            {
                if (eofSent)
                {
                    //Console.WriteLine(" -> NoItemEof");
                    noItemEof();
                }
                else
                {
                    //Console.WriteLine(" -> NoItemWait");
                    noItemWait();
                }
            }
            else
            {
                int index = queue.Count - 1;
                T item = queue[index];
                queue = queue.RemoveAt(index);
                //Console.WriteLine(" -> Item");
                withItem(item);
            }
        }

        private void AddWaitingReceiver(Action<ReceiveResult<T>> waitingReceiver, CancellationToken cToken)
        {
            ulong id = nextWaitingReceiverId;
            ++nextWaitingReceiverId;

            //Console.WriteLine($"{this.id} AddWaitingReceiver {id}");

            ReceiverCallbackInfo wri = new ReceiverCallbackInfo()
            {
                callback = waitingReceiver,
                id = id,
                ctr = Option<CancellationTokenRegistration>.None,
                //isCancelled = false
            };

            SimpleChannelUtils.PostRegistration
            (
                cToken,
                ctr =>
                {
                    lock (syncRoot)
                    {
                        if (!activeWaitingReceivers.ContainsKey(id))
                        {
                            ctr.Dispose();
                        }
                        else
                        {
                            wri.ctr = Option<CancellationTokenRegistration>.Some(ctr);
                        }
                    }
                },
                () =>
                {
                    lock (syncRoot)
                    {
                        CancelWaitingReceiver(id);
                    }
                }
            );

            activeWaitingReceivers = activeWaitingReceivers.Add(id, wri);
            waitingReceivers = waitingReceivers.Insert(0, wri);
        }

        private void CancelWaitingReceiver(ulong id)
        {
            //Console.WriteLine($"{this.id} CancelWaitingReceiver {id}");

            if (activeWaitingReceivers.TryGetValue(id, out ReceiverCallbackInfo? value))
            {
                activeWaitingReceivers = activeWaitingReceivers.Remove(id);
                if (value.ctr.HasValue)
                {
                    value.ctr.Value.Dispose();
                    value.ctr = Option<CancellationTokenRegistration>.None;
                }
                PostCancellationToReceiver(value.callback);
            }
            else
            {
                // ignore
            }
        }

        private void TryRemoveWaitingReceiver
        (
            Action<Action<ReceiveResult<T>>> withWaitingReceiver,
            Action noWaitingReceiver
        )
        {
            //Console.WriteLine($"{this.id} TryRemoveWaitingReceiver");

        theLoop:

            if (waitingReceivers.IsEmpty)
            {
                //Console.WriteLine($"{this.id} NoWaitingReceiver");
                noWaitingReceiver();
            }
            else
            {

                int index = waitingReceivers.Count - 1;
                ReceiverCallbackInfo wri = waitingReceivers[index];
                waitingReceivers = waitingReceivers.RemoveAt(index);

                if (!activeWaitingReceivers.ContainsKey(wri.id))
                {
                    //Console.WriteLine($"{this.id} Looping...");
                    goto theLoop;
                }
                else
                {
                    activeWaitingReceivers.Remove(wri.id);
                    if (wri.ctr.HasValue)
                    {
                        wri.ctr.Value.Dispose();
                        wri.ctr = Option<CancellationTokenRegistration>.None;
                    }

                    //Console.WriteLine($"{this.id} WithWaitingReceiver");
                    withWaitingReceiver(wri.callback);
                }
            }
        }

        private void PostItemToReceiver(T item, Action<ReceiveResult<T>> receiver)
        {
            //Console.WriteLine($"{this.id} PostItemToReceiver");
            ThreadPool.QueueUserWorkItem
            (
                _ =>
                {
                    receiver(new ReceivedItem<T>(item));
                }
            );
        }

        private void PostEofToReceiver(Action<ReceiveResult<T>> receiver)
        {
            //Console.WriteLine($"{this.id} PostEofToReceiver");
            ThreadPool.QueueUserWorkItem
            (
                _ =>
                {
                    receiver(ReceivedEof<T>.Value);
                }
            );
        }

        private void PostCancellationToReceiver(Action<ReceiveResult<T>> receiver)
        {
            //Console.WriteLine($"{this.id} PostCancellationToReceiver");
            ThreadPool.QueueUserWorkItem
            (
                _ =>
                {
                    receiver(ReceiveCancelled<T>.Value);
                }
            );
        }

        private sealed class SenderImpl : ISimpleChannelSender<T>
        {
            private ChannelImpl<T> parent;

            public SenderImpl(ChannelImpl<T> parent)
            {
                this.parent = parent;
            }

            public ulong ID => parent.id;

            public void Send(T value)
            {
                //Console.WriteLine($"{parent.id} Send");
                lock (parent.syncRoot)
                {
                    parent.TryRemoveWaitingReceiver
                    (
                        receiver =>
                        {
                            parent.PostItemToReceiver(value, receiver);
                        },
                        () =>
                        {
                            parent.AddItem(value);
                        }
                    );
                }
            }

            public void SendEof()
            {
                //Console.WriteLine($"{parent.id} SendEof");
                lock (parent.syncRoot)
                {
                    parent.eofSent = true;
                    if (parent.queue.IsEmpty)
                    {
                        bool more = true;
                        while (more)
                        {
                            parent.TryRemoveWaitingReceiver
                            (
                                parent.PostEofToReceiver,
                                () =>
                                {
                                    more = false;
                                }
                            );
                        }
                    }
                }
            }
        }

        private sealed class ReceiverImpl : ISimpleChannelReceiver<T>
        {
            private ChannelImpl<T> parent;

            public ReceiverImpl(ChannelImpl<T> parent)
            {
                this.parent = parent;
            }

            public ulong ID => parent.id;

            public void ReceiveWithCallback(Action<ReceiveResult<T>> callback, CancellationToken cToken)
            {
                //Console.WriteLine($"{parent.id} ReceiveWithCallback");
                lock (parent.syncRoot)
                {
                    parent.TryRemoveItem
                    (
                        item =>
                        {
                            parent.PostItemToReceiver(item, callback);
                        },
                        () =>
                        {
                            parent.PostEofToReceiver(callback);
                        },
                        () =>
                        {
                            parent.AddWaitingReceiver(callback, cToken);
                        }
                    );
                }
            }
        }
    }

    public sealed class SenderReceiverPair<T>
    {
        private readonly ISimpleChannelSender<T> sender;
        private readonly ISimpleChannelReceiver<T> receiver;

        public SenderReceiverPair
        (
            ISimpleChannelSender<T> sender,
            ISimpleChannelReceiver<T> receiver
        )
        {
            this.sender = sender;
            this.receiver = receiver;
        }

        public ISimpleChannelSender<T> Sender => sender;

        public ISimpleChannelReceiver<T> Receiver => receiver;
    }
}
