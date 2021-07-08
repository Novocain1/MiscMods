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
        public Queue<IncomingNotification> IncomingNotifications { get; set; } = new Queue<IncomingNotification>();

        public int maxElements = 5;

        public void CreateNotification(string text, double[] color = null, float expiryTime = 2.0f)
        {
            var notif = new IncomingNotification(text, color, expiryTime);
            IncomingNotifications.Enqueue(notif);
        }
        
        long id;

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;
            
            capi.Network.RegisterChannel("VSHUD.Notification").RegisterMessageType<AssetLocation>().SetMessageHandler<AssetLocation>((a) =>
            {
                try
                {
                    string notification = a.ToShortString();
                    CreateNotification(notification);
                }
                catch (Exception)
                {
                    CreateNotification("Tried Parsing Bad Notification Packet, Ignoring.");
                }
            });

            //move to separate thread at some point
            id = api.Event.RegisterGameTickListener((dt) =>
            {
                if (IncomingNotifications.Count > 0)
                {
                    if (Notifications.Count < maxElements)
                    {
                        for (int i = 0; i < maxElements - Notifications.Count; i++)
                        {
                            if (i > IncomingNotifications.Count) break;

                            var notif = IncomingNotifications.Dequeue();
                            var elem = new HudElementNotification(api, notif.text, notif.color, notif.expiryTime - dt);
                            Notifications.Enqueue(elem);
                        }
                    }
                    else
                    {
                        //yea
                        Notifications.First().expiryTime = -0.1f;
                    }
                }
            }, 30);

            api.RegisterCommand("notification", "creates client notification", "", (id, args) =>
            {
                string text = args.PopAll();
                text = text.Length < 1 ? "Notification" : text;
                CreateNotification(text);
            });
        }

        public override void Dispose()
        {
            capi?.Event.UnregisterGameTickListener(id);
        }
    }

    class IncomingNotification
    {
        public string text;
        public double[] color;
        public float expiryTime;

        public IncomingNotification(string text, double[] color = null, float expiryTime = 2.0f)
        {
            this.text = text;
            this.color = color ?? new double[] { 1, 1, 1, 1 };
            this.expiryTime = expiryTime;
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

        public HudElementNotification(ICoreClientAPI capi, string text, double[] color, float expiryTime) : base(capi)
        {
            Construct(capi, text, color);
            this.expiryTime = expiryTime;
        }

        void Construct(ICoreClientAPI capi, string text, double[] color)
        {
            id = Elements;
            
            font = CairoFont.WhiteDetailText();

            ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.RightTop, 0, 0, text.Count() * 12, 15);

            ElementBounds dialogBounds = ElementBounds.Fixed(EnumDialogArea.RightTop, 0, 350, textBounds.fixedWidth, textBounds.fixedHeight);

            this.color = color;
            font.Color = color;

            font = font.WithStroke(new double[] { 0.0, 0.0, 0.0, 1.0 }, 1.0).WithWeight(Cairo.FontWeight.Bold);

            SingleComposer = capi.Gui
                .CreateCompo("notification" + text + capi.Gui.OpenedGuis.Count + 1 + GetHashCode(), dialogBounds)
                .AddDynamicText(text, font, EnumTextOrientation.Right, textBounds, "text")
                .Compose();

            var dynText = SingleComposer.GetDynamicText("text");
            SingleComposer.Bounds.absOffsetY = dynText.Bounds.absInnerHeight + (id * dynText.Bounds.absInnerHeight);

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

            if (id == 0) expiryTime -= deltaTime;
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
