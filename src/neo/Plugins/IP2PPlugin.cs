using Neo.Network.P2P;

namespace Neo.Plugins
{
    public interface IP2PPlugin
    {
        bool OnP2PMessage(NeoSystem system, Message message) => true;
    }
}
