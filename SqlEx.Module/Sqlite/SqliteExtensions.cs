using Kooboo.Api;
using Kooboo.Lib.Helper;
using Kooboo.Sites.Models;
using Kooboo.Sites.Scripting.Global.Sqlite;
using Kooboo.Web.ViewModel;
using KScript;
using System;
using System.Collections.Generic;
using System.Linq;
using Cmd = Sqlite.Menager.Module.code.SqliteCommands;

namespace Sqlite.Menager.Module.code
{
    internal static class SqliteExtensions
    {
        internal static SqliteDatabase GetSqliteDatabase(this ApiCall call)
        {
            return new k(call.Context).Sqlite;
        }

        internal static void EnsureSystemTableCreated(this SqliteDatabase db)
        {
            if (!db.IsTableExists(Cmd.KoobooSystemTable))
            {
                db.Execute(Cmd.CreateSystemTable());
            }
        }

        internal static bool IsTableExists(this SqliteDatabase db, string name)
        {
            var tables = db.Query(Cmd.TableExists(name));
            return tables.Any(x => x.Values.Count > 0);
        }

        internal static List<DbTableColumn> GetAllColumns(this SqliteDatabase db, string table)
        {
            var columString = db.GetAllColumnsRow(table);
            return string.IsNullOrWhiteSpace(columString)
                ? new List<DbTableColumn>()
                : JsonHelper.Deserialize<List<DbTableColumn>>(columString);
        }

        internal static List<DatabaseItemEdit> GetAllColumnsForItemEdit(this SqliteDatabase db, string table)
        {
            var columString = db.GetAllColumnsRow(table);
            return string.IsNullOrWhiteSpace(columString)
                ? new List<DatabaseItemEdit>()
                : JsonHelper.Deserialize<List<DatabaseItemEdit>>(columString);
        }

        internal static int GetTotalCount(this SqliteDatabase db, string table)
        {
            var schema = db.Query(Cmd.GetTotalCount(table));
            if (schema.Length <= 0)
            {
                return 0;
            }

            return (int)(long)schema[0].Values["total"];
        }

        internal static Type GetClrType(this DatabaseItemEdit column)
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

        private static string GetAllColumnsRow(this SqliteDatabase db, string table)
        {
            var schema = db.Query(Cmd.GetSchema(table));
            if (schema.Length <= 0)
            {
                return null;
            }

            return (string)schema[0].Values["schema"];
        }
    }
}