using HarmonyLib;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Library;
using StrmAssistant.Common;
using System;
using System.Linq;
using System.Reflection;
using static StrmAssistant.Mod.PatchManager;

namespace StrmAssistant.Mod
{
    public class EnforceLibraryOrder : PatchBase<EnforceLibraryOrder>
    {
        private static MethodInfo _getUserViews;
        private static IUserManager _userManager;

        public EnforceLibraryOrder()
        {
            Initialize();

            if (Plugin.Instance.ExperienceEnhanceStore.GetOptions().UIFunctionOptions.EnforceLibraryOrder)
            {
                Patch();
            }
        }

        protected override void OnInitialize()
        {
            var embyServerImplementationsAssembly = Assembly.Load("Emby.Server.Implementations");
            var userViewManager =
                embyServerImplementationsAssembly.GetType("Emby.Server.Implementations.Library.UserViewManager");
            _getUserViews = userViewManager.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == "GetUserViews" &&
                                     (m.GetParameters().Length == 3 || m.GetParameters().Length == 4));
            
            // 获取 UserManager 以便在 Prefix 中使用
            _userManager = Plugin.Instance.ApplicationHost.Resolve<IUserManager>();
        }

        protected override void Prepare(bool apply)
        {
            PatchUnpatch(PatchTracker, apply, _getUserViews, nameof(GetUserViewsPrefix));
        }

        [HarmonyPrefix]
        private static bool GetUserViewsPrefix(UserViewQuery query)
        {
            try
            {
                // 从 query 中获取 UserId
                var userIdProperty = query.GetType().GetProperty("UserId");
                if (userIdProperty != null)
                {
                    var userId = userIdProperty.GetValue(query) as string;
                    if (!string.IsNullOrEmpty(userId) && _userManager != null)
                    {
                        var user = _userManager.GetUserById(userId);
                        if (user != null)
                        {
                            user.Configuration.OrderedViews = LibraryApi.AdminOrderedViews;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Plugin.Instance.DebugMode)
                {
                    Plugin.Instance.Logger.Debug($"EnforceLibraryOrder GetUserViewsPrefix error: {ex.Message}");
                }
            }

            return true;
        }
    }
}
