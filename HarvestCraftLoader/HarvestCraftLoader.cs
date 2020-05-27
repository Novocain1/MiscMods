using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Drawing;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;
using System.IO;
using Cairo;
using Vintagestory.API.Util;
using Path = System.IO.Path;
using System.Globalization;

namespace HarvestCraftLoader
{
    class HarvestCraftLoaderMod : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("BlockCake", typeof(BlockCake));
            api.RegisterBlockClass("BlockCropHC", typeof(BlockCropHC));
            api.RegisterBlockEntityClass("Cake", typeof(BlockEntityCake));
            api.RegisterItemClass("ItemPlantableSeedHC", typeof(ItemPlantableSeedHC));
        }
    }
}
