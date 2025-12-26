using Nanoray.PluginManager;
using Nickel;

namespace CobaltCoreArchipelago;

internal interface IRegisterable
{
    static abstract void Register(IPluginPackage<IModManifest> package, IModHelper helper);
}