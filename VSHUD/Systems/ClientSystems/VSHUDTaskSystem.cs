using Vintagestory.Client.NoObf;
using System.Collections.Concurrent;
using Vintagestory.API.Client;
using System;
using System.Collections;

namespace VSHUD
{
    class VSHUDTaskSystem : ClientSystem
    {
        public static ConcurrentQueue<Action> Actions = new ConcurrentQueue<Action>();
        public static ConcurrentQueue<Action> MainThreadActions = new ConcurrentQueue<Action>();

        public ClientMain game;

        public VSHUDTaskSystem(ClientMain game) : base(game) 
        {
            Actions = new ConcurrentQueue<Action>();
            MainThreadActions = new ConcurrentQueue<Action>();
            this.game = game; 
        }

        public override string Name => "VSHUD Tasks";

        public override EnumClientSystemType GetSystemType() => EnumClientSystemType.Misc;

        public override void OnSeperateThreadGameTick(float dt)
        {
            ProcessMainThreadActions();
            ProcessActions(Actions).MoveNext();
        }

        public void ProcessMainThreadActions()
        {
            game.EnqueueMainThreadTask(() => ProcessActions(MainThreadActions).MoveNext(), "");
        }

        public IEnumerator ProcessActions(ConcurrentQueue<Action> actions)
        {
            if (actions != null)
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    bool success = actions.TryDequeue(out Action action);
                    if (success) action?.Invoke();
                    yield return null;
                }
            }
        }

        public void Dispose(ICoreClientAPI capi) => Dispose(capi.World as ClientMain);

        public override void Dispose(ClientMain game)
        {
            base.Dispose(game);
            Actions = null;
            MainThreadActions = null;
        }
    }
}
