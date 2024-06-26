using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McHMR_Updater_v2.core.customException;
public class TestException : Exception
{
    private readonly bool _needThrow = true;

    public TestException(bool needThrow)
    {
        _needThrow = needThrow;
    }
}
