using Framework.Managers;
using System.Collections.Generic;

namespace Blasphemous.NewbieEltonLibs.Extensions.GameLibs;

internal static class ModOwnedFlagAdapter
{
    internal static bool TryGet(EventManager events, string vanillaId, out bool value)
    {
        value = false;
        Dictionary<string, FlagObject>? flags = TraverseUtils.GetValue<Dictionary<string, FlagObject>>(events, "flags");
        if (flags == null || !flags.TryGetValue(vanillaId, out FlagObject? flag) || flag == null)
            return false;

        value = flag.value;
        return true;
    }

    internal static bool TrySet(EventManager events, string vanillaId, bool value, bool preserveInNewGamePlus)
    {
        events.SetFlag(vanillaId, value, preserveInNewGamePlus);
        return true;
    }
}
