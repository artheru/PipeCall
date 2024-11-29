@echo off
echo Building solution...
dotnet build -c Debug

if errorlevel 1 (
    echo Build failed
    exit /b %errorlevel%
)

echo Copying executable to client.exe...
copy /Y "bin\Debug\net8.0\PipeCall.exe" "bin\Debug\net8.0\client.exe"

if errorlevel 1 (
    echo Failed to copy executable
    exit /b %errorlevel%
)

echo Running tests...
"bin\Debug\net8.0\PipeCall.exe"

echo Done! 