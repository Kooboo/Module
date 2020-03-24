using Kooboo.Api;
using Kooboo.Module;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyCustom.Module.code
{
    public class CustomApi : IApi
    {
        public string ModelName => "Custom";

        public bool RequireSite => false;

        public bool RequireUser => false;
        public string GetString()
        {
            return "Hello world";
        }
    }
}
