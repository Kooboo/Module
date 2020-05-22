using Kooboo.Sites.Models;
using System;
using System.Collections.Generic;

namespace SqlEx.Module.code.RelationalDatabase
{
    public class TableSchemaMapping : CoreObject
    {

        public  TableSchemaMapping()
        {
            this.ConstType = 84; 
        }

        private Guid _id;
        public override Guid Id
        {
            set { _id = value; }
            get
            {
                if (_id == default(Guid))
                {
                    if (!string.IsNullOrEmpty(this.DbType) && !string.IsNullOrWhiteSpace(this.Name))
                    {
                        string unique = this.ConstType.ToString() + this.DbType + this.Name;  
                        _id = Kooboo.Data.IDGenerator.GetId(unique);
                    }
              
                }
                return _id;
            }
        } 

        public string DbType { get; set; }

        public override string Name { get; set; }

        public List<DbTableColumn> Columns { get; set; }

        public static Guid GetId(string dbtype, string tablename)
        {

            string unique = 84.ToString() + dbtype + tablename; 

            return Kooboo.Data.IDGenerator.GetId(unique);
        }

     

        public override int GetHashCode()
        {
            string unique = ""; 
            if (this.Columns !=null)
            {
                foreach (var item in this.Columns)
                {
                    unique += item.GetHashCode().ToString(); 
                }
            }

            return Kooboo.Lib.Security.Hash.ComputeInt(unique); 
        }

    }
}
