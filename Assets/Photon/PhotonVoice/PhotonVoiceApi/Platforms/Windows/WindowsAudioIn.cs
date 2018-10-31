#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;

namespace Photon.Voice.Windows
{
    public class WindowsAudioInPusher : Voice.IAudioPusher<short>
    {
        enum SystemMode
        {
            SINGLE_CHANNEL_AEC = 0,
            OPTIBEAM_ARRAY_ONLY = 2,
            OPTIBEAM_ARRAY_AND_AEC = 4,
            SINGLE_CHANNEL_NSAGC = 5,
        }

        [DllImport("AudioIn")]
        private static extern IntPtr Photon_Audio_In_Create(SystemMode systemMode, int micDevIdx, int spkDevIdx, Action<IntPtr, int> callback, bool featrModeOn, bool noiseSup, bool agc, bool cntrClip);

        [DllImport("AudioIn")]
        private static extern void Photon_Audio_In_Destroy(IntPtr handler);

        IntPtr handle;
        Action<short[]> pushCallback;
        ObjectFactory<short[], int> bufferFactory;
        int deviceID;

        public WindowsAudioInPusher(int deviceID)
        {
            this.deviceID = deviceID;
        }
        // Supposed to be called once at voice initialization.
        // Otherwise recreate native object (instead of adding 'set callback' method to native interface)
        public void SetCallback(Action<short[]> callback, ObjectFactory<short[], int> bufferFactory)
        {
            if (handle != IntPtr.Zero)
            {
                Dispose();
            }

            this.pushCallback = callback;
            this.bufferFactory = bufferFactory;
            pushRef = push;
            // use default playback device
            handle = Photon_Audio_In_Create(SystemMode.SINGLE_CHANNEL_AEC, deviceID, -1, pushRef, true, true, true, true); // defaults in original ms sample: false, true, false, false
        }
        Action<IntPtr, int> pushRef;
        private void push(IntPtr buf, int lenBytes)
        {
            var len = lenBytes / sizeof(short);
            var bufManaged = this.bufferFactory.New(len);
            Marshal.Copy(buf, bufManaged, 0, len);
            pushCallback(bufManaged);
        }

        public int Channels { get { return 1; } }


        public int SamplingRate { get { return 16000; } }


        public void Dispose()
        {
            Photon_Audio_In_Destroy(handle);
            // TODO: Remove this from instancePerHandle
        }
    }
}
#endif