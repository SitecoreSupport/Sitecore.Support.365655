using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Commerce.Engine.Connect;
using Sitecore.Commerce.Engine.Connect.DataProvider;
using Sitecore.Commerce.Engine.Connect.Search;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.ManagedLists;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.Data;
using Sitecore.Eventing;
using Sitecore.Support.Commerce.Engine.Connect.Events;

namespace Sitecore.Support.Commerce.Engine.Connect.Search.Strategies
{
    public abstract class CatalogSystemIntervalAsynchronousStrategyBase<TCatalogEntity> : Sitecore.Commerce.Engine.Connect.Search.Strategies.CatalogSystemIntervalAsynchronousStrategyBase<TCatalogEntity> where TCatalogEntity : CatalogItemBase
    {
        public CatalogSystemIntervalAsynchronousStrategyBase(string interval, string database) : base(interval, database)
        {
        }

        protected override void IndexItems()
        {
            this.LogMessage($"Checking for entities to index in list '{this.IncrementalIndexListName}'.");
            var totalCount = 0;
            var crawledArtifactStores = new List<string>();
            var catalogRepository = new CatalogRepository();

            if (string.IsNullOrWhiteSpace(this.IncrementalIndexListName))
            {
                this.LogDebug($"skipping incremental updates because no {nameof(this.IncrementalIndexListName)} value has been specified.");
                return;
            }

            try
            {
                foreach (var environment in this.Environments)
                {
                    var artifactStoreId = IndexUtility.GetEnvironmentArtifactStoreId(environment);
                    if (crawledArtifactStores.Contains(artifactStoreId))
                    {
                        return;
                    }

                    crawledArtifactStores.Add(artifactStoreId);

                    var allIds = new List<ID>();

                    ManagedList itemsList = null;
                    var targetDatabase = Database.GetDatabase(this.DatabaseName);
                    do
                    {
                        itemsList = this.GetIncrementalEntitiesToIndex(environment, 0);
                        if (itemsList != null && itemsList.Items.Count > 0)
                        {
                            var sitecoreIdList = new List<ID>();
                            var entityIdList = new List<string>();
                            foreach (var entity in itemsList.Items.OfType<TCatalogEntity>())
                            {
                                try
                                {
                                    catalogRepository.UpdateMappingEntries(entity.DateUpdated.Value.UtcDateTime);

                                    if (!string.IsNullOrWhiteSpace(entity.SitecoreId))
                                    {
                                        sitecoreIdList.Add(ID.Parse(entity.SitecoreId));
                                        entityIdList.Add(entity.Id);
                                    }
                                    else
                                    {
                                        this.LogWarning($"No sitecore ID could be identified for the entity '{entity.Id}'.  This item will not be indexed.");
                                        entityIdList.Add(entity.Id);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    this.LogError(ex, $"An unexpected error occurred while indexing entity '{entity.Id}' in list '{this.IncrementalIndexListName}'");
                                }
                            }

                            if (sitecoreIdList.Count > 0)
                            {
                                // ensure this item is removed from sitecore and catalog repository caches.
                                foreach (var sitecoreId in sitecoreIdList)
                                {
                                    CatalogRepository.DefaultCache.RemovePrefix(sitecoreId.Guid.ToString());
                                    EngineConnectUtility.RemoveItemFromSitecoreCaches(sitecoreId);
                                }

                                var indexIdList = sitecoreIdList.Select(id => new SitecoreItemUniqueId(new ItemUri(id, targetDatabase)));
                                IndexCustodian.IncrementalUpdate(this.Index, indexIdList);
                                this.RemoveIncrementalIndexListEntities(environment, this.IncrementalIndexListName, entityIdList);
                                
                                // add modified ids to a single list
                                allIds.AddRange(sitecoreIdList);
                            }

                            totalCount += entityIdList.Count;
                        }
                    } while (itemsList != null && itemsList.Items.Count > 0);

                    if (allIds.Count() > 0)
                    {
                        var indexingCompletedEvent = new IndexingCompletedEvent
                        {
                            DatabaseName = this.DatabaseName,
                            SitecoreIds = allIds.Select(x => x.Guid.ToString()).Distinct().ToArray()
                        };

                        var eventQueue = new DefaultEventQueueProvider();
                        eventQueue.QueueEvent(indexingCompletedEvent, true, true);
                    }
                }
            }
            finally
            {
                this.LogMessage($"indexed {totalCount} entities in list '{this.IncrementalIndexListName}'.");
            }
        }
    }
}
