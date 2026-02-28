using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IrcClientCore.Handlers.BuiltIn
{
    /// <summary>
    /// Handles batch messages (IRCv3.2)
    /// Supports labeled-reply and message grouping
    ///
    /// BATCH format: @batch=reference;label=xyz START batch_reference type
    /// </summary>
    class BatchHandler : BaseHandler
    {
        // Track active batches - instance level to support multiple Irc instances
        private readonly Dictionary<string, BatchInfo> _activeBatches = new Dictionary<string, BatchInfo>();

        public static event Action<BatchInfo> OnBatchStart;
        public static event Action<string> OnBatchEnd;
        public static event Action<string, IrcMessage> OnBatchMessage;

        public override async Task<bool> HandleLine(IrcMessage parsedLine)
        {
            var command = parsedLine.CommandMessage.Command;

            if (command == "BATCH")
            {
                return await HandleBatch(parsedLine);
            }

            // Check if this message is part of a batch
            // We always return true to allow other handlers to process the message
            if (parsedLine.Metadata.TryGetValue("batch", out var batchRef))
            {
                await HandleBatchMessage(parsedLine, batchRef);
            }

            // Always return true to allow the actual command handler to process
            // (e.g., PRIVMSG handler should still handle PRIVMSG with batch metadata)
            return true;
        }

        private Task<bool> HandleBatch(IrcMessage parsedLine)
        {
            var param = parsedLine.TrailMessage.TrailingContent;

            if (param.StartsWith("+"))
            {
                // Batch start: +batchref type [params]
                var parts = param.Substring(1).Split(' ');
                var batchRef = parts[0];
                var type = parts.Length > 1 ? parts[1] : "";

                var batchInfo = new BatchInfo
                {
                    Reference = batchRef,
                    Type = type,
                    Messages = new List<IrcMessage>()
                };

                _activeBatches[batchRef] = batchInfo;
                OnBatchStart?.Invoke(batchInfo);

                Irc.ClientMessage("Server", $"[Batch start: {type}]");
            }
            else if (param.StartsWith("-"))
            {
                // Batch end: -batchref
                var batchRef = param.Substring(1).Trim();
                if (_activeBatches.TryGetValue(batchRef, out var batchInfo))
                {
                    OnBatchEnd?.Invoke(batchRef);
                    Irc.ClientMessage("Server", $"[Batch end: {batchInfo.Type}]");
                    _activeBatches.Remove(batchRef);
                }
            }

            return Task.FromResult(true);
        }

        private Task HandleBatchMessage(IrcMessage parsedLine, string batchRef)
        {
            if (_activeBatches.TryGetValue(batchRef, out var batchInfo))
            {
                batchInfo.Messages.Add(parsedLine);
                OnBatchMessage?.Invoke(batchRef, parsedLine);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets message label for labeled-reply support
        /// </summary>
        public static string GetLabel(IrcMessage parsedLine)
        {
            if (parsedLine.Metadata.TryGetValue("label", out var label))
            {
                return label;
            }
            return null;
        }

        /// <summary>
        /// Get batch info by reference
        /// </summary>
        public BatchInfo GetBatch(string reference)
        {
            _activeBatches.TryGetValue(reference, out var batch);
            return batch;
        }
    }

    /// <summary>
    /// Information about an active batch
    /// </summary>
    public class BatchInfo
    {
        public string Reference { get; set; }
        public string Type { get; set; }
        public List<IrcMessage> Messages { get; set; }

        // Common batch types:
        // - "labeled-response" - for labeled reply
        // - "netjoin" - for channel joins (when multiple users join at once)
        // - "draft/multiline" - for multiline messages (IRCv3.3)
    }
}
