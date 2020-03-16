using Kooboo.Lib.Helper;
using Kooboo.Sites.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqlite.Menager.Module.RelationalDatabase
{
    public abstract class RelationalDatabaseRawCommands : IRelationalDatabaseRawCommands
    {
        protected const string IndexNameFormat = "idx_{0}_{1}";
        protected const string UniqueNameFormat = "idx_uniq_{0}_{1}";

        public virtual string DbStringTypeName => "TEXT";

        public string KoobooSchemaTable => "_sys_kooboo_schema";

        public abstract string ListTables();

        public abstract string IsExistTable(string table);

        public string CreateSystemTable()
        {
            var columns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "table_name", IsPrimaryKey = true, DataType = DbStringTypeName, Length = 512 },
                new DbTableColumn { Name = "schema", DataType = DbStringTypeName },
            };
            return CreateTableInternal(KoobooSchemaTable, columns);
        }

        public string CreateTableAndSchema(string table, List<DbTableColumn> columns, out object param)
        {
            columns = columns ?? new List<DbTableColumn>();
            EnsureDefaultIdField(columns);
            var sb = new StringBuilder();
            sb.AppendLine(CreateTableInternal(table, columns));
            sb.AppendLine($"INSERT INTO `{KoobooSchemaTable}`(`table_name`, `schema`) VALUES(@tableName, @schema);");
            param = new
            {
                tableName = table,
                schema = JsonHelper.Serialize(columns)
            };

            return sb.ToString();
        }

        public virtual string DeleteTables(string[] tables)
        {
            var drops = string.Join("", tables.Select(table => $"DROP TABLE {table};"));
            var schemas = $"DELETE FROM {KoobooSchemaTable} WHERE table_name in (\"" + string.Join("\",\"", tables) + "\");";
            return drops + "\r\n" + schemas;
        }

        public abstract string UpdateColumn(string table, List<DbTableColumn> originalColumns, List<DbTableColumn> columns);

        public virtual string GetTotalCount(string table)
        {
            return $"SELECT COUNT(1) AS total FROM {table};";
        }

        public abstract string GetPagedData(string table, int totalskip, int pageSize, string sortfield);

        public virtual string DeleteData(string table, List<Guid> ids)
        {
            var idString = string.Join("\",\"", ids);
            return $"DELETE FROM {table} WHERE _id IN (\"{idString}\");";
        }

        public abstract string DbTypeToDataType(string type);

        public abstract string DbTypeToControlType(string type);

        protected void EnsureDefaultIdField(List<DbTableColumn> columns)
        {
            if (columns.All(x => x.Name != Kooboo.IndexedDB.Dynamic.Constants.DefaultIdFieldName))
            {
                columns.Insert(0, GetDefaultIdColumn());
            }
        }

        protected abstract string CreateTableInternal(string table, List<DbTableColumn> columns);

        private DbTableColumn GetDefaultIdColumn()
        {
            return new DbTableColumn
            {
                Name = "_id",
                IsPrimaryKey = true,
                IsIndex = true,
                IsUnique = true,
                DataType = "String",
                IsSystem = true,
                Length = 64
            };
        }
    }
}