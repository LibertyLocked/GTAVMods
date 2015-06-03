using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTAV_AngryPeds
{
    public class AngryPedsScript : Script
    {
        const float _KILL_RADIUS = 1000f;

        GTA.Menu menu;

        public AngryPedsScript()
        {
            GTA.MenuButton btnKill = new MenuButton("Kill All", delegate { ExecuteKill(KillMode.KillAll); });
            GTA.MenuButton btnExplode = new MenuButton("Explode All", delegate { ExecuteKill(KillMode.ExplodeAll); });
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
            if (e.KeyCode == Keys.F5)
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
                // dont kill friendly peds, players, or peds in player's vehicle
                if (IsPedFriendly(p) || p.IsPlayer || (playerPed.IsInVehicle() && p.IsInVehicle(playerPed.CurrentVehicle))) continue;
                if (p.IsAlive)
                {
                    switch (killMode)
                    {
                        case KillMode.KillAll:
                            p.Kill();
                            break;
                        case KillMode.ExplodeAll:
                            KillPedWithExplosion(playerPed, p, Vector3.Zero, 17, 8f, 0f);
                            break;
                        case KillMode.SafeKill:
                            Relationship rel = p.GetRelationshipWithPed(playerPed);
                            if (!p.IsInVehicle() && !p.IsGettingIntoAVehicle && IsPedEnemyOrNeutral(playerPed))
                                KillPedWithExplosion(playerPed, p, new Vector3(0,0,0.5f), 14, 1f, 0f);
                                //KillPedWithBullet(playerPed, p);
                            break;
                        case KillMode.Disarm:
                            p.Weapons.RemoveAll();
                            break;
                    }

                }
            }
        }

        void KillPedWithExplosion(Ped ownerPed, Ped enemyPed, Vector3 posOffset, int explosionType, float radius, float cameraShake)
        {
            Vector3 position = enemyPed.Position + posOffset;
            Function.Call(Hash.ADD_OWNED_EXPLOSION, ownerPed, position.X, position.Y, position.Z, explosionType, radius, true, false, cameraShake);
        }

        void KillPedWithBullet(Ped ownerPed, Ped enemyPed)
        {
            //void SHOOT_SINGLE_BULLET_BETWEEN_COORDS(float x1, float y1, float z1, float x2, float y2, float z2, int damage, BOOL p7, Hash weaponHash, Ped ownerPed, BOOL p10, BOOL p11, float speed) // 867654CBC7606F2C CB7415AC
            Vector3 enemyPos = enemyPed.Position;
            Vector3 bulletPos = enemyPos + new Vector3(0,0,2f);
            Function.Call(Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS, bulletPos.X, bulletPos.Y, bulletPos.Z, enemyPos.X, enemyPos.Y, enemyPos.Z, 200, true, (long)WeaponHash.Pistol, ownerPed, true, true, 100f);
        }

        bool IsPedFriendly(Ped p)
        {
            Relationship rel = p.GetRelationshipWithPed(Game.Player.Character);
            return (rel == Relationship.Companion || rel == Relationship.Like || rel == Relationship.Respect);
        }

        bool IsPedEnemyOrNeutral(Ped p)
        {
            Relationship rel = p.GetRelationshipWithPed(Game.Player.Character);
            return (rel == Relationship.Hate || rel == Relationship.Dislike || rel == Relationship.Neutral);
        }
    }

    enum KillMode
    {
        ExplodeAll,
        KillAll,
        SafeKill,
        Disarm,
    }
}
