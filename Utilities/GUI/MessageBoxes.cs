﻿using System;
using System.Windows.Forms;

namespace Utilities.GUI
{
    public static class MessageBoxes
    {
        public static void Error(Exception ex, string msg)
        {
            string message = Logging.Error(ex, msg);
            WPFDoEvents.InvokeInUIThread(() =>
            {
                MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            });
        }

        public static void Error(Exception ex, string msg, params object[] args)
        {
            string message = Logging.Error(ex, msg, args);
            WPFDoEvents.InvokeInUIThread(() =>
            {
                MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            });
        }

        public static void Error(string msg)
        {
            string message = Logging.Error(msg);
            WPFDoEvents.InvokeInUIThread(() =>
            {
                MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            });
        }

        public static void Error(string msg, params object[] args)
        {
            string message = Logging.Error(msg, args);
            WPFDoEvents.InvokeInUIThread(() =>
            {
                MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            });
        }

        public static void Warn(string msg)
        {
            string message = Logging.Warn(msg);
            WPFDoEvents.InvokeInUIThread(() =>
            {
                MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            });
        }

        public static void Warn(string msg, params object[] args)
        {
            string message = Logging.Warn(msg, args);
            WPFDoEvents.InvokeInUIThread(() =>
            {
                MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            });
        }

        public static void Info(string msg)
        {
            string message = Logging.Info(msg);
            WPFDoEvents.InvokeInUIThread(() =>
            {
                MessageBox.Show(message, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }

        public static void Info(string msg, params object[] args)
        {
            string message = Logging.Info(msg, args);
            WPFDoEvents.InvokeInUIThread(() =>
            {
                MessageBox.Show(message, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }

        public static bool AskQuestion(string msg)
        {
            return AskErrorQuestion(false, msg);
        }

        public static bool AskQuestion(string msg, params object[] args)
        {
            return AskErrorQuestion(false, msg, args);
        }

        public static bool AskErrorQuestion(bool isError, string message)
        {
            DialogResult dialog_result = DialogResult.Yes;
            WPFDoEvents.InvokeInUIThread(() =>
            {
                dialog_result = MessageBox.Show(message, isError ? "Problem" : "Question", MessageBoxButtons.YesNo,
                isError ? MessageBoxIcon.Error : MessageBoxIcon.Question);
            });
            return (dialog_result == DialogResult.Yes);
        }

        public static bool AskErrorQuestion(bool isError, string msg, params object[] args)
        {
            string message = String.Format(msg, args);
            DialogResult dialog_result = DialogResult.Yes;
            WPFDoEvents.InvokeInUIThread(() =>
            {
                dialog_result = MessageBox.Show(message, isError ? "Problem" : "Question", MessageBoxButtons.YesNo,
                isError ? MessageBoxIcon.Error : MessageBoxIcon.Question);
            });
            return (dialog_result == DialogResult.Yes);
        }
    }
}
