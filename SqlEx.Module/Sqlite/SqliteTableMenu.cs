using Kooboo.Data.Context;
using Kooboo.Data.Language;
using Kooboo.Web.Menus;
using System.Collections.Generic;

namespace SqlEx.Module.Sqlite
{
    public class SqliteTableMenu : ISideBarMenu
    {
        public SideBarSection Parent => SideBarSection.Database;

        public string Name => Hardcoded.GetValue("sqlite.table");

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
