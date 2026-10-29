# ProductivityTools.TechBricks.Cmdlet

PowerShell module for [TechBricks](https://pt-techbricks.web.app). It reads pallets and bricks of the signed in user from the TechBricks API (`ProductivityTools.TechBricks.Api`).

- **Pallet** - a named group of bricks (e.g. `GAS`, `git`, `GCP`), the list on the left side of the web application.
- **Brick** - a single note in a pallet: `Description` (title) and `Brick` (content, often multi-line code).

The module is read-only. Pallets and bricks are edited in the web application.

## Requirements

- PowerShell 7.4+ (.NET 8) - Windows, Linux or macOS

## Installation

### From PowerShell Gallery

```powershell
Install-Module ProductivityTools.TechBricks -Scope CurrentUser
Import-Module ProductivityTools.TechBricks
```

### From sources

```powershell
dotnet build -c Release
Import-Module ./ProductivityTools.TechBricks.Cmdlet/bin/Release/net8.0/ProductivityTools.TechBricks.psd1
```

Rebuild in a new PowerShell session - a loaded module DLL cannot be replaced in a running session.

### Linux notes

If .NET and PowerShell were installed into the home directory (`dotnet-install.sh`, `dotnet tool install --global PowerShell`) they are not on `PATH`. Add to `~/.bashrc`:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
```

To import the module built from sources by name (and to get it auto-loaded on the first cmdlet call), link the build output into the user module directory:

```bash
mkdir -p ~/.local/share/powershell/Modules
ln -s "$PWD/ProductivityTools.TechBricks.Cmdlet/bin/Release/net8.0" \
      ~/.local/share/powershell/Modules/ProductivityTools.TechBricks
```

## Quick start

```powershell
Connect-TechBricks                      # one-time browser sign in
Get-TechBricksPallet                    # list of your pallets
Get-TechBricksBrick GAS | Format-List   # all bricks of the GAS pallet
```

## Cmdlets

| Cmdlet | Description |
| --- | --- |
| `Connect-TechBricks` | Signs in through the browser (Google account) and stores the session locally. |
| `Disconnect-TechBricks` | Removes the locally stored session. |
| `Get-TechBricksPallet [[-Name] <wildcard>] [-Owner <gmail login>]` | Returns pallets visible to the signed in user, sorted by name. |
| `Get-TechBricksBrick [[-Pallet] <wildcard>] [[-Description] <wildcard>] [-Content <wildcard>] [-Owner <gmail login>]` | Returns bricks of the pallets as `Pallet`, `Description`, `Brick` objects. |

All name filters are case-insensitive and support wildcards (`*`, `?`, `[a-z]`). Without a wildcard the value has to match exactly (`GAS` does not match `GAS-old`).

### Connect-TechBricks / Disconnect-TechBricks

```powershell
PS> Connect-TechBricks
Opening browser for sign in: http://localhost:41275/
If the browser did not open, open the address above manually.
Signed in to TechBricks as pwujczyk@gmail.com

PS> Disconnect-TechBricks
Signed out from TechBricks
```

Calling `Connect-TechBricks` is optional - every cmdlet starts the sign in automatically when there is no valid session. See [Authentication](#authentication).

### Get-TechBricksPallet

| Parameter | Position | Description |
| --- | --- | --- |
| `-Name` | 0 | Pallet name, wildcards supported. All pallets when omitted. |
| `-Owner` | | Gmail login (without `@gmail.com`) of the pallet owner. Default: signed in user. |

Returned object (`ProductivityTools.TechBricks.Data.Models.Pallet`): `Name`, `DocumentId`, `Owners`, `Bricks`.

```powershell
# All pallets
Get-TechBricksPallet

# Pallets starting with G, with the number of bricks
PS> Get-TechBricksPallet G* | Format-Table Name, Owners, @{n='Bricks'; e={$_.Bricks.Count}}

Name      Owners     Bricks
----      ------     ------
GAS       {pwujczyk}      6
GCP       {pwujczyk}      4
GCP-old   {pwujczyk}     15
GCP-VPC   {pwujczyk}      3
git       {pwujczyk}      9

# Single pallet by exact name
Get-TechBricksPallet GAS

# Pallets of another user that are shared with you
Get-TechBricksPallet -Owner colleague

# Empty pallets
Get-TechBricksPallet | Where-Object { $_.Bricks.Count -eq 0 } | Select-Object Name
```

### Get-TechBricksBrick

| Parameter | Position | Description |
| --- | --- | --- |
| `-Pallet` | 0 | Pallet name, wildcards supported. All pallets when omitted. |
| `-Description` | 1 | Brick title, wildcards supported. Aliases: `-Title`, `-Key`. |
| `-Content` | | Text searched in the brick content, wildcards supported - use `*text*` to search anywhere. |
| `-Owner` | | Gmail login (without `@gmail.com`) of the pallet owner. Default: signed in user. |

Returned object: `Pallet`, `Description`, `Brick`.

```powershell
# All bricks of a pallet (Format-List shows multi-line content, a table truncates it)
PS> Get-TechBricksBrick GAS | Format-List

Pallet      : GAS
Description : Limitation
Brick       : GAS could run only for 6 minutes

Pallet      : GAS
Description : Various
Brick       : You can create a GAS function and invoke it in Google Sheet like =myFunction(a1,b1)
...

# Overview - pallet and title only
Get-TechBricksBrick GAS | Format-Table Pallet, Description

# Brick by pallet and title (positional parameters)
Get-TechBricksBrick GAS SideBar

# Title with a wildcard in all pallets starting with "GCP"
Get-TechBricksBrick -Pallet GCP* -Description *firewall*

# Full-text search in the content of all pallets
Get-TechBricksBrick -Content *kubectl*

# Title OR content contains a phrase
Get-TechBricksBrick | Where-Object { $_.Description -like '*rebase*' -or $_.Brick -like '*rebase*' }

# Only the content - e.g. to paste it somewhere
(Get-TechBricksBrick GAS SideBar).Brick

# Copy the content to the clipboard (Linux needs xclip, xsel or wl-clipboard)
(Get-TechBricksBrick git *rebase* | Select-Object -First 1).Brick | Set-Clipboard

# Pick a brick interactively (Windows: Out-GridView, any OS: Microsoft.PowerShell.ConsoleGuiTools)
Get-TechBricksBrick | Out-GridView -PassThru | Select-Object -ExpandProperty Brick

# Number of bricks per pallet
Get-TechBricksBrick | Group-Object Pallet | Sort-Object Count -Descending | Select-Object Count, Name
```

### Export / backup

```powershell
# Flat list of bricks to CSV
Get-TechBricksBrick | Export-Csv ~/techbricks.csv

# Whole pallets (with owners and bricks) to JSON
Get-TechBricksPallet | ConvertTo-Json -Depth 5 | Set-Content ~/techbricks.json

# One Markdown file per pallet
$fence = '```'
New-Item -ItemType Directory ~/techbricks -Force | Out-Null
Get-TechBricksPallet | ForEach-Object {
    $md = foreach ($b in $_.Bricks) { "## $($b.Title)", '', $fence, $b.Text, $fence, '' }
    $md | Set-Content (Join-Path ~/techbricks "$($_.Name).md")
}
```

## Authentication

The API accepts only Firebase ID tokens (project `ptprojectsweb`, the same as the web application) with a verified e-mail.

1. The cmdlet starts a temporary HTTP server on `http://localhost:<random port>/` and opens it in the browser.
2. The page signs in with Google using the Firebase JS SDK and posts the Firebase refresh token back to the cmdlet (protected by a random `state` value).
3. The refresh token is stored in `%APPDATA%\ProductivityTools.TechBricks\session.json` (`~/.config/ProductivityTools.TechBricks/session.json` on Linux, readable only by the user).
4. Short-lived (1h) ID tokens are minted from the refresh token via `securetoken.googleapis.com`, so the browser sign in is needed only once.

Sign in is started automatically by any cmdlet when there is no valid session. `localhost` has to be on the list of authorized domains in Firebase Authentication (it is there by default).

### Sign in on a remote machine (SSH)

When PowerShell runs on a remote machine (e.g. a cloud VM over SSH) the browser on your computer cannot reach `localhost` of that machine. Forward the port printed by the cmdlet:

```bash
# on your computer, in a second terminal (41275 = port from "Opening browser for sign in: ...")
ssh -L 41275:localhost:41275 <remote host>
```

and open `http://localhost:41275/` in the local browser. Alternatively sign in on a machine with a browser and copy `session.json` to the same location on the remote machine.

## Publish to PowerShell Gallery

Increase `ModuleVersion` in `ProductivityTools.TechBricks.Cmdlet/ProductivityTools.TechBricks.psd1`, then:

```powershell
$env:PSGALLERY_API_KEY = '<key from https://www.powershellgallery.com/account/apikeys>'
./publish.ps1 -WhatIf   # dry run
./publish.ps1
```

The script builds the solution, stages the module in `artifacts/ProductivityTools.TechBricks` (Publish-Module requires a directory named like the module) and publishes it.
