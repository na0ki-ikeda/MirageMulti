#if UNITY_IOS || (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#if (UNITY_IPHONE && !UNITY_EDITOR) || __IOS__
#define DLL_IMPORT_INTERNAL
#endif
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Voice = Photon.Voice;

namespace Photon.Voice.Apple
{
    public class MonoPInvokeCallbackAttribute : System.Attribute
    {
        private Type type;
        public MonoPInvokeCallbackAttribute(Type t) { type = t; }
    }

    public class AppleAudioInPusher : Voice.IAudioPusher<float>
    {
#if DLL_IMPORT_INTERNAL
		const string lib_name = "__Internal";
#else
        const string lib_name = "AudioIn";
#endif
        [DllImport(lib_name)]
        private static extern IntPtr Photon_Audio_In_CreatePusher(int instanceID, int deviceID, Action<int, IntPtr, int> pushCallback);
        [DllImport(lib_name)]
        private static extern void Photon_Audio_In_Destroy(IntPtr handler);

        private delegate void CallbackDelegate(int instanceID, IntPtr buf, int len);

        public AppleAudioInPusher(int deviceID)
        {
            this.deviceID = deviceID;
			if (handle != IntPtr.Zero)
			{
				Dispose();
			}
			handle = Photon_Audio_In_CreatePusher(instanceCnt, deviceID, nativePushCallback);
			instancePerHandle.Add(instanceCnt++, this);
        }

        private int deviceID;
        // IL2CPP does not support marshaling delegates that point to instance methods to native code.
        // Using static method and per instance table.
        static int instanceCnt;
        private static Dictionary<int, AppleAudioInPusher> instancePerHandle = new Dictionary<int, AppleAudioInPusher>();
        [MonoPInvokeCallbackAttribute(typeof(CallbackDelegate))]
        private static void nativePushCallback(int instanceID, IntPtr buf, int len)
        {
            AppleAudioInPusher instance;
            if (instancePerHandle.TryGetValue(instanceID, out instance))
            {
                instance.push(buf, len);
            }
        }

        IntPtr handle;
        Action<float[]> pushCallback;
        Voice.ObjectFactory<float[], int> bufferFactory;

        // Supposed to be called once at voice initialization.
        // Otherwise recreate native object (instead of adding 'set callback' method to native interface)
        public void SetCallback(Action<float[]> callback, Voice.ObjectFactory<float[], int> bufferFactory)
        {
            this.pushCallback = callback;
            this.bufferFactory = bufferFactory;
        }
        private void push(IntPtr buf, int len)
        {            
            var bufManaged = bufferFactory.New(len);
            Marshal.Copy(buf, bufManaged, 0, len);
            pushCallback(bufManaged);
        }

        public int Channels { get { return 1; } }

#if (UNITY_IPHONE && !UNITY_EDITOR) || __IOS__
        public int SamplingRate { get { return 48000; } }
#else
		public int SamplingRate { get { return 44100; } }
#endif

        public void Dispose()
        {
            Photon_Audio_In_Destroy(handle);
            // TODO: Remove this from instancePerHandle
        }
    }
}
#endif