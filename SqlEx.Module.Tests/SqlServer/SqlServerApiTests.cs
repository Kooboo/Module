using Kooboo.Sites.Models;
using Kooboo.Web.ViewModel;
using KScript;
using Moq;
using SqlEx.Module.code.RelationalDatabase;
using SqlEx.Module.code.SqlServer;
using System;
using System.Collections.Generic;
using Xunit;

namespace SqlEx.Module.Tests.SqlServer
{
    public class SqlServerApiTests
    {
        [Fact]
        public void DbType_Should_Be_MySql()
        {
            var sqlServer = new SqlServerApi();
            Assert.Equal("SqlServer", sqlServer.DbType);
        }

        [Fact]
        public void ModelName_Should_Be_MySql()
        {
            var sqlServer = new SqlServerApi();
            Assert.Equal("SqlServer", sqlServer.ModelName);
        }

        [Fact]
        public void RequireSite_Should_Be_True()
        {
            var sqlServer = new SqlServerApi();
            Assert.True(sqlServer.RequireSite);
        }

        [Fact]
        public void RequireUser_Should_Be_False()
        {
            var sqlServer = new SqlServerApi();
            Assert.False(sqlServer.RequireUser);
        }

        [Fact]
        public void ConvertDataValue_Should_Convert_Correctly()
        {
            var data = new Mock<IDynamicTableObject>();
            data.SetupGet(x => x.Values)
                .Returns(new Dictionary<string, object>
                {
                    { "RowNum", 1 },
                    { "b", true },
                    { "s", "string1" },
                    { "d", new DateTime(2010,1,1) },
                    { "n", 1.2 },
                    { "unknown", "unknonw" },
                    { "null", null },
                });
            var columns = new List<DbTableColumn>
            {
                new DbTableColumn { DataType = "bool", Name = "b" },
                new DbTableColumn { DataType = "string", Name = "s" },
                new DbTableColumn { DataType = "datetime", Name = "d" },
                new DbTableColumn { DataType = "number", Name = "n" },
            };
            var sqlServer = new SqlServerApiMock();
            var result = sqlServer.ConvertDataValue(new[] { data.Object }, columns);

            Assert.Collection(result, x =>
                Assert.Collection(x,
                    c => { Assert.Equal("b", c.key); Assert.True((bool)c.value); },
                    c => { Assert.Equal("s", c.key); Assert.Equal("string1", c.value); },
                    c => { Assert.Equal("d", c.key); Assert.Equal(new DateTime(2010, 1, 1), c.value); },
                    c => { Assert.Equal("n", c.key); Assert.Equal(1.2, c.value); },
                    c => { Assert.Equal("unknown", c.key); Assert.Equal("unknonw", c.value); },
                    c => { Assert.Equal("null", c.key); Assert.Null(c.value); }
                    )
                );
        }

        [Theory]
        [InlineData("number", typeof(double))]
        [InlineData("Number", typeof(double))]
        [InlineData("bool", typeof(bool))]
        [InlineData("Bool", typeof(bool))]
        [InlineData("datetime", typeof(DateTime))]
        [InlineData("Datetime", typeof(DateTime))]
        [InlineData("string", typeof(string))]
        [InlineData("String", typeof(string))]
        [InlineData("any", typeof(string))]
        public void GetClrType_Should_Return_Type_Correctly(string dataType, Type expected)
        {
            var sqlServer = new SqlServerApiMock();

            var result = sqlServer.GetClrType(new DatabaseItemEdit { DataType = dataType });

            Assert.Equal(expected, result);
        }

        class SqlServerApiMock : SqlServerApi
        {
            public new List<List<DataValue>> ConvertDataValue(IDynamicTableObject[] data, List<DbTableColumn> columns)
            {
                return base.ConvertDataValue(data, columns);
            }

            public new Type GetClrType(DatabaseItemEdit column)
            {
                return base.GetClrType(column);
            }
        }
    }
}
