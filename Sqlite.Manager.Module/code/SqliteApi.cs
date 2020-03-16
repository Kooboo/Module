//Copyright (c) 2018 Yardi Technology Limited. Http://www.kooboo.com 
//All rights reserved.
using Kooboo.Api;
using Kooboo.Sites.Scripting.Interfaces;
using KScript;

namespace Sqlite.Menager.Module.code
{
    public class SqliteApi : RelationalDatabaseApi
    {
        public override string ModelName => "Database";

        public override bool RequireSite => true;

        public override bool RequireUser => false;

        protected override IRelationalDatabase GetDatabase(ApiCall call)
        {
            return new k(call.Context).Sqlite;
        }
    }
}
