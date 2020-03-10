using Kooboo.Data.Context;
using Kooboo.Web.Menus;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyCustom.Module
{
    public class CustomMenu : IFeatureMenu
    {
        public string Name => "CustomMenu";

        public string Icon => "";

        public string Url => @"/testpage.html";

        public int Order => 1;

        public List<ICmsMenu> SubItems { get; set; }

        public string GetDisplayName(RenderContext Context)
        {
            return Name;
        }
    }
}
