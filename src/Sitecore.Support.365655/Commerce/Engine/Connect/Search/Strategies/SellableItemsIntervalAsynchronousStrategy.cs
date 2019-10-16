using Sitecore.Commerce.Engine.Connect.Search;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.ManagedLists;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Support.Commerce.Engine.Connect.Search.Strategies
{
    public class SellableItemsIntervalAsynchronousStrategy : CatalogSystemIntervalAsynchronousStrategyBase<SellableItem>
    {
        public SellableItemsIntervalAsynchronousStrategy(string interval, string database) : base(interval, database)
        {
        }

        protected override ManagedList GetIncrementalEntitiesToIndex(string environment, int skip)
        {
            Assert.ArgumentNotNullOrEmpty(environment, "environment");
            return IndexUtility.GetSellableItemsToIndex(environment, base.IncrementalIndexListName, skip, base.ItemsToTake);
        }
    }
}