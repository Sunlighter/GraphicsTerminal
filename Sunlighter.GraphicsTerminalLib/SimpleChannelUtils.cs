namespace Sunlighter.SimpleChannelLib
{
    internal static class SimpleChannelUtils
    {
        public static void PostRegistration(this CancellationToken ct, Action<CancellationTokenRegistration> setRegistration, Action callback)
        {
            ThreadPool.QueueUserWorkItem
            (
                notUsed =>
                {
                    CancellationTokenRegistration ctr = ct.Register
                    (
                        () =>
                        {
                            ThreadPool.QueueUserWorkItem
                            (
                                notUsed2 =>
                                {
                                    callback();
                                }
                            );
                        }
                    );

                    setRegistration(ctr);
                }
            );
        }
    }
}
