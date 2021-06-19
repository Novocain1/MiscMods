using System.Threading;
using Vintagestory.API.Common;
using System.Collections.Generic;

namespace VSHUD
{
    public class CheckAppSideAnywhere
    {
        public static Dictionary<int, EnumAppSide> FastSideLookup = new Dictionary<int, EnumAppSide>();

        public static void CacheCurrentThread()
        {
            if (Thread.CurrentThread.Name == "SingleplayerServer")
            {
                FastSideLookup[Thread.CurrentThread.ManagedThreadId] = EnumAppSide.Server;
            }
            else
            {
                FastSideLookup[Thread.CurrentThread.ManagedThreadId] = EnumAppSide.Client;
            }
        }

        private static EnumAppSide GetAppSide()
        {
            if (!FastSideLookup.TryGetValue(Thread.CurrentThread.ManagedThreadId, out EnumAppSide side))
            {
                CacheCurrentThread();
                side = FastSideLookup[Thread.CurrentThread.ManagedThreadId];
            }

            return side;
        }

        public static EnumAppSide Side { get => GetAppSide(); }

        public static void Dispose()
        {
            FastSideLookup.Clear();
        }
    }
}
