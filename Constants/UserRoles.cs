namespace VEPS_Plus.Constants
{
    public static class UserRoles
    {
        public const string User = "user";
        public const string Engineer1 = "engineer1";
        public const string Engineer2 = "engineer2";
        public const string Engineer3 = "engineer3";
        public const string EngineerP = "engineerp";
        public const string Boss = "boss";
        public const string Leader = "Leader";

        public static string GetDisplayName(string role)
        {
            return role switch
            {
                User => "Пользователь",
                Engineer1 => "Инженер 1 категории",
                Engineer2 => "Инженер 2 категории",
                Engineer3 => "Инженер 3 категории",
                EngineerP => "Инженер-программист",
                Boss => "Руководитель",
                Leader => "Лидер",
                _ => role
            };
        }

        public static bool HasPermission(string userRole, string requiredRole)
        {
            var roleHierarchy = new Dictionary<string, int>
            {
                { User, 1 },
                { Engineer3, 2 },
                { Engineer2, 3 },
                { Engineer1, 4 },
                { EngineerP, 5 },
                { Leader, 6 },
                { Boss, 7 }
            };

            if (roleHierarchy.TryGetValue(userRole, out int userLevel) && 
                roleHierarchy.TryGetValue(requiredRole, out int requiredLevel))
            {
                return userLevel >= requiredLevel;
            }

            return false;
        }
    }
}
