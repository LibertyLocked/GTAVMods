/*
 * Coordinates Recorder
 * Author: libertylocked
 * Version: 1.1
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
        Keys enableKey;
        Keys saveKey;
        bool enable;

        string coordStr = "";
        string nameStr = "";
        bool enteringText = false;
        UIText nameText;
        const int controlIndex = 1;

        public CoordRecorder()
        {
            LoadSettings();
            this.Tick += OnTick;
            this.KeyDown += OnKeyDown;
        }

        void OnTick(object sender, EventArgs e)
        {
            if (enable)
            {
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

            if (enteringText)
            {
                //nameText.Position = new Point(UI.WIDTH / 2, UI.HEIGHT / 2);
                //nameText.Centered = true;
                nameText.Caption = "Enter a name for " + coordStr + "\n" + nameStr;
                nameText.Draw();
                Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, controlIndex);
            }
            else
            {
                //Function.Call(Hash.ENABLE_ALL_CONTROL_ACTIONS, controlIndex);
            }
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (enteringText)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    // Stop entering text and save coord
                    WriteToFile(nameStr, coordStr);
                    enteringText = false;
                    Function.Call(Hash.ENABLE_ALL_CONTROL_ACTIONS, controlIndex);
                }
                else
                {
                    if (e.KeyCode == Keys.Back && nameStr.Length > 0)
                        nameStr = nameStr.Substring(0, nameStr.Length - 1);
                    else if (e.KeyCode == Keys.Escape || e.KeyCode == saveKey)
                    {
                        // Stop entering text but don't save coord
                        enteringText = false;
                        Function.Call(Hash.ENABLE_ALL_CONTROL_ACTIONS, controlIndex);
                    }
                    else
                        nameStr += KeysToString(e.KeyCode);
                }
            }
            else
            {
                if (enable && e.KeyCode == saveKey)
                {
                    // Pop up the enter name text, start entering text
                    enteringText = true;
                    coordStr = text.Caption;
                    nameStr = "";
                }
                if (e.KeyCode == enableKey)
                    enable = !enable;
            }
        }

        void LoadSettings()
        {
            ScriptSettings settings = ScriptSettings.Load(@".\scripts\CoordRecorder.ini");
            this.enable = settings.GetValue("Core", "Enable", true);
            this.enableKey = (Keys)Enum.Parse(typeof(Keys), settings.GetValue("Core", "EnableKey"), true);
            this.saveKey = (Keys)Enum.Parse(typeof(Keys), settings.GetValue("Core", "SaveKey"), true);

            container = new UIContainer(new Point(UI.WIDTH / 2 - PANEL_WIDTH / 2, 0), new Size(PANEL_WIDTH, PANEL_HEIGHT), backColor);
            text = new UIText("", new Point(PANEL_WIDTH / 2, 0), 0.42f, textColor, GTA.Font.Pricedown, true);
            container.Items.Add(text);

            nameText = new UIText("", new Point(UI.WIDTH / 2, UI.HEIGHT / 2), 0.5f, Color.White, 0, true);
        }

        void WriteToFile(string name, string coord)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(@".\scripts\CoordRecorder_Coords.txt", true))
                {
                    string datetimeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff");
                    sw.WriteLine("[" + datetimeStr + "] (" + name + ") " + coord);
                }
                UI.ShowSubtitle("Coords saved! " + coord, 5000);
            }
            catch
            {
                UI.Notify("Failed to save coord!");
            }
        }

        string KeysToString(Keys key)
        {
            string keyStr = "";  
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                keyStr = key.ToString().Substring(1);
            }
            else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                keyStr = key.ToString().Substring(6);
            }
            else if (key >= Keys.A && key <= Keys.Z)
            {
                keyStr = key.ToString();
            }
            else if (key == Keys.Space)
            {
                keyStr = " ";
            }
            return keyStr;
        }
    }
}
