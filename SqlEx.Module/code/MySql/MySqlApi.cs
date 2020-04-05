//Copyright (c) 2018 Yardi Technology Limited. Http://www.kooboo.com 
//All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Kooboo.Api;
using Kooboo.Sites.Models;
using Kooboo.Sites.Scripting.Interfaces;
using Kooboo.Web.ViewModel;
using KScript;
using SqlEx.Module.code.RelationalDatabase;

namespace SqlEx.Module.code.MySql
{
    public class MySqlApi : RelationalDatabaseApi<MySqlCommands>
    {
        public override string DbType => "MySql";

        public override bool RequireSite => true;

        public override bool RequireUser => false;

        protected override IRelationalDatabase GetDatabase(ApiCall call)
        {
            return new k(call.Context).Mysql;
        }

        protected override bool IsExistTable(IRelationalDatabase db, string name)
        {
            using (var conn = db.SqlExecuter.CreateConnection())
            {
                var cmd = Cmd.IsExistTable(name, out var param);
                var dbName = conn.Database;
                var exists = conn.ExecuteScalar<bool>(string.Format(cmd, dbName), param);
                return exists;
            }
        }

        protected override List<string> ListTables(IRelationalDatabase db)
        {
            using (var conn = db.SqlExecuter.CreateConnection())
            {
                var cmd = Cmd.ListTables();
                var dbName = conn.Database;
                return conn.Query<string>(string.Format(cmd, dbName)).ToList();
            }
        }

        protected override List<List<DataValue>> ConvertDataValue(IDynamicTableObject[] data, List<DbTableColumn> columns)
        {
            var bools = columns.Where(c => c.DataType.ToLower() == "bool").ToArray();
            return data
                .Select(x => x.Values.Select(kv =>
                {
                    if (kv.Value != null)
                    {
                        var value = bools.Any(b => b.Name == kv.Key)
                            ? Convert.ChangeType(kv.Value, typeof(bool))
                            : kv.Value;
                        return new DataValue { key = kv.Key, value = value };
                    }

                    return new DataValue { key = kv.Key, value = null };
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
                    return typeof(DateTime);
                case "string":
                default:
                    return typeof(string);
            }
        }

        internal override string[] GetIndexColumns(IRelationalDatabase db, string table)
        {
            return db.Query($"show index from `{table}`").Select(s => s.obj["Column_name"]).Cast<string>().ToArray();
        }

        internal override void UpdateIndex(IRelationalDatabase db, string tablename, List<DbTableColumn> columns)
        {
            var cols = columns.Where(w => w.IsIndex).Select(s => s.Name);
            var tableIndexs = db.Query($"show index from `{tablename}`");
            var removed = tableIndexs.Where(w => !cols.Contains(w.obj["Column_name"]));

            foreach (var item in removed)
            {
                try
                {
                    db.Execute($"DROP INDEX `{item.obj["Key_name"]}` ON `{tablename}`");
                }
                catch (Exception)
                {
                }
            }

            foreach (var item in cols)
            {
                try
                {
                    db.GetTable(tablename).createIndex(item);
                }
                catch (Exception)
                {
                    try
                    {

                        if (tableIndexs.Any(a => a.obj["Column_name"].ToString() == item)) continue;
                        db.Execute($"create fulltext index `{item}` on `{tablename}`(`{item}`)");
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
    }
}
