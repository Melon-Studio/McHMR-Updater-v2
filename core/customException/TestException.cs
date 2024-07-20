using System;

namespace McHMR_Updater_v2.core.customException;
public class TestException : Exception
{
    private readonly bool _needThrow = true;

    public TestException(bool needThrow)
    {
        _needThrow = needThrow;
    }
}
