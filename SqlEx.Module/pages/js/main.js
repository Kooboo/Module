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
  KoobooModule.prototype.getTemplate = function(url) {
    var newPath = koobooModule.path.resolve(url);
    return Kooboo.getTemplate(newPath);
  };
  KoobooModule.prototype.sqlexModule = { component: {} };
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
  var MySqlModel = new Kooboo.HttpClientModel("MySql");
  KoobooModule.prototype.MySqlModel = {
    Tables: function(para) {
      return MySqlModel.executeGet("Tables", para);
    },
    CreateTable: function(para) {
      return MySqlModel.executePost("CreateTable", para);
    },
    DeleteTables: function(para) {
      return MySqlModel.executePost("DeleteTables", para);
    },
    GetEdit: function(para) {
      return MySqlModel.executeGet("GetEdit", para);
    },
    Data: function(para) {
      return MySqlModel.executeGet("Data", para);
    },
    DeleteData: function(para) {
      return MySqlModel.executePost("DeleteData", para);
    },
    UpdateData: function(para) {
      return MySqlModel.executePost("UpdateData", para);
    },
    Columns: function(para) {
      return MySqlModel.executePost("Columns", para);
    },
    UpdateColumn: function(para) {
      return MySqlModel.executePost("UpdateColumn", para);
    }
  };
  var SqlServerModel = new Kooboo.HttpClientModel("SqlServer");
  KoobooModule.prototype.SqlServerModel = {
    Tables: function(para) {
      return SqlServerModel.executeGet("Tables", para);
    },
    CreateTable: function(para) {
      return SqlServerModel.executePost("CreateTable", para);
    },
    DeleteTables: function(para) {
      return SqlServerModel.executePost("DeleteTables", para);
    },
    GetEdit: function(para) {
      return SqlServerModel.executeGet("GetEdit", para);
    },
    Data: function(para) {
      return SqlServerModel.executeGet("Data", para);
    },
    DeleteData: function(para) {
      return SqlServerModel.executePost("DeleteData", para);
    },
    UpdateData: function(para) {
      return SqlServerModel.executePost("UpdateData", para);
    },
    Columns: function(para) {
      return SqlServerModel.executePost("Columns", para);
    },
    UpdateColumn: function(para) {
      return SqlServerModel.executePost("UpdateColumn", para);
    }
  };
  window.koobooModule = new KoobooModule();
})(window);
