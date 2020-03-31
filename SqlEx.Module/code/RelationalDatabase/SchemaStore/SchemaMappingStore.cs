using Kooboo.Api;
using Kooboo.IndexedDB;
using Kooboo.Sites.Models;
using System.Collections.Generic;

namespace SqlEx.Module.code.RelationalDatabase.SchemaStore
{
    public class SchemaMappingStore : ISchemaMappingStore
    {
        private readonly string dbType;
        private readonly ObjectStore<string, TableSchemaMapping> store;

        public SchemaMappingStore(string dbType, ApiCall call)
        {
            this.dbType = dbType;
            store = GetSchemaObjectStore(call);
        }

        public List<DbTableColumn> GetColumns(string table)
        {
            return store.get(TableSchemaMapping.GetKey(dbType, table))?.Columns
                   ?? new List<DbTableColumn>();
        }

        public void AddSchema(string tableName, List<DbTableColumn> columns)
        {
            var value = new TableSchemaMapping { DbType = dbType, TableName = tableName, Columns = columns };
            store.add(value.Key, value);
        }

        public void UpdateSchema(string tableName, List<DbTableColumn> columns)
        {
            var value = new TableSchemaMapping { DbType = dbType, TableName = tableName, Columns = columns };
            store.update(value.Key, value);
        }

        public void DeleteTableSchemas(string[] tables)
        {
            foreach (var table in tables)
            {
                store.delete(TableSchemaMapping.GetKey(dbType, table));
            }
        }

        public List<TableSchemaMapping> SelectAll()
        {
            return store.Where(x => x.DbType == dbType).SelectAll();
        }

        private ObjectStore<string, TableSchemaMapping> GetSchemaObjectStore(ApiCall call)
        {
            var storeParameters = new ObjectStoreParameters
            {
                EnableVersion = false,
                EnableLog = false,
            };
            storeParameters.AddIndex<TableSchemaMapping>(x => x.DbType, 30);
            storeParameters.AddIndex<TableSchemaMapping>(x => x.TableName, 120);
            storeParameters.SetPrimaryKeyField<TableSchemaMapping>(x => x.Key, 150);
            var database = Kooboo.Data.DB.GetKDatabase(call.WebSite);
            return database.GetOrCreateObjectStore<string, TableSchemaMapping>("RelationalTableSchema", storeParameters);
        }
    }
}
