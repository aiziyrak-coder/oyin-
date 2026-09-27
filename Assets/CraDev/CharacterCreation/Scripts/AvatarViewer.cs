using CraDev.Face;
using CraDev.Wardrobe;
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
        [Tooltip("Garderob: kiyim va soch rangini bo'yovchi material (Hidden/CraDev/OutfitRecolor).")]
        [SerializeField] Material outfitPaint;
        [Tooltip("O'yinga Standard shaderning silliqlik xaritali variantini qo'shish uchun (garderob charm, jinsi kabi matolarda ishlatadi).")]
        [SerializeField] Material glossVariant;

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
        [SerializeField] Button[] viewButtons = System.Array.Empty<Button>();
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
        Outfit outfit = new Outfit();
        RenderTexture faceTexture, hairTexture, bodyTexture, glossTexture, hairCardTexture;
        RenderTexture previewBody, previewGloss, previewHead, previewHair;
        readonly System.Collections.Generic.List<(Material material, Texture original)> heads =
            new System.Collections.Generic.List<(Material, Texture)>();
        Material bodyMaterial, hairCardMaterial;
        Texture bodyOriginal, hairCardOriginal;
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
            current = null;
            currentOption = option;
            CollectMaterials();
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
            CollectMaterials();
            ApplyHeads();
            ApplyOutfit();
        }

        /// <summary>Hozirgi avatar (garderob rasmchalari uchun).</summary>
        public AvatarOption CurrentOption => currentOption;
        public GameObject CurrentModel => current;
        public Transform ModelRoot => turntable;
        public bool LockRotation { get; set; }
        public AvatarViewer Replica(Transform parent, AvatarOption option)
        {
            var copy = new GameObject("PartyAvatar").AddComponent<AvatarViewer>();
            copy.transform.SetParent(parent, false);
            copy.turntable = new GameObject("PartyModel").transform;
            copy.turntable.SetParent(copy.transform, false);
            copy.facePaint = facePaint; copy.outfitPaint = outfitPaint; copy.glossVariant = glossVariant;
            copy.maleIdle = maleIdle; copy.femaleIdle = femaleIdle;
            copy.driveCamera = false; copy.LockRotation = true;
            copy.SetAvatar(option);
            return copy;
        }

        /// <summary>Hozirgi kiyim (nusxasi).</summary>
        public Outfit Outfit => outfit.Clone();

        /// <summary>O'yinchi yuzini qo'yadi (null - olib tashlaydi). Avatar almashsa yuz yangisiga ham qo'yiladi.</summary>
        public void SetFace(FaceData data)
        {
            face = data;
            ApplyHeads();
        }

        /// <summary>Kiyimni qo'yadi (garderob). Avatar almashsa kiyim yangisiga ham qo'yiladi.</summary>
        public void SetOutfit(Outfit value)
        {
            var next = value?.Clone() ?? new Outfit();
            if (outfit.SameAs(next)) return;
            bool hairChanged = outfit.hair != next.hair || outfit.hairColor != next.hairColor;
            outfit = next;
            if (hairChanged) ApplyHeads();
            ApplyOutfit();
        }

        /// <summary>Katalog kartasi: aynan shu avatar va matodan shaffof 3D tasvir. Profilga yozmaydi.</summary>
        public RenderTexture CaptureGarment(WardrobeItem item)
        {
            if (current == null) return null;
            // Preview uses small, separate GPU targets. Never repaint the player's face or
            // read pixels back to the CPU while navigating the wardrobe.
            var preview = new Outfit(); preview.Set(item.Slot,item.Id,item.Color);
            var materials = new System.Collections.Generic.List<(Material material, Texture texture)>();
            foreach(var head in heads) materials.Add((head.material,head.material.mainTexture));
            if(bodyMaterial!=null) materials.Add((bodyMaterial,bodyMaterial.mainTexture));
            if(hairCardMaterial!=null) materials.Add((hairCardMaterial,hairCardMaterial.mainTexture));
            var oldGloss = bodyMaterial != null ? bodyMaterial.GetTexture("_MetallicGlossMap") : null;
            bool oldGlossEnabled = bodyMaterial != null && bodyMaterial.IsKeywordEnabled("_METALLICGLOSSMAP");
            var rotation = turntable.rotation;
            var transforms = current.GetComponentsInChildren<Transform>();
            var layers = new int[transforms.Length];
            var cameraGo = new GameObject("WardrobePreviewCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.enabled = false; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear; camera.cullingMask = 1 << 30;
            camera.orthographic = true; camera.allowHDR = false; camera.nearClipPlane = .05f;
            float y = item.Slot == OutfitSlot.Top ? bodyHeight * .67f : item.Slot == OutfitSlot.Bottom ? bodyHeight * .33f :
                item.Slot == OutfitSlot.Shoes ? .13f : bodyHeight * .91f;
            camera.orthographicSize = item.Slot == OutfitSlot.Top ? .49f : item.Slot == OutfitSlot.Bottom ? .52f : item.Slot == OutfitSlot.Shoes ? .23f : .27f;
            camera.transform.position = turntable.position + new Vector3(0,y,-3);
            camera.transform.rotation = Quaternion.identity;
            var target = new RenderTexture(192,192,24,RenderTextureFormat.ARGB32) {name="Garment_"+currentOption.id+"_"+item.Id};
            target.Create();
            var active = RenderTexture.active;
            try
            {
                for(int i=0;i<transforms.Length;i++) {layers[i]=transforms[i].gameObject.layer;transforms[i].gameObject.layer=30;}
                turntable.rotation = Quaternion.Euler(0,180,0);
                if(item.Slot==OutfitSlot.Hair)
                {
                    foreach(var head in heads)
                        head.material.mainTexture=OutfitPainter.PaintHair(head.original,currentOption,preview,outfitPaint,false,ref previewHead,512)?previewHead:head.original;
                    if(hairCardMaterial!=null)
                        hairCardMaterial.mainTexture=OutfitPainter.PaintHair(hairCardOriginal,currentOption,preview,outfitPaint,true,ref previewHair,512)?previewHair:hairCardOriginal;
                }
                else if(bodyMaterial!=null && OutfitPainter.PaintBody(bodyOriginal,currentOption,preview,outfitPaint,ref previewBody,ref previewGloss,512))
                {
                    bodyMaterial.mainTexture=previewBody;
                    bodyMaterial.SetTexture("_MetallicGlossMap",previewGloss);
                    bodyMaterial.EnableKeyword("_METALLICGLOSSMAP");
                }
                camera.targetTexture=target; camera.Render();
                return target;
            }
            catch
            {
                target.Release(); Destroy(target); throw;
            }
            finally
            {
                foreach(var state in materials) state.material.mainTexture=state.texture;
                if(bodyMaterial!=null)
                {
                    bodyMaterial.SetTexture("_MetallicGlossMap",oldGloss);
                    if(oldGlossEnabled) bodyMaterial.EnableKeyword("_METALLICGLOSSMAP");
                    else bodyMaterial.DisableKeyword("_METALLICGLOSSMAP");
                }
                turntable.rotation=rotation;
                for(int i=0;i<transforms.Length;i++) transforms[i].gameObject.layer=layers[i];
                RenderTexture.active=active;camera.targetTexture=null;
                Destroy(cameraGo);
            }
        }

        /// <summary>Kamerani yuzga yaqinlashtirib, qahramonni old tomoniga buradi: yuz natijasi ko'rinadi.</summary>
        public void FocusFace()
        {
            RotateTo(0f);
            targetZoom = 1f;
        }

        /// <summary>Avatar materiallarining nusxalari (asl model materiallariga tegilmaydi): bosh, tana, soch/kipriklar.</summary>
        void CollectMaterials()
        {
            heads.Clear();
            bodyMaterial = hairCardMaterial = null;
            bodyOriginal = hairCardOriginal = null;
            if (current == null)
                return;
            foreach (var renderer in current.GetComponentsInChildren<Renderer>())
                foreach (var material in renderer.materials)
                {
                    if (material.mainTexture == null)
                        continue;
                    if (material.name.Contains("_head"))
                        heads.Add((material, material.mainTexture));
                    else if (material.name.Contains("_body") && bodyMaterial == null)
                        (bodyMaterial, bodyOriginal) = (material, material.mainTexture);
                    else if (material.name.Contains("_opacity") && hairCardMaterial == null)
                        (hairCardMaterial, hairCardOriginal) = (material, material.mainTexture);
                }
        }

        /// <summary>Bosh: avval o'yinchi yuzi chiziladi, ustidan soch rangi.</summary>
        void ApplyHeads()
        {
            if (current == null)
                return;
            var old = faceTexture;
            faceTexture = null;
            bool paint = face != null && currentOption != null && currentOption.SupportsFace && facePaint != null;
            foreach (var (material, original) in heads)
            {
                if (paint && faceTexture == null)
                    faceTexture = FacePainter.Paint(original, face, currentOption.faceUv, currentOption.faceTriangles, facePaint);
                Texture result = paint ? faceTexture : original;
                if (OutfitPainter.PaintHair(result, currentOption, outfit, outfitPaint, false, ref hairTexture))
                    result = hairTexture;
                material.mainTexture = result;
            }
            if (old != null)
                old.Release();

            if (hairCardMaterial != null)
                hairCardMaterial.mainTexture = OutfitPainter.PaintHair(hairCardOriginal, currentOption, outfit, outfitPaint, true, ref hairCardTexture)
                    ? hairCardTexture : hairCardOriginal;
        }

        /// <summary>Tana: ustki kiyim, shim, oyoq kiyim rangi va matosi (charm yaltiroq, jinsi xira).</summary>
        void ApplyOutfit()
        {
            if (bodyMaterial == null)
                return;
            if (OutfitPainter.PaintBody(bodyOriginal, currentOption, outfit, outfitPaint, ref bodyTexture, ref glossTexture))
            {
                bodyMaterial.mainTexture = bodyTexture;
                bodyMaterial.SetTexture("_MetallicGlossMap", glossTexture);
                bodyMaterial.SetFloat("_GlossMapScale", 1f);
                bodyMaterial.EnableKeyword("_METALLICGLOSSMAP");
            }
            else
            {
                bodyMaterial.mainTexture = bodyOriginal;
                bodyMaterial.DisableKeyword("_METALLICGLOSSMAP");
            }
        }

        void OnDestroy()
        {
            foreach (var texture in new[] { faceTexture, hairTexture, bodyTexture, glossTexture, hairCardTexture, previewBody, previewGloss, previewHead, previewHair })
                if (texture != null)
                    texture.Release();
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
            if (LockRotation) return;
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
