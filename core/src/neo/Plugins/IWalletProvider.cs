using Neo.Wallets;
using System;

namespace Neo.Plugins
{
    public interface IWalletProvider
    {
        event EventHandler<Wallet> WalletChanged;

        Wallet GetWallet();
    }
}
