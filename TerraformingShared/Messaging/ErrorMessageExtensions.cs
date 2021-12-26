using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

#if BelowZero
using TMPro;
#else
using UnityEngine.UI;
#endif

namespace Terraforming.Messaging
{
    static class ErrorMessageExtensions
    {
        public static ErrorMessage._Message pendingMessageToRemove;

        public static ErrorMessage._Message AddReturnMessage(string messageString)
        {
            ErrorMessage.AddMessage(messageString);

            var main = ErrorMessage.main;

            return main.GetExistingMessage(messageString);
        }

        public static ErrorMessage._Message GetExistingMessage(this ErrorMessage errorMessage, string messageString)
        {
            return errorMessage.GetExistingMessage(messageString);
        }

        public static void SetMessageTimeEnd(ErrorMessage._Message message, float timeEnd)
        {
            message.timeEnd = timeEnd;
        }

        public static void AddMessageTimeEnd(ErrorMessage._Message message, float delayTime)
        {
            var messageTimeEnd = message.timeEnd;

            message.timeEnd = messageTimeEnd + delayTime;
        }

        public static float GetMessageTimeEnd(ErrorMessage._Message message)
        {
            return message.timeEnd;
        }

        public static float GetTimeFadeOut()
        {
            var main = ErrorMessage.main;

            return main.timeFadeOut;
        }

        public static float GetTimeInvisible()
        {
            var main = ErrorMessage.main;

            return main.timeInvisible;
        }

        public static void RemoveOffsetY(float offsetToRemove)
        {
            var main = ErrorMessage.main;
            var offsetY = main.offsetY;

            Logger.Debug($"Removing 'offsetY' of {offsetY} by {offsetToRemove}");

            main.offsetY = offsetY - offsetToRemove;
        }

#if BelowZero
        public static TextMeshProUGUI GetMessageEntry(ErrorMessage._Message message)
        {
            return message.entry;
        }
#else
        public static Text GetMessageEntry(ErrorMessage._Message message)
        {
            return message.entry;
        }
#endif
    }
}
