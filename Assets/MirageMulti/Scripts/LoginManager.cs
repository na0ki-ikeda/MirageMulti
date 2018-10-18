using System.Threading.Tasks;
using TwitterKit.Unity;
using UnityEngine;

public class LoginManager : SingletonMonoBehaviour<LoginManager>
{
    #region InternalClass Definition

    abstract class BaseAuth
    {
        public abstract void Initialize();
        public abstract void Login();
        public abstract bool IsLogin();
    }

    class TwitterAuth : BaseAuth
    {
        string AccessToken_;
        string Secret_;
        string Name_;

        void _LoginComplete(TwitterSession session)
        {
            AccessToken_ = session.authToken.token;
            Secret_ = session.authToken.secret;
            Name_ = session.userName;
        }

        void _LoginFailure(ApiError error)
        {
            Debug.LogError("failed to login Twitter");
        }

        public override void Initialize()
        {
            //1度だけ呼ぶ必要がある
            Twitter.AwakeInit();
            Twitter.Init();
        }

        public override void Login()
        {
            //セッションを取得
            TwitterSession session = Twitter.Session;
            if (session == null)
            {
                //ログインを試す
                Twitter.LogIn(_LoginComplete, _LoginFailure);
            }
            else
            {
                //トークンの取得
                _LoginComplete(session);
            }
        }

        public override bool IsLogin()
        {
            return AccessToken_ != null;
        }

        public string GetToken()
        {
            return AccessToken_;
        }

        public string GetSecret()
        {
            return Secret_;
        }

        public string GetName()
        {
            return Name_;
        }
    }

    class FirebaseAuthWrapper
    {
        Firebase.Auth.FirebaseUser User_ { set; get; }

        public FirebaseAuthWrapper()
        {

        }

        public async Task<bool> Initialize()
        {
            //FirebaseAppを使う前に1度だけ呼ぶ必要がある
            var status = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync();

            //チェック
            if (status != Firebase.DependencyStatus.Available)
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + status);
                return false;
            }

            return true;
        }

        public async Task<bool> TwitterSignIn(string token, string secret)
        {
            //チェック
            Debug.Assert(token != null && token.Length != 0, "Invalid token");
            Debug.Assert(secret != null && secret.Length != 0, "Invalid secret");

            var auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
            var credential = Firebase.Auth.TwitterAuthProvider.GetCredential(token, secret);

            try
            {
                User_ = await auth.SignInWithCredentialAsync(credential);
            }
            catch (System.Exception)
            {
                Debug.LogError("Could not signIn to Twitter. Did you enable Twitter auth on Firebase?");
                return false;
            }

            Debug.LogFormat("User signed in successfully: {0} ({1})", User_.DisplayName, User_.UserId);

            return true;
        }

        public async Task<bool> AnonymouslySignIn()
        {
            Firebase.Auth.FirebaseAuth auth = Firebase.Auth.FirebaseAuth.DefaultInstance;

            try
            {
                User_ = await auth.SignInAnonymouslyAsync();
            }
            catch (System.Exception)
            {
                Debug.LogError("Could not signIn as an Anonymous. Did you enable Anonymous auth on Firebase?");
                return false;
            }

            Debug.LogFormat("User signed in successfully: {0} ({1})", User_.DisplayName, User_.UserId);

            return true;
        }

    }

    #endregion

    readonly TwitterAuth Twitter_ = new TwitterAuth();
    readonly FirebaseAuthWrapper Firebase_ = new FirebaseAuthWrapper();

    bool Initialized_ = false;

    new void Awake()
    {
        base.Awake();
    }

    async void Start()
    {
        Twitter_.Initialize();
        await Firebase_.Initialize();

        Initialized_ = true;
    }

    void Update()
    {
        //初期化待ち
        if (!Initialized_) return;
    }

    public async Task<bool> TwitterSignInAsync(int retry = 100)
    {
        if (!Initialized_) return false;

        Twitter_.Login();

        while (--retry > 0 && !Twitter_.IsLogin())
        {
            await Task.Delay(10);
        }

        //リトライ上限だった
        if (retry == 0) return false;

        //Firebaseでサインイン
        var result = await Firebase_.TwitterSignIn(Twitter_.GetToken(), Twitter_.GetSecret());

        //失敗
        if (!result) return false;

        return true;
    }

    public async Task<bool> AnonymouslySignIn()
    {
        if (!Initialized_) return false;

        //Firebaseでサインイン
        var result = await Firebase_.AnonymouslySignIn();

        //失敗
        if (!result) return false;

        return true;
    }

}
