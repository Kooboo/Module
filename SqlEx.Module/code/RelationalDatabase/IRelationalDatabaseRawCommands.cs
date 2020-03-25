using System;
using System.Collections.Generic;
using Kooboo.Sites.Models;

namespace SqlEx.Module.code.RelationalDatabase
{
    public interface IRelationalDatabaseRawCommands
    {
        string ListTables();
        string DeleteTables(string[] tables, char quotationLeft, char quotationRight);
        string UpdateTable(string table, List<DbTableColumn> originalColumns, List<DbTableColumn> columns);
        string GetTotalCount(string table);
        string GetPagedData(string table, int totalskip, int pageSize, string sortfield);
        string DeleteData(string table, List<Guid> ids);
        string IsExistTable(string table);
        string DbTypeToDataType(string type);
        string DbTypeToControlType(string type);
        List<DbTableColumn> GetDefaultColumns();
    }
}