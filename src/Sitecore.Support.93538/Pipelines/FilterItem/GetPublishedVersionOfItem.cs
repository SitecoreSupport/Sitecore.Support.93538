using Sitecore.Common;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Pipelines.FilterItem;
using Sitecore.SecurityModel;
using Sitecore.Sites;
using Sitecore.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Support.Pipelines.FilterItem
{
    internal class GetPublishedVersionOfItem
    {
        // Methods
        private static Item GetPublishableVersion(Item item, DateTime date, Data.Version requestedVersion, bool requireApprovedVersion)
        {
            if (!item.Publishing.IsPublishable(date, true))
            {
                return null;
            }
            if (item.Publishing.IsValid(date, requireApprovedVersion))
            {
                return item;
            }
            if (requestedVersion != Data.Version.Latest)
            {
                return null;
            }
            return item.Publishing.GetValidVersion(date, requireApprovedVersion);
        }

        public void Process(FilterItemPipelineArgs args)
        {
            SiteContext site = Context.Site;
            if (site != null)
            {
                using (new SecurityDisabler())
                {
                    site.DisableFiltering = true;
                    try
                    {
                        Item item = Publish(args.Item, site.DisplayDate, args.RequestedVersion, args.RequireApprovedVersion);
                        if (item == null)
                        {
                            args.FilteredItem = null;
                        }
                        else
                        {
                            UpdateValidPeriod(item, site.DisplayDate);
                            args.FilteredItem = item;
                        }
                    }
                    finally
                    {
                        site.DisableFiltering = false;
                    }
                }
            }
        }

        private static Item Publish(Item item, DateTime displayDate, Data.Version requestedVersion, bool requireApprovedVersion)
        {
            Item item2 = GetPublishableVersion(item, displayDate, requestedVersion, requireApprovedVersion);
            if (item2 == null)
            {
                return null;
            }
            Replacer replacer = Factory.GetReplacer("publish");
            if (replacer != null&&!IsEditMode())
            {
                item2.RuntimeSettings.Temporary = true;
                replacer.ReplaceFieldValues(item2);
            }
            return item2;
        }

        private static void UpdateValidPeriod(Item item, DateTime displayDate)
        {
            DateTimeRange visibleInterval = item.Publishing.GetVisibleInterval(displayDate, true);
            item.RuntimeSettings.ValidFrom = visibleInterval.From;
            item.RuntimeSettings.ValidTo = visibleInterval.To;
        }

        private static bool IsEditMode()
        {
            bool flag = false;
            SiteContext site = Context.Site;
            if ((site != null) && (site.DisplayMode == DisplayMode.Edit))
            {
                flag = true;
            }
            return flag;
        }
    }
}