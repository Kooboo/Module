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

namespace SqlEx.Module.code.Sqlite
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

        protected override DbConstrain[] GetConstrains(IRelationalDatabase db, string table)
        {
            return db.Query(Cmd.GetConstrains(table))
                .Select(x => new DbConstrain
                {
                    Table = table,
                    Name = (string)x.Values["name"],
                    Type = DbConstrain.ConstrainType.Index
                })
                .ToArray();
        }

        protected override List<List<DataValue>> ConvertDataValue(IDynamicTableObject[] data, List<DbTableColumn> columns)
        {
            var bools = columns.Where(c => c.DataType.ToLower() == "bool").ToArray();
            return data
                .Select(x => x.Values.Select(kv =>
                {
                    var value = bools.Any(b => b.Name == kv.Key)
                        ? Convert.ChangeType(kv.Value, typeof(bool))
                        : kv.Value;
                    return new DataValue { key = kv.Key, value = value };
                }).ToList())
                .ToList();
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
