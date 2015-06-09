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
            GTA.MenuButton btnSafeKill = new MenuButton("Electrocute Hostiles", delegate { ExecuteKill(KillMode.SafeKill); });
            GTA.MenuButton btnDisarmAll = new MenuButton("Disarm All", delegate { ExecuteKill(KillMode.Disarm); });
            GTA.MenuButton btnSpawnTank = new MenuButton("Spawn Tank", delegate { SpawnVehicle(VehicleHash.Rhino); });
            GTA.MenuButton btnSpawnBuzzard = new MenuButton("Spawn Buzzard", delegate { SpawnVehicle(VehicleHash.Buzzard); });
            GTA.MenuButton btnSpawnKuruma = new MenuButton("Spawn Kuruma", delegate { SpawnVehicle(VehicleHash.Kuruma2); });
            menu = new GTA.Menu("Ultimate Kill", new GTA.MenuItem[]
            { 
                btnKill, btnExplode, btnDisarmAll, btnSafeKill, btnSpawnTank, btnSpawnBuzzard, btnSpawnKuruma,
            });
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
                            if (!p.IsInVehicle() && !p.IsGettingIntoAVehicle && IsPedEnemyOrNeutral(p))
                            {
                                //KillPedWithExplosion(playerPed, p, new Vector3(0, 0, 0.5f), 14, 1f, 0f);
                                KillPedWithStunGun(playerPed, p, 200);
                            }
                            break;
                        case KillMode.Disarm:
                            p.Weapons.RemoveAll();
                            break;
                    }

                }
            }
        }

        void SpawnVehicle(VehicleHash hash)
        {
            Vector3 pos = Game.Player.Character.Position + Game.Player.Character.ForwardVector * 6f;
            //Vehicle veh = Function.Call<Vehicle>(Hash.CREATE_VEHICLE, (ulong)hash, pos.X, pos.Y, pos.Z, heading, true, false);
            Vehicle veh = World.CreateVehicle(new Model(hash), pos);
        }

        void KillPedWithExplosion(Ped ownerPed, Ped enemyPed, Vector3 posOffset, int explosionType, float radius, float cameraShake)
        {
            Vector3 position = enemyPed.Position + posOffset;
            Function.Call(Hash.ADD_OWNED_EXPLOSION, ownerPed, position.X, position.Y, position.Z, explosionType, radius, true, false, cameraShake);
        }

        void KillPedWithStunGun(Ped ownerPed, Ped enemyPed, int damage)
        {
            //void SHOOT_SINGLE_BULLET_BETWEEN_COORDS(float x1, float y1, float z1, float x2, float y2, float z2, int damage, BOOL p7, Hash weaponHash, Ped ownerPed, BOOL p10, BOOL p11, float speed) // 867654CBC7606F2C CB7415AC
            Vector3 shootTo = enemyPed.Position;
            Vector3 shootFrom = enemyPed.Position + enemyPed.ForwardVector * 1f;
            Model stunGunModel = new Model(WeaponHash.StunGun);
            Function.Call(Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS, shootFrom.X, shootFrom.Y, shootFrom.Z, shootTo.X, shootTo.Y, shootTo.Z,
                damage, true, stunGunModel.Hash, ownerPed, true, true, 1f);
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
