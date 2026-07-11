Unicode true
RequestExecutionLevel admin
SilentInstall silent
AutoCloseWindow true

!include "FileFunc.nsh"
!include "LogicLib.nsh"

Name "HandShaker Windows Maintained"
OutFile "..\dist\HandShaker-Windows-Maintained-Setup.exe"

Section
  InitPluginsDir
  SetOutPath "$PLUGINSDIR"
  File /oname=OfficialSetup.exe "..\original\HandShaker_Official_Web_Setup.exe"
  File /oname=HandShaker.Detector.exe "..\dist\HandShaker.Detector.exe"

  ExecWait '"$PLUGINSDIR\OfficialSetup.exe"' $0
  ${If} $0 != 0
    MessageBox MB_ICONSTOP "HandShaker 官方安装程序未成功完成, 错误码: $0"
    Abort
  ${EndIf}

  SetRegView 32
  ReadRegStr $1 HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\HandShaker" "DisplayIcon"
  ${If} $1 == ""
    SetRegView 64
    ReadRegStr $1 HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\HandShaker" "DisplayIcon"
  ${EndIf}
  ${GetParent} "$1" $2

  ${IfNot} ${FileExists} "$2\HandShaker.exe"
    MessageBox MB_ICONSTOP "已完成官方安装, 但无法定位 HandShaker 安装目录."
    Abort
  ${EndIf}

  nsExec::ExecToLog 'taskkill /F /IM HandShaker.Detector.exe'
  nsExec::ExecToLog 'taskkill /F /IM HandShaker.exe'
  nsExec::ExecToLog 'taskkill /F /IM HandShakerStart.exe'
  CopyFiles /SILENT "$PLUGINSDIR\HandShaker.Detector.exe" "$2\HandShaker.Detector.exe"

  ${IfNot} ${FileExists} "$2\HandShaker.Detector.exe"
    MessageBox MB_ICONSTOP "维护版 Detector 安装失败."
    Abort
  ${EndIf}

  ${If} ${FileExists} "$2\HandShakerStart.exe"
    ExecShell "open" "$2\HandShakerStart.exe"
  ${Else}
    ExecShell "open" "$2\HandShaker.exe"
  ${EndIf}
SectionEnd
