@echo off
@set YEAR=%DATE:~10,4%
@set MONTH=%DATE:~4,2%
@set DAY=%DATE:~7,2%
@set DAYOFWEEK=%DATE:~0,3%
@set HOUR=%TIME:~0,2%
::replace leading 0 in hour
@if "%HOUR:~0,1%" == " " set HOUR=0%HOUR:~1,1%
@set MINUTE=%TIME:~3,2%
@set SECOND=%TIME:~6,2%
@set TIMESTAMP=%DAYOFWEEK% %MONTH%/%DAY%/%YEAR% %HOUR%:%MINUTE%:%SECOND%
@echo %TIMESTAMP%> "..\..\BuildDate.txt"

::for testing
::@echo %date% %time:~0,8%> "BuildDate.txt"