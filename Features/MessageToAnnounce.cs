using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Models;

namespace CobaltCoreArchipelago.Features;

public class MessageToAnnounce
{
    public enum MessageType
    {
        ItemReceived,
        DeathlinkReceived
    }

    public enum DeathlinkType
    {
        Missing,
        Damage,
        Death
    }

    public const MessageType ItemReceived = MessageType.ItemReceived;
    public const MessageType DeathlinkReceived = MessageType.DeathlinkReceived;

    public required MessageType type;
    public ItemInfo? item;
    public DeathLink? deathlink;
    public DeathlinkType? deathlinkType;
    public bool? lottaDeathlinks;
    public bool? critical;
}