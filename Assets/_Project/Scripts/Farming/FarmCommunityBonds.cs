using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// Shared farm standing with the village. This is deliberately farm-owned:
    /// co-op players contribute together, while future personal friendships can
    /// live in the authoritative player profile rather than in the farm save.
    /// </summary>
    [Serializable]
    public sealed class FarmCommunityProgress
    {
        public List<FarmCommunityBond> Bonds = new();

        public int GetFavor(string contactId)
        {
            if (string.IsNullOrWhiteSpace(contactId)) return 0;
            var bond = Bonds?.Find(candidate => candidate != null && string.Equals(candidate.ContactId, contactId, StringComparison.OrdinalIgnoreCase));
            return Mathf.Max(0, bond?.Favor ?? 0);
        }

        public int AddFavor(string contactId, int amount)
        {
            if (string.IsNullOrWhiteSpace(contactId) || amount <= 0) return GetFavor(contactId);
            Bonds ??= new List<FarmCommunityBond>();
            var bond = Bonds.Find(candidate => candidate != null && string.Equals(candidate.ContactId, contactId, StringComparison.OrdinalIgnoreCase));
            if (bond == null)
            {
                bond = new FarmCommunityBond { ContactId = contactId };
                Bonds.Add(bond);
            }
            bond.Favor = Mathf.Max(0, bond.Favor + amount);
            return bond.Favor;
        }

        public bool HasGiftedOnDay(string contactId, int day)
        {
            if (string.IsNullOrWhiteSpace(contactId)) return false;
            var bond = Bonds?.Find(candidate => candidate != null && string.Equals(candidate.ContactId, contactId, StringComparison.OrdinalIgnoreCase));
            return bond != null && bond.LastGiftDay == Mathf.Max(1, day);
        }

        public FarmCommunityProgress Clone()
        {
            var clone = new FarmCommunityProgress();
            if (Bonds == null) return clone;
            foreach (var bond in Bonds)
                if (bond != null && !string.IsNullOrWhiteSpace(bond.ContactId)) clone.Bonds.Add(bond.Clone());
            return clone;
        }
    }

    [Serializable]
    public sealed class FarmCommunityBond
    {
        public string ContactId;
        public int Favor;
        public int LastGiftDay;
        public FarmCommunityBond Clone() => new() { ContactId = ContactId, Favor = Favor, LastGiftDay = LastGiftDay };
    }

    public readonly struct FarmCommunityDeliveryResult
    {
        public readonly string ContactId;
        public readonly int FavorGained;
        public readonly int TotalFavor;
        public readonly int NewBondLevel;
        public readonly int MilestoneReward;

        public bool ReachedMilestone => MilestoneReward > 0;

        public FarmCommunityDeliveryResult(string contactId, int favorGained, int totalFavor, int newBondLevel, int milestoneReward)
        {
            ContactId = contactId;
            FavorGained = favorGained;
            TotalFavor = totalFavor;
            NewBondLevel = newBondLevel;
            MilestoneReward = milestoneReward;
        }
    }

    public readonly struct FarmCommunityGiftResult
    {
        public readonly string ContactId;
        public readonly string ItemId;
        public readonly int FavorGained;
        public readonly int TotalFavor;
        public readonly int NewBondLevel;
        public readonly int MilestoneReward;

        public bool ReachedMilestone => MilestoneReward > 0;

        public FarmCommunityGiftResult(string contactId, string itemId, int favorGained, int totalFavor, int newBondLevel, int milestoneReward)
        {
            ContactId = contactId;
            ItemId = itemId;
            FavorGained = favorGained;
            TotalFavor = totalFavor;
            NewBondLevel = newBondLevel;
            MilestoneReward = milestoneReward;
        }
    }

    public readonly struct FarmCommunityContact
    {
        public readonly string Id;
        public readonly string NameKey;
        public readonly string FallbackName;

        public FarmCommunityContact(string id, string nameKey, string fallbackName)
        {
            Id = id;
            NameKey = nameKey;
            FallbackName = fallbackName;
        }

        public string LocalizedName => FarmLocalization.Get(NameKey, FallbackName);
    }

    /// <summary>
    /// Data boundary for the first community loop. Replace these entries with
    /// ScriptableObject content later without changing save IDs or reward logic.
    /// </summary>
    public static class FarmCommunityCatalog
    {
        public const int NeighborhoodUnlockBondLevel = 2;
        private static readonly FarmCommunityContact[] Contacts =
        {
            new("elara", "community.contact.elara", "Elara"),
            new("bram", "community.contact.bram", "Bram"),
            new("niko", "community.contact.niko", "Niko")
        };

        public static IReadOnlyList<FarmCommunityContact> AllContacts => Contacts;

        public static bool IsKnownContact(string contactId)
        {
            foreach (var contact in Contacts)
                if (string.Equals(contact.Id, contactId, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static FarmCommunityContact GetContact(string contactId)
        {
            foreach (var contact in Contacts)
                if (string.Equals(contact.Id, contactId, StringComparison.OrdinalIgnoreCase)) return contact;
            return Contacts[0];
        }

        public static string PreferredGiftId(string contactId) => contactId switch
        {
            // Artisan goods make every social bond a meaningful alternative to
            // immediate market sale, while tea and stew remain useful meals.
            "elara" => "egg_preserve",
            "bram" => "smoked_fish",
            "niko" => "pumpkin_jam",
            _ => string.Empty
        };

        public static int FavorForGift(string contactId, string itemId) =>
            string.Equals(PreferredGiftId(contactId), itemId, StringComparison.OrdinalIgnoreCase) ? 3 : 0;

        public static string GetRequesterId(int day, int slot)
        {
            // Use a multiplier that is not divisible by the contact count so a
            // single board offers three different relationship choices.
            var index = Mathf.Abs((Mathf.Max(1, day) * 5) + Mathf.Max(0, slot) * 7) % Contacts.Length;
            return Contacts[index].Id;
        }

        public static int GetBondLevel(int favor)
        {
            if (favor >= 9) return 3;
            if (favor >= 5) return 2;
            if (favor >= 2) return 1;
            return 0;
        }

        public static int FavorToNextBond(int favor)
        {
            if (favor < 2) return 2 - favor;
            if (favor < 5) return 5 - favor;
            if (favor < 9) return 9 - favor;
            return 0;
        }

        public static int GetMilestoneReward(int bondLevel) => bondLevel switch
        {
            1 => 25,
            2 => 50,
            3 => 100,
            _ => 0
        };

        public static bool HasNeighborhoodUnlock(FarmCommunityProgress community, string contactId) =>
            community != null && GetBondLevel(community.GetFavor(contactId)) >= NeighborhoodUnlockBondLevel;

        public static string NeighborhoodUnlockDescription(string contactId) => contactId switch
        {
            "elara" => FarmLocalization.Get("community.unlock.elara", "Elara's Seed Share: seed packs include +1 seed."),
            "bram" => FarmLocalization.Get("community.unlock.bram", "Bram's Pond Tip: the pond allows +1 catch per day."),
            "niko" => FarmLocalization.Get("community.unlock.niko", "Niko's Workshop Shelf: the workbench queue gains +1 slot."),
            _ => string.Empty
        };
    }
}
