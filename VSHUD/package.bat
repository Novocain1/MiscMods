@echo off
cd..
mkdir tmp
copy resources\* tmp\
copy mods\VSHUD.dll tmp\
copy mods\VSHUD.dll release\
7z a -tzip release\VSHUDCompat.zip .\tmp\*
rd /s /q tmp