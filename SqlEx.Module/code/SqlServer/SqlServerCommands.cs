﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kooboo.Sites.Models;
using SqlEx.Module.code.RelationalDatabase;

namespace SqlEx.Module.code.SqlServer
{
    public class SqlServerCommands : RelationalDatabaseRawCommands
    {
        public override string ListTables()
        {
            return "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';";
        }

        public override string IsExistTable(string table)
        {
            return "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME = '" + table + "';";
        }

        public override string DbTypeToDataType(string type)
        {
            switch (type.ToLower())
            {
                case "bigint":
                case "decimal":
                case "float":
                case "int":
                case "money":
                case "numeric":
                case "real":
                case "smallint":
                case "smallmoney":
                case "tinyint":
                    return "Number";
                case "bit":
                    return "Bool";
                case "date":
                case "datetime":
                case "datetime2":
                case "datetimeoffset":
                case "smalldatetime":
                case "time":
                case "timestamp":
                    return "DateTime";
                case "binary":
                case "char":
                case "geography":
                case "geometry":
                case "hierarchyid":
                case "image":
                case "nchar":
                case "ntext":
                case "nvarchar":
                case "sql_variant":
                case "sysname":
                case "text":
                case "uniqueidentifier":
                case "varbinary":
                case "varchar":
                case "xml":
                    return "String";
                default:
                    return "String";
            }
        }

        public override string DbTypeToControlType(string type)
        {
            switch (type.ToLower())
            {
                case "bigint":
                case "decimal":
                case "float":
                case "int":
                case "money":
                case "numeric":
                case "real":
                case "smallint":
                case "smallmoney":
                case "tinyint":
                    return "Number";
                case "bit":
                    return "Boolean";
                case "date":
                case "datetime":
                case "datetime2":
                case "datetimeoffset":
                case "smalldatetime":
                case "time":
                case "timestamp":
                    return "DateTime";
                case "binary":
                case "geography":
                case "geometry":
                case "hierarchyid":
                case "image":
                case "sql_variant":
                case "sysname":
                case "xml":
                    return "TextArea";
                case "char":
                case "nchar":
                case "ntext":
                case "nvarchar":
                case "uniqueidentifier":
                case "text":
                case "varbinary":
                case "varchar":
                    return "TextBox";
                default:
                    return "TextBox";
            }
        }

        protected override string CreateTableInternal(string table, List<DbTableColumn> columns)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE [{table}](");

            foreach (var column in columns)
            {
                sb.AppendLine(GenerateColumnDefine(column) + ",");
            }

            sb.Remove(sb.Length - Environment.NewLine.Length - 1, Environment.NewLine.Length + 1);
            sb.AppendLine(");");

            return sb.ToString();
        }

        public override string GetPagedData(string table, int totalskip, int pageSize, string sortfield)
        {
            var orderByDesc = string.IsNullOrWhiteSpace(sortfield) ? "" : $"ORDER BY {sortfield} DESC";
            return "SELECT * FROM " +
                   $"( SELECT ROW_NUMBER () OVER ( {orderByDesc} ) AS RowNum, * FROM {table} ) AS RowConstrainedResult " +
                   $"WHERE RowNum > {totalskip} AND RowNum <= {totalskip + pageSize}" +
                   "ORDER BY RowNum";
        }

        public override string UpdateTable(
            string table,
            List<DbTableColumn> originalColumns,
            List<DbTableColumn> columns)
        {
            var sb = new StringBuilder();

            // add column and update index
            foreach (var column in columns)
            {
                var ori = originalColumns.FirstOrDefault(x => x.Name == column.Name);
                if (ori == null)
                {
                    sb.AppendLine($"ALTER TABLE [{table}] ADD {GenerateColumnDefine(column)};");
                }
                else if (ori.Length != column.Length)
                {
                    sb.AppendLine($"ALTER TABLE [{table}] ALTER COLUMN {GenerateColumnDefine(column)};");
                }
            }

            // remove column
            foreach (var column in originalColumns)
            {
                var keep = columns.FirstOrDefault(x => x.Name == column.Name);
                if (keep != null)
                {
                    continue;
                }

                sb.AppendLine($"ALTER TABLE [{table}] DROP COLUMN [{column.Name}];");
            }

            return sb.ToString();
        }

        private string GenerateColumnDefine(DbTableColumn column)
        {
            string dataType;
            switch (column.DataType.ToLower())
            {
                case "number":
                    dataType = "float";
                    break;
                case "bool":
                    dataType = "bit";
                    break;
                case "datetime":
                    dataType = "datetime";
                    break;
                case "string":
                default:
                    dataType = "nvarchar";
                    break;
            }

            var length = dataType != "nvarchar" ? "" : "(max)";
            return $"[{column.Name}] {dataType}{length}";
        }
    }
}
