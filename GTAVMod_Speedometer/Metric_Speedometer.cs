/*
 * Simple Metric/Imperial Speedometer
 * Author: libertylocked
 * Version: 1.30.3a
 */
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using GTA;
using GTA.Native;

namespace GTAVMod_Speedometer
{
    public class Metric_Speedometer : Script
    {
        UIContainer speedContainer;
        UIText speedText;
        UIContainer odometerContainer;
        UIText odometerText;
        int speedoMode = 1; // 0 off, 1 simple, 2 detailed
        float distanceKm = 0;

        ScriptSettings settings;
        bool toggleable;
        Keys toggleKey;
        bool resettable;
        bool saveStats;
        Keys resetKey; // odometer reset key
        bool useMph;

        public Metric_Speedometer()
        {
            ParseSettings();
            this.Tick += OnTick;
            if (this.toggleable) this.KeyDown += OnKeyDown;
        }

        void OnTick(object sender, EventArgs e)
        {
            if (saveStats)
            {
                bool isPausePressed = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 2, 199) ||
                    Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 2, 200); // pause or pause alternate button
                if (isPausePressed) SaveStats();
            }

            Player player = Game.Player;
            if (player != null && player.CanControlCharacter && player.IsAlive 
                && player.Character != null && player.Character.IsInVehicle())
            {
                Vehicle vehicle = player.Character.CurrentVehicle;
                float speedKph = vehicle.Speed * 3600 / 1000;   // convert from m/s to km/h
                float distanceLastFrame = vehicle.Speed * Game.LastFrameTime / 1000; // increment odometer counter
                distanceKm += distanceLastFrame;
                
                if (useMph)
                {
                    float speedMph = KmToMiles(speedKph);
                    float distanceMiles = KmToMiles(distanceKm);
                    speedText.Caption = Math.Floor(speedMph).ToString("0") + " mph"; // floor speed mph
                    if (speedoMode == 2)
                    {
                        double truncated = Math.Floor(distanceMiles * 10) / 10.0;
                        odometerText.Caption = truncated.ToString("0.0") + " mi";
                    }
                }
                else
                {
                    speedText.Caption = Math.Floor(speedKph).ToString("0") + " km/h"; // floor speed km/h
                    if (speedoMode == 2)
                    {
                        double truncated = Math.Floor(distanceKm * 10) / 10.0;
                        odometerText.Caption = truncated.ToString("0.0") + " km";
                    }
                }

                if (speedoMode != 0) speedContainer.Draw();
                if (speedoMode == 2) // draw these widgets in detailed mode only
                {
                    odometerContainer.Draw();
                }
            }
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (toggleable && e.KeyCode == toggleKey)
            {
                ++speedoMode;
                speedoMode %= 3;
                //UI.Notify("Speedometer Mode " + speedoMode);
            }
            if (resettable && speedoMode == 2 && e.KeyCode == resetKey)
            {
                distanceKm = 0;
            }
            //if (saveStats && e.KeyCode == Keys.Escape)
            //{
            //    SaveStats();
            //}
        }

        void ParseSettings()
        {
            try
            {
                settings = ScriptSettings.Load(@".\scripts\Metric_Speedometer.ini");

                // Parse Core settings
                this.useMph = settings.GetValue("Core", "UseMph", false);
                this.toggleable = settings.GetValue("Core", "Toggleable", false);
                if (toggleable)
                    this.toggleKey = (Keys)Enum.Parse(typeof(Keys), settings.GetValue("Core", "ToggleKey"), true);
                this.resettable = settings.GetValue("Core", "Resettable", false);
                if (resettable)
                    this.resetKey = (Keys)Enum.Parse(typeof(Keys), settings.GetValue("Core", "ResetKey"), true);
                this.saveStats = settings.GetValue("Core", "SaveStats", false);

                // Parse UI settings
                VerticalAlignment vAlign = (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), settings.GetValue("UI", "VertAlign"), true);
                HorizontalAlign hAlign = (HorizontalAlign)Enum.Parse(typeof(HorizontalAlign), settings.GetValue("UI", "HorzAlign"), true);
                Point posOffset = new Point(settings.GetValue<int>("UI", "OffsetX", 0), settings.GetValue<int>("UI", "OffsetY", 0));
                int pWidth = settings.GetValue("UI", "PanelWidth", 66);
                int pHeight = settings.GetValue("UI", "PanelHeight", 24);
                float fontSize = float.Parse(settings.GetValue("UI", "FontSize"), CultureInfo.InvariantCulture.NumberFormat);
                int fontStyle = settings.GetValue("UI", "FontStyle", 4);
                Color backcolor = Color.FromArgb(settings.GetValue<int>("UI", "BackcolorA", 200), settings.GetValue<int>("UI", "BackcolorR", 237),
                    settings.GetValue<int>("UI", "BackcolorG", 239), settings.GetValue<int>("UI", "BackcolorB", 241));
                Color forecolor = Color.FromArgb(settings.GetValue<int>("UI", "ForecolorA", 255), settings.GetValue<int>("UI", "ForecolorR", 0),
                    settings.GetValue<int>("UI", "ForecolorG", 0), settings.GetValue<int>("UI", "ForecolorB", 0));

                // Load stats
                if (saveStats) LoadStats();

                // Set up UI elements
                Point pos = new Point(posOffset.X, posOffset.Y);
                Point odometerPos = new Point(0, 0);
                switch (vAlign)
                {
                    case VerticalAlignment.Top:
                        pos.Y += 0;
                        odometerPos.Y += pHeight; // below speed counter
                        break;
                    case VerticalAlignment.Center:
                        pos.Y += UI.HEIGHT / 2 - pHeight / 2;
                        odometerPos.Y += pHeight; // below speed counter
                        break;
                    case VerticalAlignment.Bottom:
                        pos.Y += UI.HEIGHT - pHeight;
                        odometerPos.Y -= pHeight; // above speed counter
                        break;
                }
                switch (hAlign)
                {
                    case HorizontalAlign.Left:
                        pos.X += 0;
                        break;
                    case HorizontalAlign.Center:
                        pos.X += UI.WIDTH / 2 - pWidth / 2;
                        break;
                    case HorizontalAlign.Right:
                        pos.X += UI.WIDTH - pWidth;
                        break;
                }
                odometerPos.X += pos.X;
                odometerPos.Y += pos.Y;

                this.speedContainer = new UIContainer(pos, new Size(pWidth, pHeight), backcolor);
                this.speedText = new UIText(String.Empty, new Point(pWidth / 2, 0), fontSize, forecolor, fontStyle, true);
                this.speedContainer.Items.Add(speedText);
                this.odometerContainer = new UIContainer(odometerPos, new Size(pWidth, pHeight), backcolor);
                this.odometerText = new UIText(String.Empty, new Point(pWidth / 2, 0), fontSize, forecolor, fontStyle, true);
                this.odometerContainer.Items.Add(odometerText);
            }
            catch (Exception exc)
            {
                Wait(10000);
                UI.ShowSubtitle(exc.ToString(), 10000);
                this.Abort();
            }
        }

        void LoadStats()
        {
            try
            {
                using (StreamReader sr = new StreamReader(@".\scripts\Metric_Speedometer_Stats.txt"))
                {
                    bool distanceParsed = float.TryParse(sr.ReadLine(), out distanceKm);
                }
            }
            catch { }
        }

        void SaveStats()
        {
            try
            {
                Thread thread = new Thread(DoSaveStats);
                thread.Start();
            }
            catch { }
        }

        void DoSaveStats()
        {
            using (StreamWriter sw = new StreamWriter((@".\scripts\Metric_Speedometer_Stats.txt"), false))
            {
                sw.WriteLine(distanceKm);
            }
        }

        float KmToMiles(float km)
        {
            return km * 0.6213711916666667f;
        }
    }
}
