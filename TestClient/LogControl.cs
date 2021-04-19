using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TestClient
{
    public partial class LogControl : UserControl
    {
        /// <summary>
        /// The title of this control
        /// </summary>
        public string Title
        {
            get => groupBoxLog.Text;
            set => groupBoxLog.Text = value;
        }

        public LogControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Put a message at the TOP of the panel, along with a timestamp
        /// </summary>
        /// <param name="message">the message to display</param>
        public void LogMessage(string message)
        {
            if (rtbMessages.InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => LogMessage(message)));
            }
            else
            {
                const int longestText = 100000;
                var msg = $"{DateTime.Now.ToString("h:mm:ss.fff")} {message}";
                rtbMessages.Text = msg + Environment.NewLine + rtbMessages.Text;
                if (rtbMessages.TextLength > longestText)
                {
                    rtbMessages.Text = rtbMessages.Text.Substring(0, longestText / 2);
                }
            }
        }

        public void LogMessage(string format, params object[] args)
        {
            var line = string.Format(format, args);
            LogMessage(line);
        }

        /// <summary>
        /// Put newLines at the TOP of the panel, along with a timestamp, last lines to the top
        /// </summary>
        /// <param name="newLines">the lines to display</param>
        public void LogMessages(IEnumerable<string> newLines)
        {
            if (rtbMessages.InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => LogMessages(newLines)));
            }
            else
            {
                const int longestText = 100000;
                foreach (var line in newLines)
                {
                    var msg = $"{DateTime.Now.ToString("h:mm:ss.fff")} {line}";
                    rtbMessages.Text = msg + Environment.NewLine + rtbMessages.Text;
                }
                if (rtbMessages.TextLength > longestText)
                {
                    rtbMessages.Text = rtbMessages.Text.Substring(0, longestText / 2);
                }
            }
        }

        /// <summary>
        /// Put newLines at the TOP of the panel, along with a timestamp, first lines to the top
        /// </summary>
        /// <param name="newLines">the lines to display</param>
        public void LogMessagesReversed(IEnumerable<string> newLines)
        {
            if (rtbMessages.InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => LogMessagesReversed(newLines)));
            }
            else
            {
                var list = newLines.ToList();
                list.Reverse();
                foreach (var line in list)
                {
                    LogMessage(line);
                }
            }
        }

        public void Clear()
        {
            if (rtbMessages.InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => rtbMessages.Clear()));
            }
            else
            {
                rtbMessages.Clear();
            }
        }

        /// <summary>
        /// Clear the old contents and add newLines
        /// </summary>
        /// <param name="newLines"></param>
        public void Reset(IEnumerable<string> newLines)
        {
            if (rtbMessages.InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => Reset(newLines)));
            }
            else
            {
                rtbMessages.Lines = newLines.ToArray();
            }
        }
    }
}