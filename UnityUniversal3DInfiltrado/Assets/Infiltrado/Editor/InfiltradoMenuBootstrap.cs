using System.IO;
using Infiltrado;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Infiltrado.Editor
{
    /// <summary>Creates the authored main-menu scene from the assets included with this project.</summary>
    public static class InfiltradoMenuSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/MainMenu.unity";
        const string NinjaPath = "Assets/Infiltrado/Ninja/tripo_convert_1b588db4-de91-49c6-95e8-d4c1a8bf5c1d.fbx";

        [MenuItem("Infiltrado/Construir escena de inicio", false, 0)]
        public static void Build()
        {
            EnsureFolders();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.018f, 0.045f, 0.052f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.008f, 0.025f, 0.033f);
            RenderSettings.fogDensity = 0.035f;

            var world = new GameObject("INFILTRADO / ESCENA DE ACCESO");
            var set = new GameObject("Arquitectura de red").transform;
            set.SetParent(world.transform);
            BuildArchitecture(set);

            var ninja = SpawnNinja(world.transform);
            var camera = BuildCamera(world.transform);
            BuildLights(world.transform, ninja);
            var controller = BuildUi(world.transform, ninja, camera);
            Validate(controller);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = camera.gameObject;
            Debug.Log("[Infiltrado] Menú principal creado en " + ScenePath);
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Infiltrado/Editor"))
                AssetDatabase.CreateFolder("Assets/Infiltrado", "Editor");
        }

        static void BuildArchitecture(Transform parent)
        {
            var floor = Primitive(PrimitiveType.Cube, "Piso / núcleo de acceso", parent,
                new Vector3(0, -0.12f, 2.2f), new Vector3(13, 0.18f, 14), Dark());
            floor.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            Cube("Muro de datos", parent, new Vector3(0, 3.7f, 3.7f), new Vector3(13, 7.5f, .22f), Dark());
            Cube("Muro izquierdo", parent, new Vector3(-6.3f, 3.7f, 0), new Vector3(.22f, 7.5f, 8), Dark());
            Cube("Muro derecho", parent, new Vector3(6.3f, 3.7f, 0), new Vector3(.22f, 7.5f, 8), Dark());

            var black = Dark();
            var cyan = Emissive(new Color(0.02f, .9f, .72f), 2.5f);
            var amber = Emissive(new Color(1f, .24f, .04f), 2.3f);
            for (int side = -1; side <= 1; side += 2)
            for (int z = -1; z <= 2; z++)
            {
                float x = side * 4.55f;
                float depth = z * 1.6f + .7f;
                Cube("Servidor " + side + " / " + z, parent, new Vector3(x, 1.55f, depth), new Vector3(.65f, 3.15f, .78f), black);
                Cube("Panel de red", parent, new Vector3(x - side * .337f, 1.55f, depth), new Vector3(.025f, 2.6f, .56f), cyan);
                for (int row = 0; row < 5; row++)
                    Cube("Indicador", parent, new Vector3(x - side * .354f, .52f + row * .49f, depth - .22f), new Vector3(.027f, .075f, .07f), (row + z) % 3 == 0 ? amber : cyan);
            }
            Cube("Matriz central", parent, new Vector3(0, 3.25f, 3.54f), new Vector3(5.5f, 2.5f, .03f), Emissive(new Color(.01f, .12f, .14f), .25f));
            for (int i = -6; i <= 6; i++)
            {
                Cube("Línea vertical", parent, new Vector3(i * .41f, 3.25f, 3.5f), new Vector3(.012f, 2.25f, .02f), cyan);
                Cube("Línea horizontal", parent, new Vector3(0, 2.1f + (i + 6) * .19f, 3.49f), new Vector3(5.0f, .01f, .02f), cyan);
            }
            for (int i = 0; i < 4; i++)
            {
                Cube("Canal de piso", parent, new Vector3(-3.8f + i * 2.5f, -.015f, .9f), new Vector3(1.5f, .012f, 4.6f), cyan);
            }
        }

        static Transform SpawnNinja(Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NinjaPath);
            if (prefab == null) throw new FileNotFoundException("No se encontró el ninja en " + NinjaPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = "NINJA_01 / Infiltrador";
            go.transform.SetParent(parent);
            // Pose base elegida manualmente en MainMenu. No recalcular ni corregir ejes del FBX.
            go.transform.localPosition = new Vector3(1.71f, 0f, -.68f);
            go.transform.localRotation = new Quaternion(.05041139f, .72028965f, .68833894f, -.0695066f);
            go.transform.localScale = Vector3.one * 3.5124946f;
            return go.transform;
        }

        static Camera BuildCamera(Transform parent)
        {
            var go = new GameObject("Cámara / Entrada cinemática", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.SetParent(parent);
            var cam = go.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.003f, .012f, .017f);
            cam.nearClipPlane = .1f;
            cam.farClipPlane = 80;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            return cam;
        }

        static void BuildLights(Transform parent, Transform ninja)
        {
            Light("Luz ambiente cian", parent, new Vector3(-2.9f, 3.1f, -1.6f), new Color(.02f, .72f, .7f), 6.5f, 7.3f);
            Light("Luz de perfil ámbar", parent, new Vector3(3.6f, 2.6f, 1.8f), new Color(1f, .17f, .025f), 5.8f, 6f);
            var back = Light("Contraluz de red", parent, new Vector3(0, 3.8f, 3f), new Color(.04f, .55f, 1f), 5.2f, 8f);
            back.transform.rotation = Quaternion.Euler(35, 180, 0);
            Spot("Luz principal del ninja", parent, new Vector3(-1.8f, 4.6f, -3.2f), new Color(.7f, 1f, .94f), 1800f, 10f, 42f, ninja.position + Vector3.up * 1.2f);
            Spot("Luz de recorte del ninja", parent, new Vector3(4.2f, 3.6f, 2.4f), new Color(1f, .32f, .08f), 1300f, 9f, 48f, ninja.position + Vector3.up * 1.15f);
        }

        static NinjaMenu BuildUi(Transform parent, Transform ninja, Camera camera)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(parent);

            var canvasObject = new GameObject("CANVAS / MENÚ EDITABLE", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;

            var safe = Rect("ZONA SEGURA / edita aquí", canvasObject.transform, new Vector2(.035f, .045f), new Vector2(.965f, .955f));
            Text("Clasificación", safe, "ACCESO RESTRINGIDO  //  PROTOCOLO 01", new Vector2(.02f, .925f), new Vector2(.6f, .985f), 16, new Color(.48f, 1f, .88f), TextAlignmentOptions.Left);
            Text("Estado", safe, "● SISTEMA EN LÍNEA", new Vector2(.72f, .925f), new Vector2(.98f, .985f), 15, new Color(.12f, 1f, .72f), TextAlignmentOptions.Right);

            var intro = Rect("INTRO / visible durante 5 segundos", safe, Vector2.zero, Vector2.one).gameObject;
            var introGroup = intro.AddComponent<CanvasGroup>();
            Text("Etiqueta intro", intro.transform, "OPERADOR NO IDENTIFICADO", new Vector2(.04f, .63f), new Vector2(.5f, .69f), 17, new Color(.34f, 1f, .84f), TextAlignmentOptions.Left);
            Text("Título intro", intro.transform, "INFILTRADO", new Vector2(.035f, .39f), new Vector2(.58f, .62f), 66, Color.white, TextAlignmentOptions.Left);
            var countdown = Text("Estado de transición", intro.transform, "SINCRONIZANDO OPERADOR", new Vector2(.04f, .31f), new Vector2(.52f, .365f), 18, new Color(.72f, .9f, .88f), TextAlignmentOptions.Left);
            var progressBack = Panel("Progreso / fondo", intro.transform, new Vector2(.04f, .275f), new Vector2(.42f, .287f), new Color(.08f, .2f, .2f, .9f));
            var progress = Panel("Progreso / 5 segundos", progressBack.transform, Vector2.zero, Vector2.one, new Color(.06f, 1f, .72f, 1f));
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Horizontal;
            progress.fillOrigin = 0;

            var menu = Rect("MENÚ PRINCIPAL / aparece al segundo 5", safe, Vector2.zero, Vector2.one).gameObject;
            var menuGroup = menu.AddComponent<CanvasGroup>();
            menuGroup.alpha = 0;
            var menuPanel = Panel("Panel principal", menu.transform, new Vector2(.02f, .12f), new Vector2(.48f, .84f), new Color(.012f, .035f, .045f, .92f));
            Text("Marca", menuPanel.transform, "INFILTRADO", new Vector2(.08f, .76f), new Vector2(.92f, .94f), 52, Color.white, TextAlignmentOptions.Left);
            Text("Subtítulo", menuPanel.transform, "OPERACIONES DE CIBERSEGURIDAD", new Vector2(.08f, .69f), new Vector2(.92f, .76f), 15, new Color(.23f, 1f, .79f), TextAlignmentOptions.Left);
            Text("Descripción", menuPanel.transform, "Entra sin dejar rastro.\nLee la red. Encuentra la brecha.", new Vector2(.08f, .55f), new Vector2(.92f, .68f), 19, new Color(.74f, .84f, .84f), TextAlignmentOptions.Left);
            var play = Button("Botón / Iniciar operación", menuPanel.transform, "INICIAR OPERACIÓN", .43f);
            var settings = Button("Botón / Configuración", menuPanel.transform, "CONFIGURACIÓN", .31f);
            var credits = Button("Botón / Créditos", menuPanel.transform, "CRÉDITOS", .19f);
            var exit = Button("Botón / Salir", menuPanel.transform, "SALIR", .07f);
            Text("Identidad del personaje", menu.transform, "NINJA_01  /  ACTIVO\nINFILTRACIÓN // NIVEL 4", new Vector2(.67f, .15f), new Vector2(.97f, .25f), 16, new Color(.45f, 1f, .82f), TextAlignmentOptions.Right);

            var modal = Panel("VENTANA MODAL / editable", safe, new Vector2(.22f, .2f), new Vector2(.78f, .8f), new Color(.008f, .026f, .034f, .98f)).gameObject;
            var modalGroup = modal.AddComponent<CanvasGroup>();
            var modalTitle = Text("Título modal", modal.transform, "CONFIGURACIÓN", new Vector2(.08f, .75f), new Vector2(.92f, .91f), 34, Color.white, TextAlignmentOptions.Left);
            var modalBody = Text("Contenido modal", modal.transform, "AJUSTES DEL CANAL DE AUDIO", new Vector2(.08f, .56f), new Vector2(.92f, .74f), 18, new Color(.65f, .87f, .84f), TextAlignmentOptions.Left);
            var settingsFields = Rect("AJUSTES", modal.transform, new Vector2(.08f, .36f), new Vector2(.92f, .54f)).gameObject;
            Text("Volumen label", settingsFields.transform, "VOLUMEN", new Vector2(0, .56f), new Vector2(.28f, 1), 16, Color.white, TextAlignmentOptions.Left);
            var sliderObject = Rect("Volumen", settingsFields.transform, new Vector2(.3f, .58f), new Vector2(1, .88f)).gameObject;
            var slider = sliderObject.AddComponent<Slider>();
            var sliderBack = Panel("Fondo", sliderObject.transform, new Vector2(0, .35f), new Vector2(1, .65f), new Color(.1f, .2f, .2f, 1));
            var fillArea = Rect("Fill Area", sliderObject.transform, new Vector2(.02f, .35f), new Vector2(.98f, .65f));
            var fill = Panel("Fill", fillArea, Vector2.zero, Vector2.one, new Color(.05f, 1f, .72f, 1));
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = fill;
            slider.value = .8f;
            var confirm = Button("Botón / Confirmar", modal.transform, "CONFIRMAR", .14f, .08f, .48f);
            var back = Button("Botón / Volver", modal.transform, "VOLVER", .14f, .52f, .92f);
            modal.SetActive(false);

            var fade = Panel("Fundido inicial", canvasObject.transform, Vector2.zero, Vector2.one, Color.black);
            fade.raycastTarget = false;

            var controller = parent.gameObject.AddComponent<NinjaMenu>();
            controller.menuCamera = camera;
            controller.ninja = ninja;
            controller.canvas = canvas;
            controller.menuGroup = menuGroup;
            controller.introGroup = introGroup;
            controller.modalGroup = modalGroup;
            controller.modalTitle = modalTitle;
            controller.modalBody = modalBody;
            controller.countdown = countdown;
            controller.startButton = play;
            controller.backButton = back;
            controller.volumeSlider = slider;
            controller.settingsFields = settingsFields;
            controller.fade = fade;
            controller.progress = progress;
            controller.landingPosition = ninja.position;

            UnityEventTools.AddPersistentListener(play.onClick, controller.OpenBriefing);
            UnityEventTools.AddPersistentListener(settings.onClick, controller.OpenSettings);
            UnityEventTools.AddPersistentListener(credits.onClick, controller.OpenCredits);
            UnityEventTools.AddPersistentListener(exit.onClick, controller.OpenExit);
            UnityEventTools.AddPersistentListener(confirm.onClick, controller.Confirm);
            UnityEventTools.AddPersistentListener(back.onClick, controller.ClosePanel);
            UnityEventTools.AddPersistentListener(slider.onValueChanged, controller.SetVolume);
            return controller;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        static Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var rect = Rect(name, parent, min, max);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        static TextMeshProUGUI Text(string name, Transform parent, string value, Vector2 min, Vector2 max, float size, Color color, TextAlignmentOptions alignment)
        {
            var rect = Rect(name, parent, min, max);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value;
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            label.fontSize = size;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(11, size * .48f);
            label.fontSizeMax = size;
            label.color = color;
            label.alignment = alignment;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
        }

        static Button Button(string name, Transform parent, string label, float y, float xMin = .08f, float xMax = .92f)
        {
            var image = Panel(name, parent, new Vector2(xMin, y), new Vector2(xMax, y + .09f), new Color(.025f, .12f, .13f, .98f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.25f, 1f, .8f);
            colors.pressedColor = new Color(1f, .32f, .08f);
            button.colors = colors;
            Text("Texto TMP / " + label, image.transform, label, new Vector2(.05f, .08f), new Vector2(.95f, .92f), 19, Color.white, TextAlignmentOptions.MidlineLeft);
            return button;
        }

        static void Validate(NinjaMenu controller)
        {
            var texts = controller.canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts.Length < 12) throw new System.InvalidOperationException("El Canvas no contiene todos los textos esperados.");
            foreach (var text in texts)
            {
                var rect = text.rectTransform;
                if (rect.anchorMin.x < 0 || rect.anchorMin.y < 0 || rect.anchorMax.x > 1 || rect.anchorMax.y > 1)
                    throw new System.InvalidOperationException("Texto fuera del Canvas: " + text.name);
            }

            controller.CaptureAuthoringPose();
            Vector3 cameraStart;
            Vector3 ninjaStart;
            controller.EvaluateIntro(0f);
            cameraStart = controller.menuCamera.transform.position;
            ninjaStart = controller.ninja.position;
            controller.EvaluateIntro(4.99f);
            if (controller.menuGroup.alpha != 0f) throw new System.InvalidOperationException("El menú aparece antes de cinco segundos.");
            controller.EvaluateIntro(5f);
            if (controller.menuGroup.alpha != 1f) throw new System.InvalidOperationException("El menú no aparece al segundo cinco.");
            if (Vector3.Distance(cameraStart, controller.menuCamera.transform.position) < 1f) throw new System.InvalidOperationException("La cámara no se desplaza durante la intro.");
            if (Vector3.Distance(ninjaStart, controller.landingPosition) < .5f) throw new System.InvalidOperationException("El ninja no realiza el salto.");
            controller.RestoreAuthoringPose();
            Debug.Log("[Infiltrado] Validación correcta: " + texts.Length + " textos dentro del Canvas; ninja y cámara animados; menú bloqueado hasta 5.00 s.");
        }

        static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
            => Primitive(PrimitiveType.Cube, name, parent, position, scale, material);

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return go;
        }

        static Light Light(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject(name, typeof(Light));
            go.transform.SetParent(parent);
            go.transform.position = position;
            var light = go.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
            return light;
        }

        static Light Spot(string name, Transform parent, Vector3 position, Color color, float intensity, float range, float angle, Vector3 target)
        {
            var light = Light(name, parent, position, color, intensity, range);
            light.type = LightType.Spot;
            light.spotAngle = angle;
            light.transform.LookAt(target);
            return light;
        }

        static Material Dark() => Material("Infiltrado / grafito", new Color(.012f, .028f, .034f), 0, Color.black);
        static Material Emissive(Color color, float intensity) => Material("Infiltrado / emisión", color * .18f, intensity, color);

        static Material Material(string name, Color baseColor, float emissionStrength, Color emission)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", .65f);
            material.SetFloat("_Smoothness", .75f);
            if (emissionStrength > 0)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission * emissionStrength);
            }
            return material;
        }
    }
}
