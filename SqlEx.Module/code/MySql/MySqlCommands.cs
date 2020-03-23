using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kooboo.Sites.Models;
using SqlEx.Module.code.RelationalDatabase;

namespace SqlEx.Module.code.MySql
{
    public class MySqlCommands : RelationalDatabaseRawCommands
    {
        public override string ListTables()
        {
            return "SELECT TABLE_NAME FROM information_schema.tables WHERE TABLE_SCHEMA='{0}' AND TABLE_TYPE = 'BASE TABLE';";
        }

        public override string IsExistTable(string table)
        {
            return "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE TABLE_SCHEMA='{0}' AND TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME = '" + table + "');";
        }

        public override string DbTypeToDataType(string type)
        {
            switch (type.ToLower())
            {
                case "bigint":
                case "decimal":
                case "double":
                case "float":
                case "int":
                case "integer":
                case "mediumint":
                case "numaric":
                case "real":
                case "smallint":
                case "tinyint":
                    return "Number";
                case "bit":
                    return "Bool";
                case "date":
                case "datetime":
                case "time":
                case "timestamp":
                case "year":
                    return "DateTime";
                case "binary":
                case "blob":
                case "char":
                case "enum":
                case "geometry":
                case "geometrycollection":
                case "json":
                case "linestring":
                case "longblob":
                case "longtext":
                case "mediumblob":
                case "mediumtext":
                case "multilinestring":
                case "multipoint":
                case "multipolygon":
                case "point":
                case "polygon":
                case "set":
                case "text":
                case "tinyblob":
                case "tinytext":
                case "varbinary":
                case "varchar":
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
                case "double":
                case "float":
                case "int":
                case "integer":
                case "mediumint":
                case "numaric":
                case "real":
                case "smallint":
                case "tinyint":
                    return "Number";
                case "bit":
                    return "Boolean";
                case "date":
                case "datetime":
                case "time":
                case "timestamp":
                case "year":
                    return "DateTime";
                case "binary":
                case "blob":
                case "enum":
                case "geometry":
                case "geometrycollection":
                case "json":
                case "linestring":
                case "longblob":
                case "mediumblob":
                case "mediumtext":
                case "multilinestring":
                case "multipoint":
                case "multipolygon":
                case "point":
                case "polygon":
                case "tinyblob":
                case "set":
                case "longtext":
                case "varbinary":
                    return "TextArea";
                case "char":
                case "text":
                case "tinytext":
                case "varchar":
                    return "TextBox";
                default:
                    return "TextBox";
            }
        }

        public override string GetConstrains(string table)
        {
            return @"
SELECT
	s.TABLE_NAME AS `TABLE`,
	s.INDEX_NAME AS `NAME`,
	s.COLUMN_NAME AS `COLUMN`,
	( CASE WHEN s.INDEX_Name = 'PRIMARY' THEN TRUE ELSE FALSE END ) AS `IsPrimaryKey`,
	( CASE WHEN s.INDEX_Name = 'PRIMARY' THEN 'PrimaryKey' ELSE 'Index' END ) AS `Type`
FROM
	INFORMATION_SCHEMA.STATISTICS s 
WHERE
	s.TABLE_SCHEMA = '{0}' 
	AND s.TABLE_NAME = '" + table + "';";
        }

        protected override string CreateTableInternal(string table, List<DbTableColumn> columns)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE `{table}`(");

            var primaryKeys = new List<string>();
            var indexes = new List<DbTableColumn>();
            var uniques = new List<DbTableColumn>();

            foreach (var column in columns)
            {
                if (column.IsPrimaryKey)
                {
                    primaryKeys.Add($"`{column.Name}`");
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
                var primaryKeyString = string.Join(", ", primaryKeys);
                sb.AppendLine($"PRIMARY KEY ({primaryKeyString}),");
            }

            foreach (var column in indexes)
            {
                var length = GetIndexLength(column);
                sb.AppendLine($"INDEX `{string.Format(IndexNameFormat, table, column.Name)}`({column.Name}{length}),");
            }

            foreach (var column in uniques)
            {
                var length = GetIndexLength(column);
                sb.AppendLine($"UNIQUE INDEX `{string.Format(IndexNameFormat, table, column.Name)}`({column.Name}{length}),");
            }

            sb.Remove(sb.Length - Environment.NewLine.Length - 1, Environment.NewLine.Length + 1);
            sb.AppendLine(");");
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
            sb.AppendLine($"ALTER TABLE `{table}`");

            // add column
            foreach (var column in columns)
            {
                var ori = originalColumns.FirstOrDefault(x => x.Name == column.Name);
                if (ori == null)
                {
                    sb.AppendLine($"ADD {GenerateColumnDefine(column)},");
                }

                var index = GenerateUpdateIndex(ori ?? new DbTableColumn(), column, table, constraints);
                if (index != null)
                {
                    sb.AppendLine(index);
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

                sb.AppendLine(DropColumn(column, constraints));
            }

            // update primary key
            var oriPrimaryKey = originalColumns.Where(x => x.IsPrimaryKey).Select(x => x.Name).ToArray();
            var newPrimaryKey = columns.Where(x => x.IsPrimaryKey).Select(x => x.Name).ToArray();
            if (oriPrimaryKey.Length != newPrimaryKey.Length || oriPrimaryKey.Except(newPrimaryKey).Any())
            {
                var primaryKeys = string.Join("`, `", newPrimaryKey);
                sb.AppendLine("DROP PRIMARY KEY, " +
                              $"ADD PRIMARY KEY (`{primaryKeys}`),");
            }

            sb.Remove(sb.Length - Environment.NewLine.Length - 1, Environment.NewLine.Length + 1);
            sb.AppendLine(";");
            return sb.ToString();
        }

        private string DropColumn(DbTableColumn column, IEnumerable<DbConstrain> constraints)
        {
            var columnIndexes = constraints
                .Where(x => x.Type == DbConstrain.ConstrainType.Index &&
                            x.Column.Equals(column.Name, StringComparison.OrdinalIgnoreCase));
            return string.Join(Environment.NewLine, columnIndexes.Select(x => $"DROP INDEX {x.Name},")) +
                   Environment.NewLine +
                   $"DROP COLUMN `{column.Name}`,";
        }

        private string GenerateUpdateIndex(
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
                var index = RemoveIndex();
                if (newColumn.IsIndex)
                {
                    index += "\r\n" + AddIndex();
                }

                return index;
            }

            if (!originalColumn.IsUnique && newColumn.IsUnique)
            {
                // add unique index
                var length = GetIndexLength(newColumn);
                return $"ADD UNIQUE INDEX {string.Format(UniqueNameFormat, table, newColumn.Name)}({newColumn.Name}{length}),";
            }

            if (originalColumn.IsIndex && !newColumn.IsIndex)
            {
                // remove index
                return RemoveIndex();
            }

            if (!originalColumn.IsIndex && newColumn.IsIndex)
            {
                // add index
                return AddIndex();
            }

            return null;

            string AddIndex()
            {
                var length = GetIndexLength(newColumn);
                return $"ADD INDEX {string.Format(IndexNameFormat, table, newColumn.Name)}({newColumn.Name}{length}),";
            }

            string RemoveIndex()
            {
                var constraint = constraints.First(x =>
                    x.Type == DbConstrain.ConstrainType.Index &&
                    x.Column.Equals(originalColumn.Name, StringComparison.OrdinalIgnoreCase));
                return $"DROP INDEX {constraint.Name},";
            }
        }

        private string GenerateColumnDefine(DbTableColumn column)
        {
            string dataType;
            switch (column.DataType.ToLower())
            {
                case "number":
                    dataType = "double";
                    break;
                case "bool":
                    dataType = "bit";
                    break;
                case "datetime":
                    dataType = "datetime";
                    break;
                case "string":
                default:
                    dataType = "varchar";
                    break;
            }

            if (column.IsIncremental)
            {
                return $"`{column.Name}` int(11) NOT NULL AUTO_INCREMENT";
            }

            if (dataType != "varchar")
            {
                return $"`{column.Name}` {dataType}";
            }

            if (column.IsPrimaryKey && column.Length > 512)
            {
                column.Length = 512;
            }

            var length = column.Length > 0 ? $"({column.Length})" : "(10240)";
            return $"`{column.Name}` {dataType}{length}";
        }

        private static string GetIndexLength(DbTableColumn column)
        {
            if (column.DataType.ToLower() == "string")
            {
                return column.Length <= 512 ? "" : "(512)";
            }

            return "";
        }
    }
}
