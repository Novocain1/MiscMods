using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace VSHUD
{
    public class CommandPlacementPreview : VSHUDCommand
    {
        public CommandPlacementPreview(ICoreClientAPI capi) : base(capi)
        {
            Command = "pconfig";
            RegisterSubCommand("enabled", new SubCommand((player, groupId, args) =>
            {
                Config.PRShow = (bool)args.PopBool(!Config.PRShow);
                capi.ShowChatMessage("Block placement preview set to " + Config.PRShow);
            }, "Enables/Disables the showing of the placement preview."));

            RegisterSubCommand("tinted", new SubCommand((player, groupId, args) =>
            {
                Config.PRTint = (bool)args.PopBool(!Config.PRTint);
                capi.ShowChatMessage("Block preview tinting set to " + Config.PRTint);
            }, "Enables/Disables the tinting of placement preview meshes."));

            RegisterSubCommand("tintcolorhex", new SubCommand((player, groupId, args) =>
            {
                string col = args.PopWord();
                if (col?[0] == '#')
                {
                    var color = ColorUtil.Hex2Doubles(col);
                    Config.PRTintColor = new float[]
                    {
                        (float)(color[0]) * 10.0f,
                        (float)(color[1]) * 10.0f,
                        (float)(color[2]) * 10.0f,
                    };
                }
            }, "Sets the placment preview mesh color to a set hexadecimal value starting with #."));

            RegisterSubCommand("tintcolorrgb", new SubCommand((player, groupId, args) =>
            {
                Config.PRTintColor[0] = (float)args.PopFloat(Config.PRTintColor[0]);
                Config.PRTintColor[1] = (float)args.PopFloat(Config.PRTintColor[1]);
                Config.PRTintColor[2] = (float)args.PopFloat(Config.PRTintColor[2]);
            }, "Sets the placment preview mesh color to set RGB float values r g b, ie '1.0 0.0 0.0' for red."));

            RegisterSubCommand("tintdefault", new SubCommand((player, groupId, args) =>
            {
                Config.PRTintColor = new VSHUDConfig().PRTintColor;
            }, "Sets the placment preview mesh color to the default bluish purple color."));

            RegisterSubCommand("opacity", new SubCommand((player, groupId, args) =>
            {
                Config.PRTintColor = new VSHUDConfig().PRTintColor;
            }, "Sets the placment preview mesh opacity."));

            RegisterSubCommand("opacitydefault", new SubCommand((player, groupId, args) =>
            {
                Config.PROpacity = new VSHUDConfig().PROpacity;
            }, "Sets the placment preview mesh opacity to the default opacity."));

            RegisterSubCommand("drawlines", new SubCommand((player, groupId, args) =>
            {
                Config.PRDrawLines = (bool)args.PopBool(!Config.PRDrawLines);
                capi.ShowChatMessage("Drawing of preview mesh wireframe set to " + Config.PRDrawLines);
            }, "Enables/Disables the drawing of the wireframe of the placment preview."));
        }
    }
}
