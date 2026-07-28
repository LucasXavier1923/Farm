using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// Player-owned identity for a shared farm. Farm mastery remains shared technology;
    /// this profile is deliberately per player so a four-person crew can divide work.
    /// </summary>
    [Serializable]
    public sealed class FarmPlayerRoleProfile
    {
        public string PlayerId;
        public FarmSpecialization Role;
        public int MatchingOrderContributions;
        public int LastContributionDay;

        public FarmPlayerRoleProfile Clone() => new()
        {
            PlayerId = PlayerId,
            Role = Role,
            MatchingOrderContributions = MatchingOrderContributions,
            LastContributionDay = LastContributionDay
        };
    }

    [Serializable]
    public sealed class FarmCoopRoleProgress
    {
        public int TeamworkDay = 1;
        public int TeamworkRoleMask;
        public int LastTeamworkBonusDay;
        public List<FarmPlayerRoleProfile> Players = new();

        public FarmCoopRoleProgress Clone()
        {
            var clone = new FarmCoopRoleProgress
            {
                TeamworkDay = TeamworkDay,
                TeamworkRoleMask = TeamworkRoleMask,
                LastTeamworkBonusDay = LastTeamworkBonusDay
            };
            foreach (var profile in Players ?? new List<FarmPlayerRoleProfile>())
                if (profile != null) clone.Players.Add(profile.Clone());
            return clone;
        }

        public void EnsureNormalized()
        {
            Players ??= new List<FarmPlayerRoleProfile>();
            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = Players.Count - 1; index >= 0; index--)
            {
                var profile = Players[index];
                if (profile == null || string.IsNullOrWhiteSpace(profile.PlayerId) || !unique.Add(profile.PlayerId.Trim()))
                    Players.RemoveAt(index);
                else
                {
                    profile.PlayerId = profile.PlayerId.Trim();
                    profile.MatchingOrderContributions = Mathf.Max(0, profile.MatchingOrderContributions);
                    profile.LastContributionDay = Mathf.Max(0, profile.LastContributionDay);
                }
            }
            TeamworkDay = Mathf.Max(1, TeamworkDay);
            TeamworkRoleMask = Mathf.Max(0, TeamworkRoleMask) & 0b111;
            LastTeamworkBonusDay = Mathf.Max(0, LastTeamworkBonusDay);
        }
    }

    public readonly struct FarmRoleOrderContribution
    {
        public readonly FarmSpecialization RecommendedRole;
        public readonly bool MatchedRole;
        public readonly int TeamworkBonus;

        public FarmRoleOrderContribution(FarmSpecialization recommendedRole, bool matchedRole, int teamworkBonus)
        {
            RecommendedRole = recommendedRole;
            MatchedRole = matchedRole;
            TeamworkBonus = teamworkBonus;
        }
    }

    public static class FarmCoopRoleRules
    {
        public const int MaxPlayers = 4;
        public const int TeamworkBonus = 45;
        public const int RequiredRoleMask = 0b111;

        public static FarmSpecialization RecommendedRole(FarmDailyOrderType type) => type switch
        {
            FarmDailyOrderType.Crop => FarmSpecialization.Cultivation,
            FarmDailyOrderType.Fishing or FarmDailyOrderType.Animal => FarmSpecialization.Harvesting,
            FarmDailyOrderType.Production => FarmSpecialization.Commerce,
            _ => FarmSpecialization.None
        };

        public static int Mask(FarmSpecialization role) => role switch
        {
            FarmSpecialization.Cultivation => 0b001,
            FarmSpecialization.Harvesting => 0b010,
            FarmSpecialization.Commerce => 0b100,
            _ => 0
        };

        public static string DisplayName(FarmSpecialization role) => role switch
        {
            FarmSpecialization.Cultivation => FarmLocalization.Get("roles.cultivation", "CULTIVATOR"),
            FarmSpecialization.Harvesting => FarmLocalization.Get("roles.harvesting", "GATHERER"),
            FarmSpecialization.Commerce => FarmLocalization.Get("roles.commerce", "STEWARD"),
            _ => FarmLocalization.Get("roles.none", "UNASSIGNED")
        };
    }
}
