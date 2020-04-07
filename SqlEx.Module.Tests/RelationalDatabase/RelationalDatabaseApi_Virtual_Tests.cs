using Kooboo.Api;
using Kooboo.Sites.Models;
using Kooboo.Sites.Scripting.Global.RelationalDatabase;
using Kooboo.Sites.Scripting.Interfaces;
using Kooboo.Web.ViewModel;
using KScript;
using Moq;
using SqlEx.Module.code.RelationalDatabase;
using SqlEx.Module.code.RelationalDatabase.SchemaStore;
using System;
using System.Collections.Generic;
using Xunit;

namespace SqlEx.Module.Tests.RelationalDatabaseApi
{
    public class RelationalDatabaseApi_Virtual_Tests
    {
        [Fact]
        public void AddData_Should_Call_Table_Add()
        {
            var api = new RelationalDatabaseApiMock();
            var columns = new List<DatabaseItemEdit>
            {
                new DatabaseItemEdit { Name = "id", DataType = "number", IsPrimaryKey = true },
                new DatabaseItemEdit { Name = "c1", DataType = "string" },
                new DatabaseItemEdit { Name = "c2", DataType = "bool" },
                new DatabaseItemEdit { Name = "c3", DataType = "datetime" }
            };
            var values = new List<DatabaseItemEdit>
            {
                new DatabaseItemEdit { Name = "id", Value = " " },
                new DatabaseItemEdit { Name = "c1", Value = "c1v" },
                new DatabaseItemEdit { Name = "c3", Value = null },
            };
            var table = new Mock<ITable>();

            api.AddData(table.Object, values, columns);
            Func<Dictionary<string, object>, bool> VerifyDic = d =>
            {
                Assert.Collection(d,
                    x =>
                    {
                        Assert.Equal("id", x.Key);
                        Assert.Null(x.Value);
                    },
                    x =>
                    {
                        Assert.Equal("c1", x.Key);
                        Assert.Equal("c1v", x.Value);
                    },
                    x =>
                    {
                        Assert.Equal("c3", x.Key);
                        Assert.Null(x.Value);
                    });
                return true;
            };
            table.Verify(x => x.add(It.Is<Dictionary<string, object>>(d => VerifyDic(d))), Times.Once);
        }

        [Fact]
        public void UpdateTable_Should_Call_Cmd_UpdateTable_Correctly()
        {
            var api = new RelationalDatabaseApiMock();
            var db = new Mock<IRelationalDatabase>();
            var columns = new List<DbTableColumn>();
            var originalColumns = new List<DbTableColumn>();

            api.UpdateTable(db.Object, "table1", columns, originalColumns);

            db.Verify(x => x.Execute("UpdateTable"), Times.Once);
            api.MockCmd.Verify(x => x.UpdateTable("table1", columns, originalColumns));
        }

        [Fact]
        public void ListTables_Should_Call_Cmd_UpdateTable_Correctly()
        {
            var api = new RelationalDatabaseApiMock();
            var db = new Mock<IRelationalDatabase>();
            var obj = new Mock<IDynamicTableObject>();
            obj.SetupGet(x => x.Values).Returns(
                new Dictionary<string, object> { { "name", "table1" } });
            db.Setup(x => x.Query(It.IsAny<string>()))
                .Returns(new[] { obj.Object });

            var result = api.ListTables(db.Object);

            Assert.Collection(result, x => Assert.Equal("table1", x));
            db.Verify(x => x.Query("ListTables"), Times.Once);
            api.MockCmd.Verify(x => x.ListTables());
        }

        [Fact]
        public void IsExistTable_Should_Call_Cmd_UpdateTable_Correctly()
        {
            var api = new RelationalDatabaseApiMock();
            var db = new Mock<IRelationalDatabase>();
            var obj = new Mock<IDynamicTableObject>();
            obj.SetupGet(x => x.Values).Returns(
                new Dictionary<string, object> { { "col", "table1" } });
            db.Setup(x => x.Query(It.IsAny<string>(), It.IsAny<object>()))
                .Returns(new[] { obj.Object });

            var result = api.IsExistTable(db.Object, "table1");

            Assert.True(result);
            db.Verify(x => x.Query("IsExistTable", null), Times.Once);
            object param = null;
            api.MockCmd.Verify(x => x.IsExistTable("table1", out param));
        }

        [Fact]
        public void ConvertDataValue_Should_Convert_Correctly()
        {
            var api = new RelationalDatabaseApiMock();
            var db = new Mock<IRelationalDatabase>();
            var data = new Mock<IDynamicTableObject>();
            data.SetupGet(x => x.Values)
                .Returns(new Dictionary<string, object>
                {
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

            var result = api.ConvertDataValue(new[] { data.Object }, columns);

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

        [Fact]
        public void SyncSchema_Should_Remove_SchemaMapping_When_Table_Is_Not_Exist_In_Db()
        {
            var api = new RelationalDatabaseApiMock();
            var db = new Mock<IRelationalDatabase>();
            var obj = new Mock<IDynamicTableObject>();
            obj.SetupGet(x => x.Values).Returns(
                new Dictionary<string, object> { { "name", "table1" } });
            db.Setup(x => x.Query(It.IsAny<string>()))
                .Returns(new[] { obj.Object });
            var executer = new Mock<ISqlExecuter>();
            executer.Setup(x => x.GetSchema(It.IsAny<string>()))
                .Returns(new SqliteSchema(new RelationalSchema.Item[0]));
            db.SetupGet(x => x.SqlExecuter).Returns(executer.Object);
            var store = new Mock<ISchemaMappingRepository>();
            var storeSchemas = new List<TableSchemaMapping>
            {
                new TableSchemaMapping { TableName = "a" },
                new TableSchemaMapping { TableName = "table1" },
                new TableSchemaMapping { TableName = "b" },
            };
            store.Setup(x => x.SelectAll()).Returns(storeSchemas);
            store.Setup(x => x.GetColumns(It.IsAny<string>())).Returns(new List<DbTableColumn>());

            var result = api.SyncSchema(db.Object, store.Object);

            store.Verify(x => x.DeleteTableSchemas(It.Is<string[]>(tables => tables.Length == 2 && tables[0] == "a" && tables[1] == "b")), Times.Once);
        }

        [Fact]
        public void SyncSchema_Should_Add_SchemaMapping_When_Table_Schema_Not_Exist()
        {
            var api = new RelationalDatabaseApiMock();
            var db = new Mock<IRelationalDatabase>();
            var obj = new Mock<IDynamicTableObject>();
            obj.SetupGet(x => x.Values).Returns(
                new Dictionary<string, object> { { "name", "table1" } });
            db.Setup(x => x.Query(It.IsAny<string>())).Returns(new[] { obj.Object });
            var executer = new Mock<ISqlExecuter>();
            executer.Setup(x => x.GetSchema(It.IsAny<string>()))
                .Returns(new SqliteSchema(new[]
                {
                    new RelationalSchema.Item { IsPrimaryKey = true, Name = "id", Type = "text" },
                    new RelationalSchema.Item { Name = "c1", Type = "int" },
                    new RelationalSchema.Item { Name = "c2", Type = "varchar" },
                }));
            db.SetupGet(x => x.SqlExecuter).Returns(executer.Object);
            var store = new Mock<ISchemaMappingRepository>();
            var storeSchemas = new List<TableSchemaMapping>();
            store.Setup(x => x.SelectAll()).Returns(storeSchemas);
            store.Setup(x => x.GetColumns(It.IsAny<string>())).Returns(new List<DbTableColumn>());

            var result = api.SyncSchema(db.Object, store.Object);

            Assert.Collection(result.Keys, x => Assert.Equal("table1", x));
            Assert.Collection(result.Values, x =>
            {
                Assert.Collection(x,
                    c =>
                    {
                        Assert.Equal("id", c.Name);
                        Assert.Equal("DbTypeToDataType", c.DataType);
                        Assert.True(c.IsPrimaryKey);
                        Assert.Equal("DbTypeToControlType", c.ControlType);
                    },
                    c =>
                    {
                        Assert.Equal("c1", c.Name);
                        Assert.Equal("DbTypeToDataType", c.DataType);
                        Assert.False(c.IsPrimaryKey);
                        Assert.True(c.IsIndex);
                        Assert.Equal("DbTypeToControlType", c.ControlType);
                    },
                    c =>
                    {
                        Assert.Equal("c2", c.Name);
                        Assert.Equal("DbTypeToDataType", c.DataType);
                        Assert.False(c.IsPrimaryKey);
                        Assert.Equal("DbTypeToControlType", c.ControlType);
                    });
            });

            api.MockCmd.Verify(x => x.DbTypeToControlType("text"));
            api.MockCmd.Verify(x => x.DbTypeToDataType("text"));
            api.MockCmd.Verify(x => x.DbTypeToControlType("int"));
            api.MockCmd.Verify(x => x.DbTypeToDataType("int"));
            api.MockCmd.Verify(x => x.DbTypeToControlType("varchar"));
            api.MockCmd.Verify(x => x.DbTypeToDataType("varchar"));

            store.Verify(x => x.AddOrUpdateSchema("table1", result["table1"]), Times.Once);
        }

        [Fact]
        public void SyncSchema_Should_Update_SchemaMapping_When_Table_Schema_Changed()
        {
            var api = new RelationalDatabaseApiMock();
            var db = new Mock<IRelationalDatabase>();
            var obj = new Mock<IDynamicTableObject>();
            obj.SetupGet(x => x.Values).Returns(
                new Dictionary<string, object> { { "name", "table1" } });
            db.Setup(x => x.Query(It.IsAny<string>()))
                .Returns(new[] { obj.Object });
            var executer = new Mock<ISqlExecuter>();
            executer.Setup(x => x.GetSchema(It.IsAny<string>()))
                .Returns(new SqliteSchema(new[]
                {
                    new RelationalSchema.Item { IsPrimaryKey = true, Name = "_id", Type = "text" },
                    new RelationalSchema.Item { Name = "c1", Type = "int" },
                    new RelationalSchema.Item { Name = "c2", Type = "varchar" },
                }));
            db.SetupGet(x => x.SqlExecuter).Returns(executer.Object);
            var store = new Mock<ISchemaMappingRepository>();
            var storeSchemas = new List<TableSchemaMapping>
            {
                new TableSchemaMapping
                {
                    TableName = "table1",
                    Columns = new List<DbTableColumn>
                    {
                        new DbTableColumn { IsPrimaryKey = true, Name = "_id", DataType = "string" },
                        new DbTableColumn { Name = "c1", DataType = "number", ControlType = "Number" },
                        new DbTableColumn { Name = "ac1", DataType = "string", ControlType = "TextBox" },
                    }
                }
            };
            store.Setup(x => x.SelectAll()).Returns(storeSchemas);
            store.Setup(x => x.GetColumns(It.IsAny<string>())).Returns(new List<DbTableColumn>());

            var result = api.SyncSchema(db.Object, store.Object);

            Assert.Collection(result.Keys, x => Assert.Equal("table1", x));
            Assert.Collection(result.Values, x =>
            {
                Assert.Collection(x,
                    c =>
                    {
                        Assert.Equal("c2", c.Name);
                        Assert.Equal("DbTypeToDataType", c.DataType);
                        Assert.False(c.IsPrimaryKey);
                        Assert.Equal("DbTypeToControlType", c.ControlType);
                    });
            });

            api.MockCmd.Verify(x => x.DbTypeToControlType("varchar"));
            api.MockCmd.Verify(x => x.DbTypeToDataType("varchar"));

            Func<List<DbTableColumn>, bool> storeVerify = cs =>
            {
                Assert.Collection(cs,
                    c =>
                    {
                        Assert.Equal("_id", c.Name);
                        Assert.Equal("string", c.DataType);
                        Assert.True(c.IsPrimaryKey);
                    },
                    c =>
                    {
                        Assert.Equal("c1", c.Name);
                        Assert.Equal("number", c.DataType);
                        Assert.False(c.IsPrimaryKey);
                        Assert.True(c.IsIndex);
                        Assert.Equal("Number", c.ControlType);
                    },
                    c =>
                    {
                        Assert.Equal("c2", c.Name);
                        Assert.Equal("DbTypeToDataType", c.DataType);
                        Assert.False(c.IsPrimaryKey);
                        Assert.False(c.IsIndex);
                        Assert.Equal("DbTypeToControlType", c.ControlType);
                    });
                return true;
            };
            store.Verify(x => x.AddOrUpdateSchema("table1", It.Is<List<DbTableColumn>>(cs => storeVerify(cs))), Times.Once);
        }

        class RelationalDatabaseApiMock : RelationalDatabaseApi<RelationalDatabaseCommandMock>
        {
            public Mock<IRelationalDatabaseRawCommands> MockCmd => Cmd.MockCmd;
            public override string DbType { get; }
            public override bool RequireSite { get; }
            public override bool RequireUser { get; }

            public new string AddData(ITable dbTable, List<DatabaseItemEdit> values, List<DatabaseItemEdit> columns)
            {
                return base.AddData(dbTable, values, columns);
            }

            protected override IRelationalDatabase GetDatabase(ApiCall call)
            {
                throw new NotImplementedException();
            }

            protected override Type GetClrType(DatabaseItemEdit column)
            {
                return typeof(object);
            }

            public new void UpdateTable(IRelationalDatabase db, string tablename, List<DbTableColumn> columns, List<DbTableColumn> originalColumns)
            {
                base.UpdateTable(db, tablename, columns, originalColumns);
            }

            public new List<string> ListTables(IRelationalDatabase db)
            {
                return base.ListTables(db);
            }

            public new bool IsExistTable(IRelationalDatabase db, string name)
            {
                return base.IsExistTable(db, name);
            }

            public new List<List<DataValue>> ConvertDataValue(IDynamicTableObject[] data, List<DbTableColumn> columns)
            {
                return base.ConvertDataValue(data, columns);
            }

            public new Dictionary<string, List<DbTableColumn>> SyncSchema(IRelationalDatabase db, ISchemaMappingRepository repository)
            {
                return base.SyncSchema(db, repository);
            }

            internal override void UpdateIndex(IRelationalDatabase db, string tablename, List<DbTableColumn> columns)
            {
            }

            internal override string[] GetIndexColumns(IRelationalDatabase db, string table)
            {
                return new[] { "c1" };
            }
        }
    }
}
