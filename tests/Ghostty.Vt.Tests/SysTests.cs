using Ghostty.Vt;
using Xunit;

namespace Ghostty.Vt.Tests;

public class SysTests
{
    [Fact]
    public void Sys_SetLog_Null_ClearsCallback()
    {
        Sys.SetLog(null);
    }

    [Fact]
    public void Sys_SetLog_WithCallback_DoesNotThrow()
    {
        Sys.SetLog((level, scope, message) => { });
        Sys.SetLog(null);
    }

    [Fact]
    public void Sys_SetLogStderr_DoesNotThrow()
    {
        Sys.SetLogStderr();
        Sys.SetLog(null);
    }

    [Fact]
    public void Sys_SetDecodePng_Null_ClearsCallback()
    {
        Sys.SetDecodePng(null);
    }
}
