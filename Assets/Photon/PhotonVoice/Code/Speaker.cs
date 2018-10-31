// ----------------------------------------------------------------------------
// <copyright file="Speaker.cs" company="Exit Games GmbH">
//   Photon Voice for Unity - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
// Component representing remote audio stream in local scene.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------


using System;
using UnityEngine;


namespace Photon.Voice.Unity
{
    /// <summary> Component representing remote audio stream in local scene. </summary>
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("Photon Voice/Speaker")]
    public class Speaker : VoiceComponent
    {
        #region Private Fields

        private IAudioOut audioOutput;

        #endregion

        #region Public Fields

        ///<summary>Remote audio stream playback delay to compensate packets latency variations. Try 100 - 200 if sound is choppy.</summary> 
        public int PlayDelayMs = 200;

        #if UNITY_PS4
        /// <summary>Set the PS4 User ID to determine on which controller to play audio.</summary> 
        /// <remarks>
        /// Note: at the moment, only the first Speaker can successfully set the User ID. 
        /// Subsequently initialized Speakers will play their audio on the controller set with the first Speaker initialized.
        /// </remarks>
        public int PS4UserID = 0;
        #endif

        #endregion

        #region Properties

        /// <summary>Is the speaker playing right now.</summary>
        public bool IsPlaying
        {
            get { return this.audioOutput != null && this.audioOutput.IsPlaying; }
        }

        /// <summary>Smoothed difference between (jittering) stream and (clock-driven) audioOutput.</summary>
        public int Lag
        {
            get { return this.audioOutput != null ? this.audioOutput.Lag : -1; }
        }

        /// <summary>
        /// Register a method to be called when remote voice removed.
        /// </summary>
        public Action<Speaker> OnRemoteVoiceRemoveAction { get; set; }

        /// <summary>Per room, the connected users/players are represented with a Realtime.Player, also known as Actor.</summary>
        /// <remarks>Photon Voice calls this Actor, to avoid a name-clash with the Player class in Voice.</remarks>
        public Realtime.Player Actor { get; protected internal set; }

        #endregion

        #region Private Methods

        protected override void Awake()
        {
            base.Awake();
            Func<IAudioOut> factory = () => new AudioStreamPlayer(new VoiceLogger(this, "AudioStreamPlayer", this.LogLevel),  
                new UnityAudioOut(this.GetComponent<AudioSource>()), "PhotonVoiceSpeaker:", this.Logger.IsInfoEnabled);

            #if !UNITY_EDITOR && UNITY_PS4
            this.audioOutput = new Photon.Voice.PS4.PS4AudioOut(PS4UserID, factory);
            #else
            this.audioOutput = factory();
            #endif
        }

        internal void OnRemoteVoiceInfo(VoiceInfo voiceInfo, ref RemoteVoiceOptions options)
        {
            options.OnDecodedFrameFloatAction += this.OnAudioFrame;
            options.OnRemoteVoiceRemoveAction += this.OnRemoteVoiceRemove;

            this.audioOutput.Start(voiceInfo.SamplingRate, voiceInfo.Channels, voiceInfo.FrameDurationSamples, this.PlayDelayMs);
        }

        internal void OnRemoteVoiceRemove()
        {
            if (this.audioOutput != null) this.audioOutput.Stop();
            this.Actor = null;
            if (this.OnRemoteVoiceRemoveAction != null) this.OnRemoteVoiceRemoveAction(this);
        }

        internal void OnAudioFrame(float[] frame)
        {
            this.audioOutput.Push(frame);
        }

        private void Update()
        {
            this.audioOutput.Service();
        }

        #endregion
    }
}