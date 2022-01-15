using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VSHUD
{
    static class ThreadStuff
    {
        static Type threadType;

        static ThreadStuff()
        {
            var ts = AccessTools.GetTypesFromAssembly(Assembly.GetAssembly(typeof(ClientMain)));
            threadType = ts.Where((t, b) => t.Name == "ClientThread").Single();
        }

        public static Thread InjectClientThread(this ICoreClientAPI capi, string name, bool inGameLoop, params ClientSystem[] systems) => capi.World.InjectClientThread(name, inGameLoop, systems);

        public static Thread InjectClientThread(this IClientWorldAccessor world, string name, bool inGameLoop, params ClientSystem[] systems)
        {
            object instance;
            Thread thread;

            instance = threadType.CreateInstance();
            instance.SetField("game", world as ClientMain);
            instance.SetField("threadName", name);
            instance.SetField("clientsystems", systems);
            instance.SetField("lastFramePassedTime", new Stopwatch());
            instance.SetField("totalPassedTime", new Stopwatch());
            instance.SetField("paused", false);

            List<Thread> clientThreads = (world as ClientMain).GetField<List<Thread>>("clientThreads");
            
            if (inGameLoop)
            {
                Stack<ClientSystem> vanillaSystems = new Stack<ClientSystem>((world as ClientMain).GetField<ClientSystem[]>("clientSystems"));

                foreach (var system in systems)
                {
                    vanillaSystems.Push(system);
                }

                (world as ClientMain).SetField("clientSystems", vanillaSystems.ToArray());
            }

            thread = new Thread(() => instance.CallMethod("Process"))
            {
                IsBackground = true,
                Name = name
            };
            
            thread.Start();
            
            clientThreads.Add(thread);

            return thread;
        }
    }
}
