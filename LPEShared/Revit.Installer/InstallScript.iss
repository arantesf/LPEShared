; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Plug-in Revit LPE"
#define MyAppVersion "1.1"
#define MyAppPublisher "FCA"
#define Revit2021 "\Autodesk\Revit\Addins\2021\"
#define Revit2022 "\Autodesk\Revit\Addins\2022\"
#define Revit2023 "\Autodesk\Revit\Addins\2023\"
#define Revit2024 "\Autodesk\Revit\Addins\2024\"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{264AD411-643C-43AC-9F07-34523C752100}
AppName={#MyAppName}
#define installerPath "C:\"
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={#installerPath}
DisableDirPage=yes
DefaultGroupName=Revit
DisableProgramGroupPage=yes
; Uncomment the following line to run in non administrative install mode (install for current user only.)
; PrivilegesRequired=lowest
; Password=revit
OutputDir=.\Output
SetupIconFile=C:\Revit.ico
OutputBaseFilename=Revit
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]


Source: "..\Revit.2021\bin\Release\*"; DestDir: "{commonappdata}{#Revit2021}\{#MyAppName}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Revit.Common\Revit.addin"; DestDir: "{commonappdata}{#Revit2021}\"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Revit.2022\bin\Release\*"; DestDir: "{commonappdata}{#Revit2022}\{#MyAppName}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Revit.Common\Revit.addin"; DestDir: "{commonappdata}{#Revit2022}\"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Revit.2023\bin\Release\*"; DestDir: "{commonappdata}{#Revit2023}\{#MyAppName}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Revit.Common\Revit.addin"; DestDir: "{commonappdata}{#Revit2023}\"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Revit.2024\bin\Release\*"; DestDir: "{commonappdata}{#Revit2024}\{#MyAppName}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Revit.Common\Revit.addin"; DestDir: "{commonappdata}{#Revit2024}\"; Flags: ignoreversion recursesubdirs createallsubdirs

; Source: "..\Revit.Common\PackageContents.xml"; DestDir: "{userappdata}\Autodesk\ApplicationPlugins\Revit.bundle\"; Flags: ignoreversion

; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

