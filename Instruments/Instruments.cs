using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using csogg;

namespace ModProject
{
    public class ModClass : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("BlockInstrument", typeof(BlockInstrument));
        }
    }


    public enum EnumPitch
    {
        C1, CS1, D1, DS1, E1, F1, FS1, G1, GS1, A1, AS1, B1,
        C2, CS2, D2, DS2, E2, F2, FS2, G2, GS2, A2, AS2, B2,
        C3, CS3, D3, DS3, E3, F3, FS3, G3, GS3, A3, AS3, B3,
        C4, CS4, D4, DS4, E4, F4, FS4, G4, GS4, A4, AS4, B4,
        C5, CS5, D5, DS5, E5, F5, FS5, G5, GS5, A5, AS5, B5,
    }

    public enum EnumPitchValue
    {
        C1 = -24, CS1 = -23, D1 = -22, DS1 = -21, E1 = -20, F1 = -19, FS1 = -18, G1 = -17, GS1 = -16, A1 = -15, AS1 = -14, B1 = -13,
        C2 = -12, CS2 = -11, D2 = -10, DS2 = -9, E2 = -8, F2 = -7, FS2 = -6, G2 = -5, GS2 = -4, A2 = -3, AS2 = -2, B2 = -1,
        C3 = 0, CS3 = 1, D3 = 2, DS3 = 3, E3 = 4, F3 = 5, FS3 = 6, G3 = 7, GS3 = 8, A3 = 9, AS3 = 10, B3 = 11,
        C4 = 12, CS4 = 13, D4 = 14, DS4 = 15, E4 = 16, F4 = 17, FS4 = 18, G4 = 19, GS4 = 20, A4 = 21, AS4 = 22, B4 = 23,
        C5 = 24, CS5 = 25, D5 = 26, DS5 = 27, E5 = 28, F5 = 29, FS5 = 30, G5 = 31, GS5 = 32, A5 = 33, AS5 = 34, B5 = 35,
    }

    public class BlockInstrument : Block
    {
        static int[] noteValues;
        private string instrument;
        bool sustain;
        int delay;

        static BlockInstrument()
        {
            var eValues = Enum.GetValues(typeof(EnumPitchValue));
            noteValues = new int[eValues.Length];
            int i = 0;
            foreach (EnumPitchValue val in eValues)
            {
                noteValues[i] = (int)val;
                i++;
            }
            Array.Sort(noteValues, delegate (int a, int b) { return a.CompareTo(b); });
        }

        public float GetNotePitch(EnumPitch pitch)
        {
            return (float)Math.Pow(2.0, noteValues[(int)pitch] * 1.0 / 12.0);
        }

        public void PlayNote(EntityAgent byEntity, EnumPitch pitch)
        {
            api.World.PlaySoundAt(new AssetLocation(Code.Domain + ":sounds/instrument/" + instrument), byEntity, null, GetNotePitch(pitch));
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            instrument = Attributes["instrument"].AsString();
            sustain = Attributes["sustain"].AsBool(false);
            delay = Attributes["delay"].AsInt();
        }


        public void SetSustainment(EntityAgent byEntity, bool value)
        {
            byEntity.WatchedAttributes.SetBool("sustainment", value);
        }

        public bool GetSustainment(EntityAgent byEntity)
        {
            return byEntity.WatchedAttributes.GetBool("sustainment");
        }

        public void SetNote(EntityAgent byEntity, EnumPitch pitch)
        {
            byEntity.WatchedAttributes.SetInt("noteplayed", (int)pitch);
        }

        public EnumPitch GetNote(EntityAgent byEntity)
        {
            return (EnumPitch)byEntity.WatchedAttributes.GetInt("noteplayed");
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (byEntity.Controls.Sneak)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
                return;
            }
            handHandling = EnumHandHandling.PreventDefault;

            if (byEntity.World.Side.IsServer() && firstEvent)
            {
                SetSustainment(byEntity, false);
                float normalizedPitch = 1.0f - GameMathExt.Normalize(byEntity.Pos.Pitch, 1.578125f, 4.6875f);
                SetNote(byEntity, (EnumPitch)(int)Math.Round((noteValues.Length - 1) * normalizedPitch));
                PlayNote(byEntity, GetNote(byEntity));
            }
        }
        
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.Controls.Sneak) return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
            if (byEntity.World.Side.IsServer() && sustain && !GetSustainment(byEntity))
            {
                SetSustainment(byEntity, true);
                float normalizedPitch = 1.0f - GameMathExt.Normalize(byEntity.Pos.Pitch, 1.578125f, 4.6875f);
                SetNote(byEntity, (EnumPitch)(int)Math.Round((noteValues.Length - 1) * normalizedPitch));
                PlayNote(byEntity, GetNote(byEntity));
                api.Event.RegisterCallback(dt => SetSustainment(byEntity, false), delay);
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
