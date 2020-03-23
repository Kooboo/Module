using System;
using System.Collections.Generic;
using Kooboo.Sites.Models;

namespace Sqlite.Menager.Module.RelationalDatabase
{
    public interface IRelationalDatabaseRawCommands
    {
        string ListTables();
        string CreateTable(string table, List<DbTableColumn> columns);
        string DeleteTables(string[] tables, char quotationLeft, char quotationRight);
        string UpdateColumn(
            string table,
            List<DbTableColumn> originalColumns,
            List<DbTableColumn> columns,
            DbConstrain[] constraints);
        string GetTotalCount(string table);
        string GetPagedData(string table, int totalskip, int pageSize, string sortfield);
        string DeleteData(string table, List<Guid> ids);
        string IsExistTable(string table);
        string DbTypeToDataType(string type);
        string DbTypeToControlType(string type);
        string GetConstrains(string table);
    }
}