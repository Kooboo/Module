using Authorization.Module.code.Facebook;
using Authorization.Module.code.Google;
using Authorization.Module.code.Jwt;
using Authorization.Module.code.WeChatQrCode;
using Authorization.Module.code.Weibo;
using JWT;
using JWT.Algorithms;
using JWT.Exceptions;
using JWT.Serializers;
using Kooboo.Data.Attributes;
using Kooboo.Data.Context;
using Kooboo.Data.Interface;
using Kooboo.Lib.Helper;
using Kooboo.Sites.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;

namespace Authorization.Module.code
{
    public class AuthorizationScript : IkScript
    {
        [KIgnore]
        public string Name => "Authorization";

        [KIgnore]
        public RenderContext context { get; set; }

        private static readonly WebClient _webClient = new WebClient();

        #region Wechat
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
            var tokenString = _webClient.DownloadString($"https://api.weixin.qq.com/sns/oauth2/access_token?appid={settings.Appid}&secret={settings.Secret}&code={code}&grant_type=authorization_code");
            var token = JsonHelper.Deserialize<Dictionary<string, object>>(tokenString);
            var userInfoString = _webClient.DownloadString($"https://api.weixin.qq.com/sns/userinfo?access_token={token["access_token"]}&openid={token["openid"]}");
            var userInfo = JsonHelper.Deserialize<Dictionary<string, object>>(userInfoString);


            return JsonHelper.Serialize(new
            {
                token,
                userInfo
            });
        }
        #endregion

        #region Jwt
        [Description(@"
params claims : jwt claims

{
    name:""alex"",
    id:""xxxx""
}

result: jwt token
eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJuYW1lIjoiaHVhbmVudCIsImV4cCI6MTYwMjIxNTM4OH0.ZunonM2w-3PJURhW9eBD90zdnw9NCDDIZbCMM6Izsb4
")]
        public string JwtEncode(IDictionary<string, object> claims)
        {
            var setting = GetJwtSetting();

            if (setting.EnableExp && !claims.ContainsKey("exp"))
            {
                var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                claims.Add("exp", setting.Exp + unixTimestamp);
            }

            IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
            IJsonSerializer serializer = new JsonNetSerializer();
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            IJwtEncoder encoder = new JwtEncoder(algorithm, serializer, urlEncoder);
            return encoder.Encode(claims, setting.Secret);
        }

        [Description(@"
This method will get token in http request authorization header 

result: 

success
{
    code :0,
    value :{
        name:""alex"",
        id:""xxxx""
    }
}

error
{
    code :1,
    value :""error message""
}
")]
        public string JwtDecode()
        {

            var authorizationValue = context.Request.Headers.Get("Authorization");

            if (string.IsNullOrWhiteSpace(authorizationValue))
            {
                return JsonHelper.Serialize(new
                {
                    Code = 1,
                    Value = "Not authorization header"
                });
            }

            if (!authorizationValue.StartsWith("Bearer "))
            {
                return JsonHelper.Serialize(new
                {
                    Code = 1,
                    Value = "Authorization not start with Bearer"
                });
            }

            var token = authorizationValue.Substring(7);

            return JwtDecode(token);
        }

        [Description(@"
params token : jwt token
eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJuYW1lIjoiaHVhbmVudCIsImV4cCI6MTYwMjIxNTM4OH0.ZunonM2w-3PJURhW9eBD90zdnw9NCDDIZbCMM6Izsb4

result: 

success
{
    code :0,
    value :{
        name:""alex"",
        id:""xxxx""
    }
}

error
{
    code :1,
    value :""error message""
}
")]
        public string JwtDecode(string token)
        {
            var setting = GetJwtSetting();

            try
            {
                IJsonSerializer serializer = new JsonNetSerializer();
                var provider = new UtcDateTimeProvider();
                IJwtValidator validator = new JwtValidator(serializer, provider);
                IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                IJwtAlgorithm algorithm = new HMACSHA256Algorithm(); // symmetric
                IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder, algorithm);

                var json = decoder.Decode(token, setting.Secret, verify: true);

                return JsonHelper.Serialize(new
                {
                    Code = 0,
                    Value = JsonHelper.DeserializeObject(json)
                });
            }
            catch (TokenExpiredException)
            {
                return JsonHelper.Serialize(new
                {
                    Code = 1,
                    Value = "Token has expired"
                });
            }
            catch (SignatureVerificationException)
            {
                return JsonHelper.Serialize(new
                {
                    Code = 1,
                    Value = "Token has invalid signature"
                });
            }
        }

        private JwtSetting GetJwtSetting()
        {
            var setting = context.WebSite.SiteDb().CoreSetting.GetSetting<JwtSetting>();
            if (setting == null) throw new Exception("JwtSetting is required");
            return setting;
        }

        #endregion

        #region Weibo
        [Description(@"
params code : weibo redirect code

result: 
{
    token :{
       ...
    },
    userInfo :{
        ...
    }
}
")]
        public string Weibo(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new Exception("code can't be empty");
            var settings = context.WebSite.SiteDb().CoreSetting.GetSetting<WeiboSetting>();
            var tokenString = _webClient.UploadString($"https://api.weibo.com/oauth2/access_token?client_id={settings.Appid}&client_secret={settings.Secret}&grant_type=authorization_code&redirect_uri={settings.RedirectUri}&code={code}", "");
            var token = JsonHelper.Deserialize<Dictionary<string, object>>(tokenString.ToString());
            var userInfoString = HttpHelper.GetString($"https://api.weibo.com/2/users/show.json?access_token={token["access_token"]}&uid={token["uid"]}");
            var userInfo = JsonHelper.Deserialize<Dictionary<string, object>>(userInfoString);


            return JsonHelper.Serialize(new
            {
                token,
                userInfo
            });
        }
        #endregion

        #region Facebook
        [Description(@"
params code : facebook redirect code
params fields : facebook graph fields

result: 
{
    token :{
       ...
    },
    userInfo :{
        ...
    }
}
")]
        public string Facebook(string code, string fields)
        {
            if (string.IsNullOrWhiteSpace(fields)) fields = "id,name,email,picture.type(large)";
            if (string.IsNullOrWhiteSpace(code)) throw new Exception("code can't be empty");
            var settings = context.WebSite.SiteDb().CoreSetting.GetSetting<FacebookSetting>();
            var tokenString = _webClient.DownloadString($"https://graph.facebook.com/v8.0/oauth/access_token?client_id={settings.Appid}&redirect_uri={settings.RedirectUri}&client_secret={settings.Secret}&code={code}");
            var token = JsonHelper.Deserialize<Dictionary<string, object>>(tokenString.ToString());
            var userInfoString = HttpHelper.GetString($"https://graph.facebook.com/v8.0/me?access_token={token["access_token"]}&fields={fields}");
            var userInfo = JsonHelper.Deserialize<Dictionary<string, object>>(userInfoString);


            return JsonHelper.Serialize(new
            {
                token,
                userInfo
            });
        }

        [Description(@"
params code : facebook redirect code

result: 
{
    token :{
       ...
    },
    userInfo :{
        ...
    }
}
")]
        public string Facebook(string code) => Facebook(code, null);
        #endregion

        #region Google
        //https://www.googleapis.com/userinfo/v2/me

        [Description(@"
params code : google redirect code

result: 
{
    token :{
       ...
    },
    userInfo :{
        ...
    }
}
")]
        public string Google(string code)
        {

            if (string.IsNullOrWhiteSpace(code)) throw new Exception("code can't be empty");
            var settings = context.WebSite.SiteDb().CoreSetting.GetSetting<GoogleSetting>();
            var tokenString = _webClient.UploadString($"https://oauth2.googleapis.com/token?client_id={settings.Appid}&redirect_uri={settings.RedirectUri}&client_secret={settings.Secret}&code={code}&grant_type=authorization_code","");
            var token = JsonHelper.Deserialize<Dictionary<string, object>>(tokenString.ToString());
            var userInfoString = HttpHelper.GetString($"https://www.googleapis.com/userinfo/v2/me?access_token={token["access_token"]}");
            var userInfo = JsonHelper.Deserialize<Dictionary<string, object>>(userInfoString);

            return JsonHelper.Serialize(new
            {
                token,
                userInfo
            });
        }
        #endregion

    }
}
