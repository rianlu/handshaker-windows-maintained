Unicode true
RequestExecutionLevel admin

!include "LogicLib.nsh"
!include "FileFunc.nsh"
!include "MUI2.nsh"
!include "x64.nsh"

Name "HandShaker Windows Maintained"
OutFile "..\dist\HandShaker-Windows-Maintained-Offline-Setup.exe"
InstallDir "$PROGRAMFILES32\HandShaker"
Icon "assets\HandShakerSetup.ico"
BrandingText "HandShaker Windows Maintained"
ShowInstDetails nevershow
VIProductVersion "2.6.0.0"
VIAddVersionKey /LANG=2052 "ProductName" "HandShaker Windows Maintained"
VIAddVersionKey /LANG=2052 "FileDescription" "HandShaker 离线安装程序"
VIAddVersionKey /LANG=2052 "FileVersion" "2.6.0-maintained"
VIAddVersionKey /LANG=2052 "ProductVersion" "2.6.0-maintained"
VIAddVersionKey /LANG=2052 "CompanyName" "HandShaker Maintained"
VIAddVersionKey /LANG=2052 "LegalCopyright" "HandShaker Maintained"

!define MUI_ICON "assets\HandShakerSetup.ico"
!define MUI_WELCOMEFINISHPAGE_BITMAP "assets\HandShakerWelcome.bmp"
!define MUI_ABORTWARNING
!define MUI_WELCOMEPAGE_TITLE "欢迎安装 HandShaker"
!define MUI_WELCOMEPAGE_TEXT "此安装程序包含完整的 HandShaker Windows 维护版, 无需联网下载.$\r$\n$\r$\n连接Android设备时, 请在手机USB选项中选择文件传输, 并确认配件授权弹窗."
!define MUI_DIRECTORYPAGE_TEXT_TOP "请选择 HandShaker 的安装位置. 点击浏览可更改目录."
!define MUI_FINISHPAGE_TITLE "HandShaker 安装完成"
!define MUI_FINISHPAGE_TEXT "HandShaker 已安装到此电脑."
!define MUI_FINISHPAGE_RUN "$INSTDIR\HandShaker.AoaLauncher.exe"
!define MUI_FINISHPAGE_RUN_TEXT "立即运行 HandShaker"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_LANGUAGE "SimpChinese"

Function .onInit
  SetRegView 64
  ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\HandShaker" "DisplayIcon"
  ${If} $0 == ""
    SetRegView 32
    ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\HandShaker" "DisplayIcon"
  ${EndIf}
  ${If} $0 != ""
    ${GetParent} "$0" $INSTDIR
  ${EndIf}
FunctionEnd

Section "HandShaker"
  nsExec::ExecToLog 'taskkill /F /IM HandShaker.Detector.exe'
  nsExec::ExecToLog 'taskkill /F /IM HandShaker.exe'
  nsExec::ExecToLog 'taskkill /F /IM HandShakerStart.exe'
  nsExec::ExecToLog 'taskkill /F /IM adb.exe'

  SetOutPath "$INSTDIR"
  SetOverwrite on
  File /r "..\build\windows-payload\*"

  ${If} ${RunningX64}
    ExecWait 'msiexec /i "$INSTDIR\Bonjour64.msi" /qn /norestart'
  ${Else}
    ExecWait 'msiexec /i "$INSTDIR\Bonjour.msi" /qn /norestart'
  ${EndIf}

  WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\HandShaker" "DisplayName" "HandShaker Windows Maintained"
  WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\HandShaker" "DisplayVersion" "2.6.0-maintained"
  WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\HandShaker" "Publisher" "HandShaker Maintained"
  WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\HandShaker" "DisplayIcon" "$INSTDIR\HandShaker.exe"
  WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\HandShaker" "UninstallString" "$INSTDIR\HandShakerUninst.exe"

  CreateDirectory "$SMPROGRAMS\HandShaker"
  CreateShortcut "$SMPROGRAMS\HandShaker\HandShaker.lnk" "$INSTDIR\HandShaker.AoaLauncher.exe" "" "$INSTDIR\MyIcon.ico"
  CreateShortcut "$SMPROGRAMS\HandShaker\卸载 HandShaker.lnk" "$INSTDIR\HandShakerUninst.exe"
  CreateShortcut "$DESKTOP\HandShaker.lnk" "$INSTDIR\HandShaker.AoaLauncher.exe" "" "$INSTDIR\MyIcon.ico"

SectionEnd
