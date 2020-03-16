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
using Cmd = Sqlite.Menager.Module.code.SqliteCommands;

namespace Sqlite.Menager.Module.code
{
    public abstract class RelationalDatabaseApi : IRelationalDatabaseApi
    {
        protected const string DefaultIdFieldName = Kooboo.IndexedDB.Dynamic.Constants.DefaultIdFieldName;

        public abstract string ModelName { get; }

        public abstract bool RequireSite { get; }

        public abstract bool RequireUser { get; }

        public List<string> Tables(ApiCall call)
        {
            var db = GetDatabase(call);
            var tables = db.Query(Cmd.ListTables());
            return tables.Select(x => (string)x.Values["name"]).ToList();
        }

        public void CreateTable(string name, ApiCall call)
        {
            if (!Kooboo.IndexedDB.Helper.CharHelper.IsValidTableName(name))
            {
                throw new Exception(Kooboo.Data.Language.Hardcoded.GetValue("Only Alphanumeric are allowed to use as a table", call.Context));
            }

            var db = GetDatabase(call);
            db.EnsureSystemTableCreated();

            // create table and schema
            db.Execute(Cmd.CreateTableAndSchema(name, out var param), param);
        }

        public void DeleteTables(string names, ApiCall call)
        {
            var db = GetDatabase(call);
            var tables = JsonHelper.Deserialize<string[]>(names);
            if (tables.Length <= 0)
            {
                return;
            }

            db.Execute(Cmd.DeleteTablesAndSchema(tables));
        }

        public bool IsUniqueTableName(string name, ApiCall call)
        {
            var db = GetDatabase(call);
            return !db.IsTableExists(name);
        }

        public List<DbTableColumn> Columns(string table, ApiCall call)
        {
            var db = new k(call.Context).Sqlite;
            var columns = db.GetAllColumns(table);
            return columns.Where(x => x.Name != DefaultIdFieldName).ToList();
        }

        public void UpdateColumn(string tablename, List<DbTableColumn> columns, ApiCall call)
        {
            var db = GetDatabase(call);
            var originalColumns = db.GetAllColumns(tablename);
            // table not exists, create
            if (originalColumns.Count <= 0)
            {
                db.Execute(Cmd.CreateTableAndSchema(tablename, out var param, columns), param);
                return;
            }

            // updat table
            CompareColumnDifferences(originalColumns, columns, out var shouldUpdateTable, out var shouldUpdateSchema);

            if (shouldUpdateTable)
            {
                db.Execute(Cmd.UpdateColumn(tablename, originalColumns, columns));
            }

            if (shouldUpdateSchema)
            {
                db.Execute(Cmd.UpdateSchema(tablename, columns, out var param), param);
            }
        }

        public PagedListViewModel<List<DataValue>> Data(string table, ApiCall call)
        {
            var db = GetDatabase(call);
            var sortfield = call.GetValue("sort", "orderby", "order");
            // verify sortfield. 
            var columns = db.GetAllColumns(table);
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

            var totalcount = db.GetTotalCount(table);

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
            var cloumns = db.GetAllColumnsForItemEdit(tablename);

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
            var columns = db.GetAllColumnsForItemEdit(tablename);

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
                        obj[item.Name] = Kooboo.Lib.Reflection.TypeHelper.ChangeType(value.Value, item.GetClrType());
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
                        add[item.Name] = Kooboo.Lib.Reflection.TypeHelper.ChangeType(value.Value, item.GetClrType());
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

        protected abstract IRelationalDatabase GetDatabase(ApiCall call);

        private static List<List<DataValue>> ConvertDataValue(IDynamicTableObject[] data, List<DbTableColumn> columns)
        {
            var bools = columns.Where(c => c.DataType.ToLower() == "bool").ToArray();
            return data
                .Select(x => x.Values.Select(kv =>
                {
                    var value = bools.Any(b => b.Name == kv.Key)
                        ? Convert.ChangeType(kv.Value, typeof(bool))
                        : kv.Value;
                    return new DataValue { key = kv.Key, value = value };
                }).ToList())
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
