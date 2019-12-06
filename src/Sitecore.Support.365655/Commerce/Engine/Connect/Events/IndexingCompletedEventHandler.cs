using Sitecore.Caching;
using Sitecore.Commerce.Engine.Connect.DataProvider;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Sites;
using Sitecore.StringExtensions;
using Sitecore.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.Support.Commerce.Engine.Connect.Events
{
    public class IndexingCompletedEventHandler
    {
        private int _fullCacheRefreshThreshold = 5000;

        public IndexingCompletedEventHandler()
        {
            int threashold;
            if (int.TryParse(Sitecore.Configuration.Settings.GetSetting("Sitecore.Support.365655.FullCacheCleaningThreshold", "5000"), out threashold))
            {
                _fullCacheRefreshThreshold = threashold;
            }
        }

        public virtual void OnIndexingCompleted(object sender, EventArgs e)
        {
            IndexingCompletedEventArgs indexingCompletedEventArgs = null;
            if (e == null || (indexingCompletedEventArgs = e as IndexingCompletedEventArgs) == null)
            {
                return;
            }

            Database database = Factory.GetDatabase(indexingCompletedEventArgs.DatabaseName, assert: false);
            if (database == null)
            {
                return;
            }

            bool reloadMappings = false;

            Log.Info("OnIndexingCompleted - Started for '" + database.Name + "'.", this);
            if (indexingCompletedEventArgs.SitecoreIds.Length <= _fullCacheRefreshThreshold)
            {
                Log.Info("OnIndexingCompleted - Performing incremental cache updates.", this);
                string[] sitecoreIds = indexingCompletedEventArgs.SitecoreIds;
                foreach (string text in sitecoreIds)
                {
                    ID result;
                    if (!ID.TryParse(text, out result))
                    {
                        continue;
                    }

                    Item item = database.GetItem(result);
                    if (item != null)
                    {
                        string sku = CatalogRepository.MappingEntries.FirstOrDefault(m => 
                        m.Key.Contains(result.Guid.ToString("D").ToLowerInvariant())).Value?
                        .Replace("Entity-SellableItem-", string.Empty);

                        var allSitecoreIds = CatalogRepository.MappingEntries
                            .Where(m => m.Value != null && m.Value.Contains(sku)).Select(s => s.Key).ToList();

                        // add also the original id to that list
                        allSitecoreIds.Add(result.Guid.ToString("D").ToLowerInvariant());

                        foreach (var itemIdText in allSitecoreIds)
                        {
                            Log.Info(string.Format("{0} - Removing '{1}' in database:{2} from caches.", "OnIndexingCompleted", result, database.Name), this);
                            CatalogRepository.DefaultCache.RemovePrefix(itemIdText);

                            var itemID = new ID(itemIdText);

                            database.Caches.ItemCache.RemoveItem(itemID);
                            database.Caches.DataCache.RemoveItemInformation(itemID);
                            database.Caches.StandardValuesCache.RemoveKeysContaining(itemIdText);
                            database.Caches.PathCache.RemoveKeysContaining(itemIdText);
                            Item itemForPathCache = database.GetItem(itemID);
                            if (item != null)
                            {
                                database.Caches.ItemPathsCache.Remove(new ItemPathCacheKey(itemForPathCache.Paths.FullPath, itemID));
                            }
                        }

                        this.CleanupHtmlCaches(sku);
                        //SiteInfo site = GetSite(item);
                        //if (site != null)
                        //{
                        //    Log.Info("Using Host name '" + site.HostName + "' with Site '" + site.Name + "' for HTML cache selective refresh", this);
                        //    HtmlCache htmlCache = site.HtmlCache;
                        //    if (htmlCache != null)
                        //    {
                        //        htmlCache.RemoveKeysContaining(item.Name);
                        //        htmlCache.RemoveKeysContaining(text);
                        //    }
                        //}
                    }
                    else
                    {
                        Log.Info(string.Format("{0} - Found new item '{1}'.", "OnIndexingCompleted", result), this);
                        reloadMappings = true;
                    }
                }
                
                if (reloadMappings)
                {
                    Log.Info("OnIndexingCompleted - Updating mapping entries.", this);
                    new CatalogRepository().UpdateMappingEntries(DateTime.UtcNow);
                }
            }
            else
            {
                Log.Info("OnIndexingCompleted - Performing full cache refresh.", this);
                CacheManager.ClearAllCaches();

                Log.Info("OnIndexingCompleted - Updating mapping entries.", this);
                new CatalogRepository().UpdateMappingEntries(DateTime.UtcNow);
            }
        }

        private SiteInfo GetSite(Item item)
        {
            List<SiteInfo> siteInfoList = Factory.GetSiteInfoList();
            SiteInfo result = null;
            try
            {
                foreach (SiteInfo item2 in siteInfoList)
                {
                    string value = "{0}{1}".FormatWith(item2.RootPath, item2.StartItem);
                    if (!string.IsNullOrWhiteSpace(value) && item.Paths.FullPath.StartsWith(value, StringComparison.InvariantCultureIgnoreCase))
                    {
                        result = item2;
                        return result;
                    }
                }

                return result;
            }
            catch (Exception)
            {
                Log.Warn($"Indexing Complete Event Handler - Cannot get SiteInfo for item {item.Name}:{item.ID}", this);
                return result;
            }
        }

        public static void Run(IndexingCompletedEvent evt)
        {
            IndexingCompletedEventArgs args = new IndexingCompletedEventArgs(evt);
            Event.RaiseEvent("indexing:completed:remote", args);
        }

        private void CleanupHtmlCaches(string sku)
        {
            try
            {
                var htmlCaches = CacheManager.GetAllCaches().Where(c => c.Name.Contains("[html]"));
                if (htmlCaches.Any())
                {
                    foreach (var cache in htmlCaches)
                    {
                        var htmlCache = CacheManager.GetHtmlCache(
                            SiteContext.GetSite(cache.Name.Substring(0, cache.Name.IndexOf('['))));
                        htmlCache.RemoveKeysContaining(sku.Replace(" ", "-"));
                    }
                }
            }
            catch (Exception e)
            {
                Sitecore.Diagnostics.Log.Error("RefreshCache for product update throws the exeption", e, this);
            }
        }
    }
}
