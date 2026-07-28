using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace FarmPrototype.Farming
{
    public readonly struct FarmSessionCommandResult
    {
        public readonly bool Succeeded;
        public readonly string Message;

        public FarmSessionCommandResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
        }
    }

    /// <summary>
    /// Host-only command boundary. A peer can request an action but never chooses
    /// its result; malformed, duplicate, or unauthorised messages are rejected
    /// before they can reach FarmTestPlot.
    /// </summary>
    public sealed class FarmHostIntentRouter
    {
        private readonly FarmTestPlot plot;
        private readonly HashSet<string> processedIntentIds = new(StringComparer.Ordinal);

        public FarmHostIntentRouter(FarmTestPlot plot) => this.plot = plot;

        public async Task<FarmSessionCommandResult> ExecuteAsync(FarmSessionIntent intent)
        {
            if (!FarmSessionTime.IsSimulationAuthority) return Fail("Only the host can execute shared farm actions.");
            if (plot == null || intent == null || string.IsNullOrWhiteSpace(intent.IntentId) || string.IsNullOrWhiteSpace(intent.RequestedBy))
                return Fail("Invalid farm session command.");
            if (!processedIntentIds.Add(intent.IntentId)) return Fail("This farm session command was already processed.");
            if (processedIntentIds.Count > 256) processedIntentIds.Clear();

            return intent.Kind switch
            {
                FarmSessionIntentKind.ToolAction => await ExecuteToolAsync(intent),
                FarmSessionIntentKind.SleepReadiness => ExecuteSleep(intent),
                FarmSessionIntentKind.Commerce => ExecuteCommerce(intent),
                FarmSessionIntentKind.AnimalCare => ExecuteAnimalCare(intent),
                FarmSessionIntentKind.Production => ExecuteProduction(intent),
                FarmSessionIntentKind.Consumption => ExecuteConsumption(intent),
                FarmSessionIntentKind.CommunityGift => ExecuteCommunityGift(intent),
                FarmSessionIntentKind.FestivalContribution => ExecuteFestivalContribution(intent),
                FarmSessionIntentKind.CommunityProject => ExecuteCommunityProject(intent),
                FarmSessionIntentKind.Fishing => ExecuteFishing(intent),
                FarmSessionIntentKind.DailyOrder => ExecuteDailyOrder(intent),
                FarmSessionIntentKind.Stewardship => ExecuteStewardship(intent),
                FarmSessionIntentKind.Progression => ExecuteProgression(intent),
                FarmSessionIntentKind.CollectionMilestone => ExecuteCollectionMilestone(intent),
                FarmSessionIntentKind.PlayerRole => ExecutePlayerRole(intent),
                FarmSessionIntentKind.ForecastPlan => ExecuteForecastPlan(intent),
                FarmSessionIntentKind.RestRecovery => ExecuteRestRecovery(intent),
                FarmSessionIntentKind.HotbarSelection or FarmSessionIntentKind.HotbarAssignment =>
                    new FarmSessionCommandResult(true, "Hotbar preferences stay local to each player."),
                _ => Fail("This command is not supported by the current host session.")
            };
        }

        private async Task<FarmSessionCommandResult> ExecuteToolAsync(FarmSessionIntent intent)
        {
            var values = ReadValues(intent.Payload);
            if (!values.TryGetValue("tool", out var toolText) || !Enum.TryParse(toolText, true, out FarmTool tool) ||
                !values.TryGetValue("tile", out var tileText) || !int.TryParse(tileText, out var tileIndex))
                return Fail("Invalid tool command.");
            if (!plot.TryExecuteRemoteToolAction(tool, tileIndex, values.TryGetValue("item", out var itemId) ? itemId : string.Empty, out var execution))
                return Fail("The host rejected this tool command.");
            return new FarmSessionCommandResult(true, await execution);
        }

        private FarmSessionCommandResult ExecuteSleep(FarmSessionIntent intent)
        {
            var values = ReadValues(intent.Payload);
            if (!values.TryGetValue("ready", out var readyText) || !bool.TryParse(readyText, out var ready)) return Fail("Invalid sleep readiness command.");
            return plot.TrySetRemoteSleepReadiness(intent.RequestedBy, ready)
                ? new FarmSessionCommandResult(true, "Sleep readiness updated.")
                : Fail("The player is not in this farm session.");
        }

        private FarmSessionCommandResult ExecuteCommerce(FarmSessionIntent intent)
        {
            if (string.IsNullOrWhiteSpace(intent.Payload)) return Fail("Invalid commerce command.");
            var request = JsonUtility.FromJson<FarmCommerceRequest>(intent.Payload);
            if (request == null) return Fail("Invalid commerce command.");
            request.RequestedBy = intent.RequestedBy;
            var result = plot.SessionCommerce?.Execute(request);
            return result.HasValue
                ? new FarmSessionCommandResult(result.Value.Succeeded, result.Value.Message)
                : Fail("Commerce is unavailable.");
        }

        private FarmSessionCommandResult ExecuteAnimalCare(FarmSessionIntent intent)
        {
            var values = ReadValues(intent.Payload);
            if (!values.TryGetValue("action", out var action) || string.IsNullOrWhiteSpace(action))
                return Fail("Invalid animal-care command.");
            var animals = plot.AnimalSystem;
            if (animals == null) return Fail("Animal care is unavailable.");
            return animals.ExecuteHostCareAction(action, intent.RequestedBy);
        }

        private FarmSessionCommandResult ExecuteProduction(FarmSessionIntent intent)
        {
            var values = ReadValues(intent.Payload);
            if (!values.TryGetValue("action", out var action)) return Fail("Invalid production command.");
            if (string.Equals(action, "craft", StringComparison.OrdinalIgnoreCase) && values.TryGetValue("recipe", out var recipeId))
                return plot.CraftingSystem != null ? plot.CraftingSystem.ExecuteHostCraft(recipeId, intent.RequestedBy) : Fail("Crafting is unavailable.");
            if (string.Equals(action, "process", StringComparison.OrdinalIgnoreCase))
            {
                values.TryGetValue("recipe", out var processRecipeId);
                return plot.ProcessorSystem != null ? plot.ProcessorSystem.ExecuteHostProcess(intent.RequestedBy, processRecipeId) : Fail("Processing is unavailable.");
            }
            return Fail("Invalid production command.");
        }

        private FarmSessionCommandResult ExecuteConsumption(FarmSessionIntent intent)
        {
            var values = ReadValues(intent.Payload);
            if (!values.TryGetValue("item", out var itemId)) return Fail("Invalid meal command.");
            return plot.MealSystem != null
                ? plot.MealSystem.ExecuteHostConsume(itemId, intent.RequestedBy)
                : Fail("Meals are unavailable.");
        }

        private FarmSessionCommandResult ExecuteCollectionMilestone(FarmSessionIntent intent)
        {
            if (plot.GameState == null) return Fail("Collection rewards are unavailable.");
            return plot.GameState.TryClaimNextCollectionMilestone(out var milestone, out var error)
                ? new FarmSessionCommandResult(true, FarmLocalization.Format("collection.milestone.claimed", "Collection reward claimed: {0} x{1}.", FarmContentDatabase.GetItem(milestone.RewardItemId)?.LocalizedName ?? milestone.RewardItemId, milestone.RewardAmount))
                : Fail(error);
        }

        private FarmSessionCommandResult ExecuteCommunityGift(FarmSessionIntent intent)
        {
            var values = ReadValues(intent.Payload);
            if (!values.TryGetValue("contact", out var contactId) || !values.TryGetValue("item", out var itemId) || plot.GameState == null)
                return Fail("Invalid community gift command.");
            if (!plot.GameState.TryGiveCommunityGift(contactId, itemId, out var result, out var error)) return Fail(error);
            var contact = FarmCommunityCatalog.GetContact(result.ContactId);
            return new FarmSessionCommandResult(true, FarmLocalization.Format("gift.sent", "Gift delivered to {0}: +{1} Favor.", contact.LocalizedName, result.FavorGained));
        }

        private FarmSessionCommandResult ExecuteFestivalContribution(FarmSessionIntent intent)
        {
            var values = ReadValues(intent.Payload);
            if (!values.TryGetValue("item", out var itemId) || plot.GameState == null) return Fail("Invalid festival contribution command.");
            return plot.GameState.TryContributeFestival(itemId, out var amount, out var complete, out var reward, out var error)
                ? new FarmSessionCommandResult(true, complete ? $"Festival complete: +${reward}." : $"Festival contribution {amount} accepted.")
                : Fail(error);
        }

        private FarmSessionCommandResult ExecuteCommunityProject(FarmSessionIntent intent)
        {
            var values = ReadValues(intent.Payload);
            if (!values.TryGetValue("item", out var itemId) || plot.GameState == null) return Fail("Invalid community project command.");
            return plot.GameState.TryContributeCommunityProject(itemId, out var complete, out var error)
                ? new FarmSessionCommandResult(true, complete ? "Market Route complete." : "Project material accepted.")
                : Fail(error);
        }

        private FarmSessionCommandResult ExecuteFishing(FarmSessionIntent intent)
        {
            var values = ReadValues(intent.Payload);
            if (!values.TryGetValue("action", out var action) || !string.Equals(action, "catch", StringComparison.OrdinalIgnoreCase))
                return Fail("Invalid fishing command.");
            return plot.FishingSystem != null
                ? plot.FishingSystem.ExecuteHostCatch(intent.RequestedBy)
                : Fail("Fishing is unavailable.");
        }

        private FarmSessionCommandResult ExecuteDailyOrder(FarmSessionIntent intent)
        {
            var values = ReadValues(intent.Payload);
            if (!values.TryGetValue("index", out var indexText) || !int.TryParse(indexText, out var index) || plot.GameState == null)
                return Fail("Invalid daily-order command.");
            var orders = FarmDailyOrderGenerator.Generate(plot.GameState.WorldSeed, plot.GameState.DayNumber);
            if (index < 0 || index >= orders.Count) return Fail("Invalid daily-order command.");
            return plot.GameState.TryCompleteDailyOrder(orders[index], index, intent.RequestedBy, out var earned, out var bonus, out var community, out var roleContribution, out var error)
                ? new FarmSessionCommandResult(true, roleContribution.TeamworkBonus > 0
                    ? FarmLocalization.Format("roles.teamwork_bonus", "Teamwork complete: +${0} shared bonus.", roleContribution.TeamworkBonus)
                    : bonus > 0 ? $"Order delivered: +${earned}, including board bonus." : $"Order delivered: +${earned}; +{community.FavorGained} Favor.")
                : Fail(error);
        }

        private FarmSessionCommandResult ExecuteStewardship(FarmSessionIntent intent)
        {
            var values = ReadValues(intent.Payload);
            if (!values.TryGetValue("node", out var nodeText) || !int.TryParse(nodeText, out var nodeIndex)) return Fail("Invalid stewardship command.");
            return plot.MiningSystem != null
                ? plot.MiningSystem.ExecuteHostStewardNode(nodeIndex, intent.RequestedBy)
                : Fail("Stewardship is unavailable.");
        }

        private FarmSessionCommandResult ExecuteProgression(FarmSessionIntent intent)
        {
            var values = ReadValues(intent.Payload);
            if (!values.TryGetValue("specialization", out var specializationText) ||
                !Enum.TryParse(specializationText, true, out FarmSpecialization specialization))
                return Fail("Invalid progression command.");
            if (plot.GameState == null) return Fail("Progression is unavailable.");
            if (!plot.GameState.TrySetSpecialization(specialization, out var error))
                return Fail(string.IsNullOrEmpty(error) ? "Progression request was rejected." : error);
            return new FarmSessionCommandResult(true, FarmLocalization.Format("mastery.specialization.changed", "Specialization committed: {0}.", specialization));
        }

        private FarmSessionCommandResult ExecutePlayerRole(FarmSessionIntent intent)
        {
            var values = ReadValues(intent.Payload);
            if (!values.TryGetValue("role", out var roleText) || !Enum.TryParse(roleText, true, out FarmSpecialization role) || plot.GameState == null)
                return Fail("Invalid co-op role command.");
            return plot.GameState.TrySetCoopRole(intent.RequestedBy, role, out var error)
                ? new FarmSessionCommandResult(true, FarmLocalization.Format("roles.set", "Co-op role set: {0}.", FarmCoopRoleRules.DisplayName(role)))
                : Fail(error);
        }

        private FarmSessionCommandResult ExecuteForecastPlan(FarmSessionIntent intent)
        {
            if (plot.GameState == null) return Fail("Forecast planning is unavailable.");
            return plot.GameState.TryPrepareTomorrowForecastPlan(out var routeKey, out var error)
                ? new FarmSessionCommandResult(true, FarmLocalization.Format("forecast.plan.confirmed", "Forecast plan prepared: {0}.", FarmForecastPlanRules.Description(routeKey)))
                : Fail(error);
        }

        private FarmSessionCommandResult ExecuteRestRecovery(FarmSessionIntent intent)
        {
            if (plot.GameState == null) return Fail("Homestead rest is unavailable.");
            return plot.GameState.TryPrepareEveningTea(out var error)
                ? new FarmSessionCommandResult(true, FarmLocalization.Get("rest.prepared", "Evening Tea prepared. Tomorrow begins with 3 Comfort charges."))
                : Fail(error);
        }

        private static Dictionary<string, string> ReadValues(string payload)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in (payload ?? string.Empty).Split(';'))
            {
                var separator = pair.IndexOf('=');
                if (separator <= 0) continue;
                values[pair[..separator].Trim()] = pair[(separator + 1)..].Trim();
            }
            return values;
        }

        private static FarmSessionCommandResult Fail(string message) => new(false, message);
    }
}
