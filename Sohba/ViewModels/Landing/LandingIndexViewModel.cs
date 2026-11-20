using System.Collections.Generic;

namespace Sohba.ViewModels.Landing
{
    /// <summary>
    /// Auth state for the public landing page.
    /// Section copy and per-section data live in the section partials
    /// under Views/Landing/Partials — only what the controller knows goes here.
    /// </summary>
    public class LandingIndexViewModel
    {
        public bool IsAuthenticated { get; set; }

        public string UserName { get; set; } = "User";
    }
}
