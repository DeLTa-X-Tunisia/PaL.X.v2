namespace PaL.X.Shared.Enums
{
    public enum UserStatus
    {
        Online = 0,         // En ligne
        Offline = 1,        // Hors ligne
        Away = 2,           // Absent (15 min inactivité)
        BRB = 3,            // BRB (Be Right Back)
        DoNotDisturb = 4,   // Ne pas déranger
        Busy = 5,           // Occupé
        InCall = 6          // En appel
    }

    public static class UserStatusExtensions
    {
        public static string GetDisplayName(this UserStatus status)
        {
            return status switch
            {
                UserStatus.Online => "En ligne",
                UserStatus.Offline => "Hors ligne",
                UserStatus.Away => "Absent",
                UserStatus.BRB => "BRB",
                UserStatus.DoNotDisturb => "Ne pas déranger",
                UserStatus.Busy => "Occupé",
                UserStatus.InCall => "En appel",
                _ => "Inconnu"
            };
        }

        public static string GetIconPath(this UserStatus status)
        {
            return status switch
            {
                UserStatus.Online => "icon/status/en_ligne.ico",
                UserStatus.Offline => "icon/status/hors_ligne.ico",
                UserStatus.Away => "icon/status/absent.ico",
                UserStatus.BRB => "icon/status/brb.ico",
                UserStatus.DoNotDisturb => "icon/status/dnd.ico",
                UserStatus.Busy => "icon/status/occupé.ico",
                UserStatus.InCall => "Voice/en_appel.png",
                _ => "icon/status/hors_ligne.ico"
            };
        }
    }
}
