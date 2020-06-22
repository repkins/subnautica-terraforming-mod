using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Terraforming.Messaging
{
    static class ErrorMessageExtensions
    {
        private static readonly MethodInfo GetExistingMessageMethod = typeof(ErrorMessage).GetMethod("GetExistingMessage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo mainField = typeof(ErrorMessage).GetField("main", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo timeFadeOutField = typeof(ErrorMessage).GetField("timeFadeOut", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo timeInvisibleField = typeof(ErrorMessage).GetField("timeInvisible", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo offsetYField = typeof(ErrorMessage).GetField("offsetY", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly Type MessageClass = typeof(ErrorMessage).GetNestedType("_Message", BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo messageTimeEndField = MessageClass.GetField("timeEnd");
        private static readonly FieldInfo messageEntryField = MessageClass.GetField("entry");

        public static object pendingMessageToRemove;

        public static object AddReturnMessage(string messageString)
        {
            ErrorMessage.AddMessage(messageString);

            var main = mainField.GetValue(null) as ErrorMessage;

            return main.GetExistingMessage(messageString);
        }

        public static object GetExistingMessage(this ErrorMessage errorMessage, string messageString)
        {
            return GetExistingMessageMethod.Invoke(errorMessage, new object[] { messageString });
        }

        public static void SetMessageTimeEnd(object message, float timeEnd)
        {
            messageTimeEndField.SetValue(message, timeEnd);
        }

        public static void AddMessageTimeEnd(object message, float delayTime)
        {
            var messageTimeEnd = (float)messageTimeEndField.GetValue(message);

            messageTimeEndField.SetValue(message, messageTimeEnd + delayTime);
        }

        public static float GetMessageTimeEnd(object message)
        {
            return (float)messageTimeEndField.GetValue(message);
        }

        public static float GetTimeFadeOut()
        {
            var main = mainField.GetValue(null) as ErrorMessage;

            return (float)timeFadeOutField.GetValue(main);
        }

        public static float GetTimeInvisible()
        {
            var main = mainField.GetValue(null) as ErrorMessage;

            return (float)timeInvisibleField.GetValue(main);
        }

        public static void RemoveOffsetY(float offsetToRemove)
        {
            var main = mainField.GetValue(null) as ErrorMessage;
            var offsetY = (float)offsetYField.GetValue(main);

            Logger.Debug($"Removing 'offsetY' of {offsetY} by {offsetToRemove}");

            offsetYField.SetValue(main, offsetY - offsetToRemove);
        }

        public static Text GetMessageEntry(object message)
        {
            return messageEntryField.GetValue(message) as Text;
        }
    }
}
