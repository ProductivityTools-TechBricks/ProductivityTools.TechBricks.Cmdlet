# ProductivityTools.TechBricks.Cmdlet

PowerShell module for [TechBricks](https://pt-techbricks.web.app). It reads pallets and bricks of the signed in user from the TechBricks API (`ProductivityTools.TechBricks.Api`).

## Requirements

- PowerShell 7.4+ (.NET 8)

## Build

```
dotnet build -c Release
Import-Module .\ProductivityTools.TechBricks.Cmdlet\bin\Release\net8.0\ProductivityTools.TechBricks.psd1
```

## Cmdlets

| Cmdlet | Description |
| --- | --- |
| `Connect-TechBricks` | Signs in through the browser (Google account) and stores the session locally. |
| `Disconnect-TechBricks` | Removes the locally stored session. |
| `Get-TechBricksPallet [-Name <wildcard>] [-Owner <gmail login>]` | Returns pallets visible to the signed in user. |
| `Get-TechBricksBrick [-Pallet <wildcard>] [-Description <wildcard>] [-Content <wildcard>] [-Owner <gmail login>]` | Returns bricks (`Description` = title, `Brick` = content) of the pallets. `-Key`/`-Title` are aliases of `-Description`. |

```
Get-TechBricksBrick -Pallet VS* -Description *debug*
Get-TechBricksBrick GAS -Content *Spreadsheet* | Format-List
```

## Authentication

The API accepts only Firebase ID tokens (project `ptprojectsweb`, the same as the web application) with a verified e-mail.

1. The cmdlet starts a temporary HTTP server on `http://localhost:<random port>/` and opens it in the browser.
2. The page signs in with Google using the Firebase JS SDK and posts the Firebase refresh token back to the cmdlet (protected by a random `state` value).
3. The refresh token is stored in `%APPDATA%\ProductivityTools.TechBricks\session.json` (`~/.config/ProductivityTools.TechBricks/session.json` on Linux, readable only by the user).
4. Short-lived (1h) ID tokens are minted from the refresh token via `securetoken.googleapis.com`, so the browser sign in is needed only once.

Sign in is started automatically by any cmdlet when there is no valid session. `localhost` has to be on the list of authorized domains in Firebase Authentication (it is there by default).
