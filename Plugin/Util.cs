/*
Trajectories
Copyright 2014, Youen Toupin

This file is part of Trajectories, under MIT license.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Trajectories
{
    static class Util
    {
        private static Dictionary<string, ScreenMessage> messages = new Dictionary<string, ScreenMessage>();

        public static void PostSingleScreenMessage(string id, string message)
        {
            if (messages.ContainsKey(id))
                ScreenMessages.RemoveMessage(messages[id]);
            messages[id] = ScreenMessages.PostScreenMessage(message);
        }
    }
}
