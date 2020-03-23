@ECHO off

set projectDir=%1
set projectName=%2
set slnDir=%3
set releaseDir=%projectDir%\bin\Release\netstandard2.0\

set zipFile=%releaseDir%\%projectName%.zip
::delete existed zipFile
if exist "%zipFile%" (del %zipFile%)

C:\Windows\System32\robocopy  %projectDir% %releaseDir%modules /s /xd kbtest .vscode bin obj Properties /xf *.cs *.csproj *.csproj.user
for %%i in (%slnDir%Kooboo\*.dll) do (
	if exist %releaseDir%\%%~nxi (del %releaseDir%\%%~nxi)
)
C:\Windows\System32\robocopy  %releaseDir% %releaseDir%modules *.dll /xf Kooboo.*.dll

::zip folder
%slnDir%tools\7z\7z.exe a -tzip %zipFile% %releaseDir%modules\* -r -mx9

rd /s /q %releaseDir%modules