#define NMLMOD
#if NMLMOD
using NeoModLoader.General;
using NeoModLoader.General.UI.Tab;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using NCMS.Utils;
using System.Collections.Generic;
namespace WorldSphereMod.UI
{
    struct ButtonData
    {
        public PowerToggleAction Action;
        public string Name;
        public string Description;
        public string IconPath;
        public bool IsActive;
        public ButtonData(string Name, string Description, string IconPath, bool IsActive, PowerToggleAction Action)
        {
            this.Name = Name;
            this.Description = Description;
            this.IconPath = IconPath;
            this.IsActive = IsActive;
            this.Action = Action;
        }
    }
    public static class WorldSphereTab
    {
        public static PowersTab Tab;
        public static Sprite ModIcon;
        static GameObject Space;
        static GameObject Line;
        static void CreateTabTools()
        {
            Space = ResourcesFinder.FindResource<GameObject>("_space");
            Line = Object.Instantiate(ResourcesFinder.FindResource<GameObject>("_line"));
            Line.transform.localScale = new Vector3(Line.transform.localScale.x, Line.transform.localScale.y * 6, Line.transform.localScale.z);
        }

        public static void Begin()
        {
            CreateTabTools();
            CreateTab();
            CreateButtons();
        }
        static void AddLine()
        {
            Object.Instantiate(Line).transform.SetParent(Tab.transform);
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
            // CreateToggleButton("InvertedWorld", "ModResources/icon", "Inverted World", "fuck my life", ToggleWorld, Core.savedSettings.InvertedWorld);
            CreateWindowButton("Sprite Settings", "ModResources/icon", "Sprite Settings", "settings about the sprites in the game", "WARNING! THESE ARE EXPENSIVE", new List<ButtonData>()
            {
               new ButtonData("Sprites Rotate To Camera", "will sprites rotate to the camera?", "ModResources/icon", Core.savedSettings.RotateStuffToCamera, ToggleRotations),
               new ButtonData("Advanced Rotations", "sprites will rotate to the camera in a less buggy, but more expensive method!", "ModResources/icon", Core.savedSettings.RotateStuffToCameraAdvanced, ToggleAdvancedRotations)
            }
           );
        }
        static void Toggle3D()
        {
            Core.savedSettings.Is3D = !Core.savedSettings.Is3D;
            Core.SaveSettings();
        }
        static void ToggleRotations(string _)
        {
            Core.savedSettings.RotateStuffToCamera = !Core.savedSettings.RotateStuffToCamera;
            Core.SaveSettings();
        }
        static void ToggleAdvancedRotations(string _)
        {
            Core.savedSettings.RotateStuffToCameraAdvanced = !Core.savedSettings.RotateStuffToCameraAdvanced;
            Core.SaveSettings();
        }
        static void ToggleCamera()
        {
            Core.savedSettings.InvertedCameraMovement = !Core.savedSettings.InvertedCameraMovement;
            Core.SaveSettings();
        }
        static void ToggleWorld()
        {
            Core.savedSettings.InvertedWorld = !Core.savedSettings.InvertedWorld;
            Core.SaveSettings();
        }
        #region Buttons
        static void CreateWindowButton(string ID, string IconPath, string Name, string Description, string WindowDescription, List<ButtonData> Buttons)
        {
            WindowManager.CreateWindow(ID, WindowDescription, Buttons);
            CreateButton(ID, IconPath, Name, Description, delegate () { WindowManager.OpenWindow(ID); });
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
        #endregion
      }
    static class WindowManager
    {
        public static Dictionary<string, PowerWindow> windows = new Dictionary<string, PowerWindow>();
        public static void CreateWindow(string id, string title, List<ButtonData> Buttons)
        {
            ScrollWindow window;
            GameObject content;
            window = Windows.CreateNewWindow(id, title);

            GameObject scrollView = GameObject.Find($"/Canvas Container Main/Canvas - Windows/windows/{window.name}/Background/Scroll View");
            content = GameObject.Find($"/Canvas Container Main/Canvas - Windows/windows/{window.name}/Background/Scroll View/Viewport/Content");
            if (content != null)
            {
                windows.Add(id, scrollView.AddComponent<PowerWindow>());
                scrollView.GetComponent<PowerWindow>().init(id, content, Buttons);
                scrollView.gameObject.SetActive(true);
            }
        }
        public static void OpenWindow(string ID)
        {
            windows[ID].openWindow();
        }
    }
    class PowerWindow : MonoBehaviour
    {
        private GameObject contents;
        string ID;
        public void init(string id, GameObject content, List<ButtonData> Buttons)
        {
            ID = id;
            contents = content;
            VerticalLayoutGroup layoutGroup = contents.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childScaleHeight = true;
            layoutGroup.childScaleWidth = true;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.spacing = 50;
            LoadInputOptions(Buttons);
        }
        public void openWindow()
        {
            Windows.ShowWindow(ID);
        }
        private void LoadInputOptions(List<ButtonData> Buttons)
        {
            contents.GetComponent<RectTransform>().sizeDelta += new Vector2(0, Buttons.Count * 125);
            foreach (var data in Buttons)
            {
                GodPower power = AssetManager.powers.add(new GodPower()
                {
                    id = data.Name,
                    name = data.Name,
                    toggle_name = data.Name,
                    toggle_action = data.Action
                });
                LM.AddToCurrentLocale(power.name.Underscore(), power.name);
                LM.AddToCurrentLocale($"{power.name.Underscore()}_description", data.Description);
                PlayerConfig.dict.Add(data.Name, new PlayerOptionData(data.Name));
                PowerButton activeButton = PowerButtonCreator.CreateToggleButton(
                    $"{data.Name}",
                    Resources.Load<Sprite>(data.IconPath),
                    contents.transform
                );
                PlayerConfig.dict[data.Name].boolVal = data.IsActive;
                activeButton.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(64, 64);
            }
            PowerButtonSelector.instance.checkToggleIcons();
        }
    }
}
#endif