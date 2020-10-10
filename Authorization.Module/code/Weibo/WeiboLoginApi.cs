using Kooboo.Api;
using Kooboo.Sites.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Authorization.Module.code.Weibo
{
    public class WeiboLoginApi : IApi
    {
        public string ModelName => "__weibo_login";

        public bool RequireSite => true;

        public bool RequireUser => false;

        public object GetSettings(ApiCall apiCall)
        {
            var website = apiCall.Context.WebSite;
            if (website == null) throw new Exception("not website");
            var settings = apiCall.Context.WebSite.SiteDb().CoreSetting.GetSetting<WeiboSetting>();
            if (settings == null) throw new Exception("not weibo login settings");

            return new
            {
                settings.Appid,
                settings.RedirectUri
            };
        }
    }
}
