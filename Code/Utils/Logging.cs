﻿using System;
using System.Text;
using UnityEngine;


namespace BOB
{
    /// <summary>
    /// Logging utility class.
    /// </summary>
    internal static class Logging
    {
        // Logging detail flag.
        internal static bool detailLogging = true;
        
        /// <summary>
        /// Prints a single-line debugging message to the Unity output log with an "ERROR: " prefix, regardless of the 'detailed logging' setting.
        /// </summary>
        /// <param name="messages">Message to log (individual strings will be concatenated)</param>
        internal static void Error(params object[] messages) => WriteMessage("ERROR: ", messages);


        /// <summary>
        /// Prints a single-line debugging message to the Unity output log, regardless of the 'detailed logging' setting.
        /// </summary>
        /// <param name="messages">Message to log (individual strings will be concatenated)</param>
        internal static void KeyMessage(params object[] messages) => WriteMessage(String.Empty, messages);


        /// <summary>
        /// Prints a single-line debugging message to the Unity output log if the 'detailed logging' option is set (otherwise does nothing).
        /// </summary>
        /// <param name="messages">Message to log (individual strings will be concatenated)</param>
        internal static void Message(params object[] messages)
        {
            if (detailLogging)
            {
                WriteMessage(String.Empty, messages);
            }
        }


        /// <summary>
        /// Prints an exception message to the Unity output log.
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="messages">Message to log (individual strings will be concatenated)</param>
        internal static void LogException(Exception exception, params object[] messages)
        {
            // Use StringBuilder for efficiency since we're doing a lot of manipulation here.
            // Start with mod name (to easily identify relevant messages), followed by colon to indicate start of actual message.
            StringBuilder message = new StringBuilder(BOBMod.ModName);
            message.Append(": ");

            // Add each message parameter.
            for (int i = 0; i < messages.Length; ++i)
            {
                message.Append(messages[i]);
            }

            // Finish with a new line and the exception information.
            message.AppendLine();
            message.AppendLine("Exception: ");
            message.AppendLine(exception.Message);
            message.AppendLine(exception.Source);
            message.AppendLine(exception.StackTrace);

            // Log inner exception as well, if there is one.
            if (exception.InnerException != null)
            {
                message.AppendLine("Inner exception:");
                message.AppendLine(exception.InnerException.Message);
                message.AppendLine(exception.InnerException.Source);
                message.AppendLine(exception.InnerException.StackTrace);
            }

            // Check to see if we've already reported an exception (only want to report earliest exception and not clobber with any downstream exceptions).
            if (!InfoPanelManager.wasException)
            {
                // Not previously reported - set flag to indicate that an exception has been logged, and record message.
                InfoPanelManager.wasException = true;
                InfoPanelManager.exceptionMessage = message.ToString();
            }

            // Write to log.
            Debug.Log(message);
        }


        /// <summary>
        /// Prints a single-line debugging message to the Unity output log with a specified prefix.
        /// </summary>
        /// <param name="prefix">Prefix for message, if any</param>
        /// <param name="messages">Message to log (individual strings will be concatenated)</param>
        private static void WriteMessage(string prefix, params object[] messages)
        {
            // Use StringBuilder for efficiency since we're doing a lot of manipulation here.
            // Start with mod name (to easily identify relevant messages), followed by colon to indicate start of actual message.
            StringBuilder message = new StringBuilder(BOBMod.ModName);
            message.Append(": ");

            // Append prefix.
            message.Append(prefix);

            // Add each message parameter.
            for (int i = 0; i < messages.Length; ++i)
            {
                message.Append(messages[i]);
            }

            // Terminating period to confirm end of messaage..
            message.Append(".");

            Debug.Log(message);
        }
    }
}
