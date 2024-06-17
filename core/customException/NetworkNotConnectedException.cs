using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McHMR_Updater_v2.core.customException;
public class NetworkNotConnectedException : Exception
{
    private readonly Boolean _isProcessKill;

    public NetworkNotConnectedException(string message)
        : base(message) { }
    public NetworkNotConnectedException(string message, Exception inner)
        : base(message, inner) { }
}
