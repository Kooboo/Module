using Kooboo.Sites.Models;
using Kooboo.Sites.Scripting.Interfaces;
using Kooboo.Web.ViewModel;
using KScript;
using Moq;
using SqlEx.Module.code.RelationalDatabase;
using SqlEx.Module.code.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SqlEx.Module.Tests.Sqlite
{
    public class SqliteApiTests
    {
        [Fact]
        public void DbType_Should_Be_MySql()
        {
            var sqlite = new SqliteApi();
            Assert.Equal("Sqlite", sqlite.DbType);
        }

        [Fact]
        public void ModelName_Should_Be_MySql()
        {
            var sqlite = new SqliteApi();
            Assert.Equal("Sqlite", sqlite.ModelName);
        }

        [Fact]
        public void RequireSite_Should_Be_True()
        {
            var sqlite = new SqliteApi();
            Assert.True(sqlite.RequireSite);
        }

        [Fact]
        public void RequireUser_Should_Be_False()
        {
            var sqlite = new SqliteApi();
            Assert.False(sqlite.RequireUser);
        }

        [Fact]
        public void UpdateTable_Should_Throw_Exception_When_Remove_Column_Has_Index()
        {
            var db = new Mock<IRelationalDatabase>();
            var sqls = new[]
                {
                    new { type = "table", name = "table1", sql = @"CREATE TABLE ""vb"" (
                        ""c1"" TEXT,
                        ""c2"" TEXT
                        )" },
                    new { type = "index", name = "idx_uniq_c2", sql = @"CREATE UNIQUE INDEX ""idx_uniq_c2""
                            ON ""vb"" (
                            ""c2""
                            )" },
                }
                .Select(x =>
                {
                    var obj = new Mock<IDynamicTableObject>();
                    obj.SetupGet(o => o.Values)
                        .Returns(new Dictionary<string, object>
                        { { "type", x.type }, { "name", x.name }, { "sql", x.sql } });
                    return obj.Object;
                })
                .ToArray();
            db.Setup(x => x.Query(It.IsAny<string>())).Returns(sqls);
            var columns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "c1", DataType = "string"},
            };
            var originalColumns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "c1", DataType = "string"},
                new DbTableColumn { Name = "c2", DataType = "string"},
            };
            var sqlite = new SqliteApiMock();

            var exception = Assert.Throws<Exception>(() => sqlite.UpdateTable(db.Object, "table1", columns, originalColumns));

            Assert.Equal("Cannot remove column that has index, column name: c2", exception.Message);
        }

        [Fact]
        public void UpdateTable_Should_Generate_Commnad_Text_Correct()
        {
            var db = new Mock<IRelationalDatabase>();
            var sqls = new[]
                {
                    new { type = "table", name = "table1", sql = @"CREATE TABLE ""vb"" (
                        ""id"" integer NOT NULL,
                        ""c1"" TEXT,
                        ""c2"" TEXT,
                        ""c3"" real,
                        PRIMARY KEY (""id"")
                        )" },
                    new { type = "index", name = "idx_c1", sql = @"CREATE INDEX ""idx_c1""
                            ON ""vb"" (
                            ""c1""
                            )" },
                    new { type = "index", name = "idx_uniq_c2", sql = @"CREATE UNIQUE INDEX ""idx_uniq_c2""
                            ON ""vb"" (
                            ""c2""
                            )" },
                }
                .Select(x =>
                {
                    var obj = new Mock<IDynamicTableObject>();
                    obj.SetupGet(o => o.Values)
                        .Returns(new Dictionary<string, object>
                        { { "type", x.type }, { "name", x.name }, { "sql", x.sql } });
                    return obj.Object;
                })
                .ToArray();
            db.Setup(x => x.Query(It.IsAny<string>())).Returns(sqls);
            var columns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "id", DataType = "number"},
                new DbTableColumn { Name = "c1", DataType = "string"},
                new DbTableColumn { Name = "c2", DataType = "string"},
                new DbTableColumn { Name = "c4", DataType = "String"},
                new DbTableColumn { Name = "c5", DataType = "number"},
                new DbTableColumn { Name = "c6", DataType = "bool"},
            };
            var originalColumns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "id", DataType = "number"},
                new DbTableColumn { Name = "c1", DataType = "string"},
                new DbTableColumn { Name = "c2", DataType = "string"},
                new DbTableColumn { Name = "c3", DataType = "number"},
            };
            var sqlite = new SqliteApiMock();

            sqlite.UpdateTable(db.Object, "table1", columns, originalColumns);

            Func<string, bool> verify = s =>
            {
                var removeSpace = new Regex("\r|\n| {2}");
                var standardTableName = new Regex("_old_table1_\\d{14}");
                s = removeSpace.Replace(s, "");
                s = standardTableName.Replace(s, "_old_table1_xxxx");
                Assert.Equal("DROP INDEX idx_c1;" +
                             "DROP INDEX idx_uniq_c2;" +
                             "ALTER TABLE table1 RENAME TO _old_table1_xxxx;" +
                             "CREATE TABLE \"table1\" " +
                             "(\"id\" integer NOT NULL," +
                             "\"c1\" TEXT,\"c2\" TEXT," +
                             "\"c4\" TEXT," +
                             "\"c5\" REAL," +
                             "\"c6\" INTEGER," +
                             "PRIMARY KEY (\"id\")" +
                             ");" +
                             "CREATE INDEX \"idx_c1\"ON \"vb\" (\"c1\");" +
                             "CREATE UNIQUE INDEX \"idx_uniq_c2\"ON \"vb\" (\"c2\");" +
                             "INSERT INTO table1 (\"id\",\"c1\",\"c2\")" +
                             " SELECT \"id\",\"c1\",\"c2\" FROM _old_table1_xxxx;" +
                             "DROP TABLE _old_table1_xxxx;",
                    s);
                return true;
            };
            db.Verify(x => x.Execute(It.Is<string>(sql => verify(sql))), Times.Once);
        }

        [Fact]
        public void ConvertDataValue_Should_Convert_Correctly()
        {
            var data = new Mock<IDynamicTableObject>();
            data.SetupGet(x => x.Values)
                .Returns(new Dictionary<string, object>
                {
                    { "b", "true" },
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
            var sqlite = new SqliteApiMock();
            var result = sqlite.ConvertDataValue(new[] { data.Object }, columns);

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
        [InlineData("datetime", typeof(string))]
        [InlineData("Datetime", typeof(string))]
        [InlineData("string", typeof(string))]
        [InlineData("String", typeof(string))]
        [InlineData("any", typeof(string))]
        public void GetClrType_Should_Return_Type_Correctly(string dataType, Type expected)
        {
            var sqlite = new SqliteApiMock();

            var result = sqlite.GetClrType(new DatabaseItemEdit { DataType = dataType });

            Assert.Equal(expected, result);
        }

        class SqliteApiMock : SqliteApi
        {
            public new void UpdateTable(IRelationalDatabase db, string tablename, List<DbTableColumn> columns, List<DbTableColumn> originalColumns)
            {
                base.UpdateTable(db, tablename, columns, originalColumns);
            }

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
