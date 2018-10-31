using System.Collections.Generic;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.UI;


namespace Photon.Voice.DemoVoiceUI
{
    public struct MicRef
    {
        public Recorder.MicType type;
        public string name;
        public int id;

        public MicRef(Recorder.MicType _type, string _name, int _id)
        {
            this.type = _type;
            this.name = _name;
            this.id = _id;
        }

        public override string ToString()
        {
            return "Mic reference: " + this.name;
        }
    }


    public class MicrophoneDropdownFiller : MonoBehaviour
    {
        public Recorder recorder;

        public Dropdown micDropdown;

        List<MicRef> micOptions;
        
        [SerializeField]
        private GameObject RefreshButton;

        [SerializeField]
        private GameObject ToggleButton;

        void Start()
        {
            this.RefreshMicrophones();
        }

        void SetupMicDropdown()
        {
            this.micDropdown.ClearOptions();

            this.micOptions = new List<MicRef>();
            List<string> micOptionsStrings = new List<string>();


            foreach (string x in Microphone.devices)
            {
                this.micOptions.Add(new MicRef(Recorder.MicType.Unity, x, -1));
                micOptionsStrings.Add("[Unity] " + x);
            }

            if (Recorder.PhotonMicrophoneEnumerator.IsSupported)
            {
                this.RefreshButton.SetActive(true);
                this.ToggleButton.SetActive(false);
                for (int i = 0; i < Recorder.PhotonMicrophoneEnumerator.Count; i++)
                {
                    string name = Recorder.PhotonMicrophoneEnumerator.NameAtIndex(i);
                    this.micOptions.Add(new MicRef(Recorder.MicType.Photon, name, Recorder.PhotonMicrophoneEnumerator.IDAtIndex(i)));
                    micOptionsStrings.Add("[Photon] " + name);
                }
            }
            else
            {
                this.ToggleButton.SetActive(true);
                this.RefreshButton.SetActive(!this.ToggleButton.GetComponentInChildren<Toggle>().isOn);
            }

            this.micDropdown.AddOptions(micOptionsStrings);
            this.micDropdown.onValueChanged.AddListener(delegate { this.MicDropdownValueChanged(this.micOptions[this.micDropdown.value]); });
        }

        void MicDropdownValueChanged(MicRef mic)
        {
            this.recorder.MicrophoneType = mic.type;

            switch (mic.type)
            {
                case Recorder.MicType.Unity:
                    this.recorder.UnityMicrophoneDevice = mic.name;
                    break;
                case Recorder.MicType.Photon:
                    this.recorder.PhotonMicrophoneDeviceId = mic.id;
                    break;
            }

            if (this.recorder.IsInitialized && this.recorder.RequiresInit)
            {
                this.recorder.ReInit();
            }
        }

        public void PhotonMicToggled(bool on)
        {
            this.micDropdown.gameObject.SetActive(!on);
            this.RefreshButton.SetActive(!on);
            if (on)
            {
                this.recorder.MicrophoneType = Recorder.MicType.Photon;
                this.recorder.ReInit();
            }
            else
            {
                this.RefreshMicrophones();
                this.MicDropdownValueChanged(this.micOptions[this.micDropdown.value]);
            }
        }

        public void RefreshMicrophones()
        {
            Debug.Log("Refresh Mics");
            Recorder.PhotonMicrophoneEnumerator.Refresh();
            this.SetupMicDropdown();
        }
    }
}