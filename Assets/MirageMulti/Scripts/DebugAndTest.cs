using UnityEngine;

public class DebugAndTest : MonoBehaviour
{
    public async void DoLoginTwitter()
    {
        this.enabled = false;
        await GameObject.FindObjectOfType<LoginManager>().TwitterSignInAsync();
        this.enabled = true;
    }

    public async void DoLoginAnonymous()
    {
        this.enabled = false;
        await GameObject.FindObjectOfType<LoginManager>().AnonymouslySignIn();
        this.enabled = true;
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
