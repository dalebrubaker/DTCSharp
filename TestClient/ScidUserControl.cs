using System;
using System.IO;
using System.Windows.Forms;
using DTCCommon;

namespace TestClient;

public partial class ScidUserControl : UserControl
{
    private long _count;
    private DateTime _lastReportedTimestampUtc;

    public ScidUserControl()
    {
        InitializeComponent();
    }

    private void OnLoad(object sender, EventArgs e)
    {
        if (DesignMode)
        {
            return;
        }
        checkBoxLogLastRecord.Checked = Settings1.Default.ScidLogLastRecord;
        textBoxScidPath.Text = Settings1.Default.ScidPath;
    }

    /// <summary>
    ///     Thanks to https://stackoverflow.com/questions/12474566/what-event-signals-that-a-usercontrol-is-being-destroyed
    ///     The Disposed event is TOO LATE
    /// </summary>
    /// <param name="e"></param>
    protected override void OnHandleDestroyed(EventArgs e)
    {
        try
        {
            Settings1.Default.ScidLogLastRecord = checkBoxLogLastRecord.Checked;
            Settings1.Default.ScidPath = textBoxScidPath.Text;
            Settings1.Default.Save();
            base.OnHandleDestroyed(e);
        }
        catch (Exception)
        {
            // Closing, don't throw
        }
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
        if (!checkBoxLogLastRecord.Checked)
        {
            return;
        }
        var path = textBoxScidPath.Text;
        if (!File.Exists(path))
        {
            MessageBox.Show($"File does not exist: {textBoxScidPath.Text}");
            timer1.Enabled = false;
            return;
        }
        timer1.Enabled = false;
        try
        {
            var (count, lastStartDateTimeUtc) = SierraChartUtil.GetCountAndLastTimestamp(path);
            if (count == _count)
            {
                // No change, don't report
                return;
            }
            _count = count;
            var utcNow = DateTime.UtcNow;
            var elapsed = (utcNow - _lastReportedTimestampUtc).TotalMilliseconds;
            _lastReportedTimestampUtc = utcNow;
            var delayedReport = (utcNow - lastStartDateTimeUtc).TotalMilliseconds;
            logControl1.LogMessage($"{count:N0} records ending at {lastStartDateTimeUtc.ToLocalTime()} {delayedReport} ms behind now, {elapsed} ms since last message.");
        }
        finally
        {
            timer1.Enabled = checkBoxLogLastRecord.Checked;
        }
    }

    private void checkBoxLogLastRecord_CheckedChanged(object sender, EventArgs e)
    {
        if (checkBoxLogLastRecord.Checked)
        {
            if (!File.Exists(textBoxScidPath.Text))
            {
                MessageBox.Show($"File does not exist: {textBoxScidPath.Text}");
                timer1.Enabled = false;
            }
        }
        timer1.Enabled = checkBoxLogLastRecord.Checked;
    }
}