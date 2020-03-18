//Copyright (c) 2018 Yardi Technology Limited. Http://www.kooboo.com 
//All rights reserved.

using Kooboo.Api;
using Kooboo.Lib.Helper;
using Kooboo.Sites.Models;
using Kooboo.Sites.Scripting.Interfaces;
using Kooboo.Web.ViewModel;
using KScript;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sqlite.Menager.Module.RelationalDatabase
{
    public abstract class RelationalDatabaseApi<TCommands> : IRelationalDatabaseApi
    where TCommands : IRelationalDatabaseRawCommands, new()
    {
        protected const string DefaultIdFieldName = Kooboo.IndexedDB.Dynamic.Constants.DefaultIdFieldName;
        protected static readonly IRelationalDatabaseRawCommands Cmd = Activator.CreateInstance<TCommands>();

        public abstract string ModelName { get; }

        public abstract bool RequireSite { get; }

        public abstract bool RequireUser { get; }

        public List<string> Tables(ApiCall call)
        {
            var db = GetDatabase(call);
            SyncSchema(db);
            return ListTables(db);
        }

        public void CreateTable(string name, ApiCall call)
        {
            if (!Kooboo.IndexedDB.Helper.CharHelper.IsValidTableName(name))
            {
                throw new Exception(Kooboo.Data.Language.Hardcoded.GetValue("Only Alphanumeric are allowed to use as a table", call.Context));
            }

            var db = GetDatabase(call);
            EnsureSystemTableCreated(db);

            //// create table and schema
            db.Execute(Cmd.CreateTableAndSchema(name, null, out var param), param);
        }

        public void DeleteTables(string names, ApiCall call)
        {
            var db = GetDatabase(call);
            var tables = JsonHelper.Deserialize<string[]>(names);
            if (tables.Length <= 0)
            {
                return;
            }

            db.Execute(Cmd.DeleteTables(tables));
        }

        public bool IsUniqueTableName(string name, ApiCall call)
        {
            var db = GetDatabase(call);
            return !IsExistTable(db, name);
        }

        public List<DbTableColumn> Columns(string table, ApiCall call)
        {
            var db = GetDatabase(call);
            SyncSchema(db);
            var columns = GetAllColumns(db, table);
            return columns.Where(x => x.Name != DefaultIdFieldName).ToList();
        }

        public void UpdateColumn(string tablename, List<DbTableColumn> columns, ApiCall call)
        {
            var db = GetDatabase(call);
            var originalColumns = GetAllColumns(db, tablename);
            // table not exists, create
            if (originalColumns.Count <= 0)
            {
                db.Execute(Cmd.CreateTableAndSchema(tablename, columns, out var param), param);
                return;
            }

            // update table
            var defaultIdFiled = originalColumns.FirstOrDefault(c => c.Name == DefaultIdFieldName);
            if (defaultIdFiled != null && columns.All(c => c.Name != DefaultIdFieldName))
            {
                columns.Insert(0, defaultIdFiled);
            }

            CompareColumnDifferences(originalColumns, columns, out var shouldUpdateTable, out var shouldUpdateSchema);

            if (shouldUpdateTable)
            {
                var constraints = GetConstrains(db, tablename);
                db.Execute(Cmd.UpdateColumn(tablename, originalColumns, columns, constraints));
            }

            if (shouldUpdateSchema)
            {
                db.GetTable(Cmd.KoobooSchemaTable).update(tablename,
                    new Dictionary<string, object> { { "table_schema", JsonHelper.Serialize(columns) } });
                //db.Execute(Cmd.UpdateSchema(tablename, columns, out var param), param);
            }
        }

        public PagedListViewModel<List<DataValue>> Data(string table, ApiCall call)
        {
            var db = GetDatabase(call);
            var sortfield = call.GetValue("sort", "orderby", "order");
            // verify sortfield. 
            var columns = GetAllColumns(db, table);
            if (sortfield != null)
            {
                var col = columns.FirstOrDefault(o => o.Name == sortfield);
                if (col == null)
                {
                    sortfield = null;
                }
            }

            if (sortfield == null)
            {
                var primarycol = columns.FirstOrDefault(o => o.IsPrimaryKey);
                if (primarycol != null)
                {
                    sortfield = primarycol.Name;
                }
            }


            var pager = ApiHelper.GetPager(call, 30);

            var result = new PagedListViewModel<List<DataValue>>();

            var total = db.Query(Cmd.GetTotalCount(table))[0].Values.First().Value;
            var totalcount = (int)Convert.ChangeType(total, typeof(int));

            result.TotalCount = totalcount;
            result.TotalPages = ApiHelper.GetPageCount(totalcount, pager.PageSize);
            result.PageNr = pager.PageNr;
            result.PageSize = pager.PageSize;

            var totalskip = 0;
            if (pager.PageNr > 1)
            {
                totalskip = (pager.PageNr - 1) * pager.PageSize;
            }

            var data = db.Query(Cmd.GetPagedData(table, totalskip, pager.PageSize, sortfield));

            if (data.Any())
            {
                result.List = ConvertDataValue(data, columns);
            }

            return result;
        }

        public List<DatabaseItemEdit> GetEdit(string tablename, string id, ApiCall call)
        {
            var db = GetDatabase(call);
            var result = new List<DatabaseItemEdit>();

            var obj = db.GetTable(tablename).get(id);
            var cloumns = GetAllColumnsForItemEdit(db, tablename);

            foreach (var model in cloumns)
            {
                // get value
                if (obj != null && obj.Values.ContainsKey(model.Name))
                {
                    var value = obj.Values[model.Name];
                    model.Value = model.DataType.ToLower() == "bool"
                        ? Convert.ChangeType(value, typeof(bool))
                        : value;
                }

                result.Add(model);
            }

            return result;
        }

        public Guid UpdateData(string tablename, Guid id, List<DatabaseItemEdit> values, ApiCall call)
        {
            var db = GetDatabase(call);
            var dbTable = db.GetTable(tablename);
            var columns = GetAllColumnsForItemEdit(db, tablename);

            // edit
            if (id != Guid.Empty)
            {
                var obj = dbTable.get(id).Values;
                if (obj == null)
                {
                    return Guid.Empty;
                }

                foreach (var item in columns.Where(o => !o.IsSystem))
                {
                    var value = values.Find(o => o.Name.ToLower() == item.Name.ToLower());
                    if (value == null)
                    {
                        obj.Remove(item.Name);
                    }
                    else
                    {
                        obj[item.Name] = Kooboo.Lib.Reflection.TypeHelper.ChangeType(value.Value, GetClrType(item));
                    }
                }

                dbTable.update(id, obj);
                return id;
            }

            // add
            var add = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in columns.Where(o => !o.IsSystem))
            {
                if (!item.IsIncremental)
                {
                    var value = values.Find(o => o.Name.ToLower() == item.Name.ToLower());
                    if (value == null)
                    {
                        add.Remove(item.Name);
                    }
                    else
                    {
                        add[item.Name] = Kooboo.Lib.Reflection.TypeHelper.ChangeType(value.Value, GetClrType(item));
                    }
                }
            }

            return Guid.Parse(dbTable.add(add).ToString());
        }

        public void DeleteData(string tablename, List<Guid> values, ApiCall call)
        {
            var db = GetDatabase(call);

            db.Execute(Cmd.DeleteData(tablename, values));
        }

        public void SyncSchema(ApiCall call)
        {
            var db = GetDatabase(call);
            SyncSchema(db);
        }

        protected abstract IRelationalDatabase GetDatabase(ApiCall call);

        protected abstract Type GetClrType(DatabaseItemEdit column);

        protected virtual List<string> ListTables(IRelationalDatabase db)
        {
            var tables = db.Query(Cmd.ListTables());
            return tables.Select(x => (string)x.Values.First().Value).ToList();
        }

        protected virtual bool IsExistTable(IRelationalDatabase db, string name)
        {
            var exist = db.Query(Cmd.IsExistTable(name));
            return exist.Any(x => x.Values.Count > 0);
        }

        protected virtual List<List<DataValue>> ConvertDataValue(IDynamicTableObject[] data, List<DbTableColumn> columns)
        {
            return data
                .Select(x => x.Values.Select(kv => new DataValue { key = kv.Key, value = kv.Value }).ToList())
                .ToList();
        }

        protected virtual DbConstrain[] GetConstrains(IRelationalDatabase db, string table)
        {
            var constrians = db.Query(Cmd.GetConstrains(table));
            return constrians.Select(x => new DbConstrain
            {
                Table = (string)x.Values["Table"],
                Column = (string)x.Values["Column"],
                Name = (string)x.Values["Name"],
                Type =
                    (DbConstrain.ConstrainType)Enum.Parse(typeof(DbConstrain.ConstrainType), (string)x.Values["Type"], true)
            }).ToArray();
        }

        protected virtual Dictionary<string, List<DbTableColumn>> SyncSchema(IRelationalDatabase db)
        {
            var newCloumnFromDb = new Dictionary<string, List<DbTableColumn>>();
            EnsureSystemTableCreated(db);
            var koobooSchemaTable = db.GetTable(Cmd.KoobooSchemaTable);
            var koobooSchemas = koobooSchemaTable.all().ToDictionary(
                x => (string)x.Values["table_name"],
                x => JsonHelper.Deserialize<List<DbTableColumn>>((string)x.Values["table_schema"]));
            var allTables = ListTables(db);

            var deletedTables = koobooSchemas.Keys.Except(allTables).ToArray();
            if (deletedTables.Length > 0)
            {
                db.Execute(Cmd.DeleteTables(deletedTables));
            }

            foreach (var table in allTables)
            {
                var dbSchema = db.SqlExecuter.GetSchema(table);
                koobooSchemas.TryGetValue(table, out var koobooSchema);
                if (koobooSchema == null)
                {
                    // add
                    koobooSchema = dbSchema.Items.Select(s => new DbTableColumn
                    {
                        Name = s.Name,
                        DataType = Cmd.DbTypeToDataType(s.Type),
                        IsPrimaryKey = s.IsPrimaryKey,
                        ControlType = Cmd.DbTypeToControlType(s.Type)
                    }).ToList();
                    //koobooSchemas.Add(table, koobooSchema);
                    koobooSchemaTable.add(new Dictionary<string, object>
                    {
                        { "table_name", table },
                        { "table_schema", JsonHelper.Serialize(koobooSchema) }
                    });

                    newCloumnFromDb.Add(table, koobooSchema);
                }
                else
                {
                    // update
                    var columns = koobooSchema.Select(x => x.Name).ToArray();
                    var newSchema = koobooSchema.ToList();
                    newSchema.RemoveAll(x => dbSchema.Items.All(c => c.Name != x.Name));
                    var dbNewColumn = dbSchema.Items.Where(x => !columns.Contains(x.Name))
                        .Select(s =>
                            new DbTableColumn
                            {
                                Name = s.Name,
                                DataType = Cmd.DbTypeToDataType(s.Type),
                                IsPrimaryKey = s.IsPrimaryKey,
                                ControlType = Cmd.DbTypeToControlType(s.Type)
                            })
                        .ToList();
                    newSchema.AddRange(dbNewColumn);


                    // updat schema
                    CompareColumnDifferences(koobooSchema, newSchema, out _, out var shouldUpdateSchema);

                    if (shouldUpdateSchema)
                    {
                        newCloumnFromDb.Add(table, dbNewColumn);
                        db.GetTable(Cmd.KoobooSchemaTable).update(table, new Dictionary<string, object>
                        {
                            { "table_schema", JsonHelper.Serialize(newSchema) }
                        });
                    }

                }
            }

            return newCloumnFromDb;
        }

        private void EnsureSystemTableCreated(IRelationalDatabase db)
        {
            if (!IsExistTable(db, Cmd.KoobooSchemaTable))
            {
                db.Execute(Cmd.CreateSystemTable());
            }
        }

        private List<DbTableColumn> GetAllColumns(IRelationalDatabase db, string table)
        {
            var columString = GetAllColumnsRow(db, table);
            return string.IsNullOrWhiteSpace(columString)
                ? new List<DbTableColumn>()
                : JsonHelper.Deserialize<List<DbTableColumn>>(columString);
        }

        private List<DatabaseItemEdit> GetAllColumnsForItemEdit(IRelationalDatabase db, string table)
        {
            var columString = GetAllColumnsRow(db, table);
            return string.IsNullOrWhiteSpace(columString)
                ? new List<DatabaseItemEdit>()
                : JsonHelper.Deserialize<List<DatabaseItemEdit>>(columString);
        }

        private string GetAllColumnsRow(IRelationalDatabase db, string table)
        {
            var schema = db.GetTable(Cmd.KoobooSchemaTable).get(table);
            return (string)schema?.Values["table_schema"];
        }

        private static void CompareColumnDifferences(
            IEnumerable<DbTableColumn> originalColumns,
            IEnumerable<DbTableColumn> newColumns,
            out bool shouldUpdateTable,
            out bool shouldUpdateSchema)
        {
            shouldUpdateTable = false;
            shouldUpdateSchema = false;
            var oriCols = originalColumns.Where(x => x.Name != DefaultIdFieldName)
                .OrderBy(x => x.Name).ToArray();
            var newCols = newColumns.Where(x => x.Name != DefaultIdFieldName)
                .OrderBy(x => x.Name).ToArray();
            if (oriCols.Length != newCols.Length)
            {
                shouldUpdateTable = true;
                shouldUpdateSchema = true;
                return;
            }

            for (var i = 0; i < oriCols.Length; i++)
            {
                var oriCol = oriCols[i];
                var newCol = newCols[i];
                if (GetTablePropertiesString(oriCol) != GetTablePropertiesString(newCol))
                {
                    shouldUpdateTable = true;
                    shouldUpdateSchema = true;
                    return;
                }

                if (oriCol.Setting != newCol.Setting)
                {
                    shouldUpdateSchema = true;
                }
            }

            string GetTablePropertiesString(DbTableColumn col)
            {
                return col.Name + col.DataType + col.IsIncremental + col.Seed + col.Scale + col.IsIndex +
                       col.IsPrimaryKey + col.IsUnique + col.ControlType + col.IsSystem + col.Length;
            }
        }
    }
}
