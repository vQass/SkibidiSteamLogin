namespace SkibidiSteamLogin.Core.Enums
{
    public enum AuthGuardType
    {
        Unknown = 0,
        None = 1,
        EmailCode = 2,
        DeviceCode = 3,
        DeviceConfirmation = 4,
        EmailConfirmation = 5,
        MachineToken = 6,
        LegacyMachineAuth = 7
    }
}
