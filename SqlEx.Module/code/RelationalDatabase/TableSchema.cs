using Kooboo.Sites.Models;
using System.Collections.Generic;

namespace SqlEx.Module.code.RelationalDatabase
{
    public class TableSchema
    {
        public string Key => GetKey(DbType, TableName);

        public string DbType { get; set; }

        public string TableName { get; set; }

        public List<DbTableColumn> Columns { get; set; }

        public static string GetKey(string dbType, string tableName)
        {
            return $"{dbType}_{tableName}";
        }
    }
}
