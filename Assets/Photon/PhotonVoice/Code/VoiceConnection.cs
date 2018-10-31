// ----------------------------------------------------------------------------
// <copyright file="Recorder.cs" company="Exit Games GmbH">
//   Photon Voice for Unity - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
//  Component that represents a client voice connection to Photon Servers.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

using System;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace Photon.Voice.Unity
{
    /// <summary> Component that represents a client voice connection to Photon Servers. </summary>
    [AddComponentMenu("Photon Voice/Voice Connection")]
    public class VoiceConnection : ConnectionHandler, ILoggable
    {
        #region Private Fields

        [SerializeField]
        private DebugLevel logLevel = DebugLevel.ERROR;

        /// <summary>Key to save the "Best Region Summary" in the Player Preferences.</summary>
        private const string PlayerPrefsKey = "VoiceCloudBestRegion";

        #if PHOTON_UNITY_NETWORKING
        [SerializeField]
        protected bool usePunSettings = true;
        #endif

        private LoadBalancingFrontend client;
        [SerializeField]
        private bool enableSupportLogger;

        private SupportLogger supportLoggerComponent;

        /// <summary>
        /// time [ms] between consecutive SendOutgoingCommands calls
        /// </summary>
        [SerializeField]
        private int updateInterval = 50;

        private int nextSendTickCount;

        // Used in the main thread, OnRegionsPinged is called in a separet thread and so we can't use some of the Unity methods ( like saing in playerPrefs)
        private RegionHandler cachedRegionHandler;

        #if !UNITY_ANDROID && !UNITY_IOS
        [SerializeField]
        private bool runInBackground = true;
        #endif

        /// <summary>
        /// time [ms] between statistics calculations
        /// </summary>
        [SerializeField]
        private int statsResetInterval = 1000;

        private int nextStatsTickCount;

        private float statsReferenceTime;
        private int referenceFramesLost;
        private int referenceFramesReceived;

        [SerializeField]
        private GameObject speakerPrefab;

        #endregion

        #region Public Fields

        /// <summary> Settings to be used by this voice connection</summary>
        public AppSettings Settings;
        /// <summary> Main Recorder to be used for transmission by default</summary>
        public Recorder PrimaryRecorder;

        /// <summary> Special factory to link Speaker components with incoming remote audio streams</summary>
        public Func<int, byte, object, Speaker> SpeakerFactory;
        /// <summary> Fires when a speaker has been linked to a remote audio stream</summary>
        public event Action<Speaker> SpeakerLinked;

        #endregion

        #region Properties
        /// <summary> Logger used by this component</summary>
        public VoiceLogger Logger { get; protected set; }
        /// <summary> Log level for this component</summary>
        public DebugLevel LogLevel
        {
            get
            {
                if (this.Logger != null)
                {
                    logLevel = this.Logger.LogLevel;
                }
                return logLevel;
            }
            set
            {
                logLevel = value;
                if (this.Logger == null)
                {
                    return;
                }
                this.Logger.LogLevel = logLevel;
            }
        }

        /// <summary>Returns underlying Photon LoadBalancing client.</summary>
        public new LoadBalancingFrontend Client
        {
            get
            {
                if (client == null)
                {
                    client = new LoadBalancingFrontend();
                    client.VoiceClient.OnRemoteVoiceInfoAction += OnRemoteVoiceInfo;
                    client.OpResponseReceived += OnOperationResponse;
                }
                return client;
            }
        }

        /// <summary>Returns underlying Photon Voice client.</summary>
        public VoiceClient VoiceClient { get { return Client.VoiceClient; } }

        /// <summary>Returns Photon Voice client state.</summary>
        public ClientState ClientState { get { return Client.State; } }

        /// <summary>Number of frames received per second.</summary>
        public float FramesReceivedPerSecond { get; private set; }
        /// <summary>Number of frames lost per second.</summary>
        public float FramesLostPerSecond { get; private set; }
        /// <summary>Percentage of lost frames.</summary>
        public float FramesLostPercent { get; private set; }

        /// <summary> Prefab that contains Speaker component to be instantiated when receiving a new remote audio source info</summary>
        public GameObject SpeakerPrefab
        {
            get { return this.speakerPrefab; }
            set
            {
                if (value != this.speakerPrefab)
                {
                    if (value != null && value.GetComponentInChildren<Speaker>() == null)
                    {
                        #if UNITY_EDITOR
                        Debug.LogError("SpeakerPrefab must have a component of type Speaker in its hierarchy.", this);
                        #else
                        if (this.Logger.IsErrorEnabled)
                        {
                            this.Logger.LogError("SpeakerPrefab must have a component of type Speaker in its hierarchy.");
                        }
                        #endif
                        return;
                    }
                    this.speakerPrefab = value;
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Connect to Photon server using <see cref="Settings"/>
        /// </summary>
        /// <param name="overwriteSettings">Overwrites <see cref="Settings"/> before connecting</param>
        /// <returns>If true voice connection command was sent from client</returns>
        public bool ConnectUsingSettings(AppSettings overwriteSettings = null)
        {
            if (Client.LoadBalancingPeer.PeerState != PeerStateValue.Disconnected)
            {
                if (this.Logger.IsWarningEnabled)
                {
                    this.Logger.LogWarning("ConnectUsingSettings() failed. Can only connect while in state 'Disconnected'. Current state: {0}", Client.LoadBalancingPeer.PeerState);
                }
                return false;
            }
            if (overwriteSettings != null)
            {
                Settings = overwriteSettings;
            }
            #if PHOTON_UNITY_NETWORKING
            else if (usePunSettings)
            {
                Settings = Pun.PhotonNetwork.PhotonServerSettings.AppSettings;
            }
            #endif
            if (Settings == null)
            {
                if (this.Logger.IsErrorEnabled)
                {
                    this.Logger.LogError("Settings are null");
                }
                return false;
            }

            bool selfHosted = !string.IsNullOrEmpty(Settings.Server);
            if (Settings.Protocol == ConnectionProtocol.Tcp)
            {
                if (!selfHosted)
                {
                    if (this.Logger.IsWarningEnabled)
                    {
                        this.Logger.LogWarning("Requested protocol not supported on Photon Cloud {0}. Switched to UDP.", Settings.Protocol);
                    }
                    Client.LoadBalancingPeer.TransportProtocol = ConnectionProtocol.Udp;
                }
                else
                {
                    Client.LoadBalancingPeer.TransportProtocol = ConnectionProtocol.Tcp;
                }
            }
            else if (Settings.Protocol != ConnectionProtocol.Udp)
            {
                if (this.Logger.IsWarningEnabled)
                {
                    this.Logger.LogWarning("Requested protocol not supported: {0}. Switched to UDP.", Settings.Protocol);
                }
                Client.LoadBalancingPeer.TransportProtocol = ConnectionProtocol.Udp;
            }

            Client.AppId = Settings.AppIdVoice;
            Client.AppVersion = Settings.AppVersion;

            Client.LoadBalancingPeer.DebugOut = Settings.NetworkLogging;

            if (Settings.IsMasterServerAddress)
            {
                Client.IsUsingNameServer = false;
                Client.MasterServerAddress = Settings.Port == 0 ? Settings.Server : string.Format("{0}:{1}", Settings.Server, Settings.Port);

                return Client.Connect();
            }


            if (!Settings.IsDefaultNameServer)
            {
                Client.NameServerHost = Settings.Server;
            }

            if (Settings.IsBestRegion)
            {
                return Client.ConnectToNameServer();
            }

            return Client.ConnectToRegionMaster(Settings.FixedRegion);
        }
        
        #endregion

        #region Private Methods

        protected override void Awake()
        {
            base.Awake();
            Logger = new VoiceLogger(this, string.Format("{0}.{1}", name, this.GetType().Name), logLevel);
            if (this.SpeakerFactory == null)
            {
                this.SpeakerFactory = SimpleSpeakerFactory;
            }
            if (enableSupportLogger)
            {
                this.supportLoggerComponent = this.gameObject.AddComponent<SupportLogger>();
                this.supportLoggerComponent.Client = this.Client;
                this.supportLoggerComponent.LogTrafficStats = true;
            }
            #if !UNITY_ANDROID && !UNITY_IOS
            if (runInBackground)
            {
                Application.runInBackground = runInBackground;
            }
            #endif
        }

        protected virtual void Update()
        {
            this.VoiceClient.Service();
        }

        private void FixedUpdate()
        {
            bool doDispatch = true;
            while (doDispatch)
            {
                // DispatchIncomingCommands() returns true of it found any command to dispatch (event, result or state change)
                Profiler.BeginSample("[Photon Voice]: DispatchIncomingCommands");
                doDispatch = Client.LoadBalancingPeer.DispatchIncomingCommands();
                Profiler.EndSample();
            }
        }

        private void LateUpdate()
        {
            int currentMsSinceStart = (int)(Time.realtimeSinceStartup * 1000); // avoiding Environment.TickCount, which could be negative on long-running platforms
            if (currentMsSinceStart > this.nextSendTickCount)
            {
                bool doSend = true;
                while (doSend)
                {
                    // Send all outgoing commands
                    Profiler.BeginSample("[Photon Voice]: SendOutgoingCommands");
                    doSend = Client.LoadBalancingPeer.SendOutgoingCommands();
                    Profiler.EndSample();
                }

                this.nextSendTickCount = currentMsSinceStart + this.updateInterval;
            }

            if (currentMsSinceStart > this.nextStatsTickCount)
            {
                if (this.statsResetInterval > 0)
                {
                    this.CalcStatistics();
                    this.nextStatsTickCount = currentMsSinceStart + this.statsResetInterval;
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            this.client.Dispose();
        }

        internal virtual Speaker SimpleSpeakerFactory(int playerId, byte voiceId, object userData)
        {
            Speaker speaker;
            if (SpeakerPrefab)
            {
                GameObject go = Instantiate(SpeakerPrefab);
                speaker = go.GetComponentInChildren<Speaker>();
                if (speaker == null)
                {
                    if (this.Logger.IsErrorEnabled)
                    {
                        this.Logger.LogError("SpeakerPrefab does not have a component of type Speaker in its hierarchy.");
                    }
                    return null;
                }
            }
            else
            {
                speaker = new GameObject().AddComponent<Speaker>();
            }

            // within a room, users are identified via the Realtime.Player class. this has a nickname and enables us to use custom properties, too
            speaker.Actor = (this.Client.CurrentRoom != null) ? this.Client.CurrentRoom.GetPlayer(playerId) : null;
            speaker.name = speaker.Actor != null && !string.IsNullOrEmpty(speaker.Actor.NickName) ? speaker.Actor.NickName : String.Format("Speaker for Player {0} Voice #{1}", playerId, voiceId);
            speaker.OnRemoteVoiceRemoveAction += DeleteVoiceOnRemoteVoiceRemove;
            return speaker;
        }

        internal void DeleteVoiceOnRemoteVoiceRemove(Speaker speaker)
        {
            if (this.Logger.IsInfoEnabled)
            {
                this.Logger.LogInfo("Remote voice removed, delete speaker");
            }
            Destroy(speaker.gameObject);
        }
        
        private void OnRemoteVoiceInfo(int channelId, int playerId, byte voiceId, VoiceInfo voiceInfo, ref RemoteVoiceOptions options)
        {
            if (this.Logger.IsInfoEnabled)
            {
                this.Logger.LogInfo("PhotonVoice: OnRemoteVoiceInfo channel {0} player {1} voice #{2} userData {3}", channelId, playerId, voiceId, voiceInfo.UserData);
            }

            if (SpeakerFactory != null)
            {
                Speaker speaker = SpeakerFactory(playerId, voiceId, voiceInfo.UserData);
                if (speaker != null)
                {
                    speaker.OnRemoteVoiceInfo(voiceInfo, ref options);
                    if (speaker.Actor == null && this.Client.CurrentRoom != null)
                    {
                         speaker.Actor = this.Client.CurrentRoom.GetPlayer(playerId);
                    }
                    if (SpeakerLinked != null)
                    {
                        SpeakerLinked.Invoke(speaker);
                    }
                }
            }
        }

        private void OnOperationResponse(OperationResponse opResponse)
        {
            switch (opResponse.OperationCode)
            {
                case OperationCode.GetRegions:
                    if (Settings != null && Settings.IsBestRegion)
                    {
                        Client.RegionHandler.PingMinimumOfRegions(OnRegionsPinged, BestRegionSummaryInPreferences);
                    }
                    break;
            }
        }

        /// <summary>Used to store and access the "Best Region Summary" in the Player Preferences.</summary>
        internal string BestRegionSummaryInPreferences
        {
            get
            {
                if (cachedRegionHandler != null)
                {
                    BestRegionSummaryInPreferences = cachedRegionHandler.SummaryToCache;
                    return cachedRegionHandler.SummaryToCache;
                }
                return PlayerPrefs.GetString(PlayerPrefsKey, null);
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    PlayerPrefs.DeleteKey(PlayerPrefsKey);
                }
                else
                {
                    PlayerPrefs.SetString(PlayerPrefsKey, value);
                }
            }
        }

        private void OnRegionsPinged(RegionHandler regionHandler)
        {
            cachedRegionHandler = regionHandler;
            Client.ConnectToRegionMaster(regionHandler.BestRegion.Code);
        }

        protected override void OnApplicationQuit()
        {
            if (this.Logger.IsInfoEnabled)
            {
                this.Logger.LogInfo("VoiceConnection.OnApplicationQuit() Client exists: {0} this.GetType(): {1}", this.Client != null, this.GetType());
            }
            this.StopFallbackSendAckThread();
            if (this.Client != null)
            {
                this.Client.Disconnect();
                this.Client.LoadBalancingPeer.StopThread();
            }
            SupportClass.StopAllBackgroundCalls();
        }

        protected void CalcStatistics()
        {
            float now = Time.time;
            int recv = this.VoiceClient.FramesReceived - this.referenceFramesReceived;
            int lost = this.VoiceClient.FramesLost - this.referenceFramesLost;
            float t = now - statsReferenceTime;

            if (t != 0 && (recv + lost) > 0)
            {
                this.FramesReceivedPerSecond = recv / t;
                this.FramesLostPerSecond = lost / t;
                this.FramesLostPercent = 100 * lost / (recv + lost);
            }

            referenceFramesReceived = this.VoiceClient.FramesReceived;
            referenceFramesLost = this.VoiceClient.FramesLost;
            statsReferenceTime = now;
        }

        #endregion
    }
}