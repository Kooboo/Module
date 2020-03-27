using System;
using System.Collections.Generic;
using Kooboo.Api;
using Kooboo.Sites.Models;
using Kooboo.Web.ViewModel;

namespace SqlEx.Module.code.RelationalDatabase
{
    public interface IRelationalDatabaseApi : IApi
    {
        List<string> Tables(ApiCall call);
        void CreateTable(string name, ApiCall call);
        void DeleteTables(string names, ApiCall call);
        bool IsUniqueTableName(string name, ApiCall call);
        List<DbTableColumn> Columns(string table, ApiCall call);
        void UpdateColumn(string tablename, List<DbTableColumn> columns, ApiCall call);
        PagedListViewModelWithPrimaryKey<List<DataValue>> Data(string table, ApiCall call);
        List<DatabaseItemEdit> GetEdit(string tablename, string id, ApiCall call);
        string UpdateData(string tablename, string id, List<DatabaseItemEdit> values, ApiCall call);
        void DeleteData(string tablename, List<string> values, ApiCall call);
        void SyncSchema(ApiCall call);
    }
}
