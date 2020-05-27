using Vintagestory.API.Common;
using Newtonsoft.Json;
using Vintagestory.API.Server;

namespace HarvestCraftLoader
{
    class MultiPropFood
    {
        [JsonProperty]
        public FoodNutritionProperties[] NutritionProperties { get; set; }

        [JsonProperty]
        public int Division { get; set; }

        public void AddNutrientsToPlayer(IServerPlayer player)
        {
            for (int i = 0; i < Division; i++)
            {
                for (int j = 0; j < NutritionProperties.Length; j++)
                {
                    player?.Entity.ReceiveSaturation(NutritionProperties[j].Satiety / Division, NutritionProperties[j].FoodCategory);
                }
            }
        }
    }
}
