; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Plug-in Revit LPE"
#define FolderName "LPE"
#define MyAppVersion "2.0.16"
#define MyAppPublisher "FCA"
#define Revit2021 "\Autodesk\Revit\Addins\2021\"
#define Revit2022 "\Autodesk\Revit\Addins\2022\"
#define Revit2023 "\Autodesk\Revit\Addins\2023\"
#define Revit2024 "\Autodesk\Revit\Addins\2024\"
#define Revit2025 "\Autodesk\Revit\Addins\2025\"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{264AD411-643C-43AC-9F07-34523C752100}
AppName={#MyAppName}
#define installerPath "C:\"
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\LPEApp
DisableDirPage=yes
DefaultGroupName=LPE
DisableProgramGroupPage=yes
; Uncomment the following line to run in non administrative install mode (install for current user only.)
PrivilegesRequired=lowest
; Password=revit
OutputDir=.\Output
SetupIconFile=icon-lpe-engenharia.ico
OutputBaseFilename={#MyAppName} {#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Files]

; Source: "..\Revit.2021\bin\x64\Release\*.dll"; DestDir: "{userappdata}\{#Revit2021}\{#FolderName}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Source: "..\Revit.Common\LPE-PisosEJuntas.addin"; DestDir: "{userappdata}\{#Revit2021}\"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Revit.2022\bin\x64\Release\*.dll"; DestDir: "{userappdata}\{#Revit2022}\{#FolderName}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Revit.Common\LPE-PisosEJuntas.addin"; DestDir: "{userappdata}\{#Revit2022}\"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Revit.2023\bin\x64\Release\*.dll"; DestDir: "{userappdata}\{#Revit2023}\{#FolderName}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Revit.Common\LPE-PisosEJuntas.addin"; DestDir: "{userappdata}\{#Revit2023}\"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Revit.2024\bin\x64\Release\*.dll"; DestDir: "{userappdata}\{#Revit2024}\{#FolderName}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Revit.Common\LPE-PisosEJuntas.addin"; DestDir: "{userappdata}\{#Revit2024}\"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Revit.2025\bin\Release\net8.0-windows\*.dll"; DestDir: "{userappdata}\{#Revit2025}\{#FolderName}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Revit.Common\LPE-PisosEJuntas.addin"; DestDir: "{userappdata}\{#Revit2025}\"; Flags: ignoreversion recursesubdirs createallsubdirs

; Source: "..\Revit.Common\PackageContents.xml"; DestDir: "{userappdata}\Autodesk\ApplicationPlugins\Revit.bundle\"; Flags: ignoreversion

; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

