using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace ModProject
{
    public class ModClass : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("BlockInstrument", typeof(BlockInstrument));
        }
    }

    public class BlockInstrument : Block
    {
        private readonly string[] octave = new string[] { "c3", "d3", "e3", "f3", "g3", "a3", "b3", "c4" };
        private bool tick = true;
        private string instrument;
        ICoreClientAPI capi;

        public override void OnLoaded(ICoreAPI api)
        {
            if (api.Side.IsClient()) capi = (ICoreClientAPI)api;
            instrument = Variant["instrument"];
            base.OnLoaded(api);
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            handHandling = EnumHandHandling.PreventDefault;
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (tick && byEntity.World.Side.IsServer())
            {
                int delay = Variant["instrument"] == "flute" ? 24 : 100;
                tick = false;
                Vec3d pos = byEntity.LocalPos.XYZ;
                float normalizedPitch = 1.0f - GameMathExt.Normalize(byEntity.Pos.Pitch, 1.578125f, 4.6875f);
                int note = (int)Math.Round((octave.Length - 1) * normalizedPitch);

                byEntity.World.PlaySoundAt(new AssetLocation("bunnyviking:sounds/instrument/" + instrument + "-" + octave[note]), pos.X, pos.Y, pos.Z, null, false);
                byEntity.World.RegisterCallback(dt => tick = true, delay);
            }
            return true;
        }
    }
    public class GameMathExt : GameMath
    {
        public static float Normalize(float value, float min, float max)
        {
            return (value - min) / (max - min);
        }
    }
}
