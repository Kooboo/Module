# 支持列表

- 微信
- JWT
- 微博
- Facebook
- google

# 使用步骤

请确保 Authorization.Module.zip 放置在 kooboo 的 modules 目录中。

## 微信

1. 进入站点->系统->配置->WeChatLoginSetting 填写微信开放平台 appid 和 secret
2. 新建 api

```
var result = k.authorization.weChat(k.request.code);
k.response.write(result)
```

3. 访问地址[网站域名]/\_spa/Authorization.module/wechat/qrcode_login.html?redirect_uri=[回调地址]&state=[可选的负载数据]

   - redirect_uri 需要用 encodeURIComponent 编码

   - state 可以传递会跳地址这些信息

【举例】

站点域名为 http://huanent.site:8080/

回调 api 地址为 http://huanent.site:8080/login

扫码地址：http://huanent.site:8080/_spa/Authorization.module/wechat/qrcode_login.html?redirect_uri=http%3A%2F%2Fhuanent.site%3A8080%2Flogin&state=

## JWT

1. 进入站点->系统->配置->JwtSetting 填写 jwt secret , exp 和 enableExp 选填

- secret 符合 jwt 标准的加密字符串
- exp 过期时间（秒） 例如设置 token30 秒过期 则填 30
- enableExp 是否开始过期时间检测

2. 生成 token

```
k.authorization.jwtEncode({
    name:"xxx"
})
```

3. 验证密钥

```
k.authorization.jwtDecode();

or

k.authorization.jwtDecode(token)

// 返回值为json字符串
// 成功示例：code始终为0
// { "code": 0, "value": { "name": "xxx" } }
// 失败示例： code始终为1
// { "code": 1, "value": "Token has invalid signature" }

```

## 微博

1. 进入站点->系统->配置->WeiboLoginSetting 填写 appid secret redirectUri
2. 新建 api（地址与回调地址相同）

```
var result = k.authorization.weibo(k.request.code)
k.response.write(result)
```

3. 访问地址 [网站域名]/\_spa/Authorization.module/weibo/login.html

## Facebook

1. 进入站点->系统->配置->FacebookLoginSetting 填写 appid secret redirectUri 和 scope(选填)
2. 新建 api（地址与回调地址相同）

```
var result = k.authorization.facebook(k.request.code)
k.response.write(result)
```

3. 访问地址 [网站域名]/\_spa/Authorization.module/facebook/login.html?state=[可选的负载数据]

## Google

1. 进入站点->系统->配置->GoogleLoginSetting 填写 appid secret redirectUri 和 scope(选填)
2. 新建 api（地址与回调地址相同）

```
var result = k.authorization.google(k.request.code)
k.response.write(result)
```

3. 访问地址 [网站域名]/\_spa/Authorization.module/google/login.html?state=[可选的负载数据]
