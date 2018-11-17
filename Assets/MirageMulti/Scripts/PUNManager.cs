using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PhotonNetworkのラッパークラス、ここ以外でPhotonNetworkインスタンスを呼ぶのは禁止
/// </summary>
public class PUNManager : SingletonPunBehaviour<PUNManager>
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

    List<RoomInfo> RoomList_ = null;
    float RoomJoiningTimeout_ = 0f;

    public bool RoomJoining { get; private set; } = false;

    void Start()
    {
        //Scene遷移で自動削除しない
        DontDestroyOnLoad(gameObject);

        //サーバへ接続
        PhotonNetwork.GameVersion = "0.1";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        //ロビーへ接続
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        //マスタークライアントになる際は部屋を作成
        if (WiiBeMasterClient_)
        {
            PhotonNetwork.CreateRoom(null);
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        RoomList_ = roomList;
    }

    void Update()
    {
        //マスタークライアントの場合は処理不要
        if (WiiBeMasterClient_ || PhotonNetwork.InRoom) return;

        //タイムアウト
        if (RoomJoining && Time.timeSinceLevelLoad > RoomJoiningTimeout_)
        {
            RoomJoining = false;
            Debug.LogWarning(this.name + ": join room timeout");
            return;
        }

        //ルームが取得できていれば接続
        if (!RoomJoining && RoomList_ != null && RoomList_.Count > 0)
        {
            RoomJoining = true;
            RoomJoiningTimeout_ = Time.timeSinceLevelLoad + 10.0f;//10秒タイムアウト
            PhotonNetwork.JoinRoom(RoomList_[0].Name);
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
        GUILayout.Label(PhotonNetwork.NetworkClientState.ToString());
    }

    /// <summary>
    /// ルーム入室しているか返す
    /// </summary>
    /// <returns></returns>
    public bool IsRoomJoined()
    {
        return PhotonNetwork.InRoom;
    }

}
