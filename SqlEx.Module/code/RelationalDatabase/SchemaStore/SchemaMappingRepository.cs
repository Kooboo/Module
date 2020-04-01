using Kooboo.Api;
using Kooboo.IndexedDB;
using Kooboo.Sites.Models;
using Kooboo.Sites.Repository;
using System.Collections.Generic;

namespace SqlEx.Module.code.RelationalDatabase.SchemaStore
{
    public class SchemaMappingRepository : SiteRepositoryBase<TableSchemaMapping>, ISchemaMappingRepository
    {
        private readonly string dbType;

        public SchemaMappingRepository(string dbType, ApiCall call) : base(call.WebSite)
        {
            this.dbType = dbType;
        }

        public List<DbTableColumn> GetColumns(string table)
        {
            return Get(TableSchemaMapping.GetName(dbType, table))?.Columns ?? new List<DbTableColumn>();
        }

        public void AddOrUpdateSchema(string tableName, List<DbTableColumn> columns)
        {
            var value = new TableSchemaMapping { DbType = dbType, TableName = tableName, Columns = columns };
            AddOrUpdate(value);
        }

        public void DeleteTableSchemas(string[] tables)
        {
            foreach (var table in tables)
            {
                var id = new TableSchemaMapping { DbType = dbType, TableName = table }.Id;
                Delete(id);
            }
        }

        public List<TableSchemaMapping> SelectAll()
        {
            return Query.Where(x => x.DbType == dbType).SelectAll();
        }

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
    }
}
