using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        const int PANEL_HEIGT = 24;

        public SpeedoScript()
        {
            // Set up HUD container
            this.hudContainer = new UIContainer(new Point(UI.WIDTH/2 - PANEL_WIDTH/2, UI.HEIGHT - PANEL_HEIGT), 
                new Size(PANEL_WIDTH, PANEL_HEIGT), Color.FromArgb(200, 237, 239, 241));
            this.speedText = new UIText("SPEEDO", new Point(PANEL_WIDTH/2, 0), 0.5f, Color.Black, 4, true);
            this.hudContainer.Items.Add(speedText);

            this.Tick += OnTick;
        }

        void OnTick(object sender, EventArgs e)
        {
            if (Game.Player.IsAlive && Game.Player.Character.IsInVehicle())
            {
                Vehicle vehicle = Game.Player.Character.CurrentVehicle;
                float speedKph = vehicle.Speed * 3600 / 1000;   // convert from m/s to km/h
                speedText.Text = speedKph.ToString("0") + " km/h";
                hudContainer.Draw();
            }
        }
    }
}
