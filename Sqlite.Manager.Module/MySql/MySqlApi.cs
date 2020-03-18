//Copyright (c) 2018 Yardi Technology Limited. Http://www.kooboo.com 
//All rights reserved.

using Dapper;
using Kooboo.Api;
using Kooboo.Sites.Scripting.Interfaces;
using Kooboo.Web.ViewModel;
using KScript;
using Sqlite.Menager.Module.RelationalDatabase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sqlite.Menager.Module.MySql
{
    public class MySqlApi : RelationalDatabaseApi<MySqlCommands>
    {
        public override string ModelName => "MySql";

        public override bool RequireSite => true;

        public override bool RequireUser => false;

        protected override IRelationalDatabase GetDatabase(ApiCall call)
        {
            return new k(call.Context).Mysql;
        }

        protected override DbConstrain[] GetConstrains(IRelationalDatabase db, string table)
        {
            using (var conn = db.SqlExecuter.CreateConnection())
            {
                var cmd = Cmd.GetConstrains(table);
                var dbName = conn.Database;
                return conn.Query<DbConstrain>(string.Format(cmd, dbName)).ToArray();
            }
        }

        protected override bool IsExistTable(IRelationalDatabase db, string name)
        {
            using (var conn = db.SqlExecuter.CreateConnection())
            {
                var cmd = Cmd.IsExistTable(name);
                var dbName = conn.Database;
                var exists = conn.ExecuteScalar<bool>(string.Format(cmd, dbName));
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
    }
}
