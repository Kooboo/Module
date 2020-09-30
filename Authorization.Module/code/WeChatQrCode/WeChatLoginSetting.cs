using Kooboo.Data.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Authorization.Module.code.WeChatQrCode
{
    public class WeChatLoginSetting : ISiteSetting
    {

        public string Appid { get; set; }
        public string Secret { get; set; }

        public string Name => "WeChatLoginSetting";
    }
}
