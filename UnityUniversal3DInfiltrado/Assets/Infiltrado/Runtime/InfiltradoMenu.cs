using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Infiltrado
{
    public sealed class InfiltradoMenu : MonoBehaviour
    {
        [Header("Escena de juego (opcional, incluida en Build Profiles)")]
        public string gameplayScene;
        [Header("Dirección de la introducción")]
        public Camera menuCamera;
        public Transform adam;
        public Animator adamAnimator;
        public Transform scanner;
        public Transform halo;
        public UIDocument document;
        public float introDuration = 5.5f;
        public Vector3 cameraStart = new Vector3(3.8f, 1.65f, -3.3f);
        public Vector3 cameraEnd = new Vector3(0, 1.65f, -6.4f);
        public Vector3 cameraTarget = new Vector3(0, 1.18f, 0);
        public Vector3 adamMark = new Vector3(1.35f, 0.12f, 0);

        VisualElement ui, menu, modal, fade;
        Label connection, telemetry, modalTitle, modalBody;
        Button startButton, continueButton;
        Toggle motionToggle;
        AudioSource audioSource;
        AudioClip click;
        Transform head, spine;
        Quaternion headRest, spineRest;
        Vector3 adamStart;
        float elapsed, telemetryTimer;
        bool ready, resting, reduceMotion, loading;
        string activePanel;

        void Start()
        {
            if (!menuCamera || !adam || !adamAnimator || !scanner || !halo || !document)
            {
                enabled = false;
                return;
            }
            reduceMotion = PlayerPrefs.GetInt("Infiltrado.ReducedMotion", 0) == 1;
            AudioListener.volume = PlayerPrefs.GetFloat("Infiltrado.Volume", 0.65f);
            ui = document.rootVisualElement;
            menu = ui.Q("menu");
            modal = ui.Q("modal");
            fade = ui.Q("fade");
            connection = ui.Q<Label>("connection");
            telemetry = ui.Q<Label>("telemetry");
            modalTitle = ui.Q<Label>("modal-title");
            modalBody = ui.Q<Label>("modal-body");
            startButton = ui.Q<Button>("start");
            continueButton = ui.Q<Button>("continue");
            startButton.clicked += OpenBriefing;
            ui.Q<Button>("settings").clicked += OpenSettings;
            ui.Q<Button>("credits").clicked += OpenCredits;
            ui.Q<Button>("exit").clicked += OpenExit;
            ui.Q<Button>("close").clicked += ClosePanel;
            ui.Q<Button>("skip").clicked += FinishIntro;
            continueButton.clicked += Confirm;
            var volume = ui.Q<Slider>("volume");
            volume.SetValueWithoutNotify(AudioListener.volume);
            volume.RegisterValueChangedCallback(e =>
            {
                AudioListener.volume = e.newValue;
                PlayerPrefs.SetFloat("Infiltrado.Volume", e.newValue);
            });
            motionToggle = ui.Q<Toggle>("motion");
            motionToggle.SetValueWithoutNotify(reduceMotion);
            motionToggle.RegisterValueChangedCallback(e =>
            {
                reduceMotion = e.newValue;
                PlayerPrefs.SetInt("Infiltrado.ReducedMotion", reduceMotion ? 1 : 0);
                if (reduceMotion) FinishIntro();
            });
            ui.Query<Button>().ForEach(b => b.RegisterCallback<PointerEnterEvent>(_ => Tick()));
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            click = MakeTone();
            adamStart = adamMark + new Vector3(0, 0, 2.6f);
            adam.position = adamStart;
            adamAnimator.applyRootMotion = false;
            adamAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            head = adamAnimator.isHuman ? adamAnimator.GetBoneTransform(HumanBodyBones.Head) : null;
            spine = adamAnimator.isHuman ? adamAnimator.GetBoneTransform(HumanBodyBones.Chest) : null;
            menu.style.opacity = 0;
            menu.style.visibility = Visibility.Hidden;
            if (reduceMotion) FinishIntro();
            UpdateCamera(0);
        }

        void Update()
        {
            if (ui == null) return;
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            if (!ready && (Keyboard.current?.spaceKey.wasPressedThisFrame == true ||
                           Keyboard.current?.enterKey.wasPressedThisFrame == true)) FinishIntro();
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                if (!ready) FinishIntro();
                else if (activePanel != null) ClosePanel();
                else OpenExit();
            }
            float progress = Mathf.Clamp01(elapsed / introDuration);
            if (!ready && progress >= 1) FinishIntro();
            if (!resting)
            {
                // Constant-speed translation follows the imported walk; root motion stays off.
                adam.position = Vector3.Lerp(adamStart, adamMark, Mathf.Clamp01(elapsed / 3.8f));
                if (elapsed >= 3.8f) RestPose();
            }
            UpdateCamera(progress);
            if (!reduceMotion)
            {
                halo.Rotate(0, 0, dt * 7, Space.Self);
                scanner.localPosition = new Vector3(adamMark.x, 0.3f + (Mathf.Sin(elapsed * 0.8f) + 1) * 1.05f, 0);
            }
            scanner.gameObject.SetActive(!reduceMotion);
            fade.style.opacity = loading ? fade.style.opacity.value : 1 - Mathf.Clamp01(elapsed / 1.1f);
            if (ready) menu.style.opacity = Mathf.Clamp01((elapsed - introDuration) * 2 + 0.1f);
            telemetryTimer -= dt;
            if (telemetryTimer <= 0)
            {
                telemetryTimer = 0.25f;
                connection.text = ready ? "CANAL SEGURO  /  CONEXIÓN ESTABLECIDA" :
                    elapsed < 1.7f ? "INICIANDO PROTOCOLO DE ACCESO..." :
                    elapsed < 3.8f ? "IDENTIDAD DETECTADA  /  ADAM_01" : "DESCIFRANDO CREDENCIALES...";
                telemetry.text = $"ADAM_01  /  OPERATIVO\nINTEGRIDAD  100%\nCIFRADO     AES-256\nSESIÓN      {Mathf.FloorToInt(elapsed):0000}";
                ui.Q<VisualElement>("progress-fill").style.width = Length.Percent(progress * 100);
            }
        }

        void LateUpdate()
        {
            if (!resting || reduceMotion) return;
            if (spine) spine.localRotation = spineRest * Quaternion.Euler(Mathf.Sin(elapsed * 1.6f) * 0.7f, 0, Mathf.Sin(elapsed * 0.7f) * 0.5f);
            if (head) head.localRotation = headRest * Quaternion.Euler(Mathf.Sin(elapsed) * 1.4f, -7 + Mathf.Sin(elapsed * 0.42f) * 6, 0);
        }

        void UpdateCamera(float progress)
        {
            float t = reduceMotion ? 1 : Mathf.SmoothStep(0, 1, progress);
            Vector3 position = Vector3.Lerp(cameraStart, cameraEnd, t);
            if (ready && !reduceMotion) position += new Vector3(Mathf.Sin(elapsed * 0.21f) * 0.045f, Mathf.Sin(elapsed * 0.33f) * 0.025f, 0);
            menuCamera.transform.position = position;
            menuCamera.transform.LookAt(Vector3.Lerp(adamMark + Vector3.up * 1.25f, cameraTarget, t));
            menuCamera.fieldOfView = Mathf.Lerp(39, 35, t);
        }

        void RestPose()
        {
            adam.position = adamMark;
            adamAnimator.Play("Idle", 0, 0);
            adamAnimator.Update(0);
            adamAnimator.enabled = false;
            if (head) headRest = head.localRotation;
            if (spine) spineRest = spine.localRotation;
            resting = true;
        }

        public void FinishIntro()
        {
            if (ready) return;
            elapsed = introDuration;
            ready = true;
            if (!resting) RestPose();
            menu.style.visibility = Visibility.Visible;
            ui.Q("intro").style.display = DisplayStyle.None;
            startButton.Focus();
        }

        void Panel(string panel, string title, string body, string action = null)
        {
            Tick();
            activePanel = panel;
            modalTitle.text = title;
            modalBody.text = body;
            ui.Q("settings-fields").style.display = panel == "settings" ? DisplayStyle.Flex : DisplayStyle.None;
            continueButton.style.display = action == null ? DisplayStyle.None : DisplayStyle.Flex;
            continueButton.text = action ?? "";
            modal.style.display = DisplayStyle.Flex;
            menu.SetEnabled(false);
            ui.Q<Button>("close").Focus();
        }

        void OpenBriefing()
        {
            bool canLoad = !string.IsNullOrWhiteSpace(gameplayScene) && Application.CanStreamedLevelBeLoaded(gameplayScene);
            Panel("briefing", "TU IDENTIDAD ES TU ARMA.",
                "La red ha sido comprometida. Alguien está dentro.\n\nObserva las señales. Encuentra la vulnerabilidad. Protege lo que importa.\n\n" +
                (canLoad ? "Operativo ADAM: enlace listo. Puedes iniciar la operación." :
                "OPERACIÓN EN PREPARACIÓN\nEl acceso a la primera misión estará disponible próximamente."),
                canLoad ? "INICIAR OPERACIÓN  →" : null);
        }

        void OpenSettings() => Panel("settings", "CONFIGURACIÓN", "Ajusta tu experiencia de acceso.");
        void OpenCredits() => Panel("credits", "INFILTRADO", "Una experiencia de ciberseguridad.\n\nPersonaje: Adam — Unity Technologies\nAdam Character Pack\n\nDesarrollado con Unity 6 / Universal Render Pipeline.");
        void OpenExit() => Panel("exit", "CERRAR CONEXIÓN", "¿Quieres salir de Infiltrado?", "DESCONECTAR  →");

        void ClosePanel()
        {
            if (loading) return;
            Tick();
            PlayerPrefs.Save();
            modal.style.display = DisplayStyle.None;
            activePanel = null;
            menu.SetEnabled(true);
            startButton.Focus();
        }

        void Confirm()
        {
            if (loading) return;
            if (activePanel == "exit")
            {
                PlayerPrefs.Save();
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }
            else if (activePanel == "briefing") StartCoroutine(LoadOperation());
        }

        IEnumerator LoadOperation()
        {
            loading = true;
            continueButton.SetEnabled(false);
            fade.BringToFront();
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime)
            {
                fade.style.opacity = t;
                yield return null;
            }
            PlayerPrefs.Save();
            yield return SceneManager.LoadSceneAsync(gameplayScene);
        }

        void Tick() { if (audioSource && click) audioSource.PlayOneShot(click, 0.13f); }
        static AudioClip MakeTone()
        {
            const int rate = 22050;
            var samples = new float[1323];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = Mathf.Sin(i * 2 * Mathf.PI * 880 / rate) * Mathf.Sin(Mathf.PI * i / samples.Length) * Mathf.Exp(-6f * i / samples.Length);
            var clip = AudioClip.Create("Interface / acceso", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void OnDestroy() { if (click) Destroy(click); }
    }
}
