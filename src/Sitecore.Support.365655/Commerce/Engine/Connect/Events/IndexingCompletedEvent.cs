using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.Support.Commerce.Engine.Connect.Events
{
    [DataContract]
    public class IndexingCompletedEvent
    {
        [DataMember]
        public string DatabaseName
        {
            get;
            set;
        }

        [DataMember]
        public string[] SitecoreIds
        {
            get;
            set;
        }
    }
}
