using Sitecore.Eventing;
using Sitecore.Events.Hooks;
using Sitecore.Support.Commerce.Engine.Connect.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Support.Commerce.Engine.Connect.Events
{
    public class IndexingCompletedHook : IHook
    {
        public void Initialize()
        {
            EventManager.Subscribe<IndexingCompletedEvent>(IndexingCompletedEventHandler.Run);
        }
    }
}