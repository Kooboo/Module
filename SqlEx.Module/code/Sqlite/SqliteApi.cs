//Copyright (c) 2018 Yardi Technology Limited. Http://www.kooboo.com 
//All rights reserved.

using Kooboo.Api;
using Kooboo.Sites.Models;
using Kooboo.Sites.Scripting.Interfaces;
using Kooboo.Web.ViewModel;
using KScript;
using SqlEx.Module.code.RelationalDatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlEx.Module.code.Sqlite
{
    public class SqliteApi : RelationalDatabaseApi<SqliteCommands>
    {
        private static readonly Regex ColumnNameRegex = new Regex("\"(.+?)\"", RegexOptions.Compiled);

        public override string DbType => "Sqlite";

        public override bool RequireSite => true;

        public override bool RequireUser => false;

        protected override IRelationalDatabase GetDatabase(ApiCall call)
        {
            return new k(call.Context).Sqlite;
        }

        protected override void UpdateTable(IRelationalDatabase db, string tablename, List<DbTableColumn> columns, List<DbTableColumn> originalColumns)
        {
            var sb = new System.Text.StringBuilder();
            var sqls = db.Query(Cmd.GetSqls(tablename))
                .Select(x => new
                {
                    Type = (string)x.Values["type"],
                    Name = (string)x.Values["name"],
                    Sql = (string)x.Values["sql"]
                })
                .OrderByDescending(x => x.Type)
                .ToArray();

            var oldTable = $"_old_{tablename}_{DateTime.Now:yyyyMMddHHmmss}";
            // drop old index
            foreach (var index in sqls.Where(x => x.Type == "index"))
            {
                sb.AppendLine($"DROP INDEX {index.Name};");
            }

            // rename table
            sb.AppendLine($"ALTER TABLE {tablename} RENAME TO {oldTable};");

            // create new table and index
            foreach (var sql in sqls)
            {
                if (sql.Type == "table")
                {
                    sb.AppendLine(GetCreateTableSql(tablename, sql.Sql, columns));
                }
                //else if (sql.Type == "index")
                //{
                //    sb.AppendLine(GetCreateIndexSql(sql.Sql, columns));
                //}
            }

            // copy data
            var intersect = originalColumns.Select(x => x.Name).Intersect(columns.Select(x => x.Name)).ToArray();
            if (intersect.Any())
            {
                var cols = string.Join("\",\"", intersect);
                sb.AppendLine($"INSERT INTO {tablename} (\"{cols}\") SELECT \"{cols}\" FROM {oldTable};");
            }

            // drop old table
            sb.AppendLine($"DROP TABLE {oldTable};");

            db.Execute(sb.ToString());

            foreach (var item in columns.Where(w => w.IsIndex))
            {
                db.GetTable(tablename).createIndex(item.Name);
            }
        }

        private string GetCreateIndexSql(string oldCreateIndexSql, List<DbTableColumn> newColumns)
        {
            var splited = oldCreateIndexSql.Replace("\r", "").Replace("\n", "")
                .Split(new[] { '(' }, StringSplitOptions.RemoveEmptyEntries);

            var indexColumnRemoved = ColumnNameRegex.Matches(splited[1]).Cast<Match>().Select(x => x.Groups[1].Value)
                .Where(x => newColumns.All(n => !n.Name.Equals(x, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            if (indexColumnRemoved.Any())
            {
                throw new Exception("Cannot remove column that has index, column name: "
                                    + string.Join(", ", indexColumnRemoved));
            }

            return oldCreateIndexSql + ";";
        }

        private string GetCreateTableSql(string tablename, string oldCreateTableSql, List<DbTableColumn> newColumns)
        {
            var sb = new System.Text.StringBuilder();
            var sql = oldCreateTableSql.Replace("\r", "").Replace("\n", "");
            var oldColumns = new List<string>();
            string primaryKey = null;
            if (sql.Contains("_id TEXT PRIMARY KEY"))
            {
                primaryKey = $"PRIMARY KEY (\"_id\")";
            }
            else
            {
                var primaryKeyMatch = Regex.Match(sql, "(PRIMARY *KEY *\\([ \"_a-z,]+?\\))", RegexOptions.IgnoreCase);
                if (primaryKeyMatch.Success)
                {
                    var columns = ColumnNameRegex.Matches(primaryKeyMatch.Groups[1].Value).Cast<Match>()
                        .Select(x => x.Value)
                        .Except(newColumns.Select(x => x.Name), StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    if (columns.Any())
                    {
                        primaryKey = $"PRIMARY KEY ({string.Join(", ", columns)})";
                    }
                }
                else
                {
                    sql.Insert(sql.LastIndexOf(")"), ",");
                }
            }

            // add old column
            sb.AppendLine($"CREATE TABLE \"{tablename}\" (");
            foreach (var column in newColumns)
            {
                sb.AppendLine(GenerateColumnDefine(sql, column));
            }

            // add primary key
            if (primaryKey != null)
            {
                sb.AppendLine(primaryKey);
            }
            else
            {
                sb.Remove(sb.Length - Environment.NewLine.Length - 1, Environment.NewLine.Length + 1);
            }

            sb.AppendLine(");");
            return sb.ToString();
        }

        protected override List<List<DataValue>> ConvertDataValue(IDynamicTableObject[] data, List<DbTableColumn> columns)
        {
            var bools = columns.Where(c => c.DataType.ToLower() == "bool").ToArray();
            var datetimes = columns.Where(c => c.DataType.ToLower() == "datetime").ToArray();
            return data
                .Select(x => x.Values.Select(kv =>
                {
                    if (kv.Value != null)
                    {
                        object value = null;
                        if (bools.Any(b => b.Name == kv.Key))
                        {
                            value = Convert.ChangeType(kv.Value, typeof(bool));
                        }
                        else if (datetimes.Any(d => d.Name == kv.Key))
                        {
                            if (DateTime.TryParse(kv.Value.ToString(), out var time))
                            {
                                value = time;
                            }
                        }

                        return new DataValue { key = kv.Key, value = value ?? kv.Value };
                    }

                    return new DataValue { key = kv.Key, value = null };
                }).ToList())
                .ToList();
        }

        protected override Type GetClrType(DatabaseItemEdit column)
        {
            switch (column.DataType.ToLower())
            {
                case "number":
                    return typeof(double);
                case "bool":
                    return typeof(bool);
                case "datetime":
                case "string":
                default:
                    return typeof(string);
            }
        }

        private string GenerateColumnDefine(string sql, DbTableColumn column)
        {
            var match = Regex.Match(sql, $"(\"{column.Name}\" *[a-z\\(\\d\\) ]+?,)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Value;
            }

            string dataType;
            switch (column.DataType.ToLower())
            {
                case "number":
                    dataType = "REAL";
                    break;
                case "bool":
                    dataType = "INTEGER";
                    break;
                case "string":
                case "datetime":
                default:
                    dataType = "TEXT";
                    break;
            }

            return $"\"{column.Name}\" {dataType},";
        }

        internal override string[] GetIndexColumns(IRelationalDatabase db, string table)
        {
            var columns = new List<string>();
            var indexs = db.Query($"SELECT name from pragma_index_list('{table}')").Select(s => s.obj["name"]);

            foreach (var item in indexs)
            {
                var cols = db.Query($"SELECT name from pragma_index_info('{item}')").Select(s => s.obj["name"]).Cast<string>();
                columns.AddRange(cols);
            }

            return columns.ToArray();
        }

        internal override void UpdateIndex(IRelationalDatabase db, string tablename, List<DbTableColumn> columns)
        {
            // not need implement
            throw new NotImplementedException();
        }
    }
}
