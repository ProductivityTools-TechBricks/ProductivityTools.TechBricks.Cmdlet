@{
RootModule = 'ProductivityTools.TechBricks.dll'
ModuleVersion = '1.0.0'
GUID = '6e41200c-b18a-45ba-9590-c578cdd51599'
Author = 'Pawel Wujczyk'
Description = 'PowerShell access to TechBricks (https://pt-techbricks.web.app) - pallets and bricks of the signed in user.'
PowerShellVersion = '7.4'
CompatiblePSEditions = @('Core')
CmdletsToExport = @('Connect-TechBricks', 'Disconnect-TechBricks', 'Get-TechBricksPallet', 'Get-TechBricksBrick')
FunctionsToExport = @()
AliasesToExport = @()
VariablesToExport = @()
PrivateData = @{
    PSData = @{
        Tags = @('TechBricks', 'ProductivityTools')
        ProjectUri = 'https://github.com/ProductivityTools-TechBricks/ProductivityTools.TechBricks.Cmdlet'
    }
}
}
