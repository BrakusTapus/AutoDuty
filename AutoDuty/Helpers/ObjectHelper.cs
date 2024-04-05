﻿using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.GameFunctions;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System;
using Dalamud.Game.ClientState.Objects.Enums;
using ECommons;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ECommons.Throttlers;
using AutoDuty.IPC;

namespace AutoDuty.Helpers
{
    internal static class ObjectHelper
    {
        internal static List<GameObject>? GetObjectsByObjectKind(ObjectKind objectKind) => [.. Svc.Objects.OrderBy(GetDistanceToPlayer).Where(o => o.ObjectKind == objectKind)];

        internal static GameObject? GetObjectByObjectKind(ObjectKind objectKind) => Svc.Objects.OrderBy(GetDistanceToPlayer).FirstOrDefault(o => o.ObjectKind == objectKind);

        internal static List<GameObject>? GetObjectsByRadius(float radius) => [.. Svc.Objects.OrderBy(GetDistanceToPlayer).Where(o => GetDistanceToPlayer(o) <= radius)];

        internal static GameObject? GetObjectByRadius(float radius) => Svc.Objects.OrderBy(GetDistanceToPlayer).FirstOrDefault(o => GetDistanceToPlayer(o) <= radius);

        internal static List<GameObject>? GetObjectsByName(string name) => [.. Svc.Objects.OrderBy(GetDistanceToPlayer).Where(o => o.Name.TextValue.Equals(name, StringComparison.CurrentCultureIgnoreCase))];

        internal static GameObject? GetObjectByName(string name) => Svc.Objects.OrderBy(GetDistanceToPlayer).FirstOrDefault(o => o.Name.TextValue.Equals(name, StringComparison.CurrentCultureIgnoreCase));

        internal static List<GameObject>? GetObjectsByPartialName(string name) => [.. Svc.Objects.OrderBy(GetDistanceToPlayer).Where(o => o.Name.TextValue.Contains(name, StringComparison.CurrentCultureIgnoreCase))];

        internal static GameObject? GetObjectByPartialName(string name) => Svc.Objects.OrderBy(GetDistanceToPlayer).FirstOrDefault(o => o.Name.TextValue.Contains(name, StringComparison.CurrentCultureIgnoreCase));

        internal static List<GameObject>? GetObjectsByNameAndRadius(string objectName) => [.. Svc.Objects.OrderBy(GetDistanceToPlayer).Where(g => g.Name.TextValue.Equals(objectName, StringComparison.CurrentCultureIgnoreCase) && Vector3.Distance(Player.Object.Position, g.Position) <= 10)];

        internal static GameObject? GetObjectByNameAndRadius(string objectName) => Svc.Objects.OrderBy(GetDistanceToPlayer).FirstOrDefault(g => g.Name.TextValue.Equals(objectName, StringComparison.CurrentCultureIgnoreCase) && Vector3.Distance(Player.Object.Position, g.Position) <= 10);

        internal static GameObject? GetClosestObjectByName(List<GameObject> gameObjects, string name) => gameObjects.OrderBy(GetDistanceToPlayer).FirstOrDefault(p => p.Name.TextValue.Equals(name, StringComparison.CurrentCultureIgnoreCase) && p.IsTargetable);

        internal static BattleChara? GetBossObject(int radius = 100) => GetObjectsByRadius(radius)?.OfType<BattleChara>().FirstOrDefault(b => IsBossFromIcon(b) || BossMod_IPCSubscriber.HasModule(b));

        internal unsafe static float GetDistanceToPlayer(GameObject gameObject) => GetDistanceToPlayer(gameObject.Position);

        internal unsafe static float GetDistanceToPlayer(Vector3 v3) => Vector3.Distance(v3, Player.GameObject->Position);

        internal unsafe static GameObject? GetTankPartyMember()
        {
            if (Svc.Party.PartyId == 0)
                return null;

            if (Player.Object.ClassJob.GameData?.Role == 1)
                return Player.Object;

            foreach (var partyMember in Svc.Party)
            {
                if (partyMember.ClassJob.GameData?.Role == 1)
                    return partyMember.GameObject;
            }

            return null;
        }

        internal unsafe static GameObject? GetHealerPartyMember()
        {
            if (Svc.Party.PartyId == 0)
                return null;

            if (Player.Object.ClassJob.GameData?.Role == 4)
                return Player.Object;

            foreach (var partyMember in Svc.Party)
            {
                if (partyMember.ClassJob.GameData?.Role == 4)
                    return partyMember.GameObject;
            }

            return null;
        }

        //RotationSolver
        internal unsafe static float GetBattleDistanceToPlayer(GameObject gameObject)
        {
            if (gameObject == null) return float.MaxValue;
            var player = Player.Object;
            if (player == null) return float.MaxValue;

            var distance = Vector3.Distance(player.Position, gameObject.Position) - player.HitboxRadius;
            distance -= gameObject.HitboxRadius;
            return distance;
        }

        internal static BNpcBase? GetObjectNPC(GameObject gameObject) => Svc.Data.GetExcelSheet<BNpcBase>()?.GetRow(gameObject.DataId) ?? null;

        //From RotationSolver
        internal static bool IsBossFromIcon(GameObject gameObject) => GetObjectNPC(gameObject)?.Rank is 1 or 2 or 6;

        internal static float JobRange
        {
            get
            {
                float radius = 15;
                if (!Player.Available) return radius;
                switch (Svc.Data.GetExcelSheet<ClassJob>()?.GetRow(
                    Player.Object.ClassJob.Id)?.GetJobRole() ?? JobRole.None)
                {
                    case JobRole.Tank:
                    case JobRole.Melee:
                        radius = 3;
                        break;
                }
                return radius;
            }
        }

        internal static JobRole GetJobRole(this ClassJob job)
        {
            var role = (JobRole)job.Role;

            if (role is JobRole.Ranged or JobRole.None)
            {
                role = job.ClassJobCategory.Row switch
                {
                    30 => JobRole.RangedPhysical,
                    31 => JobRole.RangedMagical,
                    32 => JobRole.DiscipleOfTheLand,
                    33 => JobRole.DiscipleOfTheHand,
                    _ => JobRole.None,
                };
            }
            return role;
        }

        /// <summary>
        /// The role of jobs.
        /// </summary>
        internal enum JobRole : byte
        {
            None = 0,
            Tank = 1,
            Melee = 2,
            Ranged = 3,
            Healer = 4,
            RangedPhysical = 5,
            RangedMagical = 6,
            DiscipleOfTheLand = 7,
            DiscipleOfTheHand = 8,
        }
        internal enum ClassJobType : uint
        {
            Adventurer = 0,
            Gladiator = 1,
            Pugilist = 2,
            Marauder = 3,
            Lancer = 4,
            Archer = 5,
            Conjurer = 6,
            Thaumaturge = 7,
            Carpenter = 8,
            Blacksmith = 9,
            Armorer = 10,
            Goldsmith = 11,
            Leatherworker = 12,
            Weaver = 13,
            Alchemist = 14,
            Culinarian = 15,
            Miner = 16,
            Botanist = 17,
            Fisher = 18,
            Paladin = 19,
            Monk = 20,
            Warrior = 21,
            Dragoon = 22,
            Bard = 23,
            WhiteMage = 24,
            BlackMage = 25,
            Arcanist = 26,
            Summoner = 27,
            Scholar = 28,
            Rogue = 29,
            Ninja = 30,
            Machinist = 31,
            DarkKnight = 32,
            Astralogian = 33,
            Astrologian = 33,
            Samurai = 34,
            RedMage = 35,
            BlueMage = 36,
            Gunbreaker = 37,
            Dancer = 38,
            Reaper = 39,
            Sage = 40,
        }

        internal static unsafe bool IsValid => Svc.Condition.Any()
        && !Svc.Condition[ConditionFlag.BetweenAreas]
        && !Svc.Condition[ConditionFlag.BetweenAreas51]
        && Player.Available
        && Player.Interactable;

        internal static bool IsJumping => Svc.Condition.Any()
        && (Svc.Condition[ConditionFlag.Jumping]
        || Svc.Condition[ConditionFlag.Jumping61]);

        internal static unsafe bool IsReady => IsValid && !IsOccupied;

        internal static unsafe bool IsOccupied => GenericHelpers.IsOccupied();

        internal static unsafe bool InCombat(this BattleChara battleChara) => battleChara.Struct()->Character.InCombat;

        internal static unsafe void InteractWithObject(GameObject? gameObject)
        {
            try
            {
                if (gameObject == null || !gameObject.IsTargetable) 
                    return;

                var gameObjectPointer = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)gameObject.Address;
                TargetSystem.Instance()->InteractWithObject(gameObjectPointer, true);
            }
            catch (Exception)
            {
                //Svc.Log.Info($"InteractWithObject: Exception: {ex}");
            }
        }
        internal static unsafe AtkUnitBase* InteractWithObjectUntilAddon(GameObject? gameObject, string addonName)
        {
            if (GenericHelpers.TryGetAddonByName<AtkUnitBase>(addonName, out var addon) && GenericHelpers.IsAddonReady(addon))
                return addon;

            if (EzThrottler.Throttle("InteractWithObjectUntilAddon"))
                InteractWithObject(gameObject);
            
            return null;
        }

        internal static unsafe bool InteractWithObjectUntilNotValid(GameObject? gameObject)
        {
            if (gameObject == null || !IsValid)
                return true;

            if (EzThrottler.Throttle("InteractWithObjectUntilNotValid"))
                InteractWithObject(gameObject);
            
            return false;
        }

        internal static unsafe bool InteractWithObjectUntilNotTargetable(GameObject? gameObject)
        {
            if (gameObject == null || !gameObject.IsTargetable)
                return true;

            if (EzThrottler.Throttle("InteractWithObjectUntilNotTargetable"))
                InteractWithObject(gameObject);

            return false;
        }

        internal static unsafe bool PlayerIsCasting => Player.Character->IsCasting;
    }
}
