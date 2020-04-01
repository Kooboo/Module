using Kooboo.Sites.Models;
using SqlEx.Module.code.MySql;
using System.Collections.Generic;
using Xunit;

namespace SqlEx.Module.Tests.MySql
{
    public class MySqlCommandsTests
    {
        [Fact]
        public void Quotation_Should_Be_Correct()
        {
            var cmd = new MySqlCommands();

            Assert.Equal('`', cmd.QuotationLeft);
            Assert.Equal('`', cmd.QuotationRight);
        }

        [Theory]
        [MemberData(nameof(data_DbTypeToDataType))]
        public void DbTypeToDataType_Should_Return_DataType_Correctly(string type, string expected)
        {
            var cmd = new MySqlCommands();

            var result = cmd.DbTypeToDataType(type);

            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(data_DbTypeToControlType))]
        public void DbTypeToControlType_Should_Return_ControlType_Correctly(string type, string expected)
        {
            var cmd = new MySqlCommands();

            var result = cmd.DbTypeToControlType(type);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetPagedData_Should_Return_Sql_Correctly()
        {
            var cmd = new MySqlCommands();

            var result = cmd.GetPagedData("table1", 10, 5, "id");

            Assert.Equal("SELECT * FROM `table1` ORDER BY `id` DESC LIMIT 10,5;", result);
        }

        [Fact]
        public void UpdateTable_Should_Return_Sql_Correctly()
        {
            var cmd = new MySqlCommands();
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
                new DbTableColumn { Name = "c6", DataType = "number" },
                new DbTableColumn { Name = "c7", DataType = "varchar" },
            };

            var result = cmd.UpdateTable("table1", oriColumn, columns)
                .Replace(System.Environment.NewLine, " ");

            Assert.Equal("ALTER TABLE `table1` " +
                         "ADD `c2` bit, " +
                         "ADD `c4` datetime, " +
                         "ADD `c5` text, " +
                         "ADD `c6` double, " +
                         "ADD `c7` text, " +
                         "DROP COLUMN `c1`; ", result);
        }

        public static readonly IEnumerable<object[]> data_DbTypeToDataType = new[]
        {
            new object[] { "bigint", "Number" },
            new object[] { "decimal", "Number" },
            new object[] { "double", "Number" },
            new object[] { "float", "Number" },
            new object[] { "int", "Number" },
            new object[] { "integer", "Number" },
            new object[] { "mediumint", "Number" },
            new object[] { "numaric", "Number" },
            new object[] { "real", "Number" },
            new object[] { "smallint", "Number" },
            new object[] { "tinyint", "Number" },

            new object[] { "bit", "Bool" },

            new object[] { "date", "DateTime" },
            new object[] { "datetime", "DateTime" },
            new object[] { "time", "DateTime" },
            new object[] { "timestamp", "DateTime" },
            new object[] { "year", "DateTime" },

            new object[] { "binary", "String" },
            new object[] { "blob", "String" },
            new object[] { "char", "String" },
            new object[] { "enum", "String" },
            new object[] { "geometry", "String" },
            new object[] { "geometrycollection", "String" },
            new object[] { "json", "String" },
            new object[] { "linestring", "String" },
            new object[] { "longblob", "String" },
            new object[] { "longtext", "String" },
            new object[] { "mediumblob", "String" },
            new object[] { "mediumtext", "String" },
            new object[] { "multilinestring", "String" },
            new object[] { "multipoint", "String" },
            new object[] { "multipolygon", "String" },
            new object[] { "point", "String" },
            new object[] { "polygon", "String" },
            new object[] { "set", "String" },
            new object[] { "text", "String" },
            new object[] { "tinyblob", "String" },
            new object[] { "tinytext", "String" },
            new object[] { "varbinary", "String" },
            new object[] { "varchar", "String" },
            new object[] { "any", "String" },
        };

        public static readonly IEnumerable<object[]> data_DbTypeToControlType = new[]
        {
            new object[] { "bigint", "Number" },
            new object[] { "decimal", "Number" },
            new object[] { "double", "Number" },
            new object[] { "float", "Number" },
            new object[] { "int", "Number" },
            new object[] { "integer", "Number" },
            new object[] { "mediumint", "Number" },
            new object[] { "numaric", "Number" },
            new object[] { "real", "Number" },
            new object[] { "smallint", "Number" },
            new object[] { "tinyint", "Number" },

            new object[] { "bit", "Boolean" },

            new object[] { "date", "DateTime" },
            new object[] { "datetime", "DateTime" },
            new object[] { "time", "DateTime" },
            new object[] { "timestamp", "DateTime" },
            new object[] { "year", "DateTime" },

            new object[] { "binary", "TextArea" },
            new object[] { "blob", "TextArea" },
            new object[] { "enum", "TextArea" },
            new object[] { "geometry", "TextArea" },
            new object[] { "geometrycollection", "TextArea" },
            new object[] { "json", "TextArea" },
            new object[] { "linestring", "TextArea" },
            new object[] { "longblob", "TextArea" },
            new object[] { "mediumblob", "TextArea" },
            new object[] { "mediumtext", "TextArea" },
            new object[] { "multilinestring", "TextArea" },
            new object[] { "multipoint", "TextArea" },
            new object[] { "multipolygon", "TextArea" },
            new object[] { "point", "TextArea" },
            new object[] { "polygon", "TextArea" },
            new object[] { "tinyblob", "TextArea" },
            new object[] { "set", "TextArea" },
            new object[] { "longtext", "TextArea" },
            new object[] { "varbinary", "TextArea" },

            new object[] { "char", "TextBox" },
            new object[] { "text", "TextBox" },
            new object[] { "tinytext", "TextBox" },
            new object[] { "varchar", "TextBox" },
            new object[] { "any", "TextBox" },
        };
    }
}
