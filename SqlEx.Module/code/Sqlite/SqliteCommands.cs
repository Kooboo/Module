using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kooboo.Sites.Models;
using SqlEx.Module.code.RelationalDatabase;

namespace SqlEx.Module.code.Sqlite
{
    public class SqliteCommands : RelationalDatabaseRawCommands
    {
        public override string ListTables()
        {
            return "SELECT name FROM sqlite_master WHERE type='table';";
        }

        public override string IsExistTable(string table)
        {
            return $"SELECT name FROM sqlite_master WHERE type='table' and name='{table}' LIMIT 1";
        }

        public override string DbTypeToDataType(string type)
        {
            if (type.StartsWith("REAL") || type.StartsWith("INTEGER"))
            {
                return "number";
            }

            return "string";
        }

        public override string DbTypeToControlType(string type)
        {
            if (type.StartsWith("REAL") || type.StartsWith("INTEGER"))
            {
                return "Number";
            }

            return "TextBox";
        }

        public string GetSqls(string table)
        {
            return $"SELECT type, name, sql FROM SQLite_master WHERE sql IS NOT NULL AND tbl_name = '{table}';";
        }

        public override string GetPagedData(string table, int totalskip, int pageSize, string sortfield)
        {
            var orderByDesc = string.IsNullOrWhiteSpace(sortfield) ? "" : $"ORDER BY {sortfield} DESC";
            return $"SELECT * FROM {table} {orderByDesc} LIMIT {totalskip},{pageSize};";
        }

        public override string UpdateTable(string table, List<DbTableColumn> originalColumns, List<DbTableColumn> columns)
        {
            throw new NotImplementedException();
        }

        private string GenerateColumnDefine(DbTableColumn column)
        {
            string dataType;
            switch (column.DataType.ToLower())
            {
                case "number":
                    dataType = "REAL";
                    break;
                case "bool":
                    dataType = "INTEGER";
                    break;
                case "string":
                case "datetime":
                default:
                    dataType = "TEXT";
                    break;
            }

            return $"\"{column.Name}\" {dataType}";
        }
    }
}
