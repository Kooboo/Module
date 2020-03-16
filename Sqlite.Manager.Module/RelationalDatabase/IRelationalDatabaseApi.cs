using Kooboo.Api;
using Kooboo.Sites.Models;
using Kooboo.Sites.Scripting.Interfaces;
using Kooboo.Web.ViewModel;
using System;
using System.Collections.Generic;

namespace Sqlite.Menager.Module.RelationalDatabase
{
    public interface IRelationalDatabaseApi : IApi
    {
        List<string> Tables(ApiCall call);
        void CreateTable(string name, ApiCall call);
        void DeleteTables(string names, ApiCall call);
        bool IsUniqueTableName(string name, ApiCall call);
        List<DbTableColumn> Columns(string table, ApiCall call);
        void UpdateColumn(string tablename, List<DbTableColumn> columns, ApiCall call);
        PagedListViewModel<List<DataValue>> Data(string table, ApiCall call);
        List<DatabaseItemEdit> GetEdit(string tablename, string id, ApiCall call);
        Guid UpdateData(string tablename, Guid id, List<DatabaseItemEdit> values, ApiCall call);
        void DeleteData(string tablename, List<Guid> values, ApiCall call);
        Dictionary<string, List<DbTableColumn>> SyncSchema(IRelationalDatabase db);
    }
}
