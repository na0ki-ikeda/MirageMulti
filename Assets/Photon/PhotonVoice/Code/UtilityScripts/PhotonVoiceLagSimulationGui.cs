// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PhotonVoiceLagSimulationGui.cs" company="Exit Games GmbH">
//   Part of: Photon Voice Utilities for Unity - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
// This MonoBehaviour is a basic GUI for the Photon Voice client's network-simulation feature.
// It can modify lag (fixed delay), jitter (random lag), packet loss and audio frames percentage loss.
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections;
using System.Collections.Generic;

namespace Photon.Voice.Unity.UtilityScripts
{
    [RequireComponent(typeof(VoiceConnection))]
    public class PhotonVoiceLagSimulationGui : MonoBehaviour
    {
        private VoiceConnection voiceConnection;

        /// <summary>Positioning rect for window.</summary>
        public Rect WindowRect = new Rect(0, 100, 120, 100);

        /// <summary>Unity GUI Window ID (must be unique or will cause issues).</summary>
        public int WindowId = 101;

        /// <summary>Shows or hides GUI (does not affect settings).</summary>
        public bool Visible = true;

        /// <summary>The peer currently in use (to set the network simulation).</summary>
        public PhotonPeer Peer { get; set; }

        public void Start()
        {
            this.voiceConnection = this.GetComponent<VoiceConnection>();
            this.Peer = voiceConnection.Client.LoadBalancingPeer;
        }

        public void OnGUI()
        {
            if (!this.Visible)
            {
                return;
            }

            if (this.Peer == null)
            {
                this.WindowRect = GUILayout.Window(this.WindowId, this.WindowRect, this.NetSimHasNoPeerWindow,
                    "Voice Netw. Sim.");
            }
            else
            {
                this.WindowRect = GUILayout.Window(this.WindowId, this.WindowRect, this.NetSimWindow, "Voice Netw. Sim.");
            }
        }

        private void NetSimHasNoPeerWindow(int windowId)
        {
            GUILayout.Label("No voice peer to communicate with. ");
        }

        private void NetSimWindow(int windowId)
        {
            GUILayout.Label(string.Format("Rtt:{0,4} +/-{1,3}", this.Peer.RoundTripTime,
                this.Peer.RoundTripTimeVariance));

            bool simEnabled = this.Peer.IsSimulationEnabled;
            bool newSimEnabled = GUILayout.Toggle(simEnabled, "Simulate");
            if (newSimEnabled != simEnabled)
            {
                this.Peer.IsSimulationEnabled = newSimEnabled;
            }

            float inOutLag = this.Peer.NetworkSimulationSettings.IncomingLag;
            GUILayout.Label("Lag " + inOutLag);
            inOutLag = GUILayout.HorizontalSlider(inOutLag, 0, 500);

            this.Peer.NetworkSimulationSettings.IncomingLag = (int)inOutLag;
            this.Peer.NetworkSimulationSettings.OutgoingLag = (int)inOutLag;

            float inOutJitter = this.Peer.NetworkSimulationSettings.IncomingJitter;
            GUILayout.Label("Jit " + inOutJitter);
            inOutJitter = GUILayout.HorizontalSlider(inOutJitter, 0, 100);

            this.Peer.NetworkSimulationSettings.IncomingJitter = (int)inOutJitter;
            this.Peer.NetworkSimulationSettings.OutgoingJitter = (int)inOutJitter;

            float loss = this.Peer.NetworkSimulationSettings.IncomingLossPercentage;
            GUILayout.Label("Loss " + loss);
            loss = GUILayout.HorizontalSlider(loss, 0, 10);

            this.Peer.NetworkSimulationSettings.IncomingLossPercentage = (int)loss;
            this.Peer.NetworkSimulationSettings.OutgoingLossPercentage = (int)loss;

            float debugLostPercent = this.voiceConnection.VoiceClient.DebugLostPercent;
            GUILayout.Label(string.Format("Lost Audio Frames {0}%", (int)debugLostPercent));
            debugLostPercent = GUILayout.HorizontalSlider(debugLostPercent, 0, 100);
            this.voiceConnection.VoiceClient.DebugLostPercent = (int)debugLostPercent;

            // if anything was clicked, the height of this window is likely changed. reduce it to be layouted again next frame
            if (GUI.changed)
            {
                this.WindowRect.height = 100;
            }

            GUI.DragWindow();
        }
    }
}