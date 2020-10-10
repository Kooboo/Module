using Kooboo.Data.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Authorization.Module.code.Weibo
{
    public class WeiboSetting : ISiteSetting
    {
        public string Name => "WeiboLoginSetting";
        public string Appid { get; set; }
        public string Secret { get; set; }
        public string RedirectUri { get; set; }
    }
}
