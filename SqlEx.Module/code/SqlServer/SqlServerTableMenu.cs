using System.Collections.Generic;
using Kooboo.Data.Context;
using Kooboo.Data.Language;
using Kooboo.Web.Menus;

namespace SqlEx.Module.code.SqlServer
{
    public class SqlServerTableMenu : ISideBarMenu
    {
        public SideBarSection Parent => SideBarSection.Database;

        public string Name => "sqlserver.table";

        public string Icon => "";

        public string Url => "sqlex.module/index.html?sqlType=SqlServer";

        public int Order => 6;

        public List<ICmsMenu> SubItems { get; set; }

        public string GetDisplayName(RenderContext Context)
        {
            return Hardcoded.GetValue(Name, Context);
        }
    }
}
