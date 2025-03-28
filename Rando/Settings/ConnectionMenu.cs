using System;
using MenuChanger;
using MenuChanger.Extensions;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using RandomizerMod.Menu;
using UnityEngine;

namespace FiveKnights.Rando
{
    public class ConnectionMenu 
    {
        // Top-level definitions
        internal static ConnectionMenu Instance { get; private set; }
        private readonly SmallButton pageRootButton;

        // Menu page and elements
        private readonly MenuPage mainPage;
        private MenuElementFactory<RandoSettings> topLevelElementFactory;

        public static void Hook()
        {
            RandomizerMenuAPI.AddMenuPage(ConstructMenu, HandleButton);
            MenuChangerMod.OnExitMainMenu += () => Instance = null;
        }

        private static bool HandleButton(MenuPage landingPage, out SmallButton button)
        {
            button = Instance.pageRootButton;
            button.Text.color = RandoManager.Settings.Enabled ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
            return true;
        }

        private static void ConstructMenu(MenuPage connectionPage)
        {
            Instance = new(connectionPage);
        }

        private ConnectionMenu(MenuPage connectionPage)
        {
            // Define connection page
            mainPage = new MenuPage("mainPage", connectionPage);
            
            topLevelElementFactory = new(mainPage, RandoManager.Settings);
            VerticalItemPanel topLevelPanel = new(mainPage, new Vector2(0, 400), 60, true, topLevelElementFactory.Elements);
            topLevelElementFactory.ElementLookup[nameof(RandoSettings.Enabled)].SelfChanged += EnableSwitch;
            topLevelPanel.ResetNavigation();
            topLevelPanel.SymSetNeighbor(Neighbor.Down, mainPage.backButton);
            topLevelPanel.SymSetNeighbor(Neighbor.Up, mainPage.backButton);
            pageRootButton = new SmallButton(connectionPage, "Pale Court");
            pageRootButton.AddHideAndShowEvent(connectionPage, mainPage);
        }

        // Define parameter changes
        private void EnableSwitch(IValueElement obj)
        {
            pageRootButton.Text.color = RandoManager.Settings.Enabled ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
        }

        private void SetButtonColor(SmallButton target, Func<bool> condition)
        {
            target.Parent.BeforeShow += () =>
            {
                target.Text.color = condition() ? Colors.TRUE_COLOR : Colors.FALSE_COLOR;
            };
        }

        // Apply proxy settings
        public void Disable()
        {
            IValueElement elem = topLevelElementFactory.ElementLookup[nameof(RandoSettings.Enabled)];
            elem.SetValue(false);
        }

        public void Apply(RandoSettings settings)
        {
            topLevelElementFactory.SetMenuValues(settings);     
        }
    }
}