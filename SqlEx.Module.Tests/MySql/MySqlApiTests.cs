using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Kooboo.Sites.Models;
using Kooboo.Sites.Scripting.Interfaces;
using Kooboo.Web.ViewModel;
using KScript;
using Moq;
using SqlEx.Module.code.MySql;
using SqlEx.Module.code.RelationalDatabase;
using Xunit;

namespace SqlEx.Module.Tests.MySql
{
    public class MySqlApiTests
    {
        [Fact]
        public void DbType_Should_Be_MySql()
        {
            var mysql = new MySqlApi();
            Assert.Equal("MySql", mysql.DbType);
        }

        [Fact]
        public void ModelName_Should_Be_MySql()
        {
            var mysql = new MySqlApi();
            Assert.Equal("MySql", mysql.ModelName);
        }

        [Fact]
        public void RequireSite_Should_Be_True()
        {
            var mysql = new MySqlApi();
            Assert.True(mysql.RequireSite);
        }

        [Fact]
        public void RequireUser_Should_Be_False()
        {
            var mysql = new MySqlApi();
            Assert.False(mysql.RequireUser);
        }

        [Fact]
        public void IsExistTable_Should_Sent_Commnad_Text_Correct()
        {
            var parameter = new Mock<IDbDataParameter>();
            var parameters = new Mock<IDataParameterCollection>();
            var cmd = new Mock<IDbCommand>();
            cmd.SetupGet(x => x.Parameters).Returns(parameters.Object);
            cmd.Setup(x => x.CreateParameter()).Returns(parameter.Object);
            var conn = new Mock<IDbConnection>();
            conn.SetupGet(x => x.Database).Returns("kooboo");
            conn.Setup(x => x.CreateCommand()).Returns(cmd.Object);
            var executer = new Mock<ISqlExecuter>();
            executer.Setup(x => x.CreateConnection()).Returns(conn.Object);
            var db = new Mock<IRelationalDatabase>();
            db.SetupGet(x => x.SqlExecuter).Returns(executer.Object);
            var mysql = new MySqlApiMock();

            var b = mysql.IsExistTable(db.Object, "table1");

            cmd.VerifySet(x => x.CommandText = "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE TABLE_SCHEMA='kooboo' AND TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME = @table);", Times.Once());
            parameter.VerifySet(x => x.Value = "table1");
            parameter.VerifySet(x => x.ParameterName = "table");
        }

        [Fact]
        public void ListTables_Should_Sent_Commnad_Text_Correct()
        {
            var dataReader = new Mock<IDataReader>();
            dataReader.Setup(m => m.FieldCount).Returns(1);
            dataReader.Setup(m => m.GetName(0)).Returns("TABLE_NAME");
            dataReader.Setup(m => m.GetFieldType(0)).Returns(typeof(string));
            dataReader.Setup(m => m.GetValue(0)).Returns("table1");
            dataReader.SetupSequence(m => m.Read()).Returns(true).Returns(false);
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(x => x.ExecuteReader(It.IsAny<CommandBehavior>())).Returns(dataReader.Object);
            var conn = new Mock<IDbConnection>();
            conn.SetupGet(x => x.Database).Returns("kooboo");
            conn.Setup(x => x.CreateCommand()).Returns(cmd.Object);
            var executer = new Mock<ISqlExecuter>();
            executer.Setup(x => x.CreateConnection()).Returns(conn.Object);
            var db = new Mock<IRelationalDatabase>();
            db.SetupGet(x => x.SqlExecuter).Returns(executer.Object);
            var mysql = new MySqlApiMock();

            var t = mysql.ListTables(db.Object);

            Assert.Collection(t, x => Assert.Equal("table1", x));
            cmd.VerifySet(x => x.CommandText = "SELECT TABLE_NAME FROM information_schema.tables WHERE TABLE_SCHEMA='kooboo' AND TABLE_TYPE = 'BASE TABLE';", Times.Once());
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
            var mysql = new MySqlApiMock();
            var result = mysql.ConvertDataValue(new[] { data.Object }, columns);

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
            var mysql = new MySqlApiMock();

            var result = mysql.GetClrType(new DatabaseItemEdit { DataType = dataType });

            Assert.Equal(expected, result);
        }

        class MySqlApiMock : MySqlApi
        {
            public new bool IsExistTable(IRelationalDatabase db, string name)
            {
                return base.IsExistTable(db, name);
            }

            public new List<string> ListTables(IRelationalDatabase db)
            {
                return base.ListTables(db);
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
