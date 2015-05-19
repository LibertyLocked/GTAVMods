/*
 * Internet Radio
 * Author: libertylocked
 * Version: 0.1a
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;

namespace InternetRadio
{
    public class InternetRadio : Script
    {
        WMPLib.WindowsMediaPlayer player = new WMPLib.WindowsMediaPlayer();
        bool isEnabled = false;
        bool playerWasInVeh = false;

        Keys toggleKey;
        string streamUrl;
        int volume;

        public InternetRadio()
        {
            ParseSettings();
            this.Tick += OnTick;
            this.KeyDown += OnKeyDown;
        }

        void OnTick(object sender, EventArgs e)
        {
            Player player = Game.Player;
            if (player != null && player.CanControlCharacter && player.IsAlive && player.Character != null
                && player.Character.IsInVehicle())
            {
                // disable/enable vehicle radio if internet radio is enabled/disabled
                Function.Call(Hash.SET_VEHICLE_RADIO_ENABLED, player.Character.CurrentVehicle, !isEnabled);
            }

            //if (player != null && player.Character != null)
            //{
            //    playerWasInVeh = player.Character.IsInVehicle();
            //}
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == toggleKey)
            {
                isEnabled = !isEnabled;
                if (isEnabled)
                {
                    PlayFromURL(streamUrl);
                    UI.ShowSubtitle("Internet Radio ~r~On" + " ~w~" + streamUrl, 5000);
                }
                else
                {
                    PlayerStop();
                    UI.ShowSubtitle("Internet Radio ~r~Off", 5000);
                }
            }
        }

        void ParseSettings()
        {
            using (Stream setupIn = File.Open(@".\scripts\InternetRadio.ini", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Structs.SetupFile settings = new Structs.SetupFile(setupIn);

                this.toggleKey = settings.GetEntryByName("Core", "ToggleKey").KeyValue;
                this.streamUrl = settings.GetDataByName("Core", "StreamUrl");
                this.volume = settings.GetEntryByName("Core", "Volume").Intvalue;
            }
        }

        void PlayFromURL(string url)
        {
            try
            {
                player.URL = url;
                player.settings.volume = volume;
                player.controls.play();
            }
            catch { }
        }

        void PlayerStop()
        {
            try
            {
                player.controls.stop();
            }
            catch { }
        }

        void SetPlayerVolume(int volume)
        {
            try
            {
                player.settings.volume = volume;
            }
            catch { }
        }
    }
}
