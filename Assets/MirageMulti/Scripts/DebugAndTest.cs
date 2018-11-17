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


    [SerializeField]
    GameObject avatarPrefab_;

    [SerializeField]
    bool playerSpawned_ = false;

    // Update is called once per frame
    void Update()
    {
        if (!playerSpawned_ && PUNManager.Instance.IsRoomJoined())
        {
            Photon.Pun.PhotonNetwork.Instantiate(avatarPrefab_.name, Vector3.zero, Quaternion.identity);

            //spawn
            playerSpawned_ = true;
        }
    }
}
