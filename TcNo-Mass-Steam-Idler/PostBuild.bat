REM If SIGNTOOL environment variable is not set then try setting it to a known location
if "%SIGNTOOL%"=="" set SIGNTOOL=%ProgramFiles(x86)%\Windows Kits\10\bin\10.0.22000.0\x64\signtool.exe

start call sign.bat "bin\Release\net6.0\idle.exe"
start call sign.bat "bin\Release\net6.0\idle.dll"
start call sign.bat "bin\Release\net6.0\TcNo-Mass-Steam-Idler.exe
start call sign.bat "bin\Release\net6.0\TcNo-Mass-Steam-Idler.dll"