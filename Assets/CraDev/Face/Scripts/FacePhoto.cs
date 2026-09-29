using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CraDev.Face
{
    /// <summary>Rasmni fayldan yoki kameradan olish va uni yuz aniqlashga tayyorlash.</summary>
    public static class FacePhoto
    {
        /// <summary>Qayta ishlanadigan rasmning eng katta tomoni (piksel): kattarog'i kichraytiriladi.</summary>
        public const int MaxSide = 1280;

        /// <summary>JPG/PNG faylni o'qiydi, telefon rasmlaridagi burilishni (EXIF) to'g'rilaydi va kichraytiradi.</summary>
        public static Texture2D Load(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                Release(texture);
                return null;
            }
            texture = Rotate(texture, ExifOrientation(bytes));
            return Fit(texture);
        }

        /// <summary>Kameradagi joriy kadr (o'qiladigan nusxa).</summary>
        public static Texture2D Capture(WebCamTexture camera)
        {
            var texture = new Texture2D(camera.width, camera.height, TextureFormat.RGBA32, false);
            texture.SetPixels32(camera.GetPixels32());
            texture.Apply();
            // Kamera rasmi burilgan bo'lishi mumkin (ba'zi qurilmalarda)
            int turns = Mathf.RoundToInt(camera.videoRotationAngle / 90f) & 3;
            for (int i = 0; i < turns; i++)
                texture = RotateClockwise(texture);
            if (camera.videoVerticallyMirrored)
                texture = FlipVertical(texture);
            return Fit(texture);
        }

        static void Release(UnityEngine.Object o)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(o);
            else UnityEngine.Object.DestroyImmediate(o);
        }

        /// <summary>Katta rasmni MaxSide ga kichraytiradi (GPU orqali, sifatli).</summary>
        static Texture2D Fit(Texture2D source)
        {
            int w = source.width, h = source.height;
            float scale = Mathf.Min(1f, (float)MaxSide / Mathf.Max(w, h));
            if (scale >= 1f)
                return source;
            int nw = Mathf.RoundToInt(w * scale), nh = Mathf.RoundToInt(h * scale);
            var rt = RenderTexture.GetTemporary(nw, nh, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            source.filterMode = FilterMode.Bilinear;
            // Bir necha bosqichda kichraytirish: "zinapoya" bo'lib qolmaydi
            Texture current = source;
            RenderTexture step = null;
            while (current.width / 2 > nw)
            {
                var half = RenderTexture.GetTemporary(current.width / 2, current.height / 2, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(current, half);
                if (step != null) RenderTexture.ReleaseTemporary(step);
                step = half;
                current = half;
            }
            Graphics.Blit(current, rt);
            if (step != null) RenderTexture.ReleaseTemporary(step);

            var result = new Texture2D(nw, nh, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            result.ReadPixels(new Rect(0, 0, nw, nh), 0, 0);
            result.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Release(source);
            return result;
        }

        // ------------------------------------------------------------------ EXIF burilishi

        /// <summary>JPEG ichidagi EXIF Orientation (1 - to'g'ri, 3 - 180°, 6 - 90° soat yo'nalishida, 8 - 270°).</summary>
        static int ExifOrientation(byte[] d)
        {
            try
            {
                if (d.Length < 4 || d[0] != 0xFF || d[1] != 0xD8)
                    return 1;
                int i = 2;
                while (i + 4 < d.Length && d[i] == 0xFF)
                {
                    int marker = d[i + 1];
                    int length = (d[i + 2] << 8) | d[i + 3];
                    if (marker == 0xE1 && i + 10 < d.Length && d[i + 4] == 'E' && d[i + 5] == 'x' && d[i + 6] == 'i' && d[i + 7] == 'f')
                    {
                        int tiff = i + 10;
                        bool little = d[tiff] == 'I';
                        int U16(int p) => little ? d[p] | (d[p + 1] << 8) : (d[p] << 8) | d[p + 1];
                        int U32(int p) => little ? d[p] | (d[p + 1] << 8) | (d[p + 2] << 16) | (d[p + 3] << 24)
                                                 : (d[p] << 24) | (d[p + 1] << 16) | (d[p + 2] << 8) | d[p + 3];
                        int ifd = tiff + U32(tiff + 4);
                        int count = U16(ifd);
                        for (int e = 0; e < count; e++)
                        {
                            int entry = ifd + 2 + e * 12;
                            if (U16(entry) == 0x0112)
                                return U16(entry + 8);
                        }
                        return 1;
                    }
                    if (marker == 0xDA)
                        break; // rasm ma'lumotlari boshlandi
                    i += 2 + length;
                }
            }
            catch (IndexOutOfRangeException) { }
            return 1;
        }

        static Texture2D Rotate(Texture2D t, int orientation)
        {
            switch (orientation)
            {
                case 3: return RotateClockwise(RotateClockwise(t));
                case 6: return RotateClockwise(t);
                case 8: return RotateClockwise(RotateClockwise(RotateClockwise(t)));
                default: return t;
            }
        }

        static Texture2D RotateClockwise(Texture2D t)
        {
            int w = t.width, h = t.height;
            var src = t.GetPixels32();
            var dst = new Color32[src.Length];
            // Unity piksellari pastdan yuqoriga: (x, y) -> (y, w - 1 - x)
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    dst[(w - 1 - x) * h + y] = src[y * w + x];
            var r = new Texture2D(h, w, TextureFormat.RGBA32, false);
            r.SetPixels32(dst);
            r.Apply();
            Release(t);
            return r;
        }

        static Texture2D FlipVertical(Texture2D t)
        {
            int w = t.width, h = t.height;
            var src = t.GetPixels32();
            var dst = new Color32[src.Length];
            for (int y = 0; y < h; y++)
                Array.Copy(src, y * w, dst, (h - 1 - y) * w, w);
            t.SetPixels32(dst);
            t.Apply();
            return t;
        }

        // ------------------------------------------------------------------ Fayl tanlash oynasi (Windows)

        public static bool CanPickFile =>
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            true;
#else
            false;
#endif

        /// <summary>Windows'ning "Open" oynasi. Tanlanmasa null. Oyna ochiq turganda o'yin kutib turadi.</summary>
        public static string PickFile()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            const int maxPath = 2048;
            IntPtr buffer = Marshal.AllocHGlobal(maxPath * 2);
            try
            {
                Marshal.Copy(new byte[maxPath * 2], 0, buffer, maxPath * 2);
                var ofn = new OpenFileName
                {
                    structSize = Marshal.SizeOf(typeof(OpenFileName)),
                    dlgOwner = GetActiveWindow(),
                    filter = "Photos (*.jpg, *.jpeg, *.png)\0*.jpg;*.jpeg;*.png\0\0",
                    file = buffer,
                    maxFile = maxPath,
                    title = Loc.T("face.dialog_title"),
                    initialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    // OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR
                    flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008,
                };
                return GetOpenFileNameW(ofn) ? Marshal.PtrToStringUni(buffer) : null;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
#else
            return null;
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        class OpenFileName
        {
            public int structSize;
            public IntPtr dlgOwner;
            public IntPtr instance;
            public string filter;
            public string customFilter;
            public int maxCustFilter;
            public int filterIndex;
            public IntPtr file;
            public int maxFile;
            public string fileTitle;
            public int maxFileTitle;
            public string initialDir;
            public string title;
            public int flags;
            public short fileOffset;
            public short fileExtension;
            public string defExt;
            public IntPtr custData;
            public IntPtr hook;
            public string templateName;
            public IntPtr reservedPtr;
            public int reservedInt;
            public int flagsEx;
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool GetOpenFileNameW([In, Out] OpenFileName ofn);

        [DllImport("user32.dll")]
        static extern IntPtr GetActiveWindow();
#endif
    }

    /// <summary>
    /// O'yinchining yuzi shu kompyuterda saqlanadi (serverga yuborilmaydi):
    /// %USERPROFILE%/AppData/LocalLow/CraDev/CraDev/Face/ ichida rasm va nuqtalar.
    /// </summary>
    public static class FaceStore
    {
        static string Folder => Path.Combine(Application.persistentDataPath, "Face");
        static string PhotoPath => Path.Combine(Folder, "photo.jpg");
        static string PointsPath => Path.Combine(Folder, "landmarks.json");

        [Serializable]
        class Points { public float[] xy; }

        /// <summary>Yuzni saqlaydi. Xato bo'lsa istisno (chaqiruvchi o'yinchiga aytadi); eski yuz buzilmaydi.</summary>
        public static void Save(FaceData face)
        {
            Directory.CreateDirectory(Folder);
            var xy = new float[face.Landmarks.Length * 2];
            for (int i = 0; i < face.Landmarks.Length; i++)
            {
                xy[i * 2] = face.Landmarks[i].x;
                xy[i * 2 + 1] = face.Landmarks[i].y;
            }
            // Avval vaqtinchalik fayllarga, keyin almashtiriladi: yozish yarmida uzilsa ham eski yuz buzilmaydi
            WriteAtomic(PhotoPath, face.Photo.EncodeToJPG(92));
            WriteAtomic(PointsPath, System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Points { xy = xy })));
        }

        static void WriteAtomic(string path, byte[] data)
        {
            string temp = path + ".tmp";
            File.WriteAllBytes(temp, data);
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temp, path);
        }

        public static FaceData Load()
        {
            Texture2D photo = null;
            try
            {
                if (!File.Exists(PhotoPath) || !File.Exists(PointsPath))
                    return null;
                photo = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                var points = JsonUtility.FromJson<Points>(File.ReadAllText(PointsPath));
                if (!photo.LoadImage(File.ReadAllBytes(PhotoPath)) || points?.xy == null || points.xy.Length != FaceTracker.LandmarkCount * 2)
                {
                    Discard(photo);
                    return null;
                }
                var landmarks = new Vector2[FaceTracker.LandmarkCount];
                for (int i = 0; i < landmarks.Length; i++)
                    landmarks[i] = new Vector2(points.xy[i * 2], points.xy[i * 2 + 1]);
                return new FaceData { Photo = photo, Landmarks = landmarks };
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CraDev] Saqlangan yuzni o'qib bo'lmadi: " + e.Message);
                if (photo != null)
                    Discard(photo);
                return null;
            }
        }

        static void Discard(UnityEngine.Object o)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(o);
            else UnityEngine.Object.DestroyImmediate(o);
        }

        public static void Delete()
        {
            if (File.Exists(PhotoPath)) File.Delete(PhotoPath);
            if (File.Exists(PointsPath)) File.Delete(PointsPath);
        }
    }
}
