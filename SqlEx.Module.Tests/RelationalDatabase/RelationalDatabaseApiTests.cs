using Kooboo.Api;
using Kooboo.Sites.Models;
using Kooboo.Sites.Scripting.Interfaces;
using Kooboo.Web.ViewModel;
using KScript;
using Moq;
using Moq.Protected;
using SqlEx.Module.code.RelationalDatabase;
using SqlEx.Module.code.RelationalDatabase.SchemaStore;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace SqlEx.Module.Tests.RelationalDatabaseApi
{
    public class RelationalDatabaseApiTests
    {
        [Fact]
        public void Tables_Should_Call_ListTables()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };
            var tables = new List<string> { "t1", "t2" };
            api.Protected().Setup<List<string>>("ListTables", ItExpr.IsAny<IRelationalDatabase>()).Returns(tables);

            var result = api.Object.Tables(new ApiCall());

            Assert.Equal(tables, result);
            api.Protected().Verify("SyncSchema", Times.Once(), api.Object.MockDb.Object, api.Object.MockRepo.Object);
            api.Protected().Verify("ListTables", Times.Once(), api.Object.MockDb.Object);
        }

        [Fact]
        public void CreateTable_Should_Throw_When_Name_Invalid()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };

            var ex = Assert.Throws<Exception>(() =>
                api.Object.CreateTable("table*name", new ApiCall()));

            Assert.Equal("Only Alphanumeric are allowed to use as a table", ex.Message);
        }

        [Fact]
        public void CreateTable_Should_Call_Table_All_And_Add_SchemaMapping()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };
            var table = new Mock<ITable>();
            api.Object.MockDb.Setup(x => x.GetTable(It.IsAny<string>())).Returns(table.Object);

            api.Object.CreateTable("tablename", new ApiCall());

            api.Object.MockDb.Verify(x => x.GetTable("tablename"), Times.Once);
            table.Verify(x => x.all(), Times.Once);
            api.Object.MockRepo.Verify(x => x.AddOrUpdateSchema(api.Object.DbType, "tablename",
                    It.Is<List<DbTableColumn>>(c => c.Count == 1 && c[0].Name == "_id")),
                Times.Once);
        }

        [Fact]
        public void DeleteTables_Should_Not_Call_Db_Execute_When_No_Names()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };

            api.Object.DeleteTables("[]", new ApiCall());

            api.Object.MockDb.Verify(x => x.Execute(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void DeleteTables_Should_Call_Db_Execute_And_Update_SchemaMapping()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };

            api.Object.DeleteTables("['table1', 'table2']", new ApiCall());

            api.Object.MockDb.Verify(x => x.Execute("DeleteTables_table1,table2"), Times.Once);
            api.Object.MockRepo.Verify(
                x => x.DeleteTableSchemas(api.Object.DbType, 
                    It.Is<string[]>(ts => ts.Length == 2 && ts[0] == "table1" && ts[1] == "table2")), Times.Once);
        }

        [Fact]
        public void IsUniqueTableName_Should_Call_IsExistTable()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };
            api.Protected().Setup<bool>("IsExistTable", ItExpr.IsAny<IRelationalDatabase>(), ItExpr.IsAny<string>()).Returns(false);

            var result = api.Object.IsUniqueTableName("table1", new ApiCall());

            api.Protected().Verify("IsExistTable", Times.Once(), api.Object.MockDb.Object, "table1");
            Assert.True(result);
        }

        [Fact]
        public void Columns_Should_Return_Columns()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };
            var columns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "_id" },
                new DbTableColumn { Name = "a" }
            };
            api.Object.MockRepo.Setup(x => x.GetColumns(api.Object.DbType, It.IsAny<string>())).Returns(columns);

            var result = api.Object.Columns("table1", new ApiCall());

            api.Protected().Verify("SyncSchema", Times.Once(), api.Object.MockDb.Object, api.Object.MockRepo.Object);
            Assert.Collection(result, x => Assert.Equal(columns[1], x));
        }

        [Fact]
        public void UpdateColumn_Should_Update_Correctly()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };
            var columns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "c1" },
                new DbTableColumn { Name = "c2" },
                new DbTableColumn { Name = "a" }
            };
            var oriColumns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "_id" },
                new DbTableColumn { Name = "c1" },
                new DbTableColumn { Name = "c2" },
            };
            api.Object.MockRepo.Setup(x => x.GetColumns(api.Object.DbType, It.IsAny<string>())).Returns(new List<DbTableColumn>());
            api.Object.MockDb.Setup(x => x.GetTable(It.IsAny<string>())).Returns(new Mock<ITable>().Object);
            api.Object.MockCmd.Setup(x => x.GetDefaultColumns()).Returns(oriColumns);
            api.Protected().Setup("UpdateTable",
                ItExpr.IsAny<IRelationalDatabase>(), ItExpr.IsAny<string>(),
                ItExpr.IsAny<List<DbTableColumn>>(), ItExpr.IsAny<List<DbTableColumn>>());

            api.Object.UpdateColumn("table1", columns, new ApiCall());

            api.Object.MockRepo.Verify(x => x.AddOrUpdateSchema(api.Object.DbType, "table1", oriColumns), Times.Once);
            api.Protected().Verify("UpdateTable", Times.Once(), api.Object.MockDb.Object, "table1", columns, oriColumns);
            api.Object.MockRepo.Verify(x => x.AddOrUpdateSchema(api.Object.DbType, "table1", columns), Times.Once);

        }

        [Fact]
        public void Data_Should_OrderBy_PrimaryKey_Column_When_Sortfield_Not_Found()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };
 
            var columns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "c1" },
                new DbTableColumn { Name = "pk", IsPrimaryKey = true },
                new DbTableColumn { Name = "c2" },
            };
            api.Object.MockRepo.Setup(x => x.GetColumns(api.Object.DbType, It.IsAny<string>())).Returns(columns);
            var count = new Mock<IDynamicTableObject>();
            count.SetupGet(x => x.Values).Returns(new Dictionary<string, object> { { "count", 1 } });
            api.Object.MockDb
                .SetupSequence(x => x.Query(It.IsAny<string>()))
                .Returns(new[] { count.Object })
                .Returns(new IDynamicTableObject[0]);
            var call = new ApiCall();
            call.Context.Request.QueryString.Add("sort", "id");

            api.Object.Data("table1", call);

            api.Object.MockCmd.Verify(x => x.GetPagedData("table1", It.IsAny<int>(), It.IsAny<int>(), "pk"));
        }

        [Fact]
        public void Data_Should_Call_Methods_Correctly()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };
            var columns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "pk", IsPrimaryKey = true },
            };
            api.Object.MockRepo.Setup(x => x.GetColumns(api.Object.DbType, It.IsAny<string>())).Returns(columns);
            var count = new Mock<IDynamicTableObject>();
            count.SetupGet(x => x.Values).Returns(new Dictionary<string, object> { { "count", 12 } });
            var data = new[] { count.Object };
            api.Object.MockDb
                .Setup(x => x.Query(It.IsAny<string>()))
                .Returns(data);
            var call = new ApiCall();
            call.Context.Request.QueryString.Add("sort", "id");
            call.Context.Request.QueryString.Add("pagenr", "3");
            call.Context.Request.QueryString.Add("pagesize", "5");

            var result = api.Object.Data("table1", call);

            api.Object.MockCmd.Verify(x => x.GetTotalCount("table1"));
            api.Object.MockDb.Verify(x => x.Query("GetTotalCount_table1"), Times.Once);
            api.Object.MockCmd.Verify(x => x.GetPagedData("table1", 10, 5, "pk"));
            api.Protected().Verify("ConvertDataValue", Times.Once(), data, columns);
            Assert.Equal("pk", result.PrimaryKey);
            Assert.Equal(3, result.PageNr);
            Assert.Equal(5, result.PageSize);
            Assert.Equal(12, result.TotalCount);
            Assert.Equal(3, result.TotalPages);
            Assert.Single(result.List);
            Assert.Collection(result.List[0], x =>
            {
                Assert.Equal("count", x.key);
                Assert.Equal(12, x.value);
            });
        }

        [Fact]
        public void GetEdit_Should_Return_Columns()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };
            var columns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "id", DataType="number" },
                new DbTableColumn { Name = "c1", DataType="string" },
                new DbTableColumn { Name = "c2",DataType="bool" },
                new DbTableColumn { Name = "c3",DataType="datetime" }
            };
            api.Object.MockRepo.Setup(x => x.GetColumns(api.Object.DbType, It.IsAny<string>())).Returns(columns);
            var table = new Mock<ITable>();
            var data = new Mock<IDynamicTableObject>();
            data.SetupGet(x => x.Values).Returns(new Dictionary<string, object>
            {
                { "id", 3 },
                { "c1", "c1" },
                { "c2", "true" },
                { "c3", "2010-01-02" },
            });
            table.Setup(x => x.get(It.IsAny<object>())).Returns(data.Object);
            api.Object.MockDb.Setup(x => x.GetTable(It.IsAny<string>())).Returns(table.Object);

            var result = api.Object.GetEdit("table1", "3", new ApiCall() { WebSite = new Kooboo.Data.Models.WebSite() { Name = System.Guid.NewGuid().ToString() } });

            Assert.Collection(result,
                x =>
                {
                    Assert.Equal("id", x.Name);
                    Assert.Equal(3, x.Value);
                },
                x =>
                {
                    Assert.Equal("c1", x.Name);
                    Assert.Equal("c1", x.Value);
                },
                x =>
                {
                    Assert.Equal("c2", x.Name);
                    Assert.True((bool)x.Value);
                },
                x =>
                {
                    Assert.Equal("c3", x.Name);
                    Assert.Equal(new DateTime(2010, 01, 02), x.Value);
                });
        }

        [Fact]
        public void UpdateData_Should_Return_Empty_When_Not_Found()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };
            var columns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "id", DataType="number" },
            };
            api.Object.MockRepo.Setup(x => x.GetColumns(api.Object.DbType, It.IsAny<string>())).Returns(columns);
            var table = new Mock<ITable>();
            api.Object.MockDb.Setup(x => x.GetTable(It.IsAny<string>())).Returns(table.Object);

            var result = api.Object.UpdateData("table1", "3", new List<DatabaseItemEdit>(), new ApiCall());

            Assert.Equal("", result);
            table.Verify(x => x.update(It.IsAny<object>(), It.IsAny<object>()), Times.Never);
            table.Verify(x => x.add(It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public void UpdateData_Should_Call_Table_Update()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };
            var columns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "id", DataType = "number", IsSystem = true },
                new DbTableColumn { Name = "c1", DataType = "string" },
                new DbTableColumn { Name = "c2", DataType = "bool" },
                new DbTableColumn { Name = "c3", DataType = "datetime" }
            };
            api.Object.MockRepo.Setup(x => x.GetColumns(api.Object.DbType, It.IsAny<string>())).Returns(columns);
            var table = new Mock<ITable>();
            var data = new Mock<IDynamicTableObject>();
            data.SetupGet(x => x.Values).Returns(new Dictionary<string, object>
            {
                { "id", 3 },
                { "c1", "c1" },
                { "c2", "true" },
                { "c3", "2010-01-02" },
            });
            table.Setup(x => x.get(It.IsAny<object>())).Returns(data.Object);
            api.Object.MockDb.Setup(x => x.GetTable(It.IsAny<string>())).Returns(table.Object);
            var values = new List<DatabaseItemEdit>
            {
                new DatabaseItemEdit { Name = "id", Value = 3 },
                new DatabaseItemEdit { Name = "c1", Value = "c1_new" },
            };

            var result = api.Object.UpdateData("table1", "3", values, new ApiCall());

            Assert.Equal("3", result);
            Func<Dictionary<string, object>, bool> VerifyDic = d =>
            {
                Assert.Collection(d,
                    x =>
                    {
                        Assert.Equal("id", x.Key);
                        Assert.Equal(3, x.Value);
                    },
                    x =>
                    {
                        Assert.Equal("c1", x.Key);
                        Assert.Equal("c1_new", x.Value);
                    });
                return true;
            };
            table.Verify(x => x.update("3", It.Is<Dictionary<string, object>>(d => VerifyDic(d))), Times.Once);
            table.Verify(x => x.add(It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public void UpdateData_Should_Call_AddData_When_Id_Empty()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };
            api.Protected().Setup<string>("AddData",
                    ItExpr.IsAny<ITable>(),
                    ItExpr.IsAny<List<DatabaseItemEdit>>(),
                    ItExpr.IsAny<List<DatabaseItemEdit>>())
                .Returns("added");
            var columns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "id", DataType = "number", IsSystem = true },
                new DbTableColumn { Name = "c1", DataType = "string" },
                new DbTableColumn { Name = "c2", DataType = "string" },
            };
            api.Object.MockRepo.Setup(x => x.GetColumns(api.Object.DbType, It.IsAny<string>())).Returns(columns);
            var table = new Mock<ITable>();
            api.Object.MockDb.Setup(x => x.GetTable(It.IsAny<string>())).Returns(table.Object);
            var values = new List<DatabaseItemEdit>
            {
                new DatabaseItemEdit { Name = "id", Value = 3 },
                new DatabaseItemEdit { Name = "c1", Value = "c1_new" },
            };

            var result = api.Object.UpdateData("table1", " ", values, new ApiCall());

            api.Protected().Verify("AddData",
                Times.Once(),
                table.Object,
                values,
                ItExpr.Is<List<DatabaseItemEdit>>(c => c.Count == 3));
        }

        [Fact]
        public void AddData_Should_Call_AddData()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };
            api.Protected().Setup<string>("AddData",
                    ItExpr.IsAny<ITable>(),
                    ItExpr.IsAny<List<DatabaseItemEdit>>(),
                    ItExpr.IsAny<List<DatabaseItemEdit>>())
                .Returns("added");
            var columns = new List<DbTableColumn>
            {
                new DbTableColumn { Name = "id", DataType = "number", IsSystem = true },
                new DbTableColumn { Name = "c1", DataType = "string" },
                new DbTableColumn { Name = "c2", DataType = "string" },
            };
            api.Object.MockRepo.Setup(x => x.GetColumns(api.Object.DbType, It.IsAny<string>())).Returns(columns);
            var table = new Mock<ITable>();
            api.Object.MockDb.Setup(x => x.GetTable(It.IsAny<string>())).Returns(table.Object);
            var values = new List<DatabaseItemEdit>
            {
                new DatabaseItemEdit { Name = "id", Value = 3 },
                new DatabaseItemEdit { Name = "c1", Value = "c1_new" },
            };

            var result = api.Object.AddData("table1", values, new ApiCall() {  WebSite = new Kooboo.Data.Models.WebSite() {  Name= System.Guid.NewGuid().ToString()} });

            api.Protected().Verify("AddData",
                Times.Once(),
                table.Object,
                values,
                ItExpr.Is<List<DatabaseItemEdit>>(c => c.Count == 3));
        }

        [Fact]
        public void DeleteData_Should_Call_ExeCute_Correctly()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };
            var sqlExecuter = new Mock<ISqlExecuter>();
            api.Object.MockDb.SetupGet(x => x.SqlExecuter).Returns(sqlExecuter.Object);
            var ids = new List<string> { "k1", "k2" };

            api.Object.DeleteData("table1", ids, new ApiCall());

            api.Object.MockCmd.Verify(x => x.DeleteData("table1", "_id", ids));
            api.Object.MockDb.Verify(x => x.Execute("DeleteData_table1__id_k1,k2"));
        }

        [Fact]
        public void SyncSchema_Should_Call_SyncSchema()
        {
            var api = new Mock<RelationalDatabaseApiMock> { CallBase = true };
            api.Protected().Setup<Dictionary<string, List<DbTableColumn>>>("SyncSchema", ItExpr.IsAny<IRelationalDatabase>(), ItExpr.IsAny<TableSchemaMappingRepository>())
                .Returns(new Dictionary<string, List<DbTableColumn>>());

            api.Object.SyncSchema(new ApiCall());

            api.Protected().Verify("SyncSchema", Times.Once(), api.Object.MockDb.Object, api.Object.MockRepo.Object);
        }

        internal class RelationalDatabaseApiMock : RelationalDatabaseApi<RelationalDatabaseCommandMock>
        {
            public Mock<IRelationalDatabaseRawCommands> MockCmd => Cmd.MockCmd;

            public Mock<IRelationalDatabase> MockDb { get; } =
                new Mock<IRelationalDatabase>();

            public Mock<TableSchemaMappingRepository> MockRepo { get; } =
                new Mock<TableSchemaMappingRepository>();

            public override string DbType { get; }
            public override bool RequireSite { get; }
            public override bool RequireUser { get; }

            protected override IRelationalDatabase GetDatabase(ApiCall call)
            {
                return MockDb.Object;
            }
          

            protected override Type GetClrType(DatabaseItemEdit column)
            {
                return typeof(object);
            }

            protected override TableSchemaMappingRepository GetSchemaMappingRepository(ApiCall call)
            {
                return MockRepo.Object;
            }

            protected override Dictionary<string, List<DbTableColumn>> SyncSchema(IRelationalDatabase db, TableSchemaMappingRepository schemaRepository)
            {
                return new Dictionary<string, List<DbTableColumn>>();
            }

            internal override void UpdateIndex(IRelationalDatabase db, string tablename, List<DbTableColumn> columns)
            {
                throw new NotImplementedException();
            }

            internal override string[] GetIndexColumns(IRelationalDatabase db, string table)
            {
                throw new NotImplementedException();
            }
        }
    }
}
