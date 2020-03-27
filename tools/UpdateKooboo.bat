rd /s /q %TMP%\_koobooTemp
git clone https://github.com/Kooboo/Module.git %TMP%\_koobooTemp
rd /s /q ..\Kooboo
C:\Windows\System32\robocopy  %TMP%\_koobooTemp\template\Kooboo ..\Kooboo /s
C:\Windows\System32\robocopy  %TMP%\_koobooTemp\template\tools ..\tools /s