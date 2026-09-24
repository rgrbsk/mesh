@echo off
chcp 65001 > nul
cd /d "C:\Users\roger1\source\repos\rgrbsk\mesh"

echo ===== ENCERRANDO A APLICACAO (se estiver rodando) ===== > saida-build.txt 2>&1
taskkill /IM Erp.exe /F >> saida-build.txt 2>&1

echo. >> saida-build.txt
echo ===== DOTNET BUILD ===== >> saida-build.txt
dotnet build --nologo -v minimal >> saida-build.txt 2>&1
echo EXITCODE=%ERRORLEVEL% >> saida-build.txt

echo. >> saida-build.txt
echo ===== FIM ===== >> saida-build.txt
