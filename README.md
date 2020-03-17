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

## Release
1. Change visual studio build target to release
2. Rebuild [you custom project name].module project
3. Into ```\bin\Release``` you can find module zip file

## Integration with Kooboo
Put you release zip file to ```Kooboo root \modules``` folder

