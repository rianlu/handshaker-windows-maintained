Unicode true
RequestExecutionLevel admin
SilentInstall silent
AutoCloseWindow true

!include "LogicLib.nsh"
!include "x64.nsh"

Name "HandShaker Windows Maintained Payload"
OutFile "..\build\HandShaker.Payload.Setup.exe"
InstallDir "$PROGRAMFILES32\HandShaker"
Icon "..\assets\HandShaker.ico"
VIProductVersion "2.6.0.0"
VIAddVersionKey /LANG=2052 "ProductName" "HandShaker Windows Maintained"
VIAddVersionKey /LANG=2052 "FileDescription" "HandShaker 离线安装引擎"
VIAddVersionKey /LANG=2052 "FileVersion" "2.6.0-maintained"
VIAddVersionKey /LANG=2052 "ProductVersion" "2.6.0-maintained"
VIAddVersionKey /LANG=2052 "CompanyName" "HandShaker Maintained"
VIAddVersionKey /LANG=2052 "LegalCopyright" "HandShaker Maintained"

Section "HandShaker"
  nsExec::ExecToLog 'taskkill /F /IM HandShaker.AoaLauncher.exe'
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

  SetRegView 32
  WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\HandShaker" "DisplayName" "HandShaker Windows Maintained"
  WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\HandShaker" "DisplayVersion" "2.6.0-maintained"
  WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\HandShaker" "Publisher" "HandShaker Maintained"
  WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\HandShaker" "DisplayIcon" "$INSTDIR\HandShaker.exe"
  WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\HandShaker" "UninstallString" '"$INSTDIR\HandShakerUninst.exe"'

  RMDir /r "$SMPROGRAMS\HandShaker"
  CreateDirectory "$SMPROGRAMS\HandShaker"
  CreateShortcut "$SMPROGRAMS\HandShaker\HandShaker.lnk" "$INSTDIR\HandShaker.AoaLauncher.exe" "" "$INSTDIR\MyIcon.ico"
  CreateShortcut "$SMPROGRAMS\HandShaker\卸载 HandShaker.lnk" "$INSTDIR\HandShakerUninst.exe"
  CreateShortcut "$DESKTOP\HandShaker.lnk" "$INSTDIR\HandShaker.AoaLauncher.exe" "" "$INSTDIR\MyIcon.ico"
SectionEnd
