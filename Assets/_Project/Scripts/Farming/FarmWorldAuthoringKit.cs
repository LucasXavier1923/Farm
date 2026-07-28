using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmPrototype.Farming
{
    public enum FarmWorldZoneKind { Spawn, Fields, Animals, Production, Storage, Expansion, Landmark }

    [Serializable]
    public sealed class FarmWorldZone
    {
        public string Id;
        public FarmWorldZoneKind Kind;
        public Vector3 Center;
        public Vector3 Size = new(8f, 1f, 8f);
        [TextArea] public string Notes;
    }

    /// <summary>
    /// Scene-only authoring data. It never spawns gameplay or changes rules;
    /// it gives world builders named reservations and a repeatable validation.
    /// </summary>
    [ExecuteAlways]
    public sealed class FarmWorldAuthoringKit : MonoBehaviour
    {
        public List<FarmWorldZone> Zones = new();
        public bool DrawZones = true;

        public bool ValidateConfiguration(out List<string> messages)
        {
            messages = new List<string>();
            if (GameObject.Find("Player") == null) messages.Add("Missing required Player object.");
            if (FindAnyObjectByType<FarmTestPlot>() == null) messages.Add("Missing Farm_Test_System / FarmTestPlot bootstrap.");
            if (Camera.main == null) messages.Add("Missing Main Camera.");
            if (FindAnyObjectByType<Light>() == null) messages.Add("Missing scene Light.");
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var zone in Zones)
            {
                if (zone == null || string.IsNullOrWhiteSpace(zone.Id)) { messages.Add("A world zone has no ID."); continue; }
                if (!seenIds.Add(zone.Id)) messages.Add($"Duplicate world-zone ID: {zone.Id}.");
                if (zone.Size.x <= 0f || zone.Size.z <= 0f) messages.Add($"World zone {zone.Id} has an invalid size.");
            }
            if (Zones.Count == 0) messages.Add("No world zones are reserved yet.");
            return messages.Count == 0;
        }

        [ContextMenu("Validate World Authoring Setup")]
        private void ValidateFromInspector()
        {
            if (ValidateConfiguration(out var messages)) Debug.Log("Farm world authoring validation passed.", this);
            else Debug.LogWarning("Farm world authoring validation:\n- " + string.Join("\n- ", messages), this);
        }

        private void OnDrawGizmos()
        {
            if (!DrawZones || Zones == null) return;
            foreach (var zone in Zones)
            {
                if (zone == null || zone.Size.x <= 0f || zone.Size.z <= 0f) continue;
                Gizmos.color = ZoneColor(zone.Kind);
                Gizmos.DrawWireCube(zone.Center + (Vector3.up * 0.15f), new Vector3(zone.Size.x, 0.3f, zone.Size.z));
            }
        }

        private static Color ZoneColor(FarmWorldZoneKind kind) => kind switch
        {
            FarmWorldZoneKind.Spawn => new Color(0.30f, 0.75f, 1f, 0.9f),
            FarmWorldZoneKind.Fields => new Color(0.35f, 0.9f, 0.38f, 0.9f),
            FarmWorldZoneKind.Animals => new Color(1f, 0.70f, 0.25f, 0.9f),
            FarmWorldZoneKind.Production => new Color(0.90f, 0.40f, 0.25f, 0.9f),
            FarmWorldZoneKind.Storage => new Color(0.70f, 0.55f, 0.95f, 0.9f),
            FarmWorldZoneKind.Expansion => new Color(0.95f, 0.92f, 0.35f, 0.9f),
            _ => new Color(0.85f, 0.85f, 0.85f, 0.9f)
        };
    }
}
