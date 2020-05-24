using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace BlockAnimTest
{
    public class ThreadCreator : ModSystem
    {
        public override void StartServerSide(ICoreServerAPI api)
        {
            MyServerSystem system = new MyServerSystem(api.World as ServerMain);
            Thread t = (api.World as ServerMain)?.CallMethod("CreateThread", "MyThread", new ServerSystem[] { system }) as Thread;
            var threads = (api.World as ServerMain)?.Serverthreads;

            Thread[] newArr = new Thread[threads.Length + 1];
            for (int i = 0; i < threads.Length; i++)
            {
                newArr[i] = threads[i];
            }
            newArr[threads.Length] = t;
            (api.World as ServerMain).Serverthreads = newArr;
        }
    }

    class MyServerSystem : ServerSystem
    {
        ICoreServerAPI sapi;
        public MyServerSystem(ServerMain server) : base(server)
        {
            sapi = (server as IServerWorldAccessor)?.Api as ICoreServerAPI;
        }

        public override void OnSeperateThreadTick(float dt)
        {
            
        }
    }
}
