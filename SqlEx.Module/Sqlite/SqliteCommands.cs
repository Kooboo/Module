using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kooboo.Sites.Models;
using Sqlite.Menager.Module.RelationalDatabase;

namespace SqlEx.Module.Sqlite
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

        public override string GetConstrains(string table)
        {
            return $"SELECT name FROM SQLite_master WHERE type = 'index' AND sql IS NOT NULL AND tbl_name = '{table}';";
        }

        protected override string CreateTableInternal(string table, List<DbTableColumn> columns)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE \"{table}\"(");

            var primaryKeys = new List<string>();
            var indexes = new List<string>();
            var uniques = new List<string>();

            foreach (var column in columns)
            {
                if (column.IsPrimaryKey)
                {
                    primaryKeys.Add($"\"{column.Name}\"");
                }
                else if (column.IsUnique)
                {
                    uniques.Add(column.Name);
                }
                else if (column.IsIndex)
                {
                    indexes.Add(column.Name);
                }

                sb.AppendLine(GenerateColumnDefine(column) + ",");
            }

            if (primaryKeys.Any())
            {
                var primaryKeyString = string.Join(", ", primaryKeys);
                sb.AppendLine($"PRIMARY KEY ({primaryKeyString})");
            }

            sb.AppendLine(");");

            foreach (var columnName in indexes)
            {
                sb.AppendLine($"CREATE INDEX {string.Format(IndexNameFormat, table, columnName)} ON {table}({columnName});");
            }

            foreach (var columnName in uniques)
            {
                sb.AppendLine($"CREATE UNIQUE INDEX {string.Format(UniqueNameFormat, table, columnName)} ON {table}({columnName});");
            }

            return sb.ToString();
        }

        public override string GetPagedData(string table, int totalskip, int pageSize, string sortfield)
        {
            var orderByDesc = string.IsNullOrWhiteSpace(sortfield) ? "" : $"ORDER BY {sortfield} DESC";
            return $"SELECT * FROM {table} {orderByDesc} LIMIT {totalskip},{pageSize};";
        }

        public override string UpdateColumn(
            string table,
            List<DbTableColumn> originalColumns,
            List<DbTableColumn> columns,
            DbConstrain[] constraints)
        {
            var sb = new StringBuilder();
            var oldTable = $"_old_{table}_{DateTime.Now:yyyyMMddHHmmss}";

            // drop index
            foreach (var constraint in constraints.Where(x => x.Type == DbConstrain.ConstrainType.Index))
            {
                sb.AppendLine($"DROP INDEX {constraint.Name};");
            }

            // rename table
            sb.AppendLine($"ALTER TABLE {table} RENAME TO {oldTable};");
            // create new table
            sb.AppendLine(CreateTableInternal(table, columns));
            // copy data
            var intersect = originalColumns.Select(x => x.Name).Intersect(columns.Select(x => x.Name)).ToArray();
            if (intersect.Any())
            {
                var cols = string.Join("\",\"", intersect);
                sb.AppendLine($"INSERT INTO {table} (\"{cols}\") SELECT \"{cols}\" FROM {oldTable};");
            }

            // drop old table
            sb.AppendLine($"DROP TABLE {oldTable};");

            return sb.ToString();
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

            var length = column.Length > 0 ? $"({column.Length})" : "";
            return $"\"{column.Name}\" {dataType}{length}";
        }
    }
}
