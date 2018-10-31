namespace Photon.Voice.Unity.Editor
{
    using UnityEngine;
    using UnityEditor;
    using Unity;

    [CustomEditor(typeof(VoiceAudioPreprocessor))]
    public class VoiceAudioPreprocessorEditor : Editor
    {
        private VoiceAudioPreprocessor processor;
        private Recorder recorder;

        private void OnEnable()
        {
            processor = target as VoiceAudioPreprocessor;
            recorder = processor.GetComponent<Recorder>();
        }

        public override void OnInspectorGUI()
        {
            if (processor.VAD && recorder.VoiceDetection) {
                EditorGUILayout.HelpBox("You have enabled VAD here and in the associated Recorder. Please use only one Voice Detection algorithm.", MessageType.Warning);
            }

            if ((processor.AEC || processor.AECMobile) && recorder.MicrophoneType == Recorder.MicType.Photon) {
                EditorGUILayout.HelpBox("You have enabled AEC here and are using a Photon Mic as input on the Recorder, which might add its own echo cancellation. Please use only one AEC algorithm.", MessageType.Warning);
            }

            DrawDefaultInspector();
        }
    }
}
