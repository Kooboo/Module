//Copyright (c) 2018 Yardi Technology Limited. Http://www.kooboo.com 
//All rights reserved.

using Kooboo.Api;
using Kooboo.Sites.Scripting.Interfaces;
using Kooboo.Web.ViewModel;
using KScript;
using System;
using Sqlite.Menager.Module.RelationalDatabase;

namespace Sqlite.Menager.Module.code
{
    public class SqliteApi : RelationalDatabaseApi<SqliteCommands>
    {
        public override string ModelName => "Sqlite";

        public override bool RequireSite => true;

        public override bool RequireUser => false;

        protected override IRelationalDatabase GetDatabase(ApiCall call)
        {
            return new k(call.Context).Sqlite;
        }

        protected override Type GetClrType(DatabaseItemEdit column)
        {
            switch (column.DataType.ToLower())
            {
                case "number":
                    return typeof(double);
                case "bool":
                    return typeof(bool);
                case "datetime":
                case "string":
                default:
                    return typeof(string);
            }
        }
    }
}
