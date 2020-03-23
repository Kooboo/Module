(function() {
  function KoobooModule() {
    self = this;
  }
  KoobooModule.prototype.getModuleName = function() {
    if (location.pathname) {
      var name = location.pathname
        .replace("/_Admin/", "")
        .replace(/\/.*\.html.*/, "");
      return name;
    }
  };
  KoobooModule.prototype.getModuleName = function() {
    if (location.pathname) {
      var name = location.pathname
        .replace("/_Admin/", "")
        .replace(/\/.*\.html.*/, "");
      return name;
    }
  };
  KoobooModule.prototype.path = {
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
  };
  KoobooModule.prototype.recombineUrl = function(url, param) {
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
  };
  KoobooModule.prototype.getModuleRootPath = function() {
    return "/_Admin/" + koobooModule.getModuleName();
  };
  KoobooModule.prototype.loadJS = function(paths, fromLayout) {
    var newPath = paths.map(function(item) {
      return koobooModule.path.resolve(item);
    });
    return Kooboo.loadJS(newPath);
  };
  var sqliteModel = new Kooboo.HttpClientModel("Sqlite");
  KoobooModule.prototype.SqliteModel = {
    Tables: function(para) {
      return sqliteModel.executeGet("Tables", para);
    },
    CreateTable: function(para) {
        return sqliteModel.executePost("CreateTable", para);
    },
    DeleteTables: function(para) {
      return sqliteModel.executePost("DeleteTables", para);
    },
    GetEdit: function(para) {
      return sqliteModel.executeGet("GetEdit", para);
    },
    Data: function(para) {
      return sqliteModel.executeGet("Data", para);
    },
    DeleteData: function(para) {
      return sqliteModel.executePost("DeleteData", para);
    },
    UpdateData: function(para) {
      return sqliteModel.executePost("UpdateData", para);
    },
    Columns: function(para) {
      return sqliteModel.executePost("Columns", para);
    },
    UpdateColumn: function(para) {
      return sqliteModel.executePost("UpdateColumn", para);
    }
  };
  window.koobooModule = new KoobooModule();
})(window);
