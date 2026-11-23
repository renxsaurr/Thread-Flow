using ThreadFlow.WinForms.ApiClient;

namespace ThreadFlow.WinForms
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            using var welcomeForm = new WelcomeForm();
            if (welcomeForm.ShowDialog() != DialogResult.OK || ApiSession.Current is null)
                return;

            Application.Run(CreateWorkspaceForCurrentUser());
        }

        /// <summary>
        /// Opens the workspace that best matches the signed-in account. Sales clerks go
        /// straight to the point-of-sale screen; tenant admins get their own admin workspace.
        /// </summary>
        internal static Form CreateWorkspaceForCurrentUser()
        {
            var user = ApiSession.Current?.User
                ?? throw new InvalidOperationException("Sign in before opening the workspace.");

            if (user.IsPlatformAdmin)
                return new PlatformAdminForm();

            if (ApiSession.HasRole("TenantAdmin"))
                return new TenantAdminForm();

            if (ApiSession.HasRole("InventoryPurchasingStaff"))
                return new InventoryPurchasingForm();

            if (ApiSession.HasRole("SalesClerk") && !ApiSession.HasRole("TenantAdmin"))
                return new SalesForm();

            return new TenantOwnerManagerForm();
        }
    }
}
