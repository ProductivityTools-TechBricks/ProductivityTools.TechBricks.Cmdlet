using Firebase.Auth;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace ProductivityTools.TechBricks.Data
{
    public class FirebaseAccess
    {
        public HttpClient HttpClient { get; private set; }
        public async Task Get()
        {
            var zz= await CustomToken();
            var z1 = await IDToken(zz);
            //var x = await GetToken();
            //var z = await GetToke2();
            this.HttpClient = new HttpClient();
            Uri url = new Uri(@"http://127.0.0.1:8080/Card");

            this.HttpClient.DefaultRequestHeaders.Accept.Clear();
            this.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            this.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", z1);
            //HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, action);



            HttpResponseMessage response = await this.HttpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var resultAsString = await response.Content.ReadAsStringAsync();
                // T result = JsonConvert.DeserializeObject<T>(resultAsString);
                // return resultAsString;.  
                ///https://firebase.google.com/docs/reference/rest/auth#section-sign-in-email-password
                /////https://firebase.google.com/docs/auth/admin/create-custom-tokens?hl=en#web
            }
            throw new Exception(response.ReasonPhrase);
        }


        async Task<string> CustomToken()
        {


            this.HttpClient = new HttpClient();
            Uri url = new Uri(@"http://127.0.0.1:8080/Token");

            this.HttpClient.DefaultRequestHeaders.Accept.Clear();
            this.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await this.HttpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var resultAsString = await response.Content.ReadAsStringAsync();
                // T result = JsonConvert.DeserializeObject<T>(resultAsString);
                return resultAsString;
            }
            throw new Exception(response.ReasonPhrase);
        }

        public class TT
        {
            public string kind { get; set; }
            public string idToken { get; set; }
            public string refreshToken { get; set; }
            public string exiresIn { get; set; }

            public bool isNewUser { get; set; }
        }

        async Task<string> IDToken(string custom_token)
        {


            this.HttpClient = new HttpClient();
            Uri url = new Uri(@"https://identitytoolkit.googleapis.com/v1/accounts:signInWithCustomToken?key=AIzaSyAr0HCAH_e80NeivpRm7PJls50O0sX2Y18");

            this.HttpClient.DefaultRequestHeaders.Accept.Clear();
            this.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            object obj = new { token = custom_token, returnSecureToken = true };
            var dataAsString = JsonConvert.SerializeObject(obj);
            var content = new StringContent(dataAsString, Encoding.UTF8, "application/json");


            HttpResponseMessage response = await this.HttpClient.PostAsync(url,content);
            if (response.IsSuccessStatusCode)
            {
                var resultAsString = await response.Content.ReadAsStringAsync();
                TT result = JsonConvert.DeserializeObject<TT>(resultAsString);
                return result.idToken;
            }
            throw new Exception(response.ReasonPhrase);
        }

        async Task<string> GetToke2()
        {

            var keyFilePath = @"C:\Users\LindaL\Documents\.credentials\ServiceAccount.json";
            var clientEmail = "1046123799103-6v9cj8jbub068jgmss54m9gkuk4q2qu8@developer.gserviceaccount.com";
            var scopes = new[] { "https://www.googleapis.com/auth/indexing" };

            GoogleCredential credential;
            using (var stream = new FileStream(@"d:\Bitbucket\all.configuration\pttechbricksapi-adminsdk.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                .CreateScoped(scopes);
            }

            var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            return token.ToString();

            //GoogleCredential credential = GoogleCredential.FromFile(@"d:\Bitbucket\all.configuration\pttechbricksapi-adminsdk.json").CreateScoped(
            //    new string[] { "https://www.googleapis.com/auth/userinfo.email" });
            //var x= await  (credential as ITokenAccess).GetAccessTokenForRequestAsync();
            //return x.ToString();
        }
        async Task<string> GetToken()
        {
          

            GoogleCredential credential;
            using (var stream = new System.IO.FileStream(@"d:\Bitbucket\all.configuration\pttechbricksapi-adminsdk.json",
                System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(
                     new string[] {
                "https://www.googleapis.com/auth/firebase.database",
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/indexing"}
                );
            }
            

            ITokenAccess c = credential as ITokenAccess;
            return await c.GetAccessTokenForRequestAsync();
        }
    }
}