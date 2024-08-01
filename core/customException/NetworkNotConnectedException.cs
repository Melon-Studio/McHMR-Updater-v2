using System;

namespace McHMR_Updater_v2.core.customException;
public class NetworkNotConnectedException : Exception
{
    public NetworkNotConnectedException(string message)
        : base(message) { }
    public NetworkNotConnectedException(string message, Exception inner)
        : base(message, inner) { }
}
