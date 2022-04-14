using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace TestClient
{
    public partial class LogControl : UserControl
    {
        private readonly ConcurrentStack<string> _messageStack;

        public LogControl()
        {
            InitializeComponent();
            _messageStack = new ConcurrentStack<string>();
            MaximumLogLengthChars = 1024 * 1024 * 100;
        }

        /// <summary>
        /// The title of this control
        /// </summary>
        public string Title
        {
            get => groupBoxLog.Text;
            set => groupBoxLog.Text = value;
        }

        public int MaximumLogLengthChars { get; set; }
        public bool HideTimestamps { get; set; }

        /// <summary>
        /// Put a message at the TOP of the panel, along with a timestamp
        /// </summary>
        /// <param name="message">the message to display</param>
        public void LogMessage(string message)
        {
            PushMessageOntoStack(message);
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
            foreach (var message in newLines)
            {
                PushMessageOntoStack(message);
            }
        }

        /// <summary>
        /// Erase the log
        /// </summary>
        public void Clear()
        {
            SafeInvoke(this, () => rtbMessages.Clear());
        }

        /// <summary>
        /// Clear the old contents and add newLines
        /// </summary>
        /// <param name="newLines"></param>
        public void Reset(IEnumerable<string> newLines)
        {
            Clear();
            foreach (var message in newLines)
            {
                PushMessageOntoStack(message);
            }
        }

        private void PushMessageOntoStack(string message)
        {
            if (HideTimestamps)
            {
                var msg = message;
                _messageStack.Push(msg);
            }
            else
            {
                var msg = $"{DateTime.Now:h:mm:ss.fff} {message}";
                _messageStack.Push(msg);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_messageStack.IsEmpty)
            {
                return;
            }
            SafeInvoke(this, LogMessagesOnStack);
        }

        /// <summary>
        /// Put messages on the stack at the TOP of the panel, along with a timestamp
        /// </summary>
        private void LogMessagesOnStack()
        {
            var sb = new StringBuilder();
            while (!_messageStack.IsEmpty)
            {
                if (_messageStack.TryPop(out var msg))
                {
                    sb.AppendLine(msg);
                }
            }
            sb.AppendLine(rtbMessages.Text);
            if (sb.Length > MaximumLogLengthChars)
            {
                var oldLength = sb.Length;
                var saveStr = sb.ToString().Substring(1, MaximumLogLengthChars / 2);
                sb = new StringBuilder();
                sb.Append("Truncated the log from ")
                    .AppendFormat("{0:N0}", oldLength).Append(" to ")
                    .AppendFormat("{0:N0}", saveStr.Length)
                    .AppendLine(" characters");
                sb.AppendLine(saveStr);
            }
            rtbMessages.Text = sb.ToString();
        }

        private void LogControl_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }

        private void tsbClear_Click(object sender, EventArgs e)
        {
            Clear();
        }
        
        public bool SafeInvoke(Control control, MethodInvoker method)
        {
            if (control is { IsDisposed: false, IsHandleCreated: true })
            {
                if (control.InvokeRequired)
                {
                    control.Invoke(method);
                }
                else
                {
                    method();
                }
                return true;
            }
            return false;
        }
    }
}