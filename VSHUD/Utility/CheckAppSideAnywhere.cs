using System.Threading;
using Vintagestory.API.Common;

namespace VSHUD
{
    public class CheckAppSideAnywhere
    {
        public static EnumAppSide Side { get => Thread.CurrentThread.Name == "SingleplayerServer" ? EnumAppSide.Server : EnumAppSide.Client; }
    }
}
