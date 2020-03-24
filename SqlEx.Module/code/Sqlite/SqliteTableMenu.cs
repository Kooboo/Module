using System.Collections.Generic;
using Kooboo.Data.Context;
using Kooboo.Data.Language;
using Kooboo.Web.Menus;

namespace SqlEx.Module.code.Sqlite
{
    public class SqliteTableMenu : ISideBarMenu
    {
        public SideBarSection Parent => SideBarSection.Database;

        public string Name => "sqlite.table";

        public string Icon => "";

        public string Url => "sqlex.module/sqlite.html";

        public int Order => 4;

        public List<ICmsMenu> SubItems { get; set; }

        public string GetDisplayName(RenderContext Context)
        {
            return Hardcoded.GetValue(Name, Context);
        }
    }
}
