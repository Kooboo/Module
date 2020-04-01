//Copyright (c) 2018 Yardi Technology Limited. Http://www.kooboo.com 
//All rights reserved.

using Kooboo.Api;
using Kooboo.Lib.Helper;
using Kooboo.Sites.Models;
using Kooboo.Sites.Scripting.Interfaces;
using Kooboo.Web.ViewModel;
using KScript;
using SqlEx.Module.code.RelationalDatabase.SchemaStore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlEx.Module.code.RelationalDatabase
{
    public abstract class RelationalDatabaseApi<TCommands> : IRelationalDatabaseApi
    where TCommands : IRelationalDatabaseRawCommands, new()
    {
        protected const string DefaultIdFieldName = Kooboo.IndexedDB.Dynamic.Constants.DefaultIdFieldName;
        protected static readonly TCommands Cmd = Activator.CreateInstance<TCommands>();

        public abstract string DbType { get; }

        public virtual string ModelName => DbType;

        public abstract bool RequireSite { get; }

        public abstract bool RequireUser { get; }

        public List<string> Tables(ApiCall call)
        {
            var db = GetDatabase(call);
            SyncSchema(db, GetSchemaMappingRepository(call));
            return ListTables(db);
        }

        public void CreateTable(string name, ApiCall call)
        {
            if (!Kooboo.IndexedDB.Helper.CharHelper.IsValidTableName(name))
            {
                throw new Exception(Kooboo.Data.Language.Hardcoded.GetValue("Only Alphanumeric are allowed to use as a table", call.Context));
            }

            var db = GetDatabase(call);

            // create table and schema
            db.GetTable(name).all();
            GetSchemaMappingRepository(call).AddOrUpdateSchema(name, Cmd.GetDefaultColumns());
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
            GetSchemaMappingRepository(call).DeleteTableSchemas(tables);
        }

        public bool IsUniqueTableName(string name, ApiCall call)
        {
            var db = GetDatabase(call);
            return !IsExistTable(db, name);
        }

        public List<DbTableColumn> Columns(string table, ApiCall call)
        {
            var db = GetDatabase(call);
            var schemaRepository = GetSchemaMappingRepository(call);
            SyncSchema(db, schemaRepository);
            var columns = schemaRepository.GetColumns(table);
            return columns.Where(x => x.Name != DefaultIdFieldName).ToList();
        }

        public void UpdateColumn(string tablename, List<DbTableColumn> columns, ApiCall call)
        {
            var db = GetDatabase(call);
            var schemaRepository = GetSchemaMappingRepository(call);
            var originalColumns = schemaRepository.GetColumns(tablename);
            // table not exists, create
            if (originalColumns.Count <= 0)
            {
                db.GetTable(tablename).all();
                originalColumns = Cmd.GetDefaultColumns();
                schemaRepository.AddOrUpdateSchema(tablename, originalColumns);
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
                UpdateTable(db, tablename, columns, originalColumns);
            }

            if (shouldUpdateSchema)
            {
                schemaRepository.AddOrUpdateSchema(tablename, columns);
            }
        }

        public PagedListViewModelWithPrimaryKey<List<DataValue>> Data(string table, ApiCall call)
        {
            var db = GetDatabase(call);
            var sortfield = call.GetValue("sort", "orderby", "order");
            // verify sortfield. 
            var columns = GetSchemaMappingRepository(call).GetColumns(table);
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
                var primarycol = columns.FirstOrDefault(o => o.IsPrimaryKey) ?? columns.FirstOrDefault();
                if (primarycol != null)
                {
                    sortfield = primarycol.Name;
                }
            }

            var pager = ApiHelper.GetPager(call, 30);

            var result = new PagedListViewModelWithPrimaryKey<List<DataValue>>
            {
                PrimaryKey = columns.FirstOrDefault(o => o.IsPrimaryKey)?.Name
            };

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
            var obj = id == Guid.Empty.ToString() ? null : db.GetTable(tablename).get(id);
            var cloumns = GetAllColumnsForItemEdit(GetSchemaMappingRepository(call), tablename);

            foreach (var model in cloumns)
            {
                // get value
                if (obj != null && obj.Values.ContainsKey(model.Name))
                {
                    var value = obj.Values[model.Name];
                    if (value != null)
                    {
                        if (model.DataType.ToLower() == "bool")
                        {
                            model.Value = Convert.ChangeType(value, typeof(bool));
                        }
                        else if (model.DataType.ToLower() == "datetime" && !(value is DateTime))
                        {
                            if (DateTime.TryParse(value.ToString(), out var time))
                            {
                                model.Value = time;
                            }
                        }

                        model.Value = model.Value ?? value;
                    }
                }

                result.Add(model);
            }

            return result;
        }

        public string UpdateData(string tablename, string id, List<DatabaseItemEdit> values, ApiCall call)
        {
            var db = GetDatabase(call);
            var dbTable = db.GetTable(tablename);
            var columns = GetAllColumnsForItemEdit(GetSchemaMappingRepository(call), tablename);

            // edit
            if (!string.IsNullOrWhiteSpace(id) && id != Guid.Empty.ToString())
            {
                var obj = dbTable.get(id)?.Values;
                if (obj == null)
                {
                    return "";
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
            return AddData(dbTable, values, columns);
        }

        public string AddData(string tablename, List<DatabaseItemEdit> values, ApiCall call)
        {
            var db = GetDatabase(call);
            var dbTable = db.GetTable(tablename);
            var columns = GetAllColumnsForItemEdit(GetSchemaMappingRepository(call), tablename);

            return AddData(dbTable, values, columns);
        }

        protected virtual string AddData(ITable dbTable, List<DatabaseItemEdit> values, List<DatabaseItemEdit> columns)
        {
            var add = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in columns.Where(o => !o.IsSystem))
            {
                if (!item.IsIncremental)
                {
                    var value = values.Find(o => o.Name.ToLower() == item.Name.ToLower());
                    if (value == null)
                    {
                        add.Remove(item.Name);
                        continue;
                    }

                    if (value.Value == null)
                    {
                        add[item.Name] = null;
                        continue;
                    }

                    if (item.IsPrimaryKey && value.Value is string s && s == " ")
                    {
                        add[item.Name] = null;
                    }
                    else
                    {
                        add[item.Name] = Kooboo.Lib.Reflection.TypeHelper.ChangeType(value.Value, GetClrType(item));
                    }
                }
            }

            return dbTable.add(add)?.ToString() ?? "";
        }

        public void DeleteData(string tablename, List<string> values, ApiCall call)
        {
            var db = GetDatabase(call);
            var primaryKey = db.SqlExecuter.GetSchema(tablename)?.PrimaryKey ?? DefaultIdFieldName;
            db.Execute(Cmd.DeleteData(tablename, primaryKey, values));
        }

        public void SyncSchema(ApiCall call)
        {
            var db = GetDatabase(call);
            SyncSchema(db, GetSchemaMappingRepository(call));
        }

        protected virtual void UpdateTable(IRelationalDatabase db, string tablename, List<DbTableColumn> columns, List<DbTableColumn> originalColumns)
        {
            db.Execute(Cmd.UpdateTable(tablename, originalColumns, columns));
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

        protected virtual Dictionary<string, List<DbTableColumn>> SyncSchema(IRelationalDatabase db, ISchemaMappingRepository schemaRepository)
        {
            var newCloumnFromDb = new Dictionary<string, List<DbTableColumn>>();
            var koobooSchemas = schemaRepository.SelectAll().Where(x => x != null).ToDictionary(x => x.TableName, x => x.Columns);
            var allTables = ListTables(db);

            var deletedTables = koobooSchemas.Keys.Except(allTables).ToArray();
            if (deletedTables.Length > 0)
            {
                schemaRepository.DeleteTableSchemas(deletedTables);
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
                        IsSystem = s.Name == DefaultIdFieldName,
                        IsUnique = s.Name == DefaultIdFieldName,
                        IsIndex = s.Name == DefaultIdFieldName,
                        Name = s.Name,
                        DataType = Cmd.DbTypeToDataType(s.Type),
                        IsPrimaryKey = s.IsPrimaryKey,
                        ControlType = Cmd.DbTypeToControlType(s.Type)
                    }).ToList();

                    schemaRepository.AddOrUpdateSchema(table, koobooSchema);
                    newCloumnFromDb.Add(table, koobooSchema);
                }
                else
                {
                    // update
                    var columns = koobooSchema.Select(x => x.Name).ToArray();
                    var newSchema = koobooSchema.ToList();
                    // remove columns that no longer exists in db
                    newSchema.RemoveAll(x => dbSchema.Items.All(c => c.Name != x.Name));
                    // new columns added in db
                    var dbNewColumn = dbSchema.Items.Where(x => !columns.Contains(x.Name))
                        .Select(s =>
                            new DbTableColumn
                            {
                                IsSystem = s.Name == DefaultIdFieldName,
                                IsUnique = s.Name == DefaultIdFieldName,
                                IsIndex = s.Name == DefaultIdFieldName,
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
                        schemaRepository.AddOrUpdateSchema(table, newSchema);
                    }
                }
            }

            return newCloumnFromDb;
        }

        private List<DatabaseItemEdit> GetAllColumnsForItemEdit(ISchemaMappingRepository schemaRepository, string table)
        {
            return schemaRepository.GetColumns(table)
                .Select(x => new DatabaseItemEdit
                {
                    ControlType = x.ControlType,
                    DataType = x.DataType,
                    Name = x.Name,
                    Setting = x.Setting,
                    IsIncremental = x.IsIncremental,
                    IsIndex = x.IsIndex,
                    IsPrimaryKey = x.IsPrimaryKey,
                    IsSystem = x.IsSystem,
                    IsUnique = x.IsUnique,
                    Scale = x.Scale,
                    Seed = x.Seed
                })
                .ToList();
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
                if (oriCol.Name != newCol.Name)
                {
                    shouldUpdateTable = true;
                    shouldUpdateSchema = true;
                    return;
                }

                if (oriCol.ControlType != newCol.ControlType || oriCol.Setting != newCol.Setting)
                {
                    shouldUpdateSchema = true;
                }
            }
        }

        protected virtual ISchemaMappingRepository GetSchemaMappingRepository(ApiCall call)
        {
            return new SchemaMappingRepository(DbType, call);
        }
    }
}
