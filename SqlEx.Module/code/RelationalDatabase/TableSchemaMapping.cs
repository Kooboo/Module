using Kooboo.Sites.Models;
using System.Collections.Generic;

namespace SqlEx.Module.code.RelationalDatabase
{
    public class TableSchemaMapping : SiteObject
    {
        private string _name;
        public override string Name
        {
            get => _name ?? (_name = GetName(DbType, TableName));
            set => _name = value;
        }

        public string DbType { get; set; }

        public string TableName { get; set; }

        public List<DbTableColumn> Columns { get; set; }

        public static string GetName(string dbType, string tableName)
        {
            return $"{dbType}_{tableName}";
        }
    }
}
