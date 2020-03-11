//Copyright (c) 2018 Yardi Technology Limited. Http://www.kooboo.com 
//All rights reserved.
using Kooboo.Api;
using Kooboo.Lib.Helper;
using Kooboo.Sites.Models;
using Kooboo.Sites.Scripting.Global.Sqlite;
using KScript;
using System;
using System.Collections.Generic;
using System.Linq;
using Cmd = Sqlite.Menager.Module.code.SqliteCommands;

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
            var tables = db.Query(Cmd.ListTables());
            return tables.Select(x => (string)x.Values["name"]).ToList();
        }

        public void CreateTable(string name, ApiCall call)
        {
            if (!Kooboo.IndexedDB.Helper.CharHelper.IsValidTableName(name))
            {
                throw new Exception(Kooboo.Data.Language.Hardcoded.GetValue("Only Alphanumeric are allowed to use as a table", call.Context));
            }

            var db = new k(call.Context).Sqlite;
            EnsureSystemTableCreated(db);

            // create table
            db.Execute(Cmd.CreateTable(name));
            // add schema
            db.GetTable(Cmd.KoobooSystemTable).add(new Dictionary<string, object>
            {
                { "table_name", name },
                { "schema", Cmd.DefaultIdSchema },
            });
        }

        public void DeleteTables(string names, ApiCall call)
        {
            var db = new k(call.Context).Sqlite;
            var tables = JsonHelper.Deserialize<string[]>(names);
            if (tables.Length <= 0)
            {
                return;
            }

            db.Execute(Cmd.DeleteTables(tables));
        }

        public bool IsUniqueTableName(string name, ApiCall call)
        {
            var db = new k(call.Context).Sqlite;
            return !IsTableExists(db, name);
        }

        public List<DbTableColumn> Columns(string table, ApiCall call)
        {
            var db = new k(call.Context).Sqlite;
            var columns = GetAllColumns(table, db);
            return columns.Where(x =>
                    x.Name != Kooboo.IndexedDB.Dynamic.Constants.DefaultIdFieldName &&
                    x.Name != Cmd.KoobooSystemTable)
                .ToList();
        }

        public void UpdateColumn(string tablename, List<DbTableColumn> columns, ApiCall call)
        {
            var db = new k(call.Context).Sqlite;

            var originalColumns = GetAllColumns(tablename, db);

            // update table
            db.Execute(Cmd.UpdateColumn(tablename, originalColumns, columns));

            // update schema
            db.Execute(Cmd.UpdateSchema(tablename, columns, out var param), param);
        }

        private void EnsureSystemTableCreated(SqliteDatabase db)
        {
            if (!IsTableExists(db, Cmd.KoobooSystemTable))
            {
                db.Execute(Cmd.CreateSystemTable());
            }
        }

        private bool IsTableExists(SqliteDatabase db, string name)
        {
            var tables = db.Query(Cmd.TableExists(name));
            return tables.Any(x => x.Values.Count > 0);
        }

        private static List<DbTableColumn> GetAllColumns(string table, SqliteDatabase db)
        {
            var schema = db.Query(Cmd.GetSchema(table));
            if (schema.Length <= 0)
            {
                return new List<DbTableColumn>();
            }

            var columString = (string)schema[0].Values["schema"];
            var columns = JsonHelper.Deserialize<List<DbTableColumn>>(columString);
            return columns;
        }
    }
}
