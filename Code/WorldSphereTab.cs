using NeoModLoader.General;
using NeoModLoader.General.UI.Tab;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace WorldSphereMod
{
    public static class WorldSphereTab
    {
        public static PowersTab Tab;
        public static Sprite ModIcon;
        static GameObject Space;
        static GameObject Line;
        static GameObject Text;
        static void CreateTabTools()
        {
            Space = ResourcesFinder.FindResource<GameObject>("_space");
            Line = Object.Instantiate(ResourcesFinder.FindResource<GameObject>("_line"));
            Line.transform.localScale = new Vector3(Line.transform.localScale.x, Line.transform.localScale.y*6, Line.transform.localScale.z);
            Text = Object.Instantiate(Space);
            Text.AddComponent<Text>();
        }
        
        public static void Init()
        {
            CreateTabTools();
            CreateTab();
            CreateButtons();
        }
        static void AddLine()
        {
            Object.Instantiate(Line).transform.SetParent(Tab.transform);
        }
        // not finushed
        static void AddText(string text)
        {
            GameObject obj = Object.Instantiate(Text);
            obj.transform.SetParent(Tab.transform);
            obj.GetComponent<Text>().text = text;
        }

        static void CreateTab()
        {
            ModIcon = Resources.Load<Sprite>("ModResources/icon");
            Tab = TabManager.CreateTab("WorldSphereMod", "WorldSphereMod", "A Mod that makes your game 3D!", ModIcon, "Created by Lord Melvin");
        }
        static void CreateButtons()
        {
            CreateToggleButton("Is3D", "ModResources/icon", "Is 3D", "This is ONLY applied once you reload the world", Toggle3D, Core.savedSettings.Is3D);
            CreateToggleButton("InvertedCamera", "ModResources/icon", "Inverted Camera", "if true, the horizontal and vertical movement of the camera will be swapped, if 3D", ToggleCamera, Core.savedSettings.InvertedCameraMovement);
        }
        static void Toggle3D()
        {
            Core.savedSettings.Is3D = !Core.savedSettings.Is3D;
            Core.SaveSettings();
        }
        static void ToggleCamera()
        {
            Core.savedSettings.InvertedCameraMovement = !Core.savedSettings.InvertedCameraMovement;
            Core.SaveSettings();
        }
        static void CreateButton(string ID, string IconPath, string name, string Description, UnityAction Action)
        {
            LM.AddToCurrentLocale(name.Underscore(), name);
            LM.AddToCurrentLocale($"{name.Underscore()}_description", Description);
            PowerButton button = PowerButtonCreator.CreateSimpleButton(ID, Action, Resources.Load<Sprite>(IconPath));
            PowerButtonCreator.AddButtonToTab(button, Tab);
        }
        static void CreateToggleButton(string ID, string IconPath, string name, string Description, UnityAction toggleAction, bool Enabled)
        {
            GodPower power = AssetManager.powers.add(new GodPower()
            {
                id = ID,
                name = name,
                toggle_name = ID,
                toggle_action = delegate
                {
                    toggleAction();
                    PlayerConfig.dict[ID].boolVal = !PlayerConfig.dict[ID].boolVal;
                    PowerButtonSelector.instance.checkToggleIcons();
                }
            });
            LM.AddToCurrentLocale(name.Underscore(), name);
            LM.AddToCurrentLocale($"{name.Underscore()}_description", Description);
            PlayerConfig.dict.Add(ID, new PlayerOptionData(ID));
            var Button = PowerButtonCreator.CreateToggleButton(
                ID,
                Resources.Load<Sprite>(IconPath),
                null,
                default,
                true
            );
            PowerButtonCreator.AddButtonToTab(Button, Tab);
            if (!Enabled)
            {
                PlayerConfig.dict[ID].boolVal = false;
                Button.checkToggleIcon();
            }
        }
    }
}
