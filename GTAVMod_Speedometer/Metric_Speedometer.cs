/*
 * Simple Metric/Imperial Speedometer
 * Author: libertylocked
 * Version: 1.2
 */
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using GTA;

namespace GTAVMod_Speedometer
{
    public class Metric_Speedometer : Script
    {
        UIContainer hudContainer;
        UIText speedText;
        bool showSpeedo = true;

        bool toggleable;
        Keys toggleKey;
        bool useMph;

        public Metric_Speedometer()
        {
            ParseSettings();
            this.Tick += OnTick;
            if (this.toggleable) this.KeyDown += OnKeyDown;
        }

        void OnTick(object sender, EventArgs e)
        {
            Player player = Game.Player;
            if (showSpeedo && player != null && player.CanControlCharacter && player.IsAlive 
                && player.Character != null && player.Character.IsInVehicle())
            {
                Vehicle vehicle = player.Character.CurrentVehicle;
                float speedKph = vehicle.Speed * 3600 / 1000;   // convert from m/s to km/h
                if (useMph)
                {
                    float speedMph = speedKph * 0.6213711916666667f; // convert km/h to mph
                    speedText.Text = speedMph.ToString("0") + " mph";
                }
                else
                {
                    speedText.Text = speedKph.ToString("0") + " km/h";
                }

                hudContainer.Draw();
            }
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (toggleable && e.KeyCode == toggleKey)
            {
                showSpeedo = !showSpeedo;
                //UI.Notify("Speedometer " + (showSpeedo ? "On" : "Off"));
            }
        }

        void ParseSettings()
        {
            try
            {
                ScriptSettings settings = ScriptSettings.Load(@".\scripts\Metric_Speedometer.ini");

                // Parse Core settings
                this.useMph = settings.GetValue("Core", "UseMph", false);
                this.toggleable = settings.GetValue("Core", "Toggleable", false);
                if (toggleable)
                    this.toggleKey = (Keys)Enum.Parse(typeof(Keys), settings.GetValue("Core", "ToggleKey"), true);

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

                // Set up UI elements
                Point pos = new Point(0, 0);
                switch (vAlign)
                {
                    case VerticalAlignment.Top:
                        pos.Y = 0;
                        break;
                    case VerticalAlignment.Center:
                        pos.Y = UI.HEIGHT / 2 - pHeight / 2;
                        break;
                    case VerticalAlignment.Bottom:
                        pos.Y = UI.HEIGHT - pHeight;
                        break;
                }
                switch (hAlign)
                {
                    case HorizontalAlign.Left:
                        pos.X = 0;
                        break;
                    case HorizontalAlign.Center:
                        pos.X = UI.WIDTH / 2 - pWidth / 2;
                        break;
                    case HorizontalAlign.Right:
                        pos.X = UI.WIDTH - pWidth;
                        break;
                }
                pos.Y += posOffset.Y;
                pos.X += posOffset.X;

                this.hudContainer = new UIContainer(pos, new Size(pWidth, pHeight), backcolor);
                this.speedText = new UIText(String.Empty, new Point(pWidth / 2, 0), fontSize, forecolor, fontStyle, true);
                this.hudContainer.Items.Add(speedText);
            }
            catch (Exception exc)
            {
                Wait(10000);
                UI.ShowSubtitle(exc.ToString(), 10000);
                this.Abort();
            }
        }
    }
}
