@ECHO off

set templatePath=%1
set projectName=%2
set releasepath=%templatePath%\bin\Release
set viewPath=%templatePath%\view

set zipFile=%releasepath%\modules.zip
::delete existed zipFile
if exist "%zipFile%" (del %zipFile%)

set copyFolder=%releasepath%\modules

C:\Windows\System32\robocopy  %viewPath% %copyFolder% /e /xd kbtest .vscode
C:\Windows\System32\robocopy  %releasepath% %copyFolder% %projectName%.dll

set zipExePath=%templatePath%\tools\7z\
::zip folder
%zipExePath%\7z.exe a -tzip %zipFile% %copyFolder%\* -r -mx9

rd /s /q %copyFolder%