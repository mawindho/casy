using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace OLS.Casy.Core
{
    public static class InteractionLogProvider
    {
        private static readonly ConcurrentStack<string> InterActionLog = new ConcurrentStack<string>();

        public static void AddInteraction(string interaction)
        {
            InterActionLog.Push(interaction);
            if (InterActionLog.Count > 20)
            {
                InterActionLog.TryPop(out string _);
            }
        }

        public static IEnumerable<string> InteractionLog => InterActionLog.AsEnumerable();
    }
}
