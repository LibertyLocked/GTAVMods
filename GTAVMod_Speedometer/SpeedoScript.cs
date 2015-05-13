using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTAVMod_Speedometer
{
    public class SpeedoScript : Script
    {
        UIContainer hudContainer;
        UIText speedText;
        const int PANEL_WIDTH = 66;
        const int PANEL_HEIGHT = 24;
        bool useMph;

        public SpeedoScript()
        {
            // Read configuration
            ParseSettings();

            this.Tick += OnTick;
        }

        void OnTick(object sender, EventArgs e)
        {
            if (Game.Player.IsAlive && Game.Player.Character.IsInVehicle())
            {
                Vehicle vehicle = Game.Player.Character.CurrentVehicle;
                float speedKph = vehicle.Speed * 3600 / 1000;   // convert from m/s to km/h
                float speedMph = speedKph * 0.6213711916666667f; // convert km/h to mph
                if (useMph)
                    speedText.Text = speedMph.ToString("0") + " mph";
                else
                    speedText.Text = speedKph.ToString("0") + " km/h";
                
                hudContainer.Draw();
            }
        }

        void ParseSettings()
        {
            try
            {
                ScriptSettings settings = ScriptSettings.Load(@".\scripts\Metric_Speedometer.ini");
                
                // Parse Core settings
                this.useMph = settings.GetValue("Core", "UseMph", true);

                // Parse UI settings
                VerticalAlignment vAlign = (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), settings.GetValue("UI", "VertAlign"));
                HorizontalAlign hAlign = (HorizontalAlign)Enum.Parse(typeof(HorizontalAlign), settings.GetValue("UI", "HorzAlign"));
                Point posOffset = new Point(settings.GetValue<int>("UI", "OffsetX", 0), settings.GetValue<int>("UI", "OffsetY", 0));
                Color backcolor = Color.FromArgb(settings.GetValue<int>("UI", "BackcolorA", 200), settings.GetValue<int>("UI", "BackcolorR", 237),
                    settings.GetValue<int>("UI", "BackcolorG", 239), settings.GetValue<int>("UI", "BackcolorB", 241));
                Color forecolor = Color.FromArgb(settings.GetValue<int>("UI", "ForecolorA", 255), settings.GetValue<int>("UI", "ForecolorR", 0),
                    settings.GetValue<int>("UI", "ForecolorG", 0), settings.GetValue<int>("UI", "ForecolorB", 0));

                // Set up HUD container
                this.hudContainer = new UIContainer(new Point(UI.WIDTH / 2 - PANEL_WIDTH / 2 + posOffset.X, UI.HEIGHT - PANEL_HEIGHT + posOffset.Y),
                    new Size(PANEL_WIDTH, PANEL_HEIGHT), backcolor);
                this.speedText = new UIText("SPEEDO", new Point(PANEL_WIDTH / 2, 0), 0.5f, forecolor, 4, true);
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
