using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VSHUD
{
    class ModSystemNotification : ClientModSystem
    {
        ICoreClientAPI capi;
        public Queue<HudElementNotification> Notifications { get; set; } = new Queue<HudElementNotification>();

        public void CreateNotification(string text)
        {
            var elem = new HudElementNotification(capi, text);
            Notifications.Enqueue(elem);
        }

        public void CreateNotification(string text, double[] color)
        {
            var elem = new HudElementNotification(capi, text, color);
            Notifications.Enqueue(elem);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;
            api.RegisterCommand("notification", "creates client notification", "", (id, args) =>
            {
                string text = args.PopAll();
                text = text.Length < 1 ? "Notification" : text;
                CreateNotification(text);
            });
        }
    }

    class HudElementNotification : HudElement
    {
        public float expiryTime = 2.0f;
        public CairoFont font;
        public double[] color;

        public override bool ShouldReceiveMouseEvents() => false;

        public Queue<HudElementNotification> Notifications { get => capi.ModLoader.GetModSystem<ModSystemNotification>().Notifications; }

        public int Elements { get => Notifications.Count; }
        public int id;

        public HudElementNotification(ICoreClientAPI capi, string text) : base(capi)
        {
            Construct(capi, text, new double[] { 1, 1, 1, 1 });
        }

        public HudElementNotification(ICoreClientAPI capi, string text, double[] color) : base(capi)
        {
            Construct(capi, text, color);
        }

        void Construct(ICoreClientAPI capi, string text, double[] color)
        {
            id = Elements;
            expiryTime = 5.0f * (Elements + 1);
            
            font = CairoFont.WhiteDetailText();

            ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.RightBottom, 0, 0, text.Count() * 12, 15);

            ElementBounds dialogBounds = ElementBounds.Fixed(EnumDialogArea.RightBottom, -10, -150, textBounds.fixedWidth, textBounds.fixedHeight);

            this.color = color;
            font.Color = color;

            font = font.WithStroke(new double[] { 0.0, 0.0, 0.0, 1.0 }, 1.0).WithWeight(Cairo.FontWeight.Bold);

            SingleComposer = capi.Gui
                .CreateCompo("notification" + text + capi.Gui.OpenedGuis.Count + 1 + GetHashCode(), dialogBounds)
                .AddDynamicText(text, font, EnumTextOrientation.Center, textBounds, "text")
                .Compose();

            var dynText = SingleComposer.GetDynamicText("text");
            SingleComposer.Bounds.absOffsetY = id * dynText.Bounds.absInnerHeight;

            TryOpen();
        }

        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);
            var dynText = SingleComposer.GetDynamicText("text");
            
            SingleComposer.Bounds.absOffsetY = id * dynText.Bounds.absInnerHeight;
            double alpha = expiryTime / 2.0;
            dynText.Font = font.WithColor(new double[] { color[0], color[1], color[2], alpha }).WithStroke(new double[] { 0.0, 0.0, 0.0, alpha }, 1.0);
            dynText.RecomposeText();

            expiryTime -= deltaTime;
            if (expiryTime < 0)
            {
                TryClose();
                Dispose();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (Notifications.Count > 0) Notifications.Dequeue();
            foreach (var val in Notifications)
            {
                val.id--;
            }
        }
    }
}
