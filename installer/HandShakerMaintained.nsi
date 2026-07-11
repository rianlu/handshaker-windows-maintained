Unicode true
RequestExecutionLevel admin

!include "LogicLib.nsh"
!include "x64.nsh"

Name "HandShaker Windows Maintained"
OutFile "..\dist\HandShaker-Windows-Maintained-Offline-Setup.exe"
InstallDir "$PROGRAMFILES32\HandShaker"
ShowInstDetails show

Page directory
Page instfiles

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
  CreateShortcut "$SMPROGRAMS\HandShaker\HandShaker.lnk" "$INSTDIR\HandShakerStart.exe"
  CreateShortcut "$SMPROGRAMS\HandShaker\卸载 HandShaker.lnk" "$INSTDIR\HandShakerUninst.exe"
  CreateShortcut "$DESKTOP\HandShaker.lnk" "$INSTDIR\HandShakerStart.exe"

  ExecShell "open" "$INSTDIR\HandShakerStart.exe"
SectionEnd
