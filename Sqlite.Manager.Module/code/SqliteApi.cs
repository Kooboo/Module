//Copyright (c) 2018 Yardi Technology Limited. Http://www.kooboo.com 
//All rights reserved.
using Kooboo.Api;
using Kooboo.Sites.Extensions;
using Kooboo.Sites.Helper;
using Kooboo.Sites.Models;
using Kooboo.Sites.Repository;
using Kooboo.Web.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using KScript;

namespace Sqlite.Menager.Module.code
{
    public class SqliteApi : IApi
    {
        public string ModelName => "Sqlite";

        public bool RequireSite => true;

        public bool RequireUser => false;

        public List<string> Tables(ApiCall call)
        {
            var db = new k(call.Context).Sqlite;
            var tables = db.Query("SELECT name FROM sqlite_master WHERE type='table';");
            return tables.Select(x => (string)x.Values["name"]).ToList();
        }
    }
}
