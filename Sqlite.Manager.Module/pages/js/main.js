window.koobooModule = {
  getModuleName: function() {
    if (location.pathname) {
      var name = location.pathname
        .replace("/_Admin/", "")
        .replace(/\/.*\.html.*/, "");
      return name;
    }
  },
  path: {
    resolve: function() {
      var args = Array.from(arguments);
      var result;
      var resolveTemp = function(itemA, itemB) {
        var count = 0;
        var matchs = itemB.match(/\.\.\//g);
        if (matchs) count = matchs.length;
        var j = count - 1;
        while (j > 0) {
          var index = itemA.lastIndexOf("/");
          if (index > 0) {
            if (itemA.lastIndexOf("../") !== index - 2) {
              itemA = itemA.slice(0, index);
            } else {
              itemA = itemA.slice(0, index + 1);
            }
          } else {
            itemA = "../" + itemA;
          }
          j--;
        }
        itemB = itemB.replace(/\.\.\//g, "").replace(/\.\//g, "");
        if (
          itemA.lastIndexOf("/") === itemA.length - 1 &&
          itemB.indexOf("/") === 0
        ) {
          itemA = itemA.slice(0, itemA.length - 1);
        } else if (
          itemA.lastIndexOf("/") !== itemA.length - 1 &&
          itemB.indexOf("/") !== 0
        ) {
          itemA = itemA + "/";
        }
        return itemA + itemB;
      };
      if (args.length) {
        var i = args.length - 1;
        if (!result) {
          result = args[i];
        }
        if (i <= 0) {
          var rootPath = koobooModule.getModuleRootPath();
          return resolveTemp(rootPath, result);
        }

        while (i >= 0) {
          if (args[i - 1]) {
            result = resolveTemp(args[i - 1], result);
          } else {
            result = resolveTemp(args[i - 1], result);
          }
          i = i - 2;
        }
      }
    }
  },
  recombineUrl: function(url, param) {
    var keys = Object.keys(param);
    if (keys.length) {
      keys.forEach(function(key, index) {
        if (index === 0) {
          url = url + "?" + key + "=" + param[key];
        } else {
          url = url + "&" + key + "=" + param[key];
        }
      });
    }
    return url;
  },
  getModuleRootPath: function() {
    return "/_Admin/" + koobooModule.getModuleName();
  },
  loadJS: function(paths, fromLayout) {
    var newPath = paths.map(function(item) {
      return koobooModule.path.resolve(item);
    });
    return Kooboo.loadJS(newPath);
  }
};
koobooModule.loadJS(["pages/js/lib/axios.min.js"]);

const KbHttpClient = {
  install: function(Vue, options) {
    Vue.prototype.$httpClient = {
      post: function(url, data, param) {
        if (param) {
          url = koobooModule.recombineUrl(url, param);
        }
        return axios.post(url, data);
      },
      get: function(url, param) {
        if (param) {
          url = koobooModule.recombineUrl(url, param);
        }
        return axios.get(url);
      }
    };
  }
};
Vue.use(KbHttpClient);

axios.interceptors.request.use(
  function(request) {
    if (request.url.indexOf("/") === 0) {
      request.url = request.url.slice(1);
    }
    request.url = location.origin + "/_api/" + request.url;
    return request;
  },
  function(error) {
    // 对请求错误做些什么
    return Promise.reject(error);
  }
);
axios.interceptors.response.use(
  function(response) {
    return response;
  },
  function(error) {
    return Promise.reject(error);
  }
);
