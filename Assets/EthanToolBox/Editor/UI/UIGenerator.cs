using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace EthanToolBox.Editor.UI
{
    public static class UIGenerator
    {
        private static Color PanelColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        private static Color ButtonColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        private static Color TextColor = Color.white;
        private static float ButtonHeight = 60f;
        private static float ButtonWidth = 300f;
        private static float Spacing = 20f;

        [MenuItem("GameObject/EthanToolBox/UI/Smart List View", false, 10)]
        public static void CreateSmartListView(MenuCommand menuCommand)
        {
            GameObject root = new GameObject("Smart List View");
            GameObjectUtility.SetParentAndAlign(root, menuCommand.context as GameObject);
            EnsureCanvas(root);

            RectTransform rootRT = root.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(300, 400);

            Image rootImage = root.AddComponent<Image>();
            rootImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            ScrollRect scrollRect = root.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 20;

            GameObject viewport = new GameObject("Viewport");
            GameObjectUtility.SetParentAndAlign(viewport, root);
            RectTransform viewportRT = viewport.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.sizeDelta = Vector2.zero;
            viewportRT.pivot = new Vector2(0, 1);

            Image viewportImage = viewport.AddComponent<Image>();
            Mask viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            scrollRect.viewport = viewportRT;

            GameObject content = new GameObject("Content");
            GameObjectUtility.SetParentAndAlign(content, viewport);
            RectTransform contentRT = content.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.sizeDelta = new Vector2(0, 300);

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 5;
            vlg.padding = new RectOffset(5, 5, 5, 5);

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRT;

            Undo.RegisterCreatedObjectUndo(root, "Create Smart List View");
            Selection.activeObject = root;
        }

        [MenuItem("GameObject/EthanToolBox/UI/Main Menu", false, 10)]
        public static void CreateMainMenu(MenuCommand menuCommand)
        {
            GameObject root = CreateFullScreenPanel("Main Menu", menuCommand.context as GameObject, PanelColor);
            
            // Controller
            root.AddComponent<EthanToolBox.UI.Scripts.MainMenuController>();

            // Title
            CreateTitle("Game Title", root.transform);

            // Buttons Container
            GameObject buttons = CreateCenteredContainer("Buttons", root.transform);
            VerticalLayoutGroup vlg = buttons.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.spacing = Spacing;
            vlg.childAlignment = TextAnchor.MiddleCenter;

            // Buttons
            CreateTMPButton("Play Button", "Play", buttons.transform, (btn) => {});
            CreateTMPButton("Quit Button", "Quit", buttons.transform, null);

            Undo.RegisterCreatedObjectUndo(root, "Create Main Menu");
            Selection.activeObject = root;
        }

        [MenuItem("GameObject/EthanToolBox/UI/Pause Menu", false, 10)]
        public static void CreatePauseMenu(MenuCommand menuCommand)
        {
            GameObject root = CreateFullScreenPanel("Pause Menu", menuCommand.context as GameObject, new Color(0, 0, 0, 0.85f));
            
            // Controller
            EthanToolBox.UI.Scripts.PauseMenuController controller = root.AddComponent<EthanToolBox.UI.Scripts.PauseMenuController>();
            controller.pauseMenuUI = root;

            // Title
            CreateTitle("Paused", root.transform);

            // Buttons Container
            GameObject buttons = CreateCenteredContainer("Buttons", root.transform);
            VerticalLayoutGroup vlg = buttons.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.spacing = Spacing;
            vlg.childAlignment = TextAnchor.MiddleCenter;

            // Buttons
            CreateTMPButton("Resume Button", "Resume", buttons.transform, null);
            CreateTMPButton("Menu Button", "Main Menu", buttons.transform, null);
            CreateTMPButton("Quit Button", "Quit", buttons.transform, null);

            Undo.RegisterCreatedObjectUndo(root, "Create Pause Menu");
            Selection.activeObject = root;
        }

        [MenuItem("GameObject/EthanToolBox/UI/Settings Panel", false, 10)]
        public static void CreateSettingsPanel(MenuCommand menuCommand)
        {
            GameObject root = CreateFullScreenPanel("Settings Panel", menuCommand.context as GameObject, PanelColor);

            // Controller
            EthanToolBox.UI.Scripts.SettingsController controller = root.AddComponent<EthanToolBox.UI.Scripts.SettingsController>();

            // Title
            CreateTitle("Settings", root.transform);

            // Content Container
            GameObject content = CreateCenteredContainer("Content", root.transform);
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = false;
            vlg.childControlWidth = false; // Let elements decide their width
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.spacing = 30;
            vlg.childAlignment = TextAnchor.MiddleCenter;

            // Elements
            controller.musicSlider = CreateTMPSlider("Music Volume", content.transform);
            controller.sfxSlider = CreateTMPSlider("SFX Volume", content.transform);
            controller.qualityDropdown = CreateTMPDropdown("Quality", content.transform);

            // Back Button (Optional, usually needed)
            CreateTMPButton("Back Button", "Back", content.transform, null);

            Undo.RegisterCreatedObjectUndo(root, "Create Settings Panel");
            Selection.activeObject = root;
        }

        [MenuItem("GameObject/EthanToolBox/UI/Loading Screen", false, 10)]
        public static void CreateLoadingScreen(MenuCommand menuCommand)
        {
            GameObject root = CreateFullScreenPanel("Loading Screen", menuCommand.context as GameObject, Color.black);

            // Controller
            EthanToolBox.UI.Scripts.LoadingScreenController controller = root.AddComponent<EthanToolBox.UI.Scripts.LoadingScreenController>();
            controller.panel = root;

            // Progress Bar Container (Bottom Center)
            GameObject container = new GameObject("Container");
            GameObjectUtility.SetParentAndAlign(container, root);
            RectTransform containerRT = container.AddComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0.5f, 0.2f);
            containerRT.anchorMax = new Vector2(0.5f, 0.2f);
            containerRT.pivot = new Vector2(0.5f, 0.5f);
            containerRT.sizeDelta = new Vector2(600, 100);

            // Slider
            GameObject sliderObj = new GameObject("Progress Bar", typeof(RectTransform));
            GameObjectUtility.SetParentAndAlign(sliderObj, container);
            RectTransform sliderRT = sliderObj.AddComponent<RectTransform>();
            sliderRT.anchorMin = new Vector2(0, 0.5f);
            sliderRT.anchorMax = new Vector2(1, 0.5f);
            sliderRT.sizeDelta = new Vector2(0, 20);
            sliderRT.anchoredPosition = Vector2.zero;

            Slider slider = sliderObj.AddComponent<Slider>();
            controller.progressBar = slider;

            // Background
            GameObject bg = new GameObject("Background", typeof(Image));
            GameObjectUtility.SetParentAndAlign(bg, sliderObj);
            bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
            RectTransform bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            GameObjectUtility.SetParentAndAlign(fillArea, sliderObj);
            RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.sizeDelta = Vector2.zero;

            // Fill
            GameObject fill = new GameObject("Fill", typeof(Image));
            GameObjectUtility.SetParentAndAlign(fill, fillArea);
            fill.GetComponent<Image>().color = new Color(0f, 0.7f, 1f); // Nice Blue
            RectTransform fillRT = fill.GetComponent<RectTransform>();
            fillRT.sizeDelta = Vector2.zero;

            slider.fillRect = fillRT;
            slider.direction = Slider.Direction.LeftToRight;
            slider.value = 0;

            // Text
            GameObject textObj = new GameObject("Progress Text");
            GameObjectUtility.SetParentAndAlign(textObj, container);
            RectTransform textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0.5f, 1f);
            textRT.anchorMax = new Vector2(0.5f, 1f);
            textRT.pivot = new Vector2(0.5f, 0);
            textRT.anchoredPosition = new Vector2(0, 20);
            textRT.sizeDelta = new Vector2(200, 50);

            TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
            txt.text = "0%";
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.fontSize = 36;
            
            controller.progressText = txt;

            Undo.RegisterCreatedObjectUndo(root, "Create Loading Screen");
            Selection.activeObject = root;
        }

        // --- Helpers ---

        private static GameObject CreateFullScreenPanel(string name, GameObject context, Color color)
        {
            GameObject root = new GameObject(name);
            GameObjectUtility.SetParentAndAlign(root, context);
            EnsureCanvas(root);

            RectTransform rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.sizeDelta = Vector2.zero;

            Image rootImage = root.AddComponent<Image>();
            rootImage.color = color;

            return root;
        }

        private static GameObject CreateCenteredContainer(string name, Transform parent)
        {
            GameObject container = new GameObject(name);
            GameObjectUtility.SetParentAndAlign(container, parent.gameObject);
            RectTransform rt = container.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(500, 500); // Arbitrary large size, layout group will control children
            return container;
        }

        private static void CreateTitle(string text, Transform parent)
        {
            GameObject title = new GameObject("Title");
            GameObjectUtility.SetParentAndAlign(title, parent.gameObject);
            RectTransform titleRT = title.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.sizeDelta = new Vector2(0, 150);
            titleRT.anchoredPosition = new Vector2(0, -50);

            TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = text;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 72;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = TextColor;
        }

        private static void CreateTMPButton(string name, string text, Transform parent, System.Action<Button> callback)
        {
            GameObject btnObj = new GameObject(name);
            GameObjectUtility.SetParentAndAlign(btnObj, parent.gameObject);
            
            Image img = btnObj.AddComponent<Image>();
            img.color = ButtonColor;

            Button btn = btnObj.AddComponent<Button>();
            
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.minHeight = ButtonHeight;
            le.minWidth = ButtonWidth;
            le.preferredHeight = ButtonHeight;
            le.preferredWidth = ButtonWidth;

            GameObject textObj = new GameObject("Text");
            GameObjectUtility.SetParentAndAlign(textObj, btnObj);
            RectTransform textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = TextColor;
            txt.fontSize = 24;

            callback?.Invoke(btn);
        }

        private static Slider CreateTMPSlider(string label, Transform parent)
        {
            GameObject container = new GameObject(label + " Container");
            GameObjectUtility.SetParentAndAlign(container, parent.gameObject);
            VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.spacing = 5;

            LayoutElement leContainer = container.AddComponent<LayoutElement>();
            leContainer.minWidth = ButtonWidth;

            // Label
            GameObject txtObj = new GameObject("Label");
            GameObjectUtility.SetParentAndAlign(txtObj, container);
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.color = TextColor;
            txt.fontSize = 20;
            txt.alignment = TextAlignmentOptions.Left;

            // Slider
            GameObject sliderObj = new GameObject("Slider", typeof(RectTransform));
            GameObjectUtility.SetParentAndAlign(sliderObj, container);
            LayoutElement leSlider = sliderObj.AddComponent<LayoutElement>();
            leSlider.minHeight = 30;
            leSlider.preferredHeight = 30;

            Slider slider = sliderObj.AddComponent<Slider>();
            
            // Background
            GameObject bg = new GameObject("Background", typeof(Image));
            GameObjectUtility.SetParentAndAlign(bg, sliderObj);
            bg.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f);
            RectTransform bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 0.25f);
            bgRT.anchorMax = new Vector2(1, 0.75f);
            bgRT.sizeDelta = Vector2.zero;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            GameObjectUtility.SetParentAndAlign(fillArea, sliderObj);
            RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0, 0.25f);
            fillAreaRT.anchorMax = new Vector2(1, 0.75f);
            fillAreaRT.sizeDelta = new Vector2(-20, 0);

            // Fill
            GameObject fill = new GameObject("Fill", typeof(Image));
            GameObjectUtility.SetParentAndAlign(fill, fillArea);
            fill.GetComponent<Image>().color = new Color(0f, 0.7f, 1f);
            RectTransform fillRT = fill.GetComponent<RectTransform>();
            fillRT.sizeDelta = Vector2.zero;

            // Handle Area
            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            GameObjectUtility.SetParentAndAlign(handleArea, sliderObj);
            RectTransform handleAreaRT = handleArea.GetComponent<RectTransform>();
            handleAreaRT.sizeDelta = new Vector2(-20, 0);
            handleAreaRT.anchorMin = new Vector2(0, 0);
            handleAreaRT.anchorMax = new Vector2(1, 1);

            // Handle
            GameObject handle = new GameObject("Handle", typeof(Image));
            GameObjectUtility.SetParentAndAlign(handle, handleArea);
            handle.GetComponent<Image>().color = Color.white;
            RectTransform handleRT = handle.GetComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(20, 0);

            slider.fillRect = fillRT;
            slider.handleRect = handleRT;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;

            return slider;
        }

        private static TMP_Dropdown CreateTMPDropdown(string label, Transform parent)
        {
            GameObject container = new GameObject(label + " Container");
            GameObjectUtility.SetParentAndAlign(container, parent.gameObject);
            VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.spacing = 5;

            LayoutElement leContainer = container.AddComponent<LayoutElement>();
            leContainer.minWidth = ButtonWidth;

            // Label
            GameObject txtObj = new GameObject("Label");
            GameObjectUtility.SetParentAndAlign(txtObj, container);
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.color = TextColor;
            txt.fontSize = 20;
            txt.alignment = TextAlignmentOptions.Left;

            // Dropdown
            GameObject dropdownObj = new GameObject("Dropdown", typeof(Image));
            GameObjectUtility.SetParentAndAlign(dropdownObj, container);
            TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
            dropdownObj.GetComponent<Image>().color = ButtonColor;
            
            LayoutElement leDropdown = dropdownObj.AddComponent<LayoutElement>();
            leDropdown.minHeight = 40;
            leDropdown.preferredHeight = 40;
            
            // Label
            GameObject labelObj = new GameObject("Label", typeof(TextMeshProUGUI));
            GameObjectUtility.SetParentAndAlign(labelObj, dropdownObj);
            TextMeshProUGUI labelText = labelObj.GetComponent<TextMeshProUGUI>();
            labelText.text = "Option A";
            labelText.color = TextColor;
            labelText.fontSize = 18;
            labelText.alignment = TextAlignmentOptions.Left;
            RectTransform labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(10, 0);
            labelRT.offsetMax = new Vector2(-20, 0);
            
            dropdown.captionText = labelText;
            dropdown.targetGraphic = dropdownObj.GetComponent<Image>();

            // Arrow
            GameObject arrowObj = new GameObject("Arrow", typeof(Image));
            GameObjectUtility.SetParentAndAlign(arrowObj, dropdownObj);
            RectTransform arrowRT = arrowObj.GetComponent<RectTransform>();
            arrowRT.anchorMin = new Vector2(1, 0.5f);
            arrowRT.anchorMax = new Vector2(1, 0.5f);
            arrowRT.sizeDelta = new Vector2(20, 20);
            arrowRT.anchoredPosition = new Vector2(-15, 0);
            arrowObj.GetComponent<Image>().color = TextColor;

            // Template (Hidden by default)
            GameObject template = new GameObject("Template", typeof(Image), typeof(ScrollRect));
            GameObjectUtility.SetParentAndAlign(template, dropdownObj);
            template.SetActive(false);
            RectTransform templateRT = template.GetComponent<RectTransform>();
            templateRT.anchorMin = new Vector2(0, 0);
            templateRT.anchorMax = new Vector2(1, 0);
            templateRT.pivot = new Vector2(0.5f, 1);
            templateRT.anchoredPosition = new Vector2(0, 2);
            templateRT.sizeDelta = new Vector2(0, 150);
            
            template.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f);
            ScrollRect scrollRect = template.GetComponent<ScrollRect>();
            scrollRect.content = null; // Needs setup
            scrollRect.viewport = null; // Needs setup

            // Simplified Template setup for TMP is complex, usually requires copying a prefab.
            // We set basic refs so it doesn't crash, but user might need to adjust.
            // For a robust tool, we'd need to build the full Item hierarchy.
            
            // Viewport
            GameObject viewport = new GameObject("Viewport", typeof(Image), typeof(Mask));
            GameObjectUtility.SetParentAndAlign(viewport, template);
            RectTransform viewportRT = viewport.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.sizeDelta = Vector2.zero;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            // Content
            GameObject content = new GameObject("Content", typeof(RectTransform));
            GameObjectUtility.SetParentAndAlign(content, viewport);
            RectTransform contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.sizeDelta = new Vector2(0, 28);

            scrollRect.viewport = viewportRT;
            scrollRect.content = contentRT;
            dropdown.template = templateRT;

            return dropdown;
        }

        private static void EnsureCanvas(GameObject root)
        {
            if (root.transform.parent == null && Object.FindAnyObjectByType<Canvas>() == null)
            {
                GameObject canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                root.transform.SetParent(canvas.transform, false);
                
                // Set Canvas Scaler to Scale With Screen Size for better mobile/desktop adaptation
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }
            else if (root.transform.parent == null && Object.FindAnyObjectByType<Canvas>() != null)
            {
                 root.transform.SetParent(Object.FindAnyObjectByType<Canvas>().transform, false);
            }
        }
    }
}
