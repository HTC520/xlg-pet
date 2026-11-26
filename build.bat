@echo off
chcp 65001 >nul
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo [Error] .NET Framework 4.0 csc.exe not found at %CSC%
    exit /b 1
)

echo Compiling StardewPet.exe ...
"%CSC%" /nologo /target:winexe /optimize+ /out:StardewPet.exe ^
  /reference:System.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  src\*.cs

if errorlevel 1 (
    echo [Error] Build failed!
    exit /b 1
)

echo [Success] Built StardewPet.exe!
