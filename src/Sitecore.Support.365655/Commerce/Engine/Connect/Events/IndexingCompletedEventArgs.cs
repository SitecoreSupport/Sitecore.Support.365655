using Sitecore.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.Support.Commerce.Engine.Connect.Events
{
    public class IndexingCompletedEventArgs : EventArgs, IPassNativeEventArgs
    {
        private readonly IndexingCompletedEvent _evt;

        public string DatabaseName => _evt.DatabaseName ?? string.Empty;

        public string[] SitecoreIds => _evt.SitecoreIds ?? new string[0];

        public IndexingCompletedEventArgs(IndexingCompletedEvent evt)
        {
            _evt = evt;
        }
    }
}
