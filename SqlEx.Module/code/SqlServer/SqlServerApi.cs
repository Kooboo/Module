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
using SqlEx.Module.code.RelationalDatabase;

namespace SqlEx.Module.code.SqlServer
{
    public class SqlServerApi : RelationalDatabaseApi<SqlServerCommands>
    {
        public override string DbType => "SqlServer";

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

        internal override string[] GetIndexColumns(IRelationalDatabase db, string table)
        {
            return db.Query($"EXEC Sp_helpindex [{table}]").Select(s => s.obj["index_keys"]).Cast<string>().SelectMany(s => s.Split(',')).ToArray();
        }

        internal override void UpdateIndex(IRelationalDatabase db, string tablename, List<DbTableColumn> columns)
        {
            var cols = columns.Where(w => w.IsIndex).Select(s => s.Name);
            var tableIndexs = db.Query($"EXEC Sp_helpindex [{tablename}]");

            var removed = tableIndexs.Where(w =>
            {
                var keys = w.obj["index_keys"].ToString().Split(',');
                return keys.Any(a => !cols.Contains(a));
            });

            foreach (var item in removed)
            {
                try
                {
                    db.Execute($"DROP INDEX [{item.obj["index_name"]}] ON [{tablename}]");
                }
                catch (Exception)
                {
                }
            }

            foreach (var item in cols)
            {
                try
                {
                    if (tableIndexs.Any(a => a.obj["index_keys"].ToString().Split(',').All(aa => aa == item))) continue;
                    db.GetTable(tablename).createIndex(item);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
