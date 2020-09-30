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
