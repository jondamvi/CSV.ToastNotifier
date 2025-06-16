@echo off
setlocal enabledelayedexpansion

echo Toast Notification Build Script
echo ===============================

:: Main Directory Structure Configuration
set "RELEASE_DIR_NAME=Release"
set "SOURCE_DIR_NAME=Source Files"

:: Configuration for build with .NET Framework compiler directly.
:: If building with dotnet MSBuild, these options will be ignored, except for SOURCE_FILE, which must be in sync with source file
:: defined in '.csproj' project file, because it is used for determining build type LocalBuild or ProjectBuild.
set "ASSEMBLY_NAME=CSV.ToastNotifier"
set "ASSEMBLY_ICON=Icon.ico"
set "SOURCE_FILE=!ASSEMBLY_NAME!.cs"
set "OUTPUT_FILE=!ASSEMBLY_NAME!.exe"
set "OUTPUT_MANIFEST=!ASSEMBLY_NAME!.manifest"

:: Configuration for build with MSBuild dotnet compiler.
set "PROJECT_NAME=!ASSEMBLY_NAME!.csproj"

set "REMOVE_BUILD_FILES=!ASSEMBLY_NAME!.pdb"

:: Default values for command line arguments
set USE_FRAMEWORK=0
set USE_DOTNET_MSBUILD=1
set SIGN_EXE=0
set CERT_PATH=
set CERT_PASS=

:parse_args
if "%~1"=="" (
    goto :continue_build
)
if /i "%~1"=="/useframework" (
    if defined USER_SET_USEDOTNET (
        echo ERROR: /useframework and /usedotnet options are mutually exclusive. Choose either /useframework or /usedotnet option.
        exit /b 1
    )
    set USER_SET_USEFRAMEWORK=1
    set USE_FRAMEWORK=1
    set USE_DOTNET_MSBUILD=0
    shift
    goto :parse_args
)
if /i "%~1"=="/usedotnet" (
    if defined USER_SET_USEFRAMEWORK (
        echo ERROR: /useframework and /usedotnet options are mutually exclusive. Choose either /useframework or /usedotnet option.
        exit /b 1
    )
    set USER_SET_USEDOTNET=1
    set USE_DOTNET_MSBUILD=1
    set USE_FRAMEWORK=0
    shift
    goto :parse_args
)
if /i "%~1"=="/sign" (
    set "SIGN_EXE=1"
    shift
    goto :parse_args
)
if /i "%~1"=="/cert" (
    set "CERT_PATH=%~2"
    shift
    shift
    goto :parse_args
)
if /i "%~1"=="/pass" (
    set "CERT_PASS=%~2"
    shift
    shift
    goto :parse_args
)
shift
goto :parse_args


:: Configure environment for build with .NET Framework compiler directly
:configure_direct_framework_build
    :: Main Compile Configuration

    :: Dependencies Configuration
    set "NET_FRAMEWORK_ROOT=C:\Windows\Microsoft.NET\Framework64"
    set "NET_FRAMEWORK_VERSION=4.0.30319"
    set "NET_FRAMEWORK_DIR=!NET_FRAMEWORK_ROOT!\v!NET_FRAMEWORK_VERSION!"
    set "NET_COMPILER=csc.exe"

    set "WIN_SDK_ROOT=C:\Program Files (x86)\Windows Kits\10"

    set "CSC_PATH=!NET_FRAMEWORK_DIR!\!NET_COMPILER!"
    if NOT EXIST "!CSC_PATH!" (
        echo ERROR: .NET Framework compiler not found at !CSC_PATH!
        pause
        exit /b 1
    )
goto :eof

:: Auto-detect Windows SDK version
:detect_windows_sdk_version
    set WINMD_PATH=
    For %%v in (26100 22621 22000 19041 18362 17763 17134 16299) do (
        if exist "!WIN_SDK_ROOT!\UnionMetadata\10.0.%%v.0\Windows.winmd" (
            set "WINMD_PATH=!WIN_SDK_ROOT!\UnionMetadata\10.0.%%v.0\Windows.winmd"
            echo Found Windows SDK version: 10.0.%%v.0
            goto :found_sdk
        )
    )
goto :eof

:found_sdk
    if "!WINMD_PATH!"=="" (
        echo ERROR: Windows SDK not found!
        echo Please install Windows SDK from:
        echo https://developer.microsoft.com/windows/downloads/windows-sdk/
        pause
        exit /b 1
    )
goto :eof

:configure_direct_framework_build_command
    set BUILD_CMD=!CSC_PATH! /target:exe /out:"!OUTPUT_DIR!\!OUTPUT_FILE!" /platform:x64 ^
        /reference:"!WINMD_PATH!" ^
        /reference:"!NET_FRAMEWORK_DIR!\System.Runtime.WindowsRuntime.dll" ^
        /reference:"!NET_FRAMEWORK_DIR!\System.Runtime.dll" ^
        /reference:"!NET_FRAMEWORK_DIR!\System.Runtime.InteropServices.WindowsRuntime.dll" ^
        /optimize+ /debug:pdbonly !MANIFEST_ARG! !WIN32ICON_ARG! ^
        "!SOURCE_DIR!\!SOURCE_FILE!"
goto :eof

:configure_dotnet_project_build_command
    set "BUILD_CMD=dotnet publish "!SOURCE_DIR!\!PROJECT_NAME!" -c Release -o "!ABS_OUTPUT_DIR!""
goto :eof

:continue_build

if ("!USE_FRAMEWORK!"=="0" if "!USE_FRAMEWORK!"=="0") (
    echo ERROR: No build option specified. Choose either /useframework or /usedotnet option.
    echo        Otherwise set default option in script USE_FRAMEWORK=1 or USE_DOTNET_MSBUILD=1.
    exit /b 1
)

:: Determine Build Environment
:: Local Folder Build or Distributed Project Folder Structure Build
set OUTPUT_DIR=
set "CURRENT_DIR=%cd%"

if exist "!SOURCE_FILE!" (
    set "SOURCE_DIR=!CURRENT_DIR!"
    set "OUTPUT_DIR=!CURRENT_DIR!\!RELEASE_DIR_NAME!"
    set "ABS_OUTPUT_DIR=!RELEASE_DIR_NAME!"
    echo Found source file '!SOURCE_FILE!' in current directory:
    echo     '!SOURCE_DIR!'
    echo Performing local directory build ...
) else (
    set "EXT_SOURCE_DIR=..\!SOURCE_DIR_NAME!"
    for %%a in ("%CURRENT_DIR%\..") do set "PROJECT_DIR=%%~fa"
    if exist "!EXT_SOURCE_DIR!" (
        :: Convert relative path to absolute path
        pushd "!EXT_SOURCE_DIR!"
        set "ABS_SOURCE_DIR=!cd!"
        popd
        :: Compare paths (case-insensitive)
        if /i "%CURRENT_DIR%"=="%ABS_SOURCE_DIR%" (
            echo ERROR: Source file '!SOURCE_FILE!' not found in expected project path:
            echo     '!ABS_SOURCE_DIR!'
            pause
            exit /b 1
        ) else (
            set "SOURCE_DIR=!ABS_SOURCE_DIR!"
            set "OUTPUT_DIR=!PROJECT_DIR!\!RELEASE_DIR_NAME!"
            set "ABS_OUTPUT_DIR=..\!RELEASE_DIR_NAME!"
        )
    ) else (
        echo ERROR: Source file '!SOURCE_FILE!' not found in current directory:
        echo     '!CURRENT_DIR!'
        echo ERROR: Directory '!SOURCE_DIR_NAME!' not found in expected project path: 
        echo     '!PROJECT_DIR!'
        pause
        exit /b 1
    )
    if exist "!SOURCE_DIR!\!SOURCE_FILE!" (
        echo Found source file '!SOURCE_FILE!' in '!SOURCE_DIR_NAME!' project directory:
        echo     '!SOURCE_DIR!'
        echo Performing project directory build ...
    ) else (
        echo ERROR: Source file '!SOURCE_FILE!' not found in expected project path:
        echo     '!SOURCE_DIR!'
        pause
        exit /b 1
    )
)
if exist "!SOURCE_DIR!\!OUTPUT_MANIFEST!" (
    echo Found '!OUTPUT_MANIFEST!' - will embed it into executable.
    set "MANIFEST_ARG=/win32manifest:"!SOURCE_DIR!\!OUTPUT_MANIFEST!""
)
if exist "!SOURCE_DIR!\!ASSEMBLY_ICON!" (
    echo Found application icon '!ASSEMBLY_ICON!' - will embed it into executable.
    set "WIN32ICON_ARG=/win32icon:"!SOURCE_DIR!\!ASSEMBLY_ICON!""
)

if "!USE_FRAMEWORK!"=="1" (
    call :configure_direct_framework_build

    call :detect_windows_sdk_version

    call :configure_direct_framework_build_command
)

if "!USE_DOTNET_MSBUILD!"=="1" (
    call :configure_dotnet_project_build_command
)



echo.
echo Compiling !OUTPUT_FILE! ...
echo Using compiler: !CSC_PATH!



if not exist "!OUTPUT_DIR!" (
    mkdir "!OUTPUT_DIR!"
) else (
    ::Pre-Compile Cleanup
    if exist "!OUTPUT_DIR!\!OUTPUT_FILE!" (
        DEL """!OUTPUT_DIR!\!OUTPUT_FILE!"""
    )
)

:build
    echo Build Command is: '!BUILD_CMD!'
    echo Output Directory is: '!OUTPUT_DIR!'
    echo.
    Call !BUILD_CMD!

if !errorlevel!==0 (
    echo.
    echo Build completed successfully!
) else (
    echo.
    echo ERROR: Compilation failed!
    echo.
    echo Make sure you have Windows SDK installed and the paths in this script match your system.
    echo Windows SDK is required for Windows Runtime WinRT APIs.
    echo.
    echo You can download Windows SDK from:
    echo https://developer.microsoft.com/windows/downloads/windows-sdk/
    echo.
    pause
    exit /b 1
)

::Post-Compile Cleanup
for %%F in (!REMOVE_BUILD_FILES!) do (
    if exist "!OUTPUT_DIR!\%%F" (
        DEL """!OUTPUT_DIR!\%%F"""
    )
)

endlocal
pause
@echo on
