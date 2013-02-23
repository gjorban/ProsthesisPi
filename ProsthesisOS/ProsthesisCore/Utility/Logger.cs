using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProsthesisCore.Utility
{
    public sealed class Logger
    {
        [System.Flags]
        public enum LoggerChannels
        {
            General = 1,
            System = 2,
            Network = 4,
            StateMachine = 8,
            Events = 16,
            Faults = 32,
            Telemetry = 64
        }

        private bool mPrintToConsole = false;
        private string mFileName = string.Empty;
        private System.Threading.Thread mWorker = null;
        private System.Threading.ManualResetEvent mMessageFlag = new System.Threading.ManualResetEvent(false);

        private System.IO.StreamWriter mWriter = null;

        private Queue<string> mQueuedMessagesForOutput = new Queue<string>();

        private LoggerChannels mActiveChannels = (LoggerChannels)~0;

        public Logger(string fileOutput, bool printToConsole)
        {
            mFileName = fileOutput;
            mPrintToConsole = printToConsole;
            mWriter = new System.IO.StreamWriter(fileOutput, true);
            mWriter.AutoFlush = true;
            mWorker = new System.Threading.Thread(DoLogging);
            mWorker.Start();
        }

        public void ShutDown()
        {
            if (mWriter != null)
            {
                lock (this)
                {
                    while (mQueuedMessagesForOutput.Count > 0)
                    {
                        string message = mQueuedMessagesForOutput.Dequeue();
                        mWriter.WriteLine(message);
                        if (mPrintToConsole)
                        {
                            Console.WriteLine(message);
                        }
                    }
                }

                mWriter.Flush();
                mWriter.Close();
                mWriter = null;
            }

            if (mWorker != null)
            {
                mWorker.Abort();
                mWorker = null;
            }
        }

        public void ActivateChannels(LoggerChannels channels)
        {
            mActiveChannels |= channels;
        }

        public void DeactivateChannels(LoggerChannels channels)
        {
            mActiveChannels &= ~channels;
        }

        public void LogMessage(string msg)
        {
            LogMessage(LoggerChannels.General, msg);
        }

        public void LogMessage(LoggerChannels channel, string msg)
        {
            LogMessage(channel, msg, true);
        }

        public void LogMessage(LoggerChannels channel, string msg, bool prefixTimestamp)
        {
            if ((channel & mActiveChannels) != channel)
            {
                return;
            }

            string timeStamp = string.Empty;
            if (prefixTimestamp)
            {
                timeStamp = System.DateTime.Now.ToString("HH:mm:ss");
            }

            string message = string.Format("{0}<{1}>: {2}", timeStamp, channel.ToString(), msg);
            lock (this)
            {
                mQueuedMessagesForOutput.Enqueue(message);
            }
            mMessageFlag.Set();
        }

        private void DoLogging()
        {
            while (mWriter != null)
            {
                Queue<string> messages = null;
                lock (this)
                {
                    messages = new Queue<string>(mQueuedMessagesForOutput);
                    mQueuedMessagesForOutput.Clear();
                }

                while (messages != null && mWriter != null && messages.Count > 0)
                {
                    string message = messages.Dequeue();
                    mWriter.WriteLine(message);
                    mWriter.Flush();
                    if (mPrintToConsole)
                    {
                        Console.WriteLine(message);
                    }
                }

                //Wait 1 second maximum between log dumps. Can this be longer? This is meant to catch instances where the main system may have been interrupted and can't raise the flag anymore
                mMessageFlag.WaitOne(1000);
                mMessageFlag.Reset();
            }
        }
    }
}
