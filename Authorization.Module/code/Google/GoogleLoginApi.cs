using Authorization.Module.code.Google;
using Kooboo.Api;
using Kooboo.Sites.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Authorization.Module.code.Facebook
{
    public class GoogleLoginApi : IApi
    {
        public string ModelName => "__google_login";

        public bool RequireSite => true;

        public bool RequireUser => false;

        public object GetSettings(ApiCall apiCall)
        {
            var website = apiCall.Context.WebSite;
            if (website == null) throw new Exception("not website");
            var settings = apiCall.Context.WebSite.SiteDb().CoreSetting.GetSetting<GoogleSetting>();
            if (settings == null) throw new Exception("not google login settings");

            return new
            {
                settings.Appid,
                settings.RedirectUri,
                Scope = string.IsNullOrWhiteSpace(settings.Scope) ? "openid email profile" : settings.Scope
            };
        }
    }
}
