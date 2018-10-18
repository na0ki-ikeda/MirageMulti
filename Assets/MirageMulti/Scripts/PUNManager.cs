using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PhotonNetworkのラッパークラス、ここ以外でPhotonNetworkインスタンスを呼ぶのは禁止
/// </summary>
public class PUNManager : SingletonMonoBehaviour<PUNManager>
{
    #region SerializedField definition

    [SerializeField]
    string networkVersion_ = "0.1";

    string NetworkVersion_
    {
        get
        {
            return networkVersion_;
        }

        set
        {
            networkVersion_ = value;
        }
    }

    [SerializeField]
    bool wiiBeMasterClient_ = false;

    bool WiiBeMasterClient_
    {
        get
        {
            return wiiBeMasterClient_;
        }

        set
        {
            wiiBeMasterClient_ = value;
        }
    }

    #endregion

    RoomInfo[] RoomList_ = null;
    float RoomJoiningTimeout_ = 0f;

    public bool RoomJoining { get; private set; } = false;

    void Start()
    {
        //Scene遷移で自動削除しない
        DontDestroyOnLoad(gameObject);

        //サーバへ接続(自動でロビーまで接続される)
        PhotonNetwork.ConnectUsingSettings(NetworkVersion_);
    }

    void OnJoinedLobby()
    {
        //マスタークライアントになる際は部屋を作成
        if (WiiBeMasterClient_)
        {
            PhotonNetwork.CreateRoom(null);
        }
    }

    void OnReceivedRoomListUpdate()
    {
        RoomList_ = PhotonNetwork.GetRoomList();
    }

    void Update()
    {
        //マスタークライアントの場合は処理不要
        if (WiiBeMasterClient_ || PhotonNetwork.inRoom) return;

        //タイムアウト
        if (RoomJoining && Time.timeSinceLevelLoad > RoomJoiningTimeout_)
        {
            RoomJoining = false;
            Debug.LogWarning(this.name + ": join room timeout");
            return;
        }

        //ルームが取得できていれば接続
        if (!RoomJoining && RoomList_ != null && RoomList_.Length > 0)
        {
            RoomJoining = true;
            RoomJoiningTimeout_ = Time.timeSinceLevelLoad + 10.0f;//10秒タイムアウト
            return;
        }

        //ルームが無い
    }

    //TODO: *must* 接続エラー時の処理が必要
    //TODO: *must* マスタークライアントが切れたときは強制切断
    //TODO: *want* Firebaseでバージョンチェックして低かったら表示

    /// <summary>
    /// デバッグ用のネットワークステート表示
    /// </summary>    
    void OnGUI()
    {
        // Photonのステータスをラベルで表示させています
        GUILayout.Label(PhotonNetwork.connectionStateDetailed.ToString());
    }

    /// <summary>
    /// ルーム入室しているか返す
    /// </summary>
    /// <returns></returns>
    public bool IsRoomJoined()
    {
        return PhotonNetwork.inRoom;
    }

}
