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

namespace GTAV_AngryPeds
{
    public class AngryPedsScript : Script
    {
		bool civAngry = false;
        UIContainer container = null;

        public AngryPedsScript()
        {
            //this.container = new UIContainer(new Point(10, 240), new Size(150, 50), Color.FromArgb(200, 237, 239, 241));
            //this.container.Items.Add(new UIText("AngryCiv On", new Point(75, 4), 0.5f, Color.Red, 4, true));

            this.KeyDown += OnKeyDown;
            //this.Interval = 200;
            this.Tick += OnTick;

        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F7)
            {
                // Toggle civ angry
                civAngry = !civAngry;
                UI.Notify("AngryCiv " + (civAngry ? "On" : "Off"));
            }
        }

        void OnTick(object sender, EventArgs e)
        {
			if (civAngry)
			{
                //this.container.Draw();
				Ped[] nearbyPeds = GTA.World.GetNearbyPeds(GTA.Game.Player.Character, 500);
                foreach (Ped p in nearbyPeds)
                {
                    // void GIVE_WEAPON_TO_PED(int pedHandle, Hash weaponAssetHash, int ammoCount, BOOL equipNow, BOOL isAmmoLoaded)
                    Function.Call(Hash.GIVE_WEAPON_TO_PED, (InputArgument)(p), (InputArgument)(int)WeaponHash.Minigun, 9999, (InputArgument)true, (InputArgument)true);
                    p.Accuracy = 10;
                    p.Task.ShootAt(Game.Player.Character.Position);
                    //p.CanSwitchWeapons = true;
                    //World.CreatePed(new Model(PedHash.ACChimp), p.Position, p.Heading);
                    //p.Kill();
                }
			}
        }
    }
}
