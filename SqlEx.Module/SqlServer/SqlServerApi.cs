//Copyright (c) 2018 Yardi Technology Limited. Http://www.kooboo.com 
//All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Kooboo.Api;
using Kooboo.Sites.Models;
using Kooboo.Sites.Scripting.Interfaces;
using Kooboo.Web.ViewModel;
using KScript;
using SqlEx.Module.RelationalDatabase;

namespace SqlEx.Module.SqlServer
{
    public class SqlServerApi : RelationalDatabaseApi<SqlServerCommands>
    {
        public override string ModelName => "SqlServer";

        public override bool RequireSite => true;

        public override bool RequireUser => false;

        protected override IRelationalDatabase GetDatabase(ApiCall call)
        {
            return new k(call.Context).SqlServer;
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
                    return typeof(DateTime);
                case "string":
                default:
                    return typeof(string);
            }
        }

        protected override List<List<DataValue>> ConvertDataValue(IDynamicTableObject[] data, List<DbTableColumn> columns)
        {
            return data.Select(x => x.Values.Where(v => v.Key != "RowNum")
                        .Select(kv => new DataValue { key = kv.Key, value = kv.Value }).ToList())
                .ToList();
        }
    }
}
