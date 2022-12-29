using System.Collections;
using System.Collections.Generic;
using NitroxClient.GameLogic.InitialSync.Base;
using NitroxClient.MonoBehaviours;
using NitroxModel.Core;
using NitroxModel.DataStructures;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.Packets;
using NitroxModel.Server;
using UnityEngine;

namespace NitroxClient.GameLogic.InitialSync
{
    public class PlayerInitialSyncProcessor : InitialSyncProcessor
    {
        private static readonly Vector3 spawnRelativeToEscapePod = new Vector3(0.9f, 2.1f, 0);

        private readonly ItemContainers itemContainers;

        public PlayerInitialSyncProcessor(ItemContainers itemContainers)
        {
            this.itemContainers = itemContainers;
        }

        public override IEnumerator Process(InitialPlayerSync packet, WaitScreen.ManualWaitItem waitScreenItem)
        {
            SetPlayerPermissions(packet.Permissions);
            waitScreenItem.SetProgress(0.14f);
            yield return null;

            SetPlayerGameObjectId(packet.PlayerGameObjectId);
            waitScreenItem.SetProgress(0.29f);
            yield return null;

            AttachPlayerToEscapePod(packet.AssignedEscapePodId);
            waitScreenItem.SetProgress(0.43f);
            yield return null;

            yield return AddStartingItemsToPlayer(packet.FirstTimeConnecting);
            waitScreenItem.SetProgress(0.57f);
            yield return null;

            SetPlayerStats(packet.PlayerStatsData);
            waitScreenItem.SetProgress(0.72f);
            yield return null;

            SetPlayerGameMode(packet.GameMode);
            waitScreenItem.SetProgress(0.86f);
            yield return null;

            SetPlayerCompletedGoals(packet.CompletedGoals);
            waitScreenItem.SetProgress(1f);
            yield return null;
        }

        private void SetPlayerPermissions(Perms permissions)
        {
            NitroxServiceLocator.LocateService<LocalPlayer>().Permissions = permissions;
        }

        private void SetPlayerGameObjectId(NitroxId id)
        {
            NitroxEntity.SetNewId(Player.mainObject, id);
            Log.Info($"Received initial sync player GameObject Id: {id}");
        }

        private void AttachPlayerToEscapePod(NitroxId escapePodId)
        {
            GameObject escapePod = NitroxEntity.RequireObjectFrom(escapePodId);

            EscapePod.main.transform.position = escapePod.transform.position;
            EscapePod.main.playerSpawn.position = escapePod.transform.position + spawnRelativeToEscapePod;

            Player.main.transform.position = EscapePod.main.playerSpawn.position;
            Player.main.transform.rotation = EscapePod.main.playerSpawn.rotation;

            Player.main.currentEscapePod = escapePod.GetComponent<EscapePod>();
        }

        private IEnumerator AddStartingItemsToPlayer(bool firstTimeConnecting)
        {
            if (firstTimeConnecting)
            {
                foreach (TechType techType in LootSpawner.main.GetEscapePodStorageTechTypes())
                {
                    TaskResult<GameObject> result = new TaskResult<GameObject>();
                    yield return CraftData.InstantiateFromPrefabAsync(techType, result, false);
                    GameObject gameObject = result.Get();
                    Pickupable pickupable = gameObject.GetComponent<Pickupable>();
                    pickupable.Initialize();
                    itemContainers.AddItem(pickupable.gameObject, NitroxEntity.GetId(Player.main.gameObject));
                    itemContainers.BroadcastItemAdd(pickupable, Inventory.main.container.tr);
                }
            }
        }

        private void SetPlayerStats(PlayerStatsData statsData)
        {
            if (statsData != null)
            {
                Player.main.oxygenMgr.AddOxygen(statsData.Oxygen);
                Player.main.liveMixin.health = statsData.Health;
                Player.main.GetComponent<Survival>().food = statsData.Food;
                Player.main.GetComponent<Survival>().water = statsData.Water;
                Player.main.infectedMixin.SetInfectedAmount(statsData.InfectionAmount);

                //If InfectionAmount is at least 1f then the infection reveal should have happened already.
                //If InfectionAmount is below 1f then the reveal has not.
                if (statsData.InfectionAmount >= 1f)
                {
                    Player.main.infectionRevealed = true;
                }

                // We need to make the player invincible before he finishes loading because in some cases he will eventually die before loading
                Player.main.liveMixin.invincible = true;
                Player.main.FreezeStats();
            }
            // We need to start it at least once for everything that's in the PDA to load
            Player.main.GetPDA().Open(PDATab.Inventory);
            Player.main.GetPDA().Close();
        }

        private void SetPlayerGameMode(ServerGameMode gameMode)
        {
            Log.Info($"Received initial sync packet with gamemode {gameMode}");
            GameModeUtils.SetGameMode((GameModeOption)(int)gameMode, GameModeOption.None);
        }

        private void SetPlayerCompletedGoals(IEnumerable<string> completedGoals)
        {
            GoalManager.main.completedGoalNames.AddRange(completedGoals);
            PlayerWorldArrows.main.completedCustomGoals.AddRange(completedGoals);
        }
    }
}
