﻿#region

using System.IO;
using System.Text;
using Rnwood.SmtpServer.Verbs;

#endregion

namespace Rnwood.SmtpServer
{
    public class DataVerb : Verb
    {
        public override void Process(IConnectionProcessor connectionProcessor, SmtpCommand command)
        {
            if (connectionProcessor.CurrentMessage == null)
            {
                connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                                   "Bad sequence of commands"));
                return;
            }

            connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.StartMailInputEndWithDot,
                                                               "End message with period"));
            using (MemoryStream dataStream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(dataStream, Encoding.Default))
                {
                    do
                    {
                        string line = connectionProcessor.ReadLine();

                        if (line != ".")
                        {
                            line = ProcessLine(line);
                            writer.WriteLine(line);
                        }
                        else
                        {
                            break;
                        }
                    } while (true);

                    writer.Flush();
                    long? maxMessageSize =
                        connectionProcessor.Server.Behaviour.GetMaximumMessageSize(connectionProcessor);

                    if (maxMessageSize.HasValue && dataStream.Length > maxMessageSize.Value)
                    {
                        connectionProcessor.WriteResponse(
                            new SmtpResponse(StandardSmtpResponseCode.ExceededStorageAllocation,
                                             "Message exceeds fixed size limit"));
                    }
                    else
                    {
                        connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Mail accepted"));
                        connectionProcessor.CurrentMessage.Data = dataStream.ToArray();
                        connectionProcessor.CommitMessage();
                    }
                }
            }
        }

        protected virtual string ProcessLine(string line)
        {
            //Remove escaping of end of message character
            if (line.StartsWith("."))
            {
                line = line.Remove(0, 1);
            }
            return line;
        }
    }
}