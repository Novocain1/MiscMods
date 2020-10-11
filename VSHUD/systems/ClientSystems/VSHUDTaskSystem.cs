using Vintagestory.Client.NoObf;
using System.Collections.Concurrent;
using Action = Vintagestory.API.Common.Action;
using Vintagestory.API.Client;

namespace VSHUD
{
    class VSHUDTaskSystem : ClientSystem
    {
        public static ConcurrentQueue<Action> Actions = new ConcurrentQueue<Action>();

        public VSHUDTaskSystem(ClientMain game) : base(game) { }

        public override string Name => "VSHUD Tasks";

        public override EnumClientSystemType GetSystemType() => EnumClientSystemType.Misc;

        public override void OnSeperateThreadGameTick(float dt) => ProcessActions();

        public void ProcessActions()
        {
            for (int i = 0; i < Actions.Count; i++)
            {
                bool success = Actions.TryDequeue(out Action action);
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
