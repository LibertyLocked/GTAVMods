using System.Windows.Forms;
using GTA;

namespace GTAV_AngryPeds
{
    public class AngryPedsScript : Script
    {
        const float _KILL_RADIUS = 1000f;

        GTA.Menu menu;

        public AngryPedsScript()
        {
            GTA.MenuButton btnKill = new MenuButton("Kill All", delegate { ExecuteKill(KillMode.Kill); });
            GTA.MenuButton btnExplode = new MenuButton("Explode All", delegate { ExecuteKill(KillMode.Explode); });
            GTA.MenuButton btnSafeKill = new MenuButton("Safe Kill", delegate { ExecuteKill(KillMode.SafeKill); });
            GTA.MenuButton btnDisarmAll = new MenuButton("Disarm All", delegate { ExecuteKill(KillMode.Disarm); });
            menu = new GTA.Menu("Ultimate Kill", new GTA.MenuItem[]{ btnKill, btnExplode, btnDisarmAll, btnSafeKill });
            menu.HasFooter = false;

            LeftKey = Keys.NumPad4;
            RightKey = Keys.NumPad6;
            UpKey = Keys.NumPad8;
            DownKey = Keys.NumPad2;
            ActivateKey = Keys.NumPad5;
            BackKey = Keys.NumPad0;

            this.KeyDown += OnKeyDown;
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F7)
            {
                View.CloseAllMenus();
                View.AddMenu(menu);
            }
        }

        void ExecuteKill(KillMode killMode)
        {
            Ped playerPed = Game.Player.Character;
            Ped[] nearbyPeds = GTA.World.GetNearbyPeds(playerPed, _KILL_RADIUS);
            foreach (Ped p in nearbyPeds)
            {
                if (p.IsPlayer || (playerPed.IsInVehicle() && p.IsInVehicle(playerPed.CurrentVehicle))) continue;
                if (p.IsAlive)
                {
                    switch (killMode)
                    {
                        case KillMode.Kill:
                            p.Kill();
                            break;
                        case KillMode.Explode:
                            World.AddOwnedExplosion(playerPed, p.Position, ExplosionType.BigFire, 7f, 0f);
                            break;
                        case KillMode.SafeKill:
                            Relationship rel = p.GetRelationshipWithPed(playerPed);
                            if (rel == Relationship.Hate || rel == Relationship.Dislike || rel == Relationship.Neutral)
                                World.AddOwnedExplosion(playerPed, p.Position, ExplosionType.Fire, 1f, 0f);
                            break;
                        case KillMode.Disarm:
                            p.Weapons.RemoveAll();
                            break;
                    }

                }
            }
        }
    }

    enum KillMode
    {
        Explode,
        Kill,
        SafeKill,
        Disarm,
    }
}
