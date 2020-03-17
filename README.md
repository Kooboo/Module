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
