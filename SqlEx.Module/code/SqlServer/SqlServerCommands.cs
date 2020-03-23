using System;
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

        public override string GetConstrains(string table)
        {
            return $@"
SELECT 
     [Table] = t.name,
     [Name] = ind.name,
     [Column] = col.name,
     (CASE WHEN ind.is_primary_key = 0 THEN 'Index'	ELSE 'PrimaryKey' END) as [Type]
FROM 
     sys.indexes ind 
INNER JOIN 
     sys.index_columns ic ON  ind.object_id = ic.object_id and ind.index_id = ic.index_id 
INNER JOIN 
     sys.columns col ON ic.object_id = col.object_id and ic.column_id = col.column_id 
INNER JOIN 
     sys.tables t ON ind.object_id = t.object_id 
WHERE 
     t.is_ms_shipped = 0 
		 AND t.name = '{table}'
UNION
SELECT
     [Table] = t.name,
     [Name] = dc.name,
     [Column] = col.name,
     'DefaultConstraint' as [Type]
FROM
     sys.tables t 
INNER JOIN 
     sys.default_constraints dc ON dc.parent_object_id = t.object_id
INNER JOIN 
     sys.columns col ON dc.parent_object_id = col.object_id and dc.parent_column_id = col.column_id 
WHERE
     t.name = '{table}'";
        }

        protected override string CreateTableInternal(string table, List<DbTableColumn> columns)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE [{table}](");

            var primaryKeys = new List<string>();
            var indexes = new List<DbTableColumn>();
            var uniques = new List<DbTableColumn>();

            foreach (var column in columns)
            {
                if (column.IsPrimaryKey)
                {
                    primaryKeys.Add($"{column.Name}");
                }
                else if (column.IsUnique)
                {
                    uniques.Add(column);
                }
                else if (column.IsIndex)
                {
                    indexes.Add(column);
                }

                sb.AppendLine(GenerateColumnDefine(column) + ",");
            }

            if (primaryKeys.Any())
            {
                var pkName = GetPrimaryKeyConstrainName(table, primaryKeys);
                var primaryKeyColumns = string.Join("], [", primaryKeys);
                sb.AppendLine($"CONSTRAINT {pkName} PRIMARY KEY ([{primaryKeyColumns}]),");
            }

            sb.Remove(sb.Length - Environment.NewLine.Length - 1, Environment.NewLine.Length + 1);
            sb.AppendLine(");");

            foreach (var column in indexes)
            {
                sb.AppendLine($"CREATE INDEX [{string.Format(IndexNameFormat, table, column.Name)}] ON [{table}]({column.Name});");
            }

            foreach (var column in uniques)
            {
                sb.AppendLine($"CREATE UNIQUE INDEX [{string.Format(IndexNameFormat, table, column.Name)}] ON [{table}]({column.Name});");
            }

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

        public override string UpdateColumn(
            string table,
            List<DbTableColumn> originalColumns,
            List<DbTableColumn> columns,
            DbConstrain[] constraints)
        {
            var sb = new StringBuilder();

            // add column and update index
            foreach (var column in columns)
            {
                var ori = originalColumns.FirstOrDefault(x => x.Name == column.Name);
                string alterColumn = null;
                if (ori == null)
                {
                    sb.AppendLine($"ALTER TABLE [{table}] ADD {GenerateColumnDefine(column)};");
                }
                else if (ori.Length != column.Length)
                {
                    alterColumn = $"ALTER TABLE [{table}] ALTER COLUMN {GenerateColumnDefine(column)};{Environment.NewLine}";
                }

                sb.Append(GenerateRemoveIndex(ori ?? new DbTableColumn(), column, table, constraints));
                sb.Append(alterColumn);
                sb.Append(GenerateAddIndex(ori ?? new DbTableColumn(), column, table));
            }

            // update primary key
            var oriPrimaryKey = originalColumns.Where(x => x.IsPrimaryKey).Select(x => x.Name).ToArray();
            var newPrimaryKey = columns.Where(x => x.IsPrimaryKey).Select(x => x.Name).ToArray();
            string addPrimaryKey = null;
            if (oriPrimaryKey.Length != newPrimaryKey.Length || oriPrimaryKey.Except(newPrimaryKey).Any())
            {
                var oriPkName = constraints.First(x => x.Type == DbConstrain.ConstrainType.PrimaryKey);
                var newPkName = GetPrimaryKeyConstrainName(table, newPrimaryKey);
                var primaryKeys = string.Join("], [", newPrimaryKey);
                sb.AppendLine($"ALTER TABLE [{table}] DROP CONSTRAINT {oriPkName.Name};");
                addPrimaryKey = $"ALTER TABLE [{table}] ADD CONSTRAINT {newPkName} PRIMARY KEY ([{primaryKeys}]);{Environment.NewLine}";
            }

            // remove column
            foreach (var column in originalColumns)
            {
                var keep = columns.FirstOrDefault(x => x.Name == column.Name);
                if (keep != null)
                {
                    continue;
                }

                AppendDropColumn(sb, column, table, constraints);
            }

            sb.Append(addPrimaryKey);
            return sb.ToString();
        }

        private static string GetPrimaryKeyConstrainName(string table, IEnumerable<string> primaryKeys)
        {
            return $"PK_{table}_{string.Join("_", primaryKeys)}";
        }

        private void AppendDropColumn(StringBuilder sb, DbTableColumn column, string table, IEnumerable<DbConstrain> constraints)
        {
            var columnConstraints = constraints
                .Where(x => x.Type != DbConstrain.ConstrainType.PrimaryKey &&
                            x.Column.Equals(column.Name, StringComparison.OrdinalIgnoreCase));

            foreach (var constraint in columnConstraints)
            {
                if (constraint.Type == DbConstrain.ConstrainType.Index)
                {
                    sb.AppendLine($"DROP INDEX [{table}].[{constraint.Name}];");
                }
                else if (constraint.Type == DbConstrain.ConstrainType.DefaultConstraint)
                {
                    sb.AppendLine($"ALTER TABLE [{table}] DROP CONSTRAINT [{constraint.Name}];");
                }
            }

            sb.AppendLine($"ALTER TABLE [{table}] DROP COLUMN [{column.Name}];");
        }

        private string GenerateRemoveIndex(
            DbTableColumn originalColumn,
            DbTableColumn newColumn,
            string table,
            DbConstrain[] constraints)
        {
            if (originalColumn.IsPrimaryKey != newColumn.IsPrimaryKey)
            {
                return null;
            }

            if (originalColumn.IsUnique && !newColumn.IsUnique)
            {
                // remove unique index
                return RemoveIndex();
            }

            if (originalColumn.IsIndex && !newColumn.IsIndex)
            {
                // remove index
                return RemoveIndex();
            }

            return null;

            string RemoveIndex()
            {
                var constraint = constraints.First(x =>
                    x.Type == DbConstrain.ConstrainType.Index &&
                    x.Column.Equals(originalColumn.Name, StringComparison.OrdinalIgnoreCase));
                return $"DROP INDEX [{table}].[{constraint.Name}];{Environment.NewLine}";
            }
        }

        private string GenerateAddIndex(DbTableColumn originalColumn, DbTableColumn newColumn, string table)
        {
            if (originalColumn.IsPrimaryKey != newColumn.IsPrimaryKey)
            {
                return null;
            }

            if (originalColumn.IsUnique && !newColumn.IsUnique && newColumn.IsIndex)
            {
                return AddIndex();
            }

            if (!originalColumn.IsUnique && newColumn.IsUnique)
            {
                // add unique index
                return $"CREATE UNIQUE INDEX [{string.Format(UniqueNameFormat, table, newColumn.Name)}] ON [{table}]({newColumn.Name});{Environment.NewLine}";
            }

            if (!originalColumn.IsIndex && newColumn.IsIndex)
            {
                // add index
                return AddIndex();
            }

            return null;

            string AddIndex()
            {
                return $"CREATE INDEX [{string.Format(IndexNameFormat, table, newColumn.Name)}] ON [{table}]({newColumn.Name});{Environment.NewLine}";
            }
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

            if (column.IsIncremental && column.IsPrimaryKey)
            {
                return $"[{column.Name}] int IDENTITY({column.Seed},{column.Scale}) NOT NULL";
            }

            if (dataType != "nvarchar")
            {
                return $"[{column.Name}] {dataType}";
            }

            var length = column.Length > 0 && column.Length <= 4000 ? $"({column.Length})" : "(max)";
            var notNull = column.IsPrimaryKey ? " NOT NULL" : "";
            return $"[{column.Name}] {dataType}{length}{notNull}";
        }
    }
}
