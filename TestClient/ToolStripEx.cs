using System;
using System.Windows.Forms;

namespace TestClient;

/// <summary>
///     Thanks to https://blogs.msdn.microsoft.com/rickbrew/2006/01/09/how-to-enable-click-through-for-net-2-0-toolstrip-and-menustrip/
/// </summary>
public class ToolStripEx : ToolStrip
{
    public ToolStripEx()
    {
        ClickThrough = true;
    }

    public bool ClickThrough { get; set; }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (ClickThrough
            && m.Msg == NativeConstants.WM_MOUSEACTIVATE
            && m.Result == (IntPtr)NativeConstants.MA_ACTIVATEANDEAT)
        {
            m.Result = (IntPtr)NativeConstants.MA_ACTIVATE;
        }
    }
}