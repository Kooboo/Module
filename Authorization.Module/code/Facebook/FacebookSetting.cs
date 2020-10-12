﻿using Kooboo.Data.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Authorization.Module.code.Facebook
{
    public class FacebookSetting : ISiteSetting
    {
        public string Appid { get; set; }
        public string Secret { get; set; }
        public string RedirectUri { get; set; }
        public string Scope { get; set; }

        public string Name => "FacebookLoginSetting";
    }
}