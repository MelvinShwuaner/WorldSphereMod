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
        public bool CanBeFalse;
        public ButtonData(string Name, string Description, string IconPath, bool IsActive, PowerToggleAction Action, bool CanBeFalse = true)
        {
            this.Name = Name;
            this.Description = Description;
            this.IconPath = IconPath;
            this.IsActive = IsActive;
            this.Action = Action;
            this.CanBeFalse = CanBeFalse;
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
            ModIcon = Resources.Load<Sprite>("WorldSphereMod/icon");
            Tab = TabManager.CreateTab("WorldSphereMod", "WorldSphereMod", "A Mod that makes your game 3D!", ModIcon, "Created by Lord Melvin");
        }
        public static Text addText(string window, string textString, GameObject parent, int sizeFont, Vector3 pos, Vector2 addSize = default(Vector2))
        {
            GameObject textRef = GameObject.Find($"/Canvas Container Main/Canvas - Windows/windows/" + window + "/Background/Title");
            GameObject textGo = Object.Instantiate(textRef, parent.transform);
            textGo.SetActive(true);

            var textComp = textGo.GetComponent<Text>();
            textComp.fontSize = sizeFont;
            textComp.resizeTextMaxSize = sizeFont;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.position = new Vector3(0, 0, 0);
            textRect.localPosition = pos + new Vector3(0, -50, 0);
            textRect.sizeDelta = new Vector2(100, 100) + addSize;
            textGo.AddComponent<GraphicRaycaster>();
            textComp.text = textString;

            return textComp;
        }
        static Slider GenerateSlider(string Name,float Min, float Max, float Current, UnityAction<float> Func, string Window)
        {
            GameObject sliderGO = new GameObject(Name, typeof(Slider), typeof(Image));
            Transform Parent = WindowManager.windows[Window].Object.transform;
            sliderGO.transform.SetParent(Parent, false);
            RectTransform rt = sliderGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(25, 5);
            rt.anchoredPosition = new Vector2(0, 0);
            Slider slider = sliderGO.GetComponent<Slider>();
            slider.minValue = Min;
            slider.maxValue = Max;
            slider.value = Current;
            slider.onValueChanged.AddListener(Func);

            GameObject trackGO = new GameObject("Track");
            trackGO.transform.SetParent(sliderGO.transform, false);
            Image trackImage = trackGO.AddComponent<Image>();
            RectTransform trackRect = trackGO.GetComponent<RectTransform>();
            trackRect.sizeDelta = new Vector2(100, 2);
            trackRect.anchoredPosition = Vector2.zero;
            trackImage.color = Color.gray;

            GameObject handleAreaGO = new GameObject("Handle Slide Area");
            handleAreaGO.transform.SetParent(sliderGO.transform, false);
            RectTransform handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRect.sizeDelta = new Vector2(100, 0);

            GameObject handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            Image handleImage = handleGO.AddComponent<Image>();
            RectTransform handleRect = handleGO.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(10, 10);
            handleImage.color = Color.white;

            slider.targetGraphic = handleImage;
            slider.handleRect = handleGO.GetComponent<RectTransform>();

            Text textGO = addText(Window, $"{Name} : {Current}", sliderGO, 10, new Vector3(0, -2));
            slider.onValueChanged.AddListener((float x) => textGO.text = $"{Name} : {x}");

            return slider;
        }
        static void CreateButtons()
        {
            CreateToggleButton("Is3D", "WorldSphereMod/icon", "Is 3D", "This is ONLY applied once you reload the world", Toggle3D, Core.savedSettings.Is3D);
            // CreateToggleButton("InvertedWorld", "WorldSphereMod/icon", "Inverted World", "fuck my life", ToggleWorld, Core.savedSettings.InvertedWorld);
            CreateWindowButton("Sprite Settings", "WorldSphereMod/icon", "Sprite Settings", "settings about the sprites in the game", "WARNING! THESE ARE EXPENSIVE", new List<ButtonData>()
            {
               new ButtonData("Sprites Rotate To Camera", "will sprites rotate to the camera?", "WorldSphereMod/icon", Core.savedSettings.RotateStuffToCamera, ToggleRotations),
               new ButtonData("Advanced Rotations", "sprites will rotate to the camera in a less buggy, but more expensive method!", "WorldSphereMod/icon", Core.savedSettings.RotateStuffToCameraAdvanced, ToggleAdvancedRotations)
            }
            );
            CreateWindowButton("Camera Settings", "WorldSphereMod/icon", "Camera Settings", "Settings for the 3D Camera", "", new List<ButtonData>()
            {
                new ButtonData("Inverted Camera", "if true, the horizontal and vertical movement of the camera will be swapped", "WorldSphereMod/icon", Core.savedSettings.InvertedCameraMovement, ToggleCamera)
            });
            GenerateSlider("Render Distance", 1, 20, Core.savedSettings.RenderRange, (float val) => { Core.savedSettings.RenderRange = val; Core.SaveSettings(); }, "Camera Settings");
            CreateWindowButton("World Shape", "WorldSphereMod/icon", "World Shape", "The Shape Of The World", "this will only apply when you regenerate the world!", new List<ButtonData>()
            {
                new ButtonData("CylindricalShape", "Makes the World a Cylinder", "WorldSphereMod/icon", Core.savedSettings.CurrentShape == 0, SetShape, false),
                new ButtonData("FlatShape", "Makes the World Flat", "WorldSphereMod/icon", Core.savedSettings.CurrentShape == 1, SetShape, false)
            });
        }
        static Dictionary<string, int> WorldShapes = new Dictionary<string, int>()
        {
            { "CylindricalShape", 0 },
            { "FlatShape", 1 }
        };
        static void SetShape(string ID)
        {
            Core.savedSettings.CurrentShape = WorldShapes[ID];
            foreach(string shape in WorldShapes.Keys)
            {
                if(shape != ID)
                {
                    PlayerOptionData tData = PlayerConfig.dict[shape];
                    tData.boolVal = false;
                }
                PowerButtonSelector.instance.checkToggleIcons();
            }
            Core.SaveSettings();
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
        static void ToggleCamera(string _)
        {
            Core.savedSettings.InvertedCameraMovement = !Core.savedSettings.InvertedCameraMovement;
            Core.SaveSettings();
        }
        #region Buttons
        static PowerWindow CreateWindowButton(string ID, string IconPath, string Name, string Description, string WindowDescription, List<ButtonData> Buttons)
        {
            WindowManager.CreateWindow(ID, WindowDescription, Buttons);
            CreateButton(ID, IconPath, Name, Description, delegate () { WindowManager.OpenWindow(ID); });
            return WindowManager.windows[ID];
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
        public GameObject Object;
        string ID;
        public void init(string id, GameObject content, List<ButtonData> Buttons)
        {
            ID = id;
            Object = content;
            VerticalLayoutGroup layoutGroup = Object.AddComponent<VerticalLayoutGroup>();
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
        static void toggleOption(string pPower)
        {
            GodPower godPower2 = AssetManager.powers.get(pPower);
            WorldTip.instance.showToolbarText(godPower2);
            if (!PlayerConfig.dict.TryGetValue(godPower2.toggle_name, out var value2))
            {
                value2 = new PlayerOptionData(godPower2.toggle_name)
                {
                    boolVal = false
                };
                PlayerConfig.instance.data.add(value2);
            }

            value2.boolVal = true;
            if (value2.boolVal && godPower2.map_modes_switch)
            {
                AssetManager.powers.disableAllOtherMapModes(pPower);
            }

            PlayerConfig.saveData();
        }
        private void LoadInputOptions(List<ButtonData> Buttons)
        {
            Object.GetComponent<RectTransform>().sizeDelta += new Vector2(0, Buttons.Count * 125);
            foreach (var data in Buttons)
            {
                GodPower power = AssetManager.powers.add(new GodPower()
                {
                    id = data.Name,
                    name = data.Name,
                    toggle_name = data.Name,
                    toggle_action = data.Action
                });
                if (!data.CanBeFalse)
                {
                    power.toggle_action = (PowerToggleAction)System.Delegate.Combine(power.toggle_action, new PowerToggleAction(toggleOption));
                }
                LM.AddToCurrentLocale(power.name.Underscore(), power.name);
                LM.AddToCurrentLocale($"{power.name.Underscore()}_description", data.Description);
                PlayerConfig.dict.Add(data.Name, new PlayerOptionData(data.Name));
                PowerButton activeButton = PowerButtonCreator.CreateToggleButton(
                    $"{data.Name}",
                    Resources.Load<Sprite>(data.IconPath),
                    Object.transform,
                    default,
                    !data.CanBeFalse
                );
                PlayerConfig.dict[data.Name].boolVal = data.IsActive;
                activeButton.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(64, 64);
            }
            PowerButtonSelector.instance.checkToggleIcons();
        }
    }
}