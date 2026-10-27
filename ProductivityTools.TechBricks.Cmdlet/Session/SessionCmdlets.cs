using System.Management.Automation;

namespace ProductivityTools.TechBricks.Cmdlet.Session
{
    /// <summary>
    /// Signs in to TechBricks through the browser (Google account). The session
    /// is stored locally, so it is needed only once - other cmdlets also start
    /// the sign in automatically when there is no session.
    /// </summary>
    [Cmdlet(VerbsCommunications.Connect, "TechBricks")]
    public class ConnectTechBricksCmdlet : TechBricksCmdletBase
    {
        protected override void Execute()
        {
            string? email = Run(token => AuthService.LoginAsync(token));
            WriteObject($"Signed in to TechBricks as {email}");
        }
    }

    /// <summary>
    /// Removes the locally stored TechBricks session.
    /// </summary>
    [Cmdlet(VerbsCommunications.Disconnect, "TechBricks")]
    public class DisconnectTechBricksCmdlet : TechBricksCmdletBase
    {
        protected override void Execute()
        {
            bool removed = AuthService.Logout();
            WriteObject(removed ? "Signed out from TechBricks" : "You were not signed in to TechBricks");
        }
    }
}
