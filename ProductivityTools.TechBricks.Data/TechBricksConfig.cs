namespace ProductivityTools.TechBricks.Data
{
    /// <summary>
    /// Production configuration of TechBricks. The same Firebase project
    /// (ptprojectsweb) is used by the web application, so a user signed in
    /// from PowerShell is the same user as in https://pt-techbricks.web.app.
    /// The Firebase web API key is not a secret - it only identifies the project.
    /// </summary>
    public static class TechBricksConfig
    {
        public const string FirebaseApiKey = "AIzaSyAxmnZm4597RcMVQeJrby3HpuDfzYyTJoU";
        public const string FirebaseAuthDomain = "ptprojectsweb.firebaseapp.com";
        public const string FirebaseProjectId = "ptprojectsweb";
        public const string FirebaseAppId = "1:93484780890:web:a9e5766ce07eeee79ff763";

        public const string ApiBaseUrl = "https://pt-techbricks-api-93484780890.europe-west1.run.app/";
    }
}
