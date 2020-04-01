using Kooboo.Sites.Models;
using SqlEx.Module.code.SqlServer;
using System.Collections.Generic;
using Xunit;

namespace SqlEx.Module.Tests.SqlServer
{
    public class SqlServerCommandsTests
    {
        [Fact]
        public void Quotation_Should_Be_Correct()
        {
            var cmd = new SqlServerCommands();

            Assert.Equal('[', cmd.QuotationLeft);
            Assert.Equal(']', cmd.QuotationRight);
        }

        [Fact]
        public void ListTables_Should_Return_Sql_Correctly()
        {
            var cmd = new SqlServerCommands();

            var result = cmd.ListTables();

            Assert.Equal("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';", result);
        }

        [Fact]
        public void IsExistTable_Should_Return_Sql_Correctly()
        {
            var cmd = new SqlServerCommands();

            var result = cmd.IsExistTable("table1");

            Assert.Equal("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME = 'table1';", result);
        }

        [Theory]
        [MemberData(nameof(data_DbTypeToDataType))]
        public void DbTypeToDataType_Should_Return_DataType_Correctly(string type, string expected)
        {
            var cmd = new SqlServerCommands();

            var result = cmd.DbTypeToDataType(type);

            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(data_DbTypeToControlType))]
        public void DbTypeToControlType_Should_Return_ControlType_Correctly(string type, string expected)
        {
            var cmd = new SqlServerCommands();

            var result = cmd.DbTypeToControlType(type);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetPagedData_Should_Return_Sql_Correctly()
        {
            var cmd = new SqlServerCommands();

            var result = cmd.GetPagedData("table1", 10, 5, "id");

            Assert.Equal("SELECT * FROM ( SELECT ROW_NUMBER () OVER ( ORDER BY [id] DESC ) AS RowNum, * FROM [table1] ) AS RowConstrainedResult WHERE RowNum > 10 AND RowNum <= 15ORDER BY RowNum", result);
        }

        [Fact]
        public void UpdateTable_Should_Return_Sql_Correctly()
        {
            var cmd = new SqlServerCommands();
            var oriColumn = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "id", DataType = "String" },
                new DbTableColumn { Name = "c1", DataType = "number" },
                new DbTableColumn { Name = "c3", DataType = "number" },
            };
            var columns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "id", DataType = "String" },
                new DbTableColumn { Name = "c2", DataType = "bool" },
                new DbTableColumn { Name = "c3", DataType = "number", Length = 2 },
                new DbTableColumn { Name = "c4", DataType = "datetime" },
                new DbTableColumn { Name = "c5", DataType = "string" },
                new DbTableColumn { Name = "c6", DataType = "varchar" },
            };

            var result = cmd.UpdateTable("table1", oriColumn, columns)
                .Replace(System.Environment.NewLine, " ");

            Assert.Equal("ALTER TABLE [table1] ADD [c2] bit; " +
                         "ALTER TABLE [table1] ALTER COLUMN [c3] float; " +
                         "ALTER TABLE [table1] ADD [c4] datetime; " +
                         "ALTER TABLE [table1] ADD [c5] nvarchar(max); " +
                         "ALTER TABLE [table1] ADD [c6] nvarchar(max); " +
                         "ALTER TABLE [table1] DROP COLUMN [c1]; "
                , result);
        }

        public static readonly IEnumerable<object[]> data_DbTypeToDataType = new[]
        {
            new object[] { "bigint", "Number" },
            new object[] { "decimal", "Number" },
            new object[] { "float", "Number" },
            new object[] { "int", "Number" },
            new object[] { "money", "Number" },
            new object[] { "numeric", "Number" },
            new object[] { "real", "Number" },
            new object[] { "smallint", "Number" },
            new object[] { "smallmoney", "Number" },
            new object[] { "tinyint", "Number" },

            new object[] { "bit", "Bool" },

            new object[] { "date", "DateTime" },
            new object[] { "datetime", "DateTime" },
            new object[] { "datetime2", "DateTime" },
            new object[] { "datetimeoffset", "DateTime" },
            new object[] { "smalldatetime", "DateTime" },
            new object[] { "time", "DateTime" },
            new object[] { "timestamp", "DateTime" },

            new object[] { "binary", "String" },
            new object[] { "char", "String" },
            new object[] { "geography", "String" },
            new object[] { "geometry", "String" },
            new object[] { "hierarchyid", "String" },
            new object[] { "image", "String" },
            new object[] { "nchar", "String" },
            new object[] { "ntext", "String" },
            new object[] { "nvarchar", "String" },
            new object[] { "sql_variant", "String" },
            new object[] { "sysname", "String" },
            new object[] { "text", "String" },
            new object[] { "uniqueidentifier", "String" },
            new object[] { "varbinary", "String" },
            new object[] { "varchar", "String" },
            new object[] { "xml", "String" },
            new object[] { "any", "String" },
        };

        public static readonly IEnumerable<object[]> data_DbTypeToControlType = new[]
        {
            new object[] { "bigint", "Number" },
            new object[] { "decimal", "Number" },
            new object[] { "float", "Number" },
            new object[] { "int", "Number" },
            new object[] { "money", "Number" },
            new object[] { "numeric", "Number" },
            new object[] { "real", "Number" },
            new object[] { "smallint", "Number" },
            new object[] { "smallmoney", "Number" },
            new object[] { "tinyint", "Number" },

            new object[] { "bit", "Boolean" },

            new object[] { "date", "DateTime" },
            new object[] { "datetime", "DateTime" },
            new object[] { "datetime2", "DateTime" },
            new object[] { "datetimeoffset", "DateTime" },
            new object[] { "smalldatetime", "DateTime" },
            new object[] { "time", "DateTime" },
            new object[] { "timestamp", "DateTime" },

            new object[] { "binary", "TextArea" },
            new object[] { "geography", "TextArea" },
            new object[] { "geometry", "TextArea" },
            new object[] { "hierarchyid", "TextArea" },
            new object[] { "image", "TextArea" },
            new object[] { "sql_variant", "TextArea" },
            new object[] { "sysname", "TextArea" },
            new object[] { "xml", "TextArea" },

            new object[] { "char", "TextBox" },
            new object[] { "nchar", "TextBox" },
            new object[] { "ntext", "TextBox" },
            new object[] { "nvarchar", "TextBox" },
            new object[] { "uniqueidentifier", "TextBox" },
            new object[] { "text", "TextBox" },
            new object[] { "varbinary", "TextBox" },
            new object[] { "varchar", "TextBox" },
            new object[] { "any", "TextBox" },
        };
    }
}
