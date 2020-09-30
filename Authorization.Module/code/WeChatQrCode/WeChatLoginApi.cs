using Kooboo.Api;
using Kooboo.Mail.Imap.Commands;
using Kooboo.Sites.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Authorization.Module.code.WeChatQrCode
{
    public class WeChatLoginApi : IApi
    {
        public string ModelName => "__wechat_login";

        public bool RequireSite => true;

        public bool RequireUser => false;

        public object GetSettings(ApiCall apiCall)
        {
            var website = apiCall.Context.WebSite;
            if (website == null) throw new Exception("not website");
            var settings = apiCall.Context.WebSite.SiteDb().CoreSetting.GetSetting<WeChatLoginSetting>();
            if (settings == null) throw new Exception("not wechat login settings");

            return new
            {
                settings.Appid
            };
        }
    }
}
