# Kooboo Module

## Environmental requirement
1. dotnet core 2.1+
2. dotnet script

## Install dotnet script
```dotnet tool install -g dotnet-script```

## Install template
```dotnet script https://raw.githubusercontent.com/Kooboo/Module/master/scripts/install.csx```

## Create Template
```dotnet new kooboomodule -n [you custom project name]```


-------
## Remove
```dotnet script https://raw.githubusercontent.com/Kooboo/Module/master/scripts/uninstall.csx```

## Release
1. Publish PreviewServer
2. Into ```PreviewServer\bin\Release\netcoreapp2.1\publish``` you can find module zip file

## Integration with Kooboo
Put you release zip file to ```Kooboo root \modules``` folder

