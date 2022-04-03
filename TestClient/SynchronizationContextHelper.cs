using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using NLog;

namespace TestClient
{
    /// <summary>
    /// This class only reduces code a bit by hiding the check "if (_syncContext != SynchronizationContext.Current)"
    /// </summary>
    public class SynchronizationContextHelper
    {
        private static readonly ILogger s_logger = LogManager.GetCurrentClassLogger();

        public SynchronizationContext SyncContext { get; }

        public int ThreadId { get; }

        public SynchronizationContextHelper()
        {
            SyncContext = SynchronizationContext.Current;
            ThreadId = Environment.CurrentManagedThreadId;
            //s_logger.Debug("Setting _syncContext");
        }

        /// <summary>
        /// Return <c>false</c> if already on the correct thread
        /// </summary>
        public bool IsContextSwitchRequired
        {
            get
            {
                var threadId = Environment.CurrentManagedThreadId;
                return threadId != ThreadId;
            }
        }

        /// <summary>
        /// Use SynchronizationContext.Send() if on the current SynchronizationContext
        /// Use Send if you need to get something done as soon as possible.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="state"></param>
        public void Send(SendOrPostCallback d, object state = null)
        {
            try
            {
                if (SyncContext != SynchronizationContext.Current)
                {
                    SyncContext.Send(d, state);
                }
                else
                {
                    d?.Invoke(state);
                }
            }
            catch (InvalidAsynchronousStateException)
            {
                // Can happen on shutdown.
                // System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation. --->
                // System.ComponentModel.InvalidAsynchronousStateException: An error occurred invoking the method.
                // The destination thread no longer exists.
                s_logger.Debug(" Ignoring InvalidAsynchronousStateException, probably due to chart closing.");
            }
            catch (TargetInvocationException)
            {
                // Can happen on shutdown.
                // System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation. --->
                // System.ComponentModel.InvalidAsynchronousStateException: An error occurred invoking the method.
                // The destination thread no longer exists.
                s_logger.Debug(" Ignoring TargetInvocationException, probably due to chart closing.");
            }
            catch (Exception ex)
            {
                s_logger.Error(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Use SynchronizationContext.Post() if on the current SynchronizationContext
        /// Use Post to wait our turn in the queue.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="state"></param>
        public void Post(SendOrPostCallback d, object state = null)
        {
            if (SyncContext != SynchronizationContext.Current)
            {
                SyncContext.Post(d, state);
            }
            else
            {
                d?.Invoke(state);
            }
        }

        public override string ToString()
        {
            return $"{SyncContext} ThreadId={ThreadId}";
        }
    }
}