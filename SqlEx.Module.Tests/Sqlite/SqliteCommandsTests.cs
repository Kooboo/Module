using System;
using System.Collections.Generic;
using Kooboo.Sites.Models;
using Newtonsoft.Json;
using SqlEx.Module.code.Sqlite;
using Xunit;

namespace SqlEx.Module.Tests.Sqlite
{
    public class SqliteCommandsTests
    {
        [Fact]
        public void Quotation_Should_Be_Correct()
        {
            var cmd = new SqliteCommands();

            Assert.Equal('"', cmd.QuotationLeft);
            Assert.Equal('"', cmd.QuotationRight);
        }

        [Fact]
        public void ListTables_Should_Return_Sql_Correctly()
        {
            var cmd = new SqliteCommands();

            var result = cmd.ListTables();

            Assert.Equal("SELECT name FROM sqlite_master WHERE type='table';", result);
        }

        [Fact]
        public void IsExistTable_Should_Return_Sql_Correctly()
        {
            var cmd = new SqliteCommands();

            var result = cmd.IsExistTable("table1", out var param);

            Assert.Equal("SELECT name FROM sqlite_master WHERE type='table' and name=@table LIMIT 1", result);
            Assert.Equal("{\"table\":\"table1\"}", JsonConvert.SerializeObject(param));
        }

        [Theory]
        [InlineData("REAL", "number")]
        [InlineData("INTEGER", "number")]
        [InlineData("STRING", "string")]
        [InlineData("TEXT", "string")]
        public void DbTypeToDataType_Should_Return_DataType_Correctly(string type, string expected)
        {
            var cmd = new SqliteCommands();

            var result = cmd.DbTypeToDataType(type);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("REAL", "Number")]
        [InlineData("INTEGER", "Number")]
        [InlineData("STRING", "TextBox")]
        [InlineData("TEXT", "TextBox")]
        public void DbTypeToControlType_Should_Return_ControlType_Correctly(string type, string expected)
        {
            var cmd = new SqliteCommands();

            var result = cmd.DbTypeToControlType(type);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetPagedData_Should_Return_Sql_Correctly()
        {
            var cmd = new SqliteCommands();

            var result = cmd.GetPagedData("table1", 10, 5, "id");

            Assert.Equal("SELECT * FROM \"table1\" ORDER BY \"id\" DESC LIMIT 10,5;", result);
        }

        [Fact]
        public void UpdateTable_Is_Not_Implemented()
        {
            var cmd = new SqliteCommands();

            Assert.Throws<NotImplementedException>(() =>
                cmd.UpdateTable("table1", new List<DbTableColumn>(), new List<DbTableColumn>()));
        }
    }
}
