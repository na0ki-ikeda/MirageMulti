namespace Photon.Voice.Unity.Editor
{
    using UnityEngine;
    using UnityEditor;
    using Unity;

    [CustomEditor(typeof(VoiceConnection))]
    public class VoiceConnectionEditor : Editor
    {
        private VoiceConnection connection;

        private SerializedProperty updateIntervalSp;
        private SerializedProperty enableSupportLoggerSp;
        #if PHOTON_UNITY_NETWORKING
        private SerializedProperty usePunSettingsSp;
        #endif
        private SerializedProperty settingsSp;
        #if !UNITY_ANDROID && !UNITY_IOS
        private SerializedProperty runInBackground;
        #endif
        private SerializedProperty keepAliveInBackgroundSp;
        private SerializedProperty applyDontDestroyOnLoadSp;
        private SerializedProperty primaryRecorderSp;
        private SerializedProperty statsResetInterval;

        private bool showSettings;

        protected virtual void OnEnable()
        {
            connection = target as VoiceConnection;
            updateIntervalSp = serializedObject.FindProperty("updateInterval");
            enableSupportLoggerSp = serializedObject.FindProperty("enableSupportLogger");
            #if PHOTON_UNITY_NETWORKING
            usePunSettingsSp = serializedObject.FindProperty("usePunSettings");
            #endif
            settingsSp = serializedObject.FindProperty("Settings");
            #if !UNITY_ANDROID && !UNITY_IOS
            runInBackground = serializedObject.FindProperty("runInBackground");
            #endif
            keepAliveInBackgroundSp = serializedObject.FindProperty("KeepAliveInBackground");
            applyDontDestroyOnLoadSp = serializedObject.FindProperty("ApplyDontDestroyOnLoad");
            primaryRecorderSp = serializedObject.FindProperty("PrimaryRecorder");
            statsResetInterval = serializedObject.FindProperty("statsResetInterval");
        }

        public override void OnInspectorGUI()
        {
            // Show default inspector property editor
            VoiceLogger.ExposeLogLevel(serializedObject, connection);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(updateIntervalSp, new GUIContent("Update Interval (ms)", "time [ms] between consecutive SendOutgoingCommands calls"));
            EditorGUILayout.PropertyField(enableSupportLoggerSp);
            #if !UNITY_ANDROID && !UNITY_IOS
            EditorGUILayout.PropertyField(runInBackground, new GUIContent("Run In Background", "Sets Unity's Application.runInBackground: Should the application keep running when the application is in the background?"));
            #endif
            #if !UNITY_IOS
            EditorGUILayout.PropertyField(keepAliveInBackgroundSp, new GUIContent("Background Timeout (ms)", "Defines for how long the Fallback Thread should keep the connection, before it may time out as usual."));
            #endif
            EditorGUILayout.PropertyField(applyDontDestroyOnLoadSp, new GUIContent("Don't Destroy On Load", "Persists the GameObject across scenes using Unity's GameObject.DontDestoryOnLoad"));
            EditorGUILayout.PropertyField(primaryRecorderSp, new GUIContent("Primary Recorder", "Main Recorder to be used for transmission by default"));
            connection.SpeakerPrefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Speaker Prefab",
                    "Prefab that contains Speaker component to be instantiated when receiving a new remote audio source info"), connection.SpeakerPrefab, 
                typeof(GameObject), false);
            #if PHOTON_UNITY_NETWORKING
            EditorGUILayout.PropertyField(usePunSettingsSp);
            showSettings = !usePunSettingsSp.boolValue && EditorGUILayout.Foldout(showSettings, new GUIContent("Settings", "Settings to be used by this voice connection"));
            #else
            showSettings = EditorGUILayout.Foldout(showSettings, new GUIContent("Settings", "Settings to be used by this voice connection"));
            #endif
            if (showSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(settingsSp.FindPropertyRelative("AppIdVoice"));
                EditorGUILayout.PropertyField(settingsSp.FindPropertyRelative("AppVersion"));
                EditorGUILayout.PropertyField(settingsSp.FindPropertyRelative("UseNameServer"));
                EditorGUILayout.PropertyField(settingsSp.FindPropertyRelative("FixedRegion"));
                EditorGUILayout.PropertyField(settingsSp.FindPropertyRelative("Server"));
                EditorGUILayout.PropertyField(settingsSp.FindPropertyRelative("Port"));
                EditorGUILayout.PropertyField(settingsSp.FindPropertyRelative("Protocol"));
                EditorGUILayout.PropertyField(settingsSp.FindPropertyRelative("EnableLobbyStatistics"));
                EditorGUILayout.PropertyField(settingsSp.FindPropertyRelative("NetworkLogging"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(statsResetInterval, new GUIContent("Stats Reset Interval (ms)", "time [ms] between statistics calculations"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField(string.Format("Frames Received /s: {0}", connection.FramesReceivedPerSecond));
                EditorGUILayout.LabelField(string.Format("Frames Lost /s: {0}", connection.FramesLostPerSecond));
                EditorGUILayout.LabelField(string.Format("Frames Lost %: {0}", connection.FramesLostPercent));
            }
        }
    }
}