using Kooboo.Sites.Models;
using Moq;
using SqlEx.Module.code.RelationalDatabase;
using System.Collections.Generic;

namespace SqlEx.Module.Tests.RelationalDatabaseApi
{
    internal class RelationalDatabaseCommandMock : IRelationalDatabaseRawCommands
    {
        private readonly IRelationalDatabaseRawCommands cmd;
        public RelationalDatabaseCommandMock()
        {
            var mock = new Mock<IRelationalDatabaseRawCommands>();
            cmd = mock.Object;
            MockCmd = mock;
        }

        public Mock<IRelationalDatabaseRawCommands> MockCmd { get; }

        public char QuotationLeft => '<';
        public char QuotationRight => '>';
        public string ListTables()
        {
            return cmd.ListTables() ?? "ListTables";
        }

        public string DeleteTables(string[] tables)
        {
            return cmd.DeleteTables(tables) ?? "DeleteTables_" + string.Join(",", tables);
        }

        public string UpdateTable(string table, List<DbTableColumn> originalColumns, List<DbTableColumn> columns)
        {
            return cmd.UpdateTable(table, originalColumns, columns) ?? "UpdateTable";
        }

        public string GetTotalCount(string table)
        {
            return cmd.GetTotalCount(table) ?? "GetTotalCount_" + table;
        }

        public string GetPagedData(string table, int totalskip, int pageSize, string sortfield)
        {
            return cmd.GetPagedData(table, totalskip, pageSize, sortfield) ?? "GetPagedData";
        }

        public string DeleteData(string table, string primaryKey, List<string> ids)
        {
            return cmd.DeleteData(table, primaryKey, ids)
                   ?? $"DeleteData_{table}_{primaryKey}_{string.Join(",", ids)}";
        }

        public string IsExistTable(string table)
        {
            return cmd.IsExistTable(table) ?? "IsExistTable";
        }

        public string DbTypeToDataType(string type)
        {
            return cmd.DbTypeToDataType(type) ?? "DbTypeToDataType";
        }

        public string DbTypeToControlType(string type)
        {
            return cmd.DbTypeToControlType(type) ?? "DbTypeToControlType";
        }

        public List<DbTableColumn> GetDefaultColumns()
        {
            return cmd.GetDefaultColumns() ?? new List<DbTableColumn>
                 {
                     new DbTableColumn { Name = "_id", DataType = "String" }
                 };
        }
    }
}
