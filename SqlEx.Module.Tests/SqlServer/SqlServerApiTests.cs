using Kooboo.Sites.Models;
using Kooboo.Sites.Scripting.Interfaces;
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

        [Fact]
        public void GetIndexColumns_Should_Return_Cloumns_Correctly()
        {
            var api = new SqlServerApiMock();
            var data1 = new Mock<IDynamicTableObject>();
            data1.SetupGet(x => x.obj)
                .Returns(new Dictionary<string, object> { { "index_keys", "c1,c2" } });
            var data2 = new Mock<IDynamicTableObject>();
            data2.SetupGet(x => x.obj)
                .Returns(new Dictionary<string, object> { { "index_keys", "c3" } });
            var db = new Mock<IRelationalDatabase>();
            db.Setup(x => x.Query(It.IsAny<string>()))
                .Returns(new[] { data1.Object, data2.Object });

            var result = api.GetIndexColumns(db.Object, "table1");

            Assert.Collection(result,
                x => Assert.Equal("c1", x),
                x => Assert.Equal("c2", x),
                x => Assert.Equal("c3", x));
            db.Verify(x => x.Query("EXEC Sp_helpindex [table1]"), Times.Once);
        }

        [Fact]
        public void UpdateIndex_Should_Work_Correctly()
        {
            var api = new SqlServerApiMock();
            var data1 = new Mock<IDynamicTableObject>();
            data1.SetupGet(x => x.obj)
                .Returns(new Dictionary<string, object>
                {
                    { "index_name", "remove_idx" },
                    { "index_keys", "remove_name" }
                });
            var db = new Mock<IRelationalDatabase>();
            db.Setup(x => x.Query(It.IsAny<string>()))
                .Returns(new[] { data1.Object });
            var table = new Mock<ITable>();
            table.Setup(x => x.createIndex(It.IsAny<string>())).Throws(new Exception());
            db.Setup(x => x.GetTable("table1")).Returns(table.Object);
            var columns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "remove_name", IsIndex = false },
                new DbTableColumn { Name = "add_name", IsIndex = true },
            };

            api.UpdateIndex(db.Object, "table1", columns);

            db.Verify(x => x.Query("EXEC Sp_helpindex [table1]"), Times.Once);
            db.Verify(x => x.Execute("DROP INDEX [remove_idx] ON [table1]"), Times.Once);
            table.Verify(x => x.createIndex("add_name"), Times.Once);
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
