using System.Collections.Generic;
using Kooboo.Sites.Models;

namespace SqlEx.Module.code.RelationalDatabase.SchemaStore
{
    public interface ISchemaMappingRepository
    {
        void AddOrUpdateSchema(string dbtype, string tableName, List<DbTableColumn> columns);
        void DeleteTableSchemas(string dbtype, string[] tables);
        List<TableSchemaMapping> SelectAll(string dbtype);
        List<DbTableColumn> GetColumns(string dbtype, string table);
    }
}