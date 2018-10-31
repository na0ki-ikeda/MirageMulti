using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Realtime;
using Photon.Voice.Unity;
using Photon.Voice.Unity.UtilityScripts;
using UnityEngine;
using UnityEngine.UI;


namespace Photon.Voice.DemoVoiceUI
{
    [RequireComponent(typeof(VoiceConnection))]
    public class DemoVoiceUI : MonoBehaviour
    {
        [SerializeField]
        private Text connectionStatusText;

        [SerializeField]
        private Text serverStatusText;

        [SerializeField]
        private Text roomStatusText;

        [SerializeField]
        private Text inputWarningText;

		[SerializeField]
        private Text packetLossWarningText;

        [SerializeField]
        private InputField localNicknameText;

		[SerializeField]
		private VoiceAudioPreprocessor voiceAudioPreprocessor;

        public Transform RemoteVoicesPanel;

        private VoiceConnection voiceConnection;

        // this demo uses a Custom Property (as explained in the Realtime API), to sync if a player muted her microphone. that value needs a string key.
        protected internal const string MutePropKey = "mute";

        private Color warningColor = new Color(0.9f,0.5f,0f,1f);
        private Color okColor = new Color(0.0f,0.6f,0.2f,1f);

        private void Awake()
        {
            this.voiceConnection = this.GetComponent<VoiceConnection>();
        }

        private void OnEnable()
        {
            this.voiceConnection.SpeakerLinked += this.OnSpeakerCreated;

            if (this.localNicknameText != null)
            {
                string savedNick = PlayerPrefs.GetString("vNick");
                if (!string.IsNullOrEmpty(savedNick))
                {
                    Debug.Log("Saved nick = " + savedNick);
                    this.localNicknameText.text = savedNick;
                    this.voiceConnection.Client.NickName = savedNick;
                }
            }
        }

        private void OnDisable()
        {
            this.voiceConnection.SpeakerLinked -= this.OnSpeakerCreated;
        }

        private void OnSpeakerCreated(Speaker speaker)
        {
            speaker.gameObject.transform.SetParent(this.RemoteVoicesPanel, false);
        }

        private void OnRemoteVoiceRemove(Speaker speaker)
        {
            Destroy(speaker.gameObject);
        }


        public void ToggleTransmit()
        {
            bool toggledTransmit = !this.voiceConnection.PrimaryRecorder.TransmitEnabled;
            this.voiceConnection.PrimaryRecorder.TransmitEnabled = toggledTransmit;

            this.voiceConnection.Client.LocalPlayer.SetCustomProperties(new Hashtable() { { MutePropKey, !toggledTransmit } }); // transmit is used as opposite of mute...
        }

        public void ToggleDebugEcho()
        {
            bool debugEcho = !this.voiceConnection.PrimaryRecorder.DebugEchoMode;
            this.voiceConnection.PrimaryRecorder.DebugEchoMode = debugEcho;
        }

        public void ToggleReliable()
        {
            bool reliable = !this.voiceConnection.PrimaryRecorder.ReliableMode;
            this.voiceConnection.PrimaryRecorder.ReliableMode = reliable;
        }

		public void ToggleAEC()
		{
			this.voiceAudioPreprocessor.AEC = !this.voiceAudioPreprocessor.AEC;
		}
            
		public void ToggleNoiseSuppression()
		{
			this.voiceAudioPreprocessor.NoiseSuppression = !this.voiceAudioPreprocessor.NoiseSuppression;
		}

		public void ToggleAGC()
		{
			this.voiceAudioPreprocessor.AGC = !this.voiceAudioPreprocessor.AGC;
		}

		public void ToggleVAD()
		{
			this.voiceAudioPreprocessor.VAD = !this.voiceAudioPreprocessor.VAD;
		}
			
        /// <summary>Called by UI.</summary>
        public void UpdateSyncedNickname(string nickname)
        {
            nickname = nickname.Trim();
            if (string.IsNullOrEmpty(nickname))
            {
                return;
            }

            Debug.Log("UpdateSyncedNickname() name: " + nickname);
            this.voiceConnection.Client.LocalPlayer.NickName = nickname;
            PlayerPrefs.SetString("vNick", nickname);
        }


        /// <summary>Called by UI.</summary>
        public void JoinOrCreateRoom(string roomname)
        {
            roomname = roomname.Trim();
            ConnectAndJoin caj = this.GetComponent<ConnectAndJoin>();
            if (caj == null) return;

            Debug.Log("JoinOrCreateRoom() roomname: " + roomname);

            if (string.IsNullOrEmpty(roomname))
            {
                caj.RoomName = string.Empty;
                caj.RandomRoom = true;
                //caj.HideRoom = false;
            }
            else
            {
                caj.RoomName = roomname;
                caj.RandomRoom = false;
                //caj.HideRoom = true;
            }
            this.voiceConnection.Client.OpLeaveRoom(false);
        }

        protected void Update()
        {

            this.connectionStatusText.text = this.voiceConnection.Client.State.ToString();
            this.serverStatusText.text = string.Format("{0}/{1}", this.voiceConnection.Client.CloudRegion, this.voiceConnection.Client.CurrentServerAddress);
            string playerDebugString = string.Empty;
            if (this.voiceConnection.Client.InRoom)
            {
                Dictionary<int, Player>.ValueCollection temp = this.voiceConnection.Client.CurrentRoom.Players.Values;
                if (temp != null && temp.Count > 1)
                {
                    foreach (Player p in temp)
                    {
                        playerDebugString += p.ToStringFull();
                    }
                }
            }
            this.roomStatusText.text = this.voiceConnection.Client.CurrentRoom == null ? string.Empty : this.voiceConnection.Client.CurrentRoom.Name + " " + playerDebugString;

            var amplitude = this.voiceConnection.PrimaryRecorder.LevelMeter.CurrentAvgAmp;
            if (amplitude > 1) {
                amplitude /= 32768;
            }
            if (amplitude > 0.1) {
                this.inputWarningText.text = "Input too loud!";
                this.inputWarningText.color = this.warningColor;
            } else {
                this.inputWarningText.text = "";
            } 

            if (this.voiceConnection.FramesReceivedPerSecond>0) {
                this.packetLossWarningText.text = string.Format("{0}% Packet Loss", this.voiceConnection.FramesLostPercent);
                this.packetLossWarningText.color = (this.voiceConnection.FramesLostPercent > 1) ? this.warningColor : this.okColor;
            } else {
                this.packetLossWarningText.text = "(no data)";
            }
        }
    }
}