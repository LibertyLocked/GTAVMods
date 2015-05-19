/*
 * Coordinates Recorder
 * Author: libertylocked
 * Version: 1.0
 */
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using GTA;
using GTA.Math;
using GTA.Native;

namespace CoordinateRecorder
{
    public class CoordRecorder : Script
    {
        const int PANEL_WIDTH = 340;
        const int PANEL_HEIGHT = 20;
        Color backColor = Color.FromArgb(100, 255, 255, 255);
        Color textColor = Color.Black; // just change this to whatever color you want

        UIContainer container;
        UIText text;
        Keys saveKey;
        bool enable;

        public CoordRecorder()
        {
            LoadSettings();
            this.Tick += OnTick;
            this.KeyDown += OnKeyDown;
        }

        void OnTick(object sender, EventArgs e)
        {
            if (!enable)
            {
                this.Abort();
                return;
            }
            Player player = Game.Player;
            if (player != null && player.CanControlCharacter && player.IsAlive && player.Character != null)
            {
                // get coords
                Vector3 pos = player.Character.Position;
                float heading = player.Character.Heading;

                text.Caption = String.Format("x:{0} y:{1} z:{2} angle:{3}", pos.X.ToString("0.000"),
                    pos.Y.ToString("0.000"), pos.Z.ToString("0.000"), heading.ToString("0.000"));
                // draw
                container.Draw();
            }
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == saveKey)
            {
                WriteToFile();
            }
        }

        void LoadSettings()
        {
            ScriptSettings settings = ScriptSettings.Load(@".\scripts\CoordRecorder.ini");
            this.enable = settings.GetValue("Core", "Enable", true);
            this.saveKey = (Keys)Enum.Parse(typeof(Keys), settings.GetValue("Core", "SaveKey"), true);

            container = new UIContainer(new Point(UI.WIDTH / 2 - PANEL_WIDTH / 2, 0), new Size(PANEL_WIDTH, PANEL_HEIGHT), backColor);
            text = new UIText("", new Point(PANEL_WIDTH / 2, 0), 0.42f, textColor, 7, true);
            container.Items.Add(text);
        }

        void WriteToFile()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(@".\scripts\CoordRecorder_Coords.txt", true))
                {
                    string datetimeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff");
                    sw.WriteLine("[" + datetimeStr + "] " + text.Caption);
                }
                UI.ShowSubtitle("Coord saved!", 5000);
            }
            catch
            {
                UI.Notify("Failed to save coord!");
            }
        }
    }
}
