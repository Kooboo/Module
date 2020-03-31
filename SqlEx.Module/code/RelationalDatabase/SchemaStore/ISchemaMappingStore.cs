using System.Collections.Generic;
using Kooboo.Sites.Models;

namespace SqlEx.Module.code.RelationalDatabase.SchemaStore
{
    public interface ISchemaMappingStore
    {
        void AddSchema(string tableName, List<DbTableColumn> columns);
        void UpdateSchema(string tableName, List<DbTableColumn> columns);
        void DeleteTableSchemas(string[] tables);
        List<TableSchemaMapping> SelectAll();
        List<DbTableColumn> GetColumns(string table);
    }
}