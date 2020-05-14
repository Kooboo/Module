using Kooboo.Api;
using Kooboo.IndexedDB;
using Kooboo.Sites.Models;
using Kooboo.Sites.Repository;
using System.Collections.Generic;

namespace SqlEx.Module.code.RelationalDatabase.SchemaStore
{
    public class TableSchemaMappingRepository : SiteRepositoryBase<TableSchemaMapping>, ISchemaMappingRepository
    {

        public override ObjectStoreParameters StoreParameters
        {
            get
            {
                var storeParameters = new ObjectStoreParameters();
                storeParameters.AddIndex<TableSchemaMapping>(x => x.DbType, 30);
                storeParameters.AddIndex<TableSchemaMapping>(x => x.TableName, 120);
                storeParameters.SetPrimaryKeyField<TableSchemaMapping>(x => x.Id);
                return storeParameters;
            }
        }

        public TableSchemaMappingRepository(string dbType, ApiCall call) : base(call.WebSite)
        { 
        }

        public TableSchemaMappingRepository()
        {

        }

        public List<DbTableColumn> GetColumns(string dbType, string table)
        {
            return Get(TableSchemaMapping.GetName(dbType, table))?.Columns ?? new List<DbTableColumn>();
        }

        public void AddOrUpdateSchema(string dbType, string tableName, List<DbTableColumn> columns)
        {
            var value = new TableSchemaMapping { DbType = dbType, TableName = tableName, Columns = columns };
            AddOrUpdate(value);
        }

        public void DeleteTableSchemas(string dbType, string[] tables)
        {
            foreach (var table in tables)
            {
                var id = new TableSchemaMapping { DbType = dbType, TableName = table }.Id;
                Delete(id);
            }
        }

        public List<TableSchemaMapping> SelectAll(string dbType)
        {
            return Query.Where(x => x.DbType == dbType).SelectAll();
        } 

    }
}
