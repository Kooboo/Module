using Kooboo.Data.Models;
using Kooboo.Sites.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlEx.Module.code.RelationalDatabase.SchemaStore
{
  public static class StoreService
    {

        public static TableSchemaMappingRepository GetMappingStore(WebSite site)
        {
            var sitedb = site.SiteDb();
            return sitedb.GetSiteRepository<TableSchemaMappingRepository>();  
        }

        

    }
}
