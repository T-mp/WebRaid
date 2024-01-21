//rem Remove-Service -Name WebRaidMain

$acl = Get-Acl "C:\WebRaid"
$aclRuleArgs = "DESKTOP-70NDVG4\WebRaidServiceUser", "Read,Write,ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($aclRuleArgs)
$acl.SetAccessRule($accessRule)
$acl | Set-Acl "C:\WebRaid\WebRaid.Main.exe"

New-Service -Name WebRaidMain -BinaryPathName "C:\WebRaid\WebRaid.Main.exe" -Credential "DESKTOP-70NDVG4\WebRaidServiceUser" -Description "WebRaid.Service" -DisplayName "WebRaid.Service" -StartupType Automatic
