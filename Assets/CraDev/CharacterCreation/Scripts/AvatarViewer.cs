using CraDev.Face;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraDev.CharacterCreation
{
    /// <summary>
    /// Tanlangan avatarni 3D sahnada ko'rsatadi: qahramon aylanma tagligida turadi va nafas oladi (idle animatsiya).
    ///
    /// Bu komponent ekran bo'ylab cho'zilgan shaffof UI qatlamida turadi: sichqoncha bilan chapga-o'ngga
    /// tortilsa qahramon aylanadi (qo'yib yuborilgach inersiya bilan sekinlashadi), g'ildirakcha bilan kamera
    /// yuziga yaqinlashadi. Qahramon ekranning o'ng tomonida turadi, chapda forma bor.
    /// </summary>
    public class AvatarViewer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        [Header("3D sahna")]
        [SerializeField] Camera stageCamera;
        [Tooltip("Qahramon shu obyekt ichida paydo bo'ladi va u bilan birga aylanadi.")]
        [SerializeField] Transform turntable;
        [SerializeField] RuntimeAnimatorController maleIdle;
        [SerializeField] RuntimeAnimatorController femaleIdle;
        [Tooltip("O'yinchi yuzini bosh teksturasiga chizuvchi material (Hidden/CraDev/FaceProject).")]
        [SerializeField] Material facePaint;

        [Header("Boshqaruv")]
        [Tooltip("Sichqoncha 1 birlik (1920x1080) surilganda necha gradus aylanadi.")]
        [SerializeField] float dragSensitivity = 0.4f;
        [Tooltip("G'ildirakchaning bir aylanishi qancha yaqinlashtiradi (0..1).")]
        [SerializeField] float zoomStep = 0.25f;
        [Tooltip("Qahramon ekran markazidan qancha o'ngda turadi (ekran kengligiga nisbatan).")]
        [SerializeField, Range(0f, 0.4f)] float screenOffset = 0.17f;
        [Tooltip("Kamerani shu komponent boshqaradimi (avatar yaratish). Bosh menyuda kamerani MenuCamera boshqaradi.")]
        [SerializeField] bool driveCamera = true;

        [Header("Ko'rinish tugmalari (Front, Side, Back)")]
        [SerializeField] Button[] viewButtons;
        [SerializeField] Image[] viewFills;
        [SerializeField] Text[] viewLabels;

        static readonly Color ActiveFill = new Color32(42, 43, 49, 255);
        static readonly Color Muted = new Color32(142, 147, 154, 255);
        static readonly float[] ButtonAngles = { 0f, 90f, 180f };
        /// <summary>Bosh suyagidan boshning tepasigacha (metr).</summary>
        const float HeadTop = 0.2f;

        GameObject current;
        AvatarOption currentOption;
        FaceData face;
        RenderTexture faceTexture;
        readonly System.Collections.Generic.List<(Material material, Texture original)> heads =
            new System.Collections.Generic.List<(Material, Texture)>();
        float bodyHeight = 1.75f;
        float yaw;        // 0 = kameraga qaragan, 90 = o'ng yoni, 180 = orqasi
        float targetYaw;
        float velocity;   // gradus/soniya, qo'yib yuborilgandan keyingi inersiya
        bool dragging;
        bool turning;     // tugma bosilganda berilgan burchakka silliq burilish
        float zoom;       // 0 = to'liq bo'y, 1 = yuz
        float targetZoom;
        float swapTime = 1f;
        Canvas canvas;

        void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            for (int i = 0; i < viewButtons.Length; i++)
            {
                float angle = ButtonAngles[i];
                viewButtons[i].onClick.AddListener(() => RotateTo(angle));
            }
        }

        /// <summary>Qahramon boshining tepasi (dunyo koordinatasi; animatsiyada tebranmaydi): nom yorlig'i shu yerga.</summary>
        public Vector3 TopOfHead => turntable.position + Vector3.up * bodyHeight;

        public void SetAvatar(AvatarOption option)
        {
            if (current != null)
                Destroy(current);
            heads.Clear();
            currentOption = option;
            if (option.model == null)
                return;

            current = Instantiate(option.model, turntable, false);
            current.name = option.id;
            current.transform.localPosition = Vector3.zero;
            current.transform.localRotation = Quaternion.identity;

            var animator = current.GetComponent<Animator>();
            if (animator == null)
                animator = current.AddComponent<Animator>();
            animator.runtimeAnimatorController = option.gender == "female" ? femaleIdle : maleIdle;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Kamera qahramon bo'yiga moslashadi: bo'y bosh suyagidan olinadi (Rocketbox skeleti: "Bip01 Head"),
            // chunki birinchi kadrgacha terining chegaralari hali hisoblanmagan bo'ladi
            bodyHeight = 1.75f;
            foreach (var bone in current.GetComponentsInChildren<Transform>())
                if (bone.name == "Bip01 Head")
                {
                    bodyHeight = Mathf.Clamp(bone.position.y - turntable.position.y + HeadTop, 1.3f, 2.2f);
                    break;
                }
            swapTime = 0f;
            ApplyFace();
        }

        /// <summary>O'yinchi yuzini qo'yadi (null - olib tashlaydi). Avatar almashsa yuz yangisiga ham qo'yiladi.</summary>
        public void SetFace(FaceData data)
        {
            face = data;
            ApplyFace();
        }

        /// <summary>Kamerani yuzga yaqinlashtirib, qahramonni old tomoniga buradi: yuz natijasi ko'rinadi.</summary>
        public void FocusFace()
        {
            RotateTo(0f);
            targetZoom = 1f;
        }

        void ApplyFace()
        {
            if (current == null)
                return;
            if (heads.Count == 0)
                foreach (var renderer in current.GetComponentsInChildren<Renderer>())
                    foreach (var material in renderer.materials) // nusxa: asl model materiallariga tegilmaydi
                        if (material.name.Contains("_head") && material.mainTexture != null)
                            heads.Add((material, material.mainTexture));

            var old = faceTexture;
            faceTexture = null;
            bool paint = face != null && currentOption != null && currentOption.SupportsFace && facePaint != null;
            foreach (var (material, original) in heads)
            {
                if (paint && faceTexture == null)
                    faceTexture = FacePainter.Paint(original, face, currentOption.faceUv, currentOption.faceTriangles, facePaint);
                material.mainTexture = paint ? faceTexture : original;
            }
            if (old != null)
                old.Release();
        }

        void OnDestroy()
        {
            if (faceTexture != null)
                faceTexture.Release();
        }

        /// <summary>Qahramonni berilgan tomonga eng qisqa yo'l bilan buradi.</summary>
        public void RotateTo(float angle)
        {
            velocity = 0f;
            turning = true;
            targetYaw = yaw + Mathf.DeltaAngle(yaw, angle);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (!dragging)
            {
                if (turning)
                {
                    yaw = Mathf.Lerp(yaw, targetYaw, 1f - Mathf.Exp(-8f * dt));
                    if (Mathf.Abs(targetYaw - yaw) < 0.1f)
                        turning = false;
                }
                else
                {
                    yaw += velocity * dt;
                    velocity *= Mathf.Exp(-4f * dt);
                }
            }

            // Yangi avatar tanlanganda qahramon qisqa burilish bilan chiqadi
            swapTime += dt;
            float swap = 1f - Ease.OutCubic(Mathf.Clamp01(swapTime / 0.45f));
            turntable.localRotation = Quaternion.Euler(0f, 180f - yaw - swap * 40f, 0f);

            zoom = Mathf.Lerp(zoom, targetZoom, 1f - Mathf.Exp(-7f * dt));
            if (driveCamera)
                PlaceCamera();
            UpdateButtons(Mathf.Repeat(yaw, 360f));
        }

        void PlaceCamera()
        {
            if (stageCamera == null)
                return;
            float halfFov = stageCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            Vector3 origin = turntable.position;

            // To'liq bo'y: qahramon kadr balandligining ~68% ini egallaydi, oyog'i pastdagi tugmalardan yuqorida.
            // Yaqin: yelkadan yuqorisi.
            float fullLook = bodyHeight * 0.47f;
            float fullDistance = bodyHeight * 0.735f / Mathf.Tan(halfFov);
            float closeLook = bodyHeight - 0.22f;
            float closeDistance = 0.42f / Mathf.Tan(halfFov);

            float t = Ease.InOutSine(zoom);
            float look = Mathf.Lerp(fullLook, closeLook, t);
            float distance = Mathf.Lerp(fullDistance, closeDistance, t);
            float height = Mathf.Lerp(bodyHeight * 0.58f, closeLook + 0.02f, t);

            // Kamera to'g'ri oldinga qaraydi va chapga suriladi: qahramon ekranning o'ng qismida ko'rinadi
            float halfWidth = distance * Mathf.Tan(halfFov) * stageCamera.aspect;
            float side = halfWidth * 2f * screenOffset;
            var position = origin + new Vector3(-side, height, -distance);
            stageCamera.transform.position = position;
            stageCamera.transform.rotation = Quaternion.LookRotation(origin + new Vector3(-side, look, 0f) - position);
        }

        void UpdateButtons(float angle)
        {
            // Yon tugmasi ikkala yon tomon uchun ham yonadi
            float folded = angle > 180f ? 360f - angle : angle;
            int active = folded < 45f ? 0 : folded <= 135f ? 1 : 2;
            for (int i = 0; i < viewButtons.Length; i++)
            {
                bool on = i == active;
                viewFills[i].color = on ? ActiveFill : new Color(0f, 0f, 0f, 0f);
                viewLabels[i].color = on ? Color.white : Muted;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = true;
            turning = false;
            velocity = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
            float delta = -eventData.delta.x / scale * dragSensitivity;
            yaw += delta;
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.001f);
            velocity = Mathf.Lerp(velocity, delta / dt, 0.3f);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!driveCamera)
                return;
            targetZoom = Mathf.Clamp01(targetZoom + Mathf.Sign(eventData.scrollDelta.y) * zoomStep);
        }
    }
}
