using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CrowdSurge
{
    /// <summary>
    /// Every screen, in Turkish and in a bright hyper-casual style: main menu with a cheering crowd behind it, level
    /// map, settings (with volume sliders), statistics, how to play, ready screen, HUD, pause, win and lose.
    /// Built in code at a 1080x1920 portrait reference.
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1f, 0.82f, 0.2f);
        private static readonly Color Green = new Color(0.25f, 0.8f, 0.35f);
        private static readonly Color Red = new Color(0.96f, 0.32f, 0.36f);
        private static readonly Color Blue = new Color(0.2f, 0.56f, 1f);
        private static readonly Color Purple = new Color(0.58f, 0.36f, 0.95f);
        private static readonly Color Orange = new Color(1f, 0.6f, 0.15f);
        private static readonly Color Teal = new Color(0.1f, 0.75f, 0.75f);
        private static readonly Color Slate = new Color(0.32f, 0.38f, 0.56f);
        private static readonly Color Grey = new Color(0.5f, 0.52f, 0.6f);
        private static readonly Color Panel = new Color(0.1f, 0.16f, 0.36f, 0.96f);
        private static readonly Color Navy = new Color(0.04f, 0.07f, 0.24f, 0.9f);
        private static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);

        private static readonly string[] Difficulty = { "KOLAY", "ORTA", "ZOR" };
        private static readonly Color[] DifficultyColors =
        {
            new Color(0.3f, 0.85f, 0.4f), new Color(1f, 0.65f, 0.2f), new Color(0.95f, 0.3f, 0.4f)
        };

        private GameObject _menu, _levels, _settings, _stats, _help, _ready, _hud, _pause, _win, _lose;

        // Menu
        private Text _menuStars, _playLabel, _playSub;
        private RectTransform _playButtonRect, _titleTop, _titleBottom;

        // Level map
        private readonly Button[] _nodeButtons = new Button[SaveData.LevelCount];
        private readonly Image[] _nodeImages = new Image[SaveData.LevelCount];
        private readonly Image[][] _nodeStars = new Image[SaveData.LevelCount][];
        private readonly GameObject[] _nodeLocks = new GameObject[SaveData.LevelCount];
        private readonly Text[] _nodeNumbers = new Text[SaveData.LevelCount];

        // Ready / HUD
        private Text _readyTitle, _readySub, _readyHint, _readyTap;
        private Image _readyChip;
        private Text _levelLabel, _crowdValue;
        private RectTransform _progressFill, _crowdChip;
        private int _lastCrowd = -1;
        private float _crowdPop;

        // Settings / stats
        private Text _sfxValue, _musicValue, _vibrationValue, _qualityValue, _fpsValue, _statsBody;
        private Image _sfxImage, _musicImage, _vibrationImage;

        // Result screens
        private RectTransform _winCard, _loseCard;
        private Text _winTitle, _winCrowd, _winNextLabel;
        private Button _winNext, _winReplay, _winMenu;
        private readonly Image[] _winStars = new Image[3];
        private float _popTimer;
        private RectTransform _popTarget;
        private int _starsToShow, _starsShown, _winCrowdTarget;
        private float _starTimer, _countTimer;
        private bool _hasNext;

        public event Action PlayPressed, TapToStart, PausePressed, ResumePressed, RestartPressed, MenuPressed, NextPressed;
        public event Action<int> LevelChosen;

        public static UIManager Create(Transform parent)
        {
            var go = new GameObject("UI");
            go.transform.SetParent(parent, false);
            var ui = go.AddComponent<UIManager>();
            ui.Build();
            return ui;
        }

        // ---------------------------------------------------------------- public API
        public void ShowMenu()
        {
            int unlocked = SaveData.UnlockedLevels;
            _menuStars.text = SaveData.TotalStars + " / " + SaveData.LevelCount * 3;
            _playSub.text = "BÖLÜM " + unlocked;
            SetScreen(_menu);
        }

        public void ShowReady(int levelIndex, string levelName, bool tutorial)
        {
            _readyTitle.text = "BÖLÜM " + (levelIndex + 1);
            int d = Mathf.Clamp(levelIndex, 0, 2);
            _readySub.text = Difficulty[d];
            _readyChip.color = DifficultyColors[d];
            _readyHint.text = tutorial ? "PARMAĞINI SÜRÜKLEYEREK YÖNLENDİR\nMAVİ KAPILARI SEÇ, KIRMIZILARDAN KAÇIN" : "";
            SetScreen(_ready);
        }

        public void ShowHud(int levelIndex)
        {
            _levelLabel.text = "BÖLÜM " + (levelIndex + 1);
            _lastCrowd = -1;
            SetProgress(0f);
            SetScreen(_hud);
        }

        public void ShowPause() => _pause.SetActive(true);

        public void HidePause() => _pause.SetActive(false);

        public void SetProgress(float t)
        {
            var max = _progressFill.anchorMax;
            max.x = Mathf.Clamp01(t);
            _progressFill.anchorMax = max;
        }

        /// <summary>Crowd size chip in the HUD; pops when the number changes.</summary>
        public void SetCrowd(int count)
        {
            if (count == _lastCrowd) return;
            _lastCrowd = count;
            _crowdValue.text = NumberFormat.Short(count);
            _crowdPop = 1f;
        }

        public void ShowWin(int levelIndex, int stars, int crowd, bool hasNextLevel, bool finalLevel)
        {
            _hasNext = hasNextLevel;
            _winTitle.text = finalLevel ? "TÜM BÖLÜMLER\nTAMAMLANDI!" : "BÖLÜM TAMAMLANDI!";
            _winNextLabel.text = hasNextLevel ? "SONRAKİ BÖLÜM" : "TEKRAR OYNA";
            _winCrowdTarget = crowd;
            _winCrowd.text = "KALABALIK  0";
            _countTimer = 0f;
            foreach (var s in _winStars)
            {
                s.color = new Color(0f, 0f, 0f, 0.35f);
                s.transform.localScale = Vector3.one;
            }
            _starsToShow = stars;
            _starsShown = 0;
            _starTimer = 0.8f;
            _winReplay.gameObject.SetActive(hasNextLevel);
            var menuRt = (RectTransform)_winMenu.transform;
            menuRt.anchoredPosition = new Vector2(hasNextLevel ? 190f : 0f, -430f);
            menuRt.sizeDelta = new Vector2(hasNextLevel ? 340f : 460f, 110f);
            SetScreen(_win);
            Pop(_winCard);
        }

        public void ShowLose()
        {
            SetScreen(_lose);
            Pop(_loseCard);
        }

        public bool InMenu => _menu.activeSelf || _levels.activeSelf || _settings.activeSelf || _stats.activeSelf || _help.activeSelf;

        // ---------------------------------------------------------------- screens
        private void SetScreen(GameObject active)
        {
            foreach (var s in new[] { _menu, _levels, _settings, _stats, _help, _ready, _hud, _pause, _win, _lose })
                s.SetActive(s == active);
        }

        private void Pop(RectTransform target)
        {
            _popTarget = target;
            _popTimer = 0.45f;
            target.localScale = Vector3.one * 0.4f;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;

            if (_menu.activeSelf)
            {
                _playButtonRect.localScale = Vector3.one * (1f + Mathf.Sin(time * 4.2f) * 0.045f);
                _titleTop.localRotation = Quaternion.Euler(0f, 0f, 3f + Mathf.Sin(time * 1.6f) * 1.6f);
                _titleBottom.localRotation = Quaternion.Euler(0f, 0f, 3f - Mathf.Sin(time * 1.6f + 0.8f) * 1.6f);
            }

            if (_ready.activeSelf)
                _readyTap.transform.localScale = Vector3.one * (1f + Mathf.Sin(time * 4f) * 0.07f);

            if (_hud.activeSelf && _crowdPop > 0f)
            {
                _crowdPop = Mathf.Max(0f, _crowdPop - dt * 4f);
                _crowdChip.localScale = Vector3.one * (1f + Mathf.Sin(_crowdPop * Mathf.PI) * 0.22f);
            }

            if (_popTimer > 0f && _popTarget != null)
            {
                _popTimer -= dt;
                float k = 1f - Mathf.Clamp01(_popTimer / 0.45f);
                const float c1 = 1.70158f, c3 = c1 + 1f;
                float e = 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
                _popTarget.localScale = Vector3.one * Mathf.LerpUnclamped(0.4f, 1f, e);
            }

            if (_win.activeSelf)
            {
                if (_starsShown < _starsToShow)
                {
                    _starTimer -= dt;
                    if (_starTimer <= 0f)
                    {
                        var star = _winStars[_starsShown];
                        star.color = Gold;
                        star.transform.localScale = Vector3.one * 1.7f;
                        AudioManager.SoundPitched(Sfx.Star, 0.75f, 1f + 0.12f * _starsShown, 0f);
                        _starsShown++;
                        _starTimer = 0.42f;
                    }
                }
                for (int i = 0; i < _starsShown && i < _winStars.Length; i++)
                    _winStars[i].transform.localScale = Vector3.Lerp(_winStars[i].transform.localScale, Vector3.one, 1f - Mathf.Exp(-9f * dt));

                if (_countTimer < 1f)
                {
                    _countTimer = Mathf.Min(1f, _countTimer + dt / 1.1f);
                    _winCrowd.text = "KALABALIK  " + Mathf.RoundToInt(_winCrowdTarget * Mathf.SmoothStep(0f, 1f, _countTimer));
                }
            }
        }

        // ---------------------------------------------------------------- construction helpers
        private void Build()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            var safe = UIFactory.Fill(transform, "SafeArea");
            safe.gameObject.AddComponent<SafeAreaFitter>();

            BuildHud(safe);
            BuildMenu(safe);
            BuildLevels(safe);
            BuildSettings(safe);
            BuildStats(safe);
            BuildHelp(safe);
            BuildReady(safe);
            BuildPause(safe);
            BuildWin(safe);
            BuildLose(safe);
            SetScreen(_menu);
        }

        private static RectTransform NewScreen(Transform parent, string name)
        {
            var rt = UIFactory.Fill(parent, name);
            rt.gameObject.SetActive(false);
            return rt;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = Mid;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static Text AddText(Transform parent, string text, int size, Color color, Vector2 pos, Vector2 box, bool outline = true)
        {
            var t = UIFactory.Label(parent, "Text", text, size, color, TextAnchor.MiddleCenter, outline);
            Place(t.rectTransform, Mid, pos, box);
            return t;
        }

        private static void AddDim(Transform parent, float alpha)
        {
            var img = parent.gameObject.AddComponent<Image>();
            img.color = new Color(0.02f, 0.04f, 0.16f, Mathf.Max(alpha, 0.82f));
        }

        private Button AddButton(Transform parent, string text, Color color, Vector2 pos, Vector2 size, Action onClick, int fontSize = 60)
        {
            var b = UIFactory.MakeButton(parent, text, text, color, size, Mid, pos, fontSize);
            b.onClick.AddListener(() =>
            {
                AudioManager.Sound(Sfx.Click, 0.7f, 0.03f, 0f);
                onClick();
            });
            return b;
        }

        private static RectTransform MakeCard(Transform parent, string title, float height)
        {
            var card = UIFactory.Anchored(parent, "Card", Mid, Mid, new Vector2(0f, 40f), new Vector2(940f, height));
            UIFactory.Panel(card, "Bg", Panel);
            var rim = UIFactory.Panel(card, "Rim", new Color(1f, 1f, 1f, 0.07f));
            rim.rectTransform.offsetMin = new Vector2(10f, 10f);
            rim.rectTransform.offsetMax = new Vector2(-10f, -10f);
            AddText(card, title, 90, Color.white, new Vector2(0f, height * 0.5f - 100f), new Vector2(900f, 130f));
            return card;
        }

        private static Image AddPill(Transform parent, Color color, Vector2 pos, Vector2 size)
        {
            var rt = UIFactory.Anchored(parent, "Pill", Mid, Mid, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UIFactory.RoundedSprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Round candy tile with an icon and a caption (menu shortcuts).</summary>
        private Button AddTile(Transform parent, string caption, Sprite icon, Color color, Vector2 pos, Action onClick)
        {
            var btn = UIFactory.MakeButton(parent, caption, "", color, new Vector2(210f, 210f), Mid, pos, 30);
            btn.onClick.AddListener(() =>
            {
                AudioManager.Sound(Sfx.Click, 0.7f, 0.03f, 0f);
                onClick();
            });
            var face = btn.transform.Find("Face");
            var iconRt = UIFactory.Anchored(face, "Icon", Mid, Mid, new Vector2(0f, 24f), new Vector2(104f, 104f));
            var img = iconRt.gameObject.AddComponent<Image>();
            img.sprite = icon;
            img.color = Color.white;
            img.raycastTarget = false;
            var label = face.Find("Text").GetComponent<Text>();
            label.text = caption;
            label.fontSize = 30;
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 0f);
            label.rectTransform.offsetMin = new Vector2(0f, 12f);
            label.rectTransform.offsetMax = new Vector2(0f, 62f);
            return btn;
        }

        // ---------------------------------------------------------------- menu
        private void BuildMenu(Transform parent)
        {
            var rt = NewScreen(parent, "Menu");
            _menu = rt.gameObject;

            var top = AddText(rt, "CROWD", 220, Color.white, new Vector2(0f, 740f), new Vector2(1060f, 240f));
            var bottom = AddText(rt, "SURGE", 240, Gold, new Vector2(0f, 520f), new Vector2(1060f, 260f));
            _titleTop = top.rectTransform;
            _titleBottom = bottom.rectTransform;

            // Star chip (top left)
            var chip = UIFactory.Anchored(rt, "StarChip", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(300f, 90f));
            var chipBg = chip.gameObject.AddComponent<Image>();
            chipBg.sprite = UIFactory.RoundedSprite;
            chipBg.type = Image.Type.Sliced;
            chipBg.color = new Color(0f, 0f, 0.1f, 0.4f);
            chipBg.raycastTarget = false;
            var icon = UIFactory.Anchored(chip, "Icon", new Vector2(0f, 0.5f), Mid, new Vector2(52f, 0f), new Vector2(66f, 66f));
            var iconImg = icon.gameObject.AddComponent<Image>();
            iconImg.sprite = UIFactory.StarSprite;
            iconImg.color = Gold;
            iconImg.raycastTarget = false;
            _menuStars = UIFactory.Label(chip, "Value", "0 / 9", 50, Color.white, TextAnchor.MiddleRight);
            _menuStars.rectTransform.offsetMin = new Vector2(100f, 0f);
            _menuStars.rectTransform.offsetMax = new Vector2(-24f, 0f);

            // Big PLAY button with the next level underneath
            var play = AddButton(rt, "OYNA", Green, new Vector2(0f, -330f), new Vector2(760f, 210f), () => PlayPressed?.Invoke(), 100);
            _playButtonRect = (RectTransform)play.transform;
            _playLabel = play.GetComponentInChildren<Text>();
            _playLabel.rectTransform.offsetMin = new Vector2(0f, 38f);
            _playSub = UIFactory.Label(play.transform.Find("Face"), "Sub", "BÖLÜM 1", 40, new Color(1f, 1f, 1f, 0.92f));
            _playSub.rectTransform.anchorMin = new Vector2(0f, 0f);
            _playSub.rectTransform.anchorMax = new Vector2(1f, 0f);
            _playSub.rectTransform.offsetMin = new Vector2(0f, 14f);
            _playSub.rectTransform.offsetMax = new Vector2(0f, 70f);

            AddTile(rt, "BÖLÜMLER", UIFactory.StarSprite, Blue, new Vector2(-360f, -650f), () => { RefreshLevels(); SetScreen(_levels); });
            AddTile(rt, "AYARLAR", UIIcons.Gear, Purple, new Vector2(-120f, -650f), () => { RefreshSettings(); SetScreen(_settings); });
            AddTile(rt, "İSTATİSTİK", UIIcons.Bars, Orange, new Vector2(120f, -650f), () => { RefreshStats(); SetScreen(_stats); });
            AddTile(rt, "YARDIM", UIIcons.Help, Teal, new Vector2(360f, -650f), () => SetScreen(_help));

            AddText(rt, "v1.0", 30, new Color(1f, 1f, 1f, 0.55f), new Vector2(0f, -880f), new Vector2(300f, 50f), false);
        }

        // ---------------------------------------------------------------- level map
        private void BuildLevels(Transform parent)
        {
            var rt = NewScreen(parent, "Levels");
            _levels = rt.gameObject;
            AddDim(rt, 0.6f);

            AddText(rt, "BÖLÜMLER", 120, Color.white, new Vector2(0f, 790f), new Vector2(1000f, 150f));

            Vector2[] positions = { new Vector2(-170f, 420f), new Vector2(170f, 20f), new Vector2(-170f, -380f) };

            // Dotted path between the nodes
            for (int i = 0; i < positions.Length - 1; i++)
            {
                for (int d = 1; d <= 5; d++)
                {
                    Vector2 p = Vector2.Lerp(positions[i], positions[i + 1], d / 6f);
                    var dot = UIFactory.Anchored(rt, "Dot", Mid, Mid, p, new Vector2(26f, 26f));
                    var img = dot.gameObject.AddComponent<Image>();
                    img.sprite = UIFactory.CircleSprite;
                    img.color = new Color(1f, 1f, 1f, 0.55f);
                    img.raycastTarget = false;
                }
            }

            for (int i = 0; i < SaveData.LevelCount; i++)
            {
                int index = i;
                var nodeRt = UIFactory.Anchored(rt, "Level" + (i + 1), Mid, Mid, positions[i], new Vector2(260f, 260f));

                var shadow = nodeRt.gameObject.AddComponent<Image>();
                shadow.sprite = UIFactory.CircleSprite;
                shadow.color = new Color(0f, 0f, 0.1f, 0.5f);
                shadow.raycastTarget = false;

                var faceRt = UIFactory.Rect(nodeRt, "Face", Vector2.zero, Vector2.one, new Vector2(0f, 12f), new Vector2(0f, 12f));
                var face = faceRt.gameObject.AddComponent<Image>();
                face.sprite = UIFactory.CircleSprite;
                face.color = DifficultyColors[i];
                _nodeImages[i] = face;

                var shine = UIFactory.Rect(faceRt, "Shine", new Vector2(0.15f, 0.5f), new Vector2(0.85f, 0.95f), Vector2.zero, Vector2.zero);
                var shineImg = shine.gameObject.AddComponent<Image>();
                shineImg.sprite = UIFactory.CircleSprite;
                shineImg.color = new Color(1f, 1f, 1f, 0.2f);
                shineImg.raycastTarget = false;

                var btn = nodeRt.gameObject.AddComponent<Button>();
                btn.targetGraphic = face;
                btn.onClick.AddListener(() =>
                {
                    if (index >= SaveData.UnlockedLevels) { AudioManager.Sound(Sfx.Error, 0.6f, 0f, 0f); return; }
                    AudioManager.Sound(Sfx.Click, 0.7f, 0.03f, 0f);
                    LevelChosen?.Invoke(index);
                });
                _nodeButtons[i] = btn;

                _nodeNumbers[i] = UIFactory.Label(faceRt, "Number", (i + 1).ToString(), 150, Color.white);

                var lockRt = UIFactory.Anchored(faceRt, "Lock", Mid, Mid, Vector2.zero, new Vector2(120f, 120f));
                var lockImg = lockRt.gameObject.AddComponent<Image>();
                lockImg.sprite = UIIcons.Lock;
                lockImg.color = new Color(1f, 1f, 1f, 0.95f);
                lockImg.raycastTarget = false;
                _nodeLocks[i] = lockRt.gameObject;

                var name = UIFactory.Label(nodeRt, "Name", Difficulty[i], 42, Color.white);
                Place(name.rectTransform, Mid, new Vector2(0f, -168f), new Vector2(300f, 56f));

                _nodeStars[i] = new Image[3];
                for (int s = 0; s < 3; s++)
                {
                    float dy = s == 1 ? -240f : -230f;
                    var starRt = UIFactory.Anchored(nodeRt, "Star" + s, Mid, Mid, new Vector2((s - 1) * 84f, dy), new Vector2(78f, 78f));
                    var img = starRt.gameObject.AddComponent<Image>();
                    img.sprite = UIFactory.StarSprite;
                    img.raycastTarget = false;
                    _nodeStars[i][s] = img;
                }
            }

            AddButton(rt, "GERİ", Slate, new Vector2(0f, -800f), new Vector2(420f, 120f), () => ShowMenu(), 56);
        }

        private void RefreshLevels()
        {
            int unlocked = SaveData.UnlockedLevels;
            for (int i = 0; i < SaveData.LevelCount; i++)
            {
                bool open = i < unlocked;
                _nodeImages[i].color = open ? DifficultyColors[i] : Grey;
                _nodeLocks[i].SetActive(!open);
                _nodeNumbers[i].gameObject.SetActive(open);
                int stars = SaveData.GetStars(i);
                for (int s = 0; s < 3; s++)
                {
                    _nodeStars[i][s].gameObject.SetActive(open);
                    _nodeStars[i][s].color = s < stars ? Gold : new Color(0f, 0f, 0f, 0.4f);
                }
            }
        }

        // ---------------------------------------------------------------- settings
        private void BuildSettings(Transform parent)
        {
            var rt = NewScreen(parent, "Settings");
            _settings = rt.gameObject;
            AddDim(rt, 0.6f);

            var card = MakeCard(rt, "AYARLAR", 1400f);
            UIExtras.MakeScroll(card, "Rows", Mid, new Vector2(0f, -20f), new Vector2(880f, 940f), false, out var content);

            AddToggleRow(content, "SES EFEKTLERİ", () => SaveData.SfxOn, v => SaveData.SfxOn = v, out _sfxValue, out _sfxImage);
            AddSliderRow(content, "EFEKT SEVİYESİ", SaveData.SfxVolume, v => { SaveData.SfxVolume = v; AudioManager.Sound(Sfx.Click, 0.7f, 0f, 0.12f); });
            AddToggleRow(content, "MÜZİK", () => SaveData.MusicOn, v => SaveData.MusicOn = v, out _musicValue, out _musicImage);
            AddSliderRow(content, "MÜZİK SEVİYESİ", SaveData.MusicVolume, v => { SaveData.MusicVolume = v; AudioManager.Instance?.ApplySettings(); });
            AddToggleRow(content, "TİTREŞİM", () => SaveData.VibrationOn, v => SaveData.VibrationOn = v, out _vibrationValue, out _vibrationImage);
            AddCycleRow(content, "GRAFİK", () => { SaveData.Quality = 1 - SaveData.Quality; GraphicsOptions.Apply(); }, out _qualityValue);
            AddCycleRow(content, "KARE HIZI", () => { SaveData.FrameRate = SaveData.FrameRate == 60 ? 30 : 60; GraphicsOptions.Apply(); }, out _fpsValue);

            AddButton(card, "GERİ", Blue, new Vector2(0f, -590f), new Vector2(460f, 130f), () => { SaveData.Save(); ShowMenu(); }, 58);
        }

        private RectTransform NewRow(RectTransform content, string label)
        {
            var row = UIFactory.Anchored(content, label, Mid, Mid, Vector2.zero, new Vector2(860f, 120f));
            var bg = row.gameObject.AddComponent<Image>();
            bg.sprite = UIFactory.RoundedSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(1f, 1f, 1f, 0.09f);
            bg.raycastTarget = false;
            var l = UIFactory.Label(row, "Label", label, 40, Color.white, TextAnchor.MiddleLeft);
            l.rectTransform.offsetMin = new Vector2(30f, 0f);
            l.rectTransform.offsetMax = new Vector2(-340f, 0f);
            return row;
        }

        private void AddToggleRow(RectTransform content, string label, Func<bool> get, Action<bool> set, out Text value, out Image image)
        {
            var row = NewRow(content, label);
            var btn = UIFactory.MakeButton(row, "Toggle", "AÇIK", Green, new Vector2(260f, 84f), new Vector2(1f, 0.5f), new Vector2(-160f, 0f), 40);
            value = btn.GetComponentInChildren<Text>();
            image = (Image)btn.targetGraphic;
            btn.onClick.AddListener(() =>
            {
                set(!get());
                SaveData.Save();
                AudioManager.Instance?.ApplySettings();
                AudioManager.Sound(Sfx.Click, 0.7f, 0.03f, 0f);
                RefreshSettings();
            });
        }

        private void AddCycleRow(RectTransform content, string label, Action advance, out Text value)
        {
            var row = NewRow(content, label);
            var btn = UIFactory.MakeButton(row, "Cycle", "", Blue, new Vector2(320f, 84f), new Vector2(1f, 0.5f), new Vector2(-190f, 0f), 32);
            value = btn.GetComponentInChildren<Text>();
            btn.onClick.AddListener(() =>
            {
                advance();
                SaveData.Save();
                AudioManager.Sound(Sfx.Click, 0.7f, 0.03f, 0f);
                RefreshSettings();
            });
        }

        private void AddSliderRow(RectTransform content, string label, float value, Action<float> onChanged)
        {
            var row = NewRow(content, label);
            UIExtras.MakeSlider(row, "Slider", new Vector2(1f, 0.5f), new Vector2(-190f, 0f), new Vector2(300f, 60f), value, onChanged, Green);
        }

        private void RefreshSettings()
        {
            SetToggle(_sfxValue, _sfxImage, SaveData.SfxOn);
            SetToggle(_musicValue, _musicImage, SaveData.MusicOn);
            SetToggle(_vibrationValue, _vibrationImage, SaveData.VibrationOn);
            _qualityValue.text = SaveData.Quality == 1 ? "YÜKSEK" : "PİL TASARRUFU";
            _fpsValue.text = SaveData.FrameRate + " FPS";
        }

        private static void SetToggle(Text text, Image image, bool on)
        {
            text.text = on ? "AÇIK" : "KAPALI";
            image.color = on ? Green : Grey;
        }

        // ---------------------------------------------------------------- stats and help
        private void BuildStats(Transform parent)
        {
            var rt = NewScreen(parent, "Stats");
            _stats = rt.gameObject;
            AddDim(rt, 0.6f);

            var card = MakeCard(rt, "İSTATİSTİKLER", 1100f);
            _statsBody = AddText(card, "", 46, Color.white, new Vector2(0f, 20f), new Vector2(840f, 700f), false);
            _statsBody.alignment = TextAnchor.MiddleLeft;
            _statsBody.lineSpacing = 1.35f;
            AddButton(card, "GERİ", Blue, new Vector2(0f, -440f), new Vector2(460f, 130f), () => ShowMenu(), 58);
        }

        private void RefreshStats()
        {
            _statsBody.text =
                "Toplam yıldız:   " + SaveData.TotalStars + " / " + SaveData.LevelCount * 3 + "\n" +
                "Oynanan oyun:   " + SaveData.TotalRuns + "\n" +
                "Kazanılan bölüm:   " + SaveData.TotalWins + "\n" +
                "Yenilen düşman:   " + SaveData.TotalDefeated + "\n" +
                "En büyük kalabalık:   " + SaveData.BestCrowd;
        }

        private void BuildHelp(Transform parent)
        {
            var rt = NewScreen(parent, "Help");
            _help = rt.gameObject;
            AddDim(rt, 0.6f);

            var card = MakeCard(rt, "NASIL OYNANIR", 1300f);
            AddHelpLine(card, "SÜRÜKLE", "Parmağını kaydırarak kalabalığı yönlendir", 380f);
            AddHelpLine(card, "KAPILAR", "Mavi kapılar büyütür, kırmızılar küçültür", 215f);
            AddHelpLine(card, "TESTERELER", "Dönen bıçaklardan kaçın", 50f);
            AddHelpLine(card, "SAVAŞ", "Kalabalığı büyük olan düşmanı yener", -115f);
            AddHelpLine(card, "YILDIZLAR", "Finişe büyük kalabalıkla girip 3 yıldız topla", -280f);
            AddButton(card, "GERİ", Blue, new Vector2(0f, -520f), new Vector2(460f, 120f), () => ShowMenu(), 58);
        }

        private static void AddHelpLine(Transform card, string head, string body, float y)
        {
            AddText(card, head, 52, Gold, new Vector2(0f, y + 34f), new Vector2(900f, 66f));
            AddText(card, body, 34, new Color(1f, 1f, 1f, 0.92f), new Vector2(0f, y - 26f), new Vector2(900f, 56f), false);
        }

        // ---------------------------------------------------------------- ready, HUD, pause
        private void BuildReady(Transform parent)
        {
            var rt = NewScreen(parent, "Ready");
            _ready = rt.gameObject;

            var catcher = rt.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            var button = rt.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                AudioManager.Sound(Sfx.Start, 0.85f, 0f, 0f);
                TapToStart?.Invoke();
            });

            _readyTitle = AddText(rt, "BÖLÜM 1", 190, Color.white, new Vector2(0f, 700f), new Vector2(1040f, 220f));
            _readyChip = AddPill(rt, DifficultyColors[0], new Vector2(0f, 540f), new Vector2(420f, 120f));
            _readySub = AddText(_readyChip.transform, "KOLAY", 70, Color.white, Vector2.zero, new Vector2(420f, 110f));
            _readyHint = AddText(rt, "", 42, new Color(1f, 1f, 1f, 0.95f), new Vector2(0f, 410f), new Vector2(1040f, 130f));
            _readyTap = AddText(rt, "BAŞLAMAK İÇİN DOKUN", 76, Color.white, new Vector2(0f, -620f), new Vector2(1040f, 120f));
        }

        private void BuildHud(Transform parent)
        {
            _hud = NewScreen(parent, "HUD").gameObject;

            _levelLabel = UIFactory.Label(_hud.transform, "Level", "BÖLÜM 1", 60, Color.white);
            Place(_levelLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(600f, 90f));

            // Progress bar with a finish star at the end
            var bar = UIFactory.Anchored(_hud.transform, "ProgressBar", new Vector2(0.5f, 1f), Mid, new Vector2(-30f, -190f), new Vector2(560f, 38f));
            var bg = bar.gameObject.AddComponent<Image>();
            bg.sprite = UIFactory.RoundedSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0f, 0f, 0.12f, 0.5f);
            bg.raycastTarget = false;

            var fillRect = UIFactory.Rect(bar, "Fill", Vector2.zero, new Vector2(0f, 1f), new Vector2(5f, 5f), new Vector2(-5f, -5f));
            var fill = fillRect.gameObject.AddComponent<Image>();
            fill.sprite = UIFactory.RoundedSprite;
            fill.type = Image.Type.Sliced;
            fill.color = Gold;
            fill.raycastTarget = false;
            _progressFill = fillRect;

            var finish = UIFactory.Anchored(bar, "Finish", new Vector2(1f, 0.5f), Mid, new Vector2(40f, 0f), new Vector2(78f, 78f));
            var finishImg = finish.gameObject.AddComponent<Image>();
            finishImg.sprite = UIFactory.StarSprite;
            finishImg.color = Gold;
            finishImg.raycastTarget = false;

            // Crowd chip (top right)
            _crowdChip = UIFactory.Anchored(_hud.transform, "CrowdChip", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(250f, 120f));
            var chipBg = _crowdChip.gameObject.AddComponent<Image>();
            chipBg.sprite = UIFactory.RoundedSprite;
            chipBg.type = Image.Type.Sliced;
            chipBg.color = new Color(0.1f, 0.4f, 0.95f, 0.9f);
            chipBg.raycastTarget = false;
            var chipLabel = UIFactory.Label(_crowdChip, "Caption", "KALABALIK", 26, new Color(1f, 1f, 1f, 0.9f));
            Place(chipLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(240f, 36f));
            _crowdValue = UIFactory.Label(_crowdChip, "Value", "0", 62, Color.white);
            Place(_crowdValue.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(240f, 80f));
            _crowdChip.pivot = new Vector2(1f, 1f);

            var pause = UIFactory.MakeButton(_hud.transform, "PauseButton", "II", new Color(0f, 0f, 0.15f, 0.45f), new Vector2(110f, 100f), new Vector2(0f, 1f), new Vector2(90f, -80f), 52);
            pause.onClick.AddListener(() =>
            {
                AudioManager.Sound(Sfx.Click, 0.7f, 0.03f, 0f);
                PausePressed?.Invoke();
            });
        }

        private void BuildPause(Transform parent)
        {
            var rt = NewScreen(parent, "Pause");
            _pause = rt.gameObject;
            AddDim(rt, 0.65f);

            var card = MakeCard(rt, "DURAKLATILDI", 900f);
            AddButton(card, "DEVAM ET", Green, new Vector2(0f, 90f), new Vector2(620f, 150f), () => ResumePressed?.Invoke(), 62);
            AddButton(card, "YENİDEN BAŞLA", Blue, new Vector2(0f, -110f), new Vector2(620f, 130f), () => RestartPressed?.Invoke(), 50);
            AddButton(card, "MENÜ", Slate, new Vector2(0f, -290f), new Vector2(620f, 130f), () => MenuPressed?.Invoke(), 56);
        }

        // ---------------------------------------------------------------- result screens
        private void BuildWin(Transform parent)
        {
            var root = NewScreen(parent, "Win");
            _win = root.gameObject;
            AddDim(root, 0.45f);

            var card = UIFactory.Anchored(root, "Card", Mid, Mid, new Vector2(0f, 20f), new Vector2(940f, 1240f));
            _winCard = card;
            UIFactory.Panel(card, "Bg", Panel);
            var rim = UIFactory.Panel(card, "Rim", new Color(1f, 1f, 1f, 0.07f));
            rim.rectTransform.offsetMin = new Vector2(10f, 10f);
            rim.rectTransform.offsetMax = new Vector2(-10f, -10f);

            // Gold ribbon behind the title
            AddPill(card, new Color(1f, 0.7f, 0.1f), new Vector2(0f, 480f), new Vector2(880f, 230f));
            _winTitle = AddText(card, "BÖLÜM TAMAMLANDI!", 74, Color.white, new Vector2(0f, 480f), new Vector2(860f, 230f));

            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 250f;
                float y = i == 1 ? 190f : 150f;
                var starRt = UIFactory.Anchored(card, "Star" + i, Mid, Mid, new Vector2(x, y), new Vector2(230f, 230f));
                var img = starRt.gameObject.AddComponent<Image>();
                img.sprite = UIFactory.StarSprite;
                img.raycastTarget = false;
                _winStars[i] = img;
            }
            _winCrowd = AddText(card, "KALABALIK  0", 64, Color.white, new Vector2(0f, -40f), new Vector2(860f, 90f));

            _winNext = AddButton(card, "NEXT", Green, new Vector2(0f, -240f), new Vector2(720f, 160f), () => NextPressed?.Invoke(), 62);
            _winNextLabel = _winNext.GetComponentInChildren<Text>();
            _winReplay = AddButton(card, "TEKRAR", Blue, new Vector2(-190f, -430f), new Vector2(340f, 110f), () => RestartPressed?.Invoke(), 44);
            _winMenu = AddButton(card, "MENÜ", Slate, new Vector2(190f, -430f), new Vector2(340f, 110f), () => MenuPressed?.Invoke(), 44);
        }

        private void BuildLose(Transform parent)
        {
            var root = NewScreen(parent, "Lose");
            _lose = root.gameObject;
            AddDim(root, 0.5f);

            _loseCard = UIFactory.Anchored(root, "Card", Mid, Mid, new Vector2(0f, 40f), new Vector2(940f, 860f));
            UIFactory.Panel(_loseCard, "Bg", Panel);
            var rim = UIFactory.Panel(_loseCard, "Rim", new Color(1f, 1f, 1f, 0.07f));
            rim.rectTransform.offsetMin = new Vector2(10f, 10f);
            rim.rectTransform.offsetMax = new Vector2(-10f, -10f);

            AddPill(_loseCard, Red, new Vector2(0f, 290f), new Vector2(820f, 200f));
            AddText(_loseCard, "BAŞARISIZ", 110, Color.white, new Vector2(0f, 290f), new Vector2(800f, 200f));
            AddText(_loseCard, "Kalabalığın yok oldu", 48, new Color(1f, 1f, 1f, 0.9f), new Vector2(0f, 90f), new Vector2(860f, 70f), false);

            AddButton(_loseCard, "TEKRAR DENE", Green, new Vector2(0f, -110f), new Vector2(660f, 150f), () => RestartPressed?.Invoke(), 58);
            AddButton(_loseCard, "MENÜ", Slate, new Vector2(0f, -290f), new Vector2(460f, 120f), () => MenuPressed?.Invoke(), 52);
        }
    }

    /// <summary>Keeps UI inside the device safe area (notches, rounded corners).</summary>
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect _last;
        private RectTransform _rt;

        private void Awake() => _rt = (RectTransform)transform;

        private void Update()
        {
            Rect safe = Screen.safeArea;
            if (safe == _last) return;
            _last = safe;
            _rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            _rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            _rt.offsetMin = _rt.offsetMax = Vector2.zero;
        }
    }
}
