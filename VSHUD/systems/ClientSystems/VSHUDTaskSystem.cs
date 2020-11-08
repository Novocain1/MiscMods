using Vintagestory.Client.NoObf;
using System.Collections.Concurrent;
using Action = Vintagestory.API.Common.Action;
using Vintagestory.API.Client;

namespace VSHUD
{
    class VSHUDTaskSystem : ClientSystem
    {
        public static ConcurrentQueue<Action> Actions = new ConcurrentQueue<Action>();
        public static ConcurrentQueue<Action> MainThreadActions = new ConcurrentQueue<Action>();

        public ClientMain game;

        public VSHUDTaskSystem(ClientMain game) : base(game) { this.game = game; }

        public override string Name => "VSHUD Tasks";

        public override EnumClientSystemType GetSystemType() => EnumClientSystemType.Misc;

        public override void OnSeperateThreadGameTick(float dt)
        {
            ProcessActions(Actions);
            ProcessMainThreadActions();
        }

        public void ProcessMainThreadActions()
        {
            game.EnqueueMainThreadTask(() => ProcessActions(MainThreadActions), "");
        }

        public void ProcessActions(ConcurrentQueue<Action> actions)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                bool success = actions.TryDequeue(out Action action);
                if (success) action.Invoke();
            }
        }

        public void Dispose(ICoreClientAPI capi) => Dispose(capi.World as ClientMain);

        public override void Dispose(ClientMain game)
        {
            base.Dispose(game);
            Actions = new ConcurrentQueue<Action>();
        }
    }
}
