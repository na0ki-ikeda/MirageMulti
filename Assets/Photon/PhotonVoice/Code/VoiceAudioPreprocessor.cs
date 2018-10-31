#if (UNITY_IOS && !UNITY_EDITOR) || __IOS__
#define DLL_IMPORT_INTERNAL
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Voice;

namespace Photon.Voice.Unity
{

    public class VoiceAudioPreprocessor : MonoBehaviour
    {
        [Header("Effects"), Tooltip("Echo Cancellation")]
        public bool AEC = false;
        [Tooltip("Reverse stream delay (hint for AEC)")]
        public int ReverseStreamDelayMs = 120;
        [Tooltip("Echo Cancellation Mobile")]
        public bool AECMobile = false; // echo control mobile
                                      //public bool AECMobileComfortNoise = false;
        [Tooltip("High Pass Filter")]
        public bool HighPass = false;
        [Tooltip("Noise Suppression")]
		public bool NoiseSuppression = true;
		[Tooltip("Automatic Gain Control")]
        public bool AGC = true;
        [Tooltip("Voice Auto Detection")]
        public bool VAD = true;
        [Tooltip("Bypass WebRTC audio")]
        public bool Bypass = false;

        private int reverseChannels;
        private Voice.WebRTCAudioProcessor proc;

        private void Awake()
        {
        }

        private void Start()
        {
        }

        private void setOutputListener(bool set)
        {
            var audioListener = FindObjectOfType<AudioListener>();
            if (audioListener != null)
            {
                var ac = audioListener.gameObject.GetComponent<Voice.Unity.AudioOutCapture>();
                if (ac != null)
                {
                    ac.OnAudioFrame -= OnAudioOutFrameFloat;
                }
                if (set)
                {
                    if (ac == null)
                    {
                        ac = audioListener.gameObject.AddComponent<Voice.Unity.AudioOutCapture>();
                    }

                    ac.OnAudioFrame += OnAudioOutFrameFloat;
                }
            }
        }

        private void OnAudioOutFrameFloat(float[] data, int outChannels)
        {
            if (outChannels != this.reverseChannels)
            {
                Debug.LogErrorFormat("WebRTCAudioProcessor AEC: OnAudioOutFrame channel count {0} != intialized {1}.", outChannels, this.reverseChannels);
                return;
            }
            proc.OnAudioOutFrameFloat(data);
        }

        bool prevAEC;
        bool prevAECMobile;
        private void Update()
        {
            if (proc != null)
            {
                if (AEC && AEC != prevAEC)
                {
                    AECMobile = false;
                }
                if (AECMobile && AECMobile != prevAECMobile)
                {
                    AEC = false;
                }
                prevAEC = AEC;
                prevAECMobile = AECMobile;
                proc.AEC = AEC;
                proc.AECMobile = AECMobile;
                setOutputListener(AEC || AECMobile);

                proc.AECMRoutingMode = 4;
                proc.AECStreamDelayMs = ReverseStreamDelayMs;
                //proc.AECMComfortNoise = AECMobileComfortNoise;
                proc.HighPass = HighPass;
                proc.NoiseSuppression = NoiseSuppression;
                proc.AGC = AGC;
                proc.VAD = VAD;
                proc.Bypass = Bypass;
            }
        }

        // Message sent by PhotonVoiceRecorder
        void PhotonVoiceCreated(Recorder.PhotonVoiceCreatedParams p)
        {
            var localVoice = p.Voice;

            if (localVoice.Info.Channels != 1)
            {
                throw new Exception("WebRTCAudioProcessor: only mono audio signals supported.");
            }
            if (!(localVoice is Voice.LocalVoiceAudioShort))
            {
                throw new Exception("WebRTCAudioProcessor: only short audio voice supported (Set PhotonVoiceRecorder.TypeConvert option).");
            }
            var v = (Voice.LocalVoiceAudioShort)localVoice;

            // can't access the AudioSettings properties in InitAEC if it's called from not main thread
            this.reverseChannels = new Dictionary<AudioSpeakerMode, int>() {
            {AudioSpeakerMode.Raw, 0},
            {AudioSpeakerMode.Mono, 1},
            {AudioSpeakerMode.Stereo, 2},
            {AudioSpeakerMode.Quad, 4},
            {AudioSpeakerMode.Surround, 5},
            {AudioSpeakerMode.Mode5point1, 6},
            {AudioSpeakerMode.Mode7point1, 8},
            {AudioSpeakerMode.Prologic, 0},
        }[AudioSettings.speakerMode];
            int playBufSize;
            int playBufNum;
            AudioSettings.GetDSPBufferSize(out playBufSize, out playBufNum);
            proc = new Voice.WebRTCAudioProcessor(new Voice.Unity.Logger(), localVoice.Info.FrameSize, localVoice.Info.SamplingRate, localVoice.Info.Channels, AudioSettings.outputSampleRate, this.reverseChannels);
            v.AddPostProcessor(proc);
            Debug.Log("WebRTCAudioDSP initialized.");
        }

        void PhotonVoiceRemoved()
        {
            reset();
        }

        private void OnDestroy()
        {
            reset();
        }

        private void reset()
        {
            if (proc != null)
            {
                setOutputListener(false);
                proc.Dispose();
                proc = null;
            }
        }
    }
}