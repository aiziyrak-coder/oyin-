using System;
using UnityEngine;
namespace CraDev
{
    public static class VoicePreferences
    {
        public static string SelectedDevice { get => PlayerPrefs.GetString("cradev.voice.device", ""); set { PlayerPrefs.SetString("cradev.voice.device", value ?? ""); PlayerPrefs.Save(); } }
        public static bool PushToTalk { get => PlayerPrefs.GetInt("cradev.voice.ptt", 0) != 0; set { PlayerPrefs.SetInt("cradev.voice.ptt", value ? 1 : 0); PlayerPrefs.Save(); } }
        public static float Volume { get => Mathf.Clamp01(PlayerPrefs.GetFloat("cradev.voice.volume", 1)); set { PlayerPrefs.SetFloat("cradev.voice.volume", Mathf.Clamp01(value)); PlayerPrefs.Save(); } }
        public static string Device
        {
            get { var devices=Microphone.devices; return devices==null||devices.Length==0?null:string.IsNullOrEmpty(SelectedDevice)?devices[0]:Array.IndexOf(devices,SelectedDevice)>=0?SelectedDevice:null; }
        }
        public static string Label => string.IsNullOrEmpty(SelectedDevice)?Loc.T("voice.device_default"):SelectedDevice;
        public static void Step(int direction)
        {
            var devices=Microphone.devices??Array.Empty<string>();
            int index=string.IsNullOrEmpty(SelectedDevice)?0:Array.IndexOf(devices,SelectedDevice)+1;
            index=(index+direction+devices.Length+1)%(devices.Length+1);
            SelectedDevice=index==0?"":devices[index-1];
        }
        public static void Reset() { SelectedDevice="";PushToTalk=false;Volume=1; }
    }
}
