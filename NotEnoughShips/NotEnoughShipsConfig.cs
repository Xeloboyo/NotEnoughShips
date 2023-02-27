using SpaceWarp.API.Configuration;
using Newtonsoft.Json;

namespace NotEnoughShips
{
    [JsonObject(MemberSerialization.OptOut)]
    [ModConfig]
    public class NotEnoughShipsConfig
    {
         [ConfigField("pi")] [ConfigDefaultValue(3.14159)] public double pi;
    }
}