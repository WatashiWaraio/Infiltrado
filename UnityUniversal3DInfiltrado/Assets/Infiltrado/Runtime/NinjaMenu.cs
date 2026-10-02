using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Infiltrado
{
    public sealed class NinjaMenu : MonoBehaviour
    {
        public const float IntroSeconds = 5f;

        [Header("Escena")]
        public Camera menuCamera;
        public Transform ninja;
        public Canvas canvas;

        [Header("Interfaz editable")]
        public CanvasGroup menuGroup;
        public CanvasGroup introGroup;
        public CanvasGroup modalGroup;
        public TextMeshProUGUI modalTitle;
        public TextMeshProUGUI modalBody;
        public TextMeshProUGUI countdown;
        public Button startButton;
        public Button backButton;
        public Slider volumeSlider;
        public GameObject settingsFields;
        public Image fade;
        public Image progress;

        [Header("Composición final")]
        public Vector3 landingPosition = new Vector3(1.45f, .03f, 0f);
        public Vector3 finalCameraPosition = new Vector3(0f, 1.8f, -7.6f);
        public Vector3 finalCameraTarget = new Vector3(0f, 1.25f, 0f);

        Quaternion restRotation;
        Vector3 restScale;
        Vector3 authoringPosition;
        Vector3 authoringCameraPosition;
        Quaternion authoringCameraRotation;
        bool poseCaptured;
        float elapsed;
        bool running;

        void Awake()
        {
            if (ninja == null) ninja = transform;
            CaptureAuthoringPose();
        }

        public void CaptureAuthoringPose()
        {
            if (ninja == null || menuCamera == null || poseCaptured) return;
            restRotation = ninja.rotation;
            restScale = ninja.localScale;
            authoringPosition = ninja.position;
            authoringCameraPosition = menuCamera.transform.position;
            authoringCameraRotation = menuCamera.transform.rotation;
            poseCaptured = true;
        }

        public void RestoreAuthoringPose()
        {
            if (!poseCaptured) return;
            ninja.position = authoringPosition;
            ninja.rotation = restRotation;
            ninja.localScale = restScale;
            menuCamera.transform.position = authoringCameraPosition;
            menuCamera.transform.rotation = authoringCameraRotation;
        }

        void Start()
        {
            if (menuCamera == null || canvas == null || menuGroup == null || introGroup == null)
            {
                Debug.LogError("[Infiltrado] Faltan referencias del menú. Reconstruye la escena desde Infiltrado/Construir escena de inicio.", this);
                enabled = false;
                return;
            }

            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Infiltrado.Volume", .8f));
                AudioListener.volume = volumeSlider.value;
            }
            ClosePanel();
            ReplayIntro();
        }

        void Update()
        {
            if (!running) return;
            elapsed = Mathf.Min(IntroSeconds, elapsed + Mathf.Min(Time.unscaledDeltaTime, .1f));
            EvaluateIntro(elapsed);
            if (elapsed >= IntroSeconds) running = false;
        }

        public void ReplayIntro()
        {
            ClosePanel();
            elapsed = 0f;
            running = true;
            EvaluateIntro(0f);
        }

        public void EvaluateIntro(float seconds)
        {
            if (ninja == null || menuCamera == null) return;
            CaptureAuthoringPose();
            float time = Mathf.Clamp(seconds, 0f, IntroSeconds);
            float normalized = time / IntroSeconds;

            Vector3 start = landingPosition + new Vector3(-2.25f, 0f, 1.15f);
            Vector3 position;
            float crouch = 0f;
            if (time < .8f)
            {
                float t = Smooth(time / .8f);
                position = start + Vector3.down * (.16f * t);
                crouch = t;
            }
            else if (time < 3.2f)
            {
                float t = (time - .8f) / 2.4f;
                float eased = Smooth(t);
                position = Vector3.Lerp(start + Vector3.down * .16f, landingPosition, eased);
                position.y += Mathf.Sin(t * Mathf.PI) * 2.15f;
                crouch = 1f - Mathf.Sin(t * Mathf.PI);
            }
            else
            {
                float t = Smooth((time - 3.2f) / 1.8f);
                position = landingPosition + Vector3.up * (Mathf.Sin(t * Mathf.PI * 3f) * .08f * (1f - t));
                crouch = (1f - t) * .35f;
            }

            ninja.position = position;
            // La orientación manual del FBX es la autoridad. El giro se aplica únicamente
            // alrededor del eje vertical global para que el modelo nunca vuelva a quedar de lado.
            ninja.rotation = Quaternion.AngleAxis(Mathf.Sin(normalized * Mathf.PI) * -8f, Vector3.up) * restRotation;
            ninja.localScale = restScale * (1f - crouch * .035f);

            float cameraT = Smooth(normalized);
            Vector3 cameraStart = new Vector3(3.8f, 2.3f, -6.1f);
            menuCamera.transform.position = Vector3.Lerp(cameraStart, finalCameraPosition, cameraT);
            Vector3 target = Vector3.Lerp(position + Vector3.up * 1.05f, finalCameraTarget, cameraT);
            menuCamera.transform.rotation = Quaternion.LookRotation(target - menuCamera.transform.position, Vector3.up);

            bool complete = time >= IntroSeconds;
            SetGroup(menuGroup, complete ? 1f : 0f, complete);
            SetGroup(introGroup, complete ? 0f : 1f, false);
            if (fade != null) fade.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(time / .35f));
            if (progress != null) progress.fillAmount = normalized;
            if (countdown != null)
            {
                countdown.text = time < .8f ? "SINCRONIZANDO OPERADOR"
                    : time < 3.2f ? "INFILTRACIÓN EN CURSO"
                    : time < IntroSeconds ? "ESTABLECIENDO ENLACE SEGURO"
                    : "ACCESO CONCEDIDO";
            }
        }

        static float Smooth(float value) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));

        static void SetGroup(CanvasGroup group, float alpha, bool interactive)
        {
            if (group == null) return;
            group.alpha = alpha;
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }

        public void OpenBriefing()
        {
            ShowPanel("INICIAR OPERACIÓN", "Conexión preparada. El módulo jugable se conectará aquí cuando esté disponible.", false, true);
        }

        public void OpenSettings()
        {
            ShowPanel("CONFIGURACIÓN", "AJUSTES DEL CANAL DE AUDIO", true, false);
        }

        public void OpenCredits()
        {
            ShowPanel("CRÉDITOS", "INFILTRADO  •  PROYECTO DE CIBERSEGURIDAD\nOperador y diseño: editable desde el Canvas.", false, false);
        }

        public void OpenExit()
        {
            ShowPanel("ABORTAR SESIÓN", "La salida está desactivada dentro del Editor.", false, false);
        }

        void ShowPanel(string title, string body, bool settings, bool primary)
        {
            if (modalGroup == null) return;
            modalGroup.gameObject.SetActive(true);
            modalGroup.alpha = 1f;
            modalGroup.interactable = true;
            modalGroup.blocksRaycasts = true;
            modalTitle.text = title;
            modalBody.text = body;
            if (settingsFields != null) settingsFields.SetActive(settings);
            if (startButton != null) startButton.gameObject.SetActive(primary);
        }

        public void SetVolume(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat("Infiltrado.Volume", value);
        }

        public void Confirm() => ClosePanel();

        public void ClosePanel()
        {
            if (modalGroup != null) modalGroup.gameObject.SetActive(false);
        }
    }
}
