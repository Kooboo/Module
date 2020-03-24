(function() {
  koobooModule.sqlexModule.component.KbSqlexModuleComponent = {
    name: "kb-sqlex-module-component",
    template: koobooModule.getTemplate(
      "pages/components/sqlex-module-component/sqlex-module-component.html"
    ),
    data: function() {
      vm = this;

      return {
        tableData: [],
        selectedRows: [],
        showCreateTableModal: false,
        siteId: Kooboo.getQueryString("SiteId"),

        createTableModel: {
          name: ""
        },
        createTableModelRules: {
          name: [
            { required: Kooboo.text.validation.required },
            {
              min: 1,
              max: 64,
              message: Kooboo.text.validation.maxLength + " 64"
            },
            {
              remote: {
                url: koobooModule.recombineUrl(
                  `/_api/${vm.sqlType}/IsUniqueTableName`,
                  { SiteId: Kooboo.getQueryString("SiteId") }
                ),
                data: function() {
                  return {
                    name: vm.createTableModel.name
                  };
                }
              },
              message: Kooboo.text.validation.taken
            }
          ]
        }
      };
    },
    mounted: function() {
      vm.getTableList();
    },
    props: {
      breads: {
        require: true
      },
      sqlType: {
        require: true
      }
    },
    methods: {
      onDeleteTable() {
        if (confirm(Kooboo.text.confirm.deleteItems)) {
          if (vm.selectedRows.length > 0) {
            koobooModule[vm.sqlType + "Model"]
              .DeleteTables({
                names: vm.selectedRows
              })
              .then(function(res) {
                if (res.success) {
                  window.info.done(Kooboo.text.info.delete.success);
                  vm.getTableList();
                }
              });
          }
        }
      },
      getTableDataUrl(name) {
        return koobooModule.recombineUrl(
          koobooModule.getModuleRootPath() + "/pages/Data.html",
          { SiteId: vm.siteId, table: name, sqlType: vm.sqlType }
        );
      },
      getTableColumnsUrl(name) {
        return koobooModule.recombineUrl(
          koobooModule.getModuleRootPath() + "/pages/Columns.html",
          { SiteId: vm.siteId, table: name, sqlType: vm.sqlType }
        );
      },
      onSaveNewTable() {
        if (vm.$refs.createTableForm.validate()) {
          koobooModule[vm.sqlType + "Model"]
            .CreateTable({
              name: vm.createTableModel.name
            })
            .then(function(res) {
              if (res.success) {
                vm.getTableList();
                vm.closeCreateTableHandle();
              }
            });
        }
      },
      getTableList() {
        koobooModule[vm.sqlType + "Model"].Tables().then(function(res) {
          if (res.success) {
            vm.tableData = res.model;
          }
        });
      },
      createTableHandle() {
        vm.showCreateTableModal = true;
      },
      closeCreateTableHandle() {
        vm.showCreateTableModal = false;
        vm.revertCreateTableState();
      },
      revertCreateTableState() {
        vm.createTableModel.name = "";
      }
    }
  };
})();
