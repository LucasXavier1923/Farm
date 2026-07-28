namespace FarmPrototype.Farming
{
    /// <summary>
    /// Shared policy boundary for a future transport. It is intentionally free of
    /// UI and persistence: a host snapshot owns state; a peer asks for actions.
    /// </summary>
    public enum FarmPermission
    {
        FieldWork,
        Commerce,
        PlaceBuildable,
        MoveBuildable,
        ReclaimBuildable,
        SpendFarmFunds,
        ManageSession
    }

    public static class FarmPermissionPolicy
    {
        public static bool IsManagement(FarmPermission permission) => permission is
            FarmPermission.PlaceBuildable or FarmPermission.MoveBuildable or
            FarmPermission.ReclaimBuildable or FarmPermission.SpendFarmFunds or
            FarmPermission.ManageSession;

        public static bool CanMutateLocally(FarmPermission permission)
        {
            if (FarmSessionTime.Role is FarmSessionRole.Solo or FarmSessionRole.Host)
                return FarmSessionTime.IsSimulationAuthority;
            return false;
        }

        public static string DenialMessage(FarmPermission permission) => IsManagement(permission)
            ? FarmLocalization.Get("permission.host_management", "Only the farm host can manage buildings, funds, or session settings.")
            : FarmLocalization.Get("backend.peer.awaiting_host", "Waiting for host confirmation.");
    }
}
