# Apply locally prepared DPAPI profiles after exiting the UI. No subscription
# downloads, device identifiers, core processes or network settings are touched.
param(
    [string]$ClientDirectory='dist-v0.59',
    [string]$PreparedPath='cache/prepared-profiles.private.dat',
    [switch]$ValidateOnly
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName System.Security
[void][Reflection.Assembly]::LoadFrom((Join-Path (Join-Path $root $ClientDirectory) 'doomraVPN.exe'))
$plain=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes((Join-Path $root $PreparedPath)),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
try {
    $profiles=[Luna.Json]::Codec().Deserialize([Text.Encoding]::UTF8.GetString($plain),[Luna.SubscriptionProfile[]])
} finally { [Array]::Clear($plain,0,$plain.Length) }
if(!$profiles -or $profiles.Count -gt 24){throw 'Invalid prepared profile count.'}
foreach($profile in $profiles){
    $uri=$null
    if(![Uri]::TryCreate($profile.Subscription,[UriKind]::Absolute,[ref]$uri) -or $uri.Scheme -ne 'https'){throw 'Invalid subscription URL; contents hidden.'}
    foreach($server in $profile.Servers){[Luna.Subscriptions]::Validate($server)}
}
if($ValidateOnly){Write-Output ('Validated prepared profiles: '+$profiles.Count);return}
$sid=[Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$mutex=New-Object Threading.Mutex($false,('Local\LunaVPN.UI.'+$sid))
$held=$false
try {
    try {$held=$mutex.WaitOne(0)} catch [Threading.AbandonedMutexException] {$held=$true}
    if(!$held){throw 'Exit the VPN client through its tray menu before applying profiles. Current settings were not changed.'}
    $path=Join-Path ([Luna.Storage]::Folder) 'settings.dat'
    $settings=[Luna.Storage]::Load($path)
    foreach($incoming in $profiles){
        $existing=$settings.Profiles | Where-Object {$_.Subscription -eq $incoming.Subscription} | Select-Object -First 1
        if($existing){$existing.Servers=$incoming.Servers;$existing.Selected=0;$selected=$existing}
        else {
            if($settings.Profiles.Count -ge 24){throw 'Profile limit reached; current settings were not changed.'}
            $settings.Profiles.Add($incoming);$selected=$incoming
        }
    }
    $settings.ActiveProfile=$selected.Id;$settings.Subscription=$selected.Subscription
    $settings.SubscriptionInfo=$selected.Info;$settings.Servers=$selected.Servers;$settings.Selected=$selected.Selected
    $settings.ProfilesInitialized=$true
    if(Test-Path -LiteralPath $path){
        $backup=$path+'.before-0.59-'+[DateTime]::UtcNow.ToString('yyyyMMddHHmmss')+'.bak'
        Copy-Item -LiteralPath $path -Destination $backup -ErrorAction Stop
    }
    [Luna.Storage]::Save($path,$settings)
    Write-Output ('Applied profiles: '+$profiles.Count+'. Encrypted backup retained. No network settings changed.')
} finally {if($held){$mutex.ReleaseMutex()};$mutex.Dispose()}
