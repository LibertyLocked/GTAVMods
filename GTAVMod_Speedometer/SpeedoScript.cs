using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
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
                this.useMph = settings.GetValue("Core", "UseMph", false);

                // Parse UI settings
                VerticalAlignment vAlign = (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), settings.GetValue("UI", "VertAlign"));
                HorizontalAlign hAlign = (HorizontalAlign)Enum.Parse(typeof(HorizontalAlign), settings.GetValue("UI", "HorzAlign"));
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
				pos.Y += posOffset.Y; // apply offset in Y
				
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
				pos.X += posOffset.X; // apply offset in X
				
                this.hudContainer = new UIContainer(pos, new Size(pWidth, pHeight), backcolor);
                this.speedText = new UIText("SPEEDO", new Point(pWidth / 2, 0), fontSize, forecolor, fontStyle, true);
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
