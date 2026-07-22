using System;
using System.Text;
using UnityEngine;

namespace OddSockets.Unity
{
    /// <summary>
    /// Message size validation service for Unity.
    /// 
    /// Validates message sizes against industry standard limits (32KB)
    /// to ensure reliable real-time messaging.
    /// </summary>
    public static class MessageSizeValidator
    {
        /// <summary>
        /// Message size limits (industry standard - matches PubNub).
        /// </summary>
        public static class Limits
        {
            /// <summary>
            /// Maximum message size in bytes (32KB).
            /// </summary>
            public const int MaxMessageSizeBytes = 32768;

            /// <summary>
            /// Maximum message size in KB.
            /// </summary>
            public const int MaxMessageSizeKB = 32;
        }

        /// <summary>
        /// Validates message size and throws an exception if it exceeds the limit.
        /// </summary>
        /// <param name="message">Message to validate</param>
        /// <exception cref="OddSocketsMessageSizeException">Thrown when message exceeds size limit</exception>
        /// <returns>The size of the message in bytes</returns>
        public static int ValidateMessageSize(object message)
        {
            if (message == null)
            {
                return 0;
            }

            string messageStr;
            
            // Convert message to string representation
            if (message is string str)
            {
                messageStr = str;
            }
            else
            {
                try
                {
                    messageStr = JsonUtility.ToJson(message);
                }
                catch (Exception ex)
                {
                    throw new OddSocketsMessageSizeException(
                        $"Failed to serialize message for size validation: {ex.Message}");
                }
            }

            // Calculate size in bytes using UTF-8 encoding
            var messageSize = Encoding.UTF8.GetByteCount(messageStr);

            if (messageSize > Limits.MaxMessageSizeBytes)
            {
                var messageSizeKB = Math.Round(messageSize / 1024.0, 1);
                throw new OddSocketsMessageSizeException(
                    $"Message size ({messageSizeKB}KB) exceeds maximum allowed size of {Limits.MaxMessageSizeKB}KB. " +
                    $"This limit matches industry standards (PubNub, Socket.IO) for reliable real-time messaging.");
            }

            return messageSize;
        }

        /// <summary>
        /// Checks if a message size is valid without throwing an exception.
        /// </summary>
        /// <param name="message">Message to check</param>
        /// <returns>True if the message size is valid, false otherwise</returns>
        public static bool IsMessageSizeValid(object message)
        {
            try
            {
                ValidateMessageSize(message);
                return true;
            }
            catch (OddSocketsMessageSizeException)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the size of a message in bytes without validation.
        /// </summary>
        /// <param name="message">Message to measure</param>
        /// <returns>Size in bytes, or -1 if measurement failed</returns>
        public static int GetMessageSize(object message)
        {
            if (message == null)
            {
                return 0;
            }

            try
            {
                string messageStr;
                
                if (message is string str)
                {
                    messageStr = str;
                }
                else
                {
                    messageStr = JsonUtility.ToJson(message);
                }

                return Encoding.UTF8.GetByteCount(messageStr);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to measure message size: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Gets the size of a message in KB without validation.
        /// </summary>
        /// <param name="message">Message to measure</param>
        /// <returns>Size in KB, or -1 if measurement failed</returns>
        public static double GetMessageSizeKB(object message)
        {
            var sizeBytes = GetMessageSize(message);
            return sizeBytes >= 0 ? Math.Round(sizeBytes / 1024.0, 2) : -1;
        }

        /// <summary>
        /// Validates multiple messages for bulk operations.
        /// </summary>
        /// <param name="messages">Messages to validate</param>
        /// <exception cref="OddSocketsMessageSizeException">Thrown when any message exceeds size limit</exception>
        /// <returns>Total size of all messages in bytes</returns>
        public static int ValidateBulkMessages(object[] messages)
        {
            if (messages == null || messages.Length == 0)
            {
                return 0;
            }

            var totalSize = 0;
            
            for (int i = 0; i < messages.Length; i++)
            {
                try
                {
                    var messageSize = ValidateMessageSize(messages[i]);
                    totalSize += messageSize;
                }
                catch (OddSocketsMessageSizeException ex)
                {
                    throw new OddSocketsMessageSizeException(
                        $"Message at index {i} failed size validation: {ex.Message}");
                }
            }

            return totalSize;
        }

        /// <summary>
        /// Gets size information for a message.
        /// </summary>
        /// <param name="message">Message to analyze</param>
        /// <returns>Size information</returns>
        public static MessageSizeInfo GetMessageSizeInfo(object message)
        {
            var sizeBytes = GetMessageSize(message);
            var isValid = sizeBytes >= 0 && sizeBytes <= Limits.MaxMessageSizeBytes;
            var sizeKB = sizeBytes >= 0 ? Math.Round(sizeBytes / 1024.0, 2) : 0;
            var percentageUsed = sizeBytes >= 0 ? Math.Round((sizeBytes / (double)Limits.MaxMessageSizeBytes) * 100, 1) : 0;

            return new MessageSizeInfo
            {
                SizeBytes = sizeBytes,
                SizeKB = sizeKB,
                IsValid = isValid,
                PercentageUsed = percentageUsed,
                RemainingBytes = isValid ? Limits.MaxMessageSizeBytes - sizeBytes : 0
            };
        }
    }

    /// <summary>
    /// Information about message size.
    /// </summary>
    [Serializable]
    public class MessageSizeInfo
    {
        [SerializeField] private int sizeBytes;
        [SerializeField] private double sizeKB;
        [SerializeField] private bool isValid;
        [SerializeField] private double percentageUsed;
        [SerializeField] private int remainingBytes;

        /// <summary>
        /// Size in bytes.
        /// </summary>
        public int SizeBytes
        {
            get => sizeBytes;
            set => sizeBytes = value;
        }

        /// <summary>
        /// Size in KB.
        /// </summary>
        public double SizeKB
        {
            get => sizeKB;
            set => sizeKB = value;
        }

        /// <summary>
        /// Whether the message size is valid.
        /// </summary>
        public bool IsValid
        {
            get => isValid;
            set => isValid = value;
        }

        /// <summary>
        /// Percentage of maximum size used.
        /// </summary>
        public double PercentageUsed
        {
            get => percentageUsed;
            set => percentageUsed = value;
        }

        /// <summary>
        /// Remaining bytes before hitting the limit.
        /// </summary>
        public int RemainingBytes
        {
            get => remainingBytes;
            set => remainingBytes = value;
        }

        public override string ToString()
        {
            return $"MessageSizeInfo(Size: {SizeKB}KB, Valid: {IsValid}, Used: {PercentageUsed}%)";
        }
    }

    /// <summary>
    /// Exception thrown when a message exceeds the size limit.
    /// </summary>
    public class OddSocketsMessageSizeException : Exception
    {
        /// <summary>
        /// Creates a new OddSocketsMessageSizeException.
        /// </summary>
        /// <param name="message">Exception message</param>
        public OddSocketsMessageSizeException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new OddSocketsMessageSizeException.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception</param>
        public OddSocketsMessageSizeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
