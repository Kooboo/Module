# Kooboo modularization development template

## How to use

1. Clone this repository
2. Clone [kooboo/kooboo](https://github.com/Kooboo/Kooboo) into Kooboo folder
3. Ensure you have [.NET Core 2.1+ SDK](https://dotnet.microsoft.com/download)
4. Run ```dotnet tool install -g dotnet-script```
5. Into tools folder and run ```dotnet script rename.csx``` rename module project name
6. open Kooboo.Module.sln

## How to Release module zip
1. Change Build target Release
2. Rebuild [you custom module name].Module project
3. You can find zip in  [you custom module name].Module/bin/Release/netstandard2.0/[you custom module name].Module.zip
