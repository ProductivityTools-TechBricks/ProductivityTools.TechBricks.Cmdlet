using System.Management.Automation;
using ProductivityTools.TechBricks.Data.Models;

namespace ProductivityTools.TechBricks.Cmdlet.Pallets
{
    /// <summary>
    /// Returns pallets visible to the signed in user.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "TechBricksPallet")]
    [OutputType(typeof(Pallet))]
    public class GetTechBricksPalletCmdlet : TechBricksCmdletBase
    {
        [Parameter(Position = 0, HelpMessage = "Pallet name, wildcards are supported")]
        [SupportsWildcards]
        public string? Name { get; set; }

        [Parameter(HelpMessage = "Gmail login of the pallet owner (default: signed in user)")]
        public string? Owner { get; set; }

        protected override void Execute()
        {
            var pallets = Run(token => CreateClient().GetPalletsAsync(Owner, token));
            foreach (var pallet in PalletFilter.ByName(pallets, Name))
            {
                WriteObject(pallet);
            }
        }
    }

    /// <summary>
    /// Returns bricks (title and content) of pallets visible to the signed in user.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "TechBricksBrick")]
    [OutputType(typeof(PSObject))]
    public class GetTechBricksBrickCmdlet : TechBricksCmdletBase
    {
        [Parameter(Position = 0, HelpMessage = "Pallet name, wildcards are supported")]
        [SupportsWildcards]
        public string? Pallet { get; set; }

        [Parameter(Position = 1, HelpMessage = "Brick title, wildcards are supported")]
        [Alias("Title", "Key")]
        [SupportsWildcards]
        public string? Description { get; set; }

        [Parameter(HelpMessage = "Text searched in the brick content, wildcards are supported (e.g. *kubectl*)")]
        [SupportsWildcards]
        public string? Content { get; set; }

        [Parameter(HelpMessage = "Gmail login of the pallet owner (default: signed in user)")]
        public string? Owner { get; set; }

        protected override void Execute()
        {
            var pallets = Run(token => CreateClient().GetPalletsAsync(Owner, token));
            var titlePattern = PalletFilter.Pattern(Description);
            var contentPattern = PalletFilter.Pattern(Content);
            foreach (var pallet in PalletFilter.ByName(pallets, Pallet))
            {
                foreach (var brick in pallet.Bricks)
                {
                    if (titlePattern != null && !titlePattern.IsMatch(brick.Title))
                    {
                        continue;
                    }
                    if (contentPattern != null && !contentPattern.IsMatch(brick.Text))
                    {
                        continue;
                    }
                    var result = new PSObject();
                    result.Properties.Add(new PSNoteProperty("Pallet", pallet.Name));
                    result.Properties.Add(new PSNoteProperty("Description", brick.Title));
                    result.Properties.Add(new PSNoteProperty("Brick", brick.Text));
                    WriteObject(result);
                }
            }
        }
    }

    internal static class PalletFilter
    {
        public static WildcardPattern? Pattern(string? value)
        {
            return string.IsNullOrEmpty(value) ? null : new WildcardPattern(value, WildcardOptions.IgnoreCase);
        }

        public static IEnumerable<Pallet> ByName(IEnumerable<Pallet> pallets, string? name)
        {
            var pattern = Pattern(name);
            return pallets
                .Where(p => pattern == null || pattern.IsMatch(p.Name))
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase);
        }
    }
}
