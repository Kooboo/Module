using System.Collections.Generic;
using Kooboo.Sites.Models;

namespace SqlEx.Module.code.RelationalDatabase.SchemaStore
{
    public interface ISchemaMappingRepository
    {
        void AddOrUpdateSchema(string tableName, List<DbTableColumn> columns);
        void DeleteTableSchemas(string[] tables);
        List<TableSchemaMapping> SelectAll();
        List<DbTableColumn> GetColumns(string table);
    }
}