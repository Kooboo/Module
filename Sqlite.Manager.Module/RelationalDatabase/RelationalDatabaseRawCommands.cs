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

        public abstract string ListTables();

        public abstract string IsExistTable(string table);

        public string CreateTable(string table, List<DbTableColumn> columns)
        {
            columns = columns ?? new List<DbTableColumn>();
            if (columns.All(x => x.Name != Kooboo.IndexedDB.Dynamic.Constants.DefaultIdFieldName))
            {
                columns.Insert(0, GetDefaultIdColumn());
            }

            var sb = new StringBuilder();
            sb.AppendLine(CreateTableInternal(table, columns));

            return sb.ToString();
        }

        public virtual string DeleteTables(string[] tables, char quotationLeft, char quotationRight)
        {
            return string.Join("", tables.Select(table => $"DROP TABLE {quotationLeft}{table}{quotationRight};"));
        }

        public abstract string UpdateColumn(
            string table,
            List<DbTableColumn> originalColumns,
            List<DbTableColumn> columns,
            DbConstrain[] constraints);

        public virtual string GetTotalCount(string table)
        {
            return $"SELECT COUNT(1) AS total FROM {table};";
        }

        public abstract string GetPagedData(string table, int totalskip, int pageSize, string sortfield);

        public virtual string DeleteData(string table, List<Guid> ids)
        {
            var idString = string.Join("', '", ids);
            return $"DELETE FROM {table} WHERE _id IN ('{idString}');";
        }

        public abstract string DbTypeToDataType(string type);

        public abstract string DbTypeToControlType(string type);

        public abstract string GetConstrains(string table);

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