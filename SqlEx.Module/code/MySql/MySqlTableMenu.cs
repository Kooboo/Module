using System.Collections.Generic;
using Kooboo.Data.Context;
using Kooboo.Data.Language;
using Kooboo.Web.Menus;

namespace SqlEx.Module.code.MySql
{
    public class MySqlTableMenu : ISideBarMenu
    {
        public SideBarSection Parent => SideBarSection.Database;

        public string Name => "mysql.table";

        public string Icon => "";

        public string Url => "sqlex.module/mysql.html";

        public int Order => 5;

        public List<ICmsMenu> SubItems { get; set; }

        public string GetDisplayName(RenderContext Context)
        {
            return Hardcoded.GetValue(Name, Context);
        }
    }
}
