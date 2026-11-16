@echo off
setlocal
set "ROOT=%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo csc.exe not found - .NET Framework 4.x is required
  exit /b 1
)

"%CSC%" /nologo /target:winexe ^
  /win32icon:"%ROOT%assets\polka.ico" ^
  /win32manifest:"%ROOT%src\app.manifest" ^
  /out:"%ROOT%Polka.exe" ^
  /optimize+ ^
  /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll /r:System.Core.dll ^
  "%ROOT%src\Theme.cs" ^
  "%ROOT%src\Program.cs" ^
  "%ROOT%src\Widgets.cs" ^
  "%ROOT%src\NoteCard.cs" ^
  "%ROOT%src\EditorForm.cs" ^
  "%ROOT%src\MainForm.cs"

if errorlevel 1 (
  echo Build failed.
  exit /b 1
)
echo Built: %ROOT%Polka.exe
