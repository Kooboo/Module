# Kooboo Module

## Environmental requirement
1. git
2. dotnet core 2.1+

## Install
cmd run

```git clone https://github.com/Kooboo/Module.git %TMP%\_km && cd %TMP%\_km\template && dotnet new -i ./```
## Update
cmd run

```rd /s /q %TMP%\_km &  git clone https://github.com/Kooboo/Module.git %TMP%\_km && cd %TMP%\_km\template && dotnet new -i ./```
## Remove
cmd run

```dotnet new -u %TMP%\_km\template && rd /s /q %TMP%\_km```

## Use
```dotnet new kooboomodule -n [you custom project name]```

## Update solution Kooboo dependency

run ```UpdateKooboo.bat``` in your ```[module folder]\tools```

## Release
1. Publish PreviewServer
2. Into ```PreviewServer\bin\Release\netcoreapp2.1\publish``` you can find module zip file

## Integration with Kooboo
Put you release zip file to ```Kooboo root \modules``` folder

