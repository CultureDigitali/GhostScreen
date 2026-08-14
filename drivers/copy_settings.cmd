@echo off
set DST=%1
set SRC=%~dp0vdd_settings.xml
copy /y "%SRC%" "%DST%\vdd_settings.xml"
exit /b %errorlevel%