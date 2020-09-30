using Authorization.Module.code.WeChatQrCode;
using Jint.Native;
using Kooboo.Data.Context;
using Kooboo.Data.Interface;
using Kooboo.Lib.Helper;
using Kooboo.Sites.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Authorization.Module.code
{
    public class AuthorizationScript : IkScript
    {
        public string Name => "Authorization";

        public RenderContext context { get; set; }


        [Description(@"
params code : wechat redirect code

result: 
{
    token :{
        ""access_token"":""ACCESS_TOKEN"",
        ""expires_in"":7200,
        ""refresh_token"":""REFRESH_TOKEN"",
        ""openid"":""OPENID"",
        ""scope"":""SCOPE""
    },
    userInfo :{
        ""openid"":""OPENID"",
        ""nickname"":""NICKNAME"",
        ""sex"":1,
        ""province"":""PROVINCE"",
        ""city"":""CITY"",
        ""country"":""COUNTRY"",
        ""headimgurl"": ""https://thirdwx.qlogo.cn/mmopen/g3MonUZtNHkdmzicIlibx6iaFqAc56vxLSUfpb6n5WKSYVY0ChQKkiaJSgQ1dZuTOgvLLrhJbERQQ4eMsv84eavHiaiceqxibJxCfHe/0"",
        ""privilege"":[
        ""PRIVILEGE1"",
        ""PRIVILEGE2""
        ],
        ""unionid"": "" o6_bmasdasdsad6_2sgVt7hMZOPfL""
    }
}
")]
        public string WeChat(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new Exception("code can't be empty");
            var settings = context.WebSite.SiteDb().CoreSetting.GetSetting<WeChatLoginSetting>();
            var tokenString = HttpHelper.GetString($"https://api.weixin.qq.com/sns/oauth2/access_token?appid={settings.Appid}&secret={settings.Secret}&code={code}&grant_type=authorization_code");
            var token = JsonHelper.Deserialize<Dictionary<string, object>>(tokenString);
            var userInfoString = HttpHelper.GetString($"https://api.weixin.qq.com/sns/userinfo?access_token={token["access_token"]}&openid={token["openid"]}");
            var userInfo = JsonHelper.Deserialize<Dictionary<string, object>>(userInfoString);


            return JsonHelper.Serialize(new
            {
                token,
                userInfo
            });
        }
    }
}
