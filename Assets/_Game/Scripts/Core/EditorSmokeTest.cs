#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CrowdSurge
{
    /// <summary>
    /// Editor-only automated play-through (command line flag -csSmoke). Tours the menus, then plays levels 1-3 with a
    /// bot that picks gates and dodges saws, taking screenshots (UI included) into Logs/Smoke. Stripped from builds.
    /// </summary>
    public sealed class EditorSmokeTest : MonoBehaviour
    {
        // Survives scene reloads between levels.
        private static int s_phase;       // 0 = menu tour, 1 = playing levels
        private static int s_attempts;
        private static bool s_pauseTested;

        private GameManager _gm;
        private CrowdController _crowd;
        private HazardManager _hazards;
        private LevelConfig _level;
        private string _dir;
        private float _t;
        private int _step;
        private float _stateTimer;
        private float _runTimer;
        private GameState _lastState = (GameState)(-1);

        public static bool Requested
        {
            get
            {
                foreach (var a in System.Environment.GetCommandLineArgs())
                    if (a == "-csSmoke") return true;
                return UnityEditor.EditorPrefs.GetBool("cs_smoke", false);
            }
        }

        private static readonly (float t, string click, string shot)[] Tour =
        {
            (0.8f, "", "menu"),
            (1.0f, "AYARLAR", ""),
            (1.5f, "", "ayarlar"),
            (1.6f, "GERİ", ""),
            (1.8f, "İSTATİSTİK", ""),
            (2.3f, "", "istatistik"),
            (2.4f, "GERİ", ""),
            (2.6f, "YARDIM", ""),
            (3.1f, "", "yardim"),
            (3.2f, "GERİ", ""),
            (3.4f, "BÖLÜMLER", ""),
            (3.9f, "", "bolumler"),
            (4.0f, "Level1", ""),
        };

        private void Start()
        {
            _gm = FindAnyObjectByType<GameManager>();
            _crowd = FindAnyObjectByType<CrowdController>();
            _hazards = (HazardManager)typeof(GameManager).GetField("_hazards", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_gm);
            var assets = GameAssets.Load();
            _level = assets.levels[Mathf.Clamp(GameSession.SelectedLevel, 0, assets.levels.Length - 1)];
            _dir = Path.Combine(Application.dataPath, "..", "Logs", "Smoke");
            Directory.CreateDirectory(_dir);

            var canvas = FindAnyObjectByType<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1f;
            Time.timeScale = 2f;
        }

        private void Capture(string name)
        {
            var cam = Camera.main;
            var rt = RenderTexture.GetTemporary(720, 1280, 24);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(720, 1280, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 720, 1280), 0, 0);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(Path.Combine(_dir, $"cs_{name}.png"), tex.EncodeToPNG());
            Destroy(tex);
        }

        private static bool Click(string buttonName)
        {
            foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (b.gameObject.name != buttonName) continue;
                b.onClick.Invoke();
                return true;
            }
            Debug.LogWarning("[Smoke] button not found: " + buttonName);
            return false;
        }

        private void Update()
        {
            _t += Time.unscaledDeltaTime;
            if (s_phase == 0) { TourUpdate(); return; }

            if (_gm.State != _lastState)
            {
                _lastState = _gm.State;
                _stateTimer = 0f;
                Debug.Log($"[Smoke] level={GameSession.SelectedLevel + 1} state={_gm.State} crowd={_crowd.Count}");
            }
            _stateTimer += Time.unscaledDeltaTime;

            switch (_gm.State)
            {
                case GameState.Ready:
                    if (_stateTimer > 0.4f && _stateTimer < 0.6f) Capture($"L{GameSession.SelectedLevel + 1}_ready");
                    if (_stateTimer > 0.8f) _gm.StartRun();
                    break;

                case GameState.Running:
                case GameState.Battle:
                    _runTimer += Time.unscaledDeltaTime;
                    if (_gm.State == GameState.Running) Bot();
                    if (!s_pauseTested && _runTimer > 6f)
                    {
                        s_pauseTested = true;
                        Click("PauseButton");
                        Capture("pause");
                        Click("DEVAM ET");
                    }
                    if (_runTimer > 10f && _runTimer < 10.1f) Capture($"L{GameSession.SelectedLevel + 1}_run");
                    if (_gm.State == GameState.Battle && _stateTimer > 0.9f && _stateTimer < 1.0f) Capture($"L{GameSession.SelectedLevel + 1}_battle");
                    break;

                case GameState.Won:
                    if (_stateTimer > 2.6f && _stateTimer < 2.8f) Capture($"L{GameSession.SelectedLevel + 1}_win");
                    if (_stateTimer > 3.2f) NextLevelOrFinish(true);
                    break;

                case GameState.Lost:
                    if (_stateTimer > 2.6f && _stateTimer < 2.8f) Capture($"L{GameSession.SelectedLevel + 1}_lose");
                    if (_stateTimer > 3.2f) NextLevelOrFinish(false);
                    break;
            }

            if (_t > 300f) Finish("timeout");
        }

        private void TourUpdate()
        {
            if (_step >= Tour.Length || _t < Tour[_step].t) return;
            var s = Tour[_step++];
            if (!string.IsNullOrEmpty(s.click)) Click(s.click);
            if (!string.IsNullOrEmpty(s.shot)) Capture(s.shot);
            if (_step == Tour.Length) s_phase = 1;
        }

        private void NextLevelOrFinish(bool won)
        {
            int level = GameSession.SelectedLevel;
            Debug.Log($"[Smoke] RESULT level={level + 1} won={won} crowd={_crowd.Count} stars={SaveData.GetStars(level)}");
            if (!won)
            {
                if (++s_attempts >= 2) { Finish("lost"); return; }
                Click("TEKRAR DENE");
                return;
            }
            s_attempts = 0;
            if (level >= 2) { Finish("all levels complete"); return; }
            Click("NEXT");
        }

        private void Finish(string why)
        {
            Debug.Log("[Smoke] finished: " + why + $" unlocked={SaveData.UnlockedLevels} stars={SaveData.TotalStars}");
            PlayerPrefs.DeleteAll();
            UnityEditor.EditorPrefs.SetBool("cs_smoke", false);
            if (Application.isBatchMode) UnityEditor.EditorApplication.Exit(0);
            else UnityEditor.EditorApplication.isPlaying = false;
        }

        // ------------------------------------------------------------ bot
        private static readonly float[] Candidates = { -3.7f, -2.6f, -1.3f, 0f, 1.3f, 2.6f, 3.7f };

        private void Bot()
        {
            float cz = _crowd.Center.z;
            float radius = _crowd.Radius;
            float time = (float)typeof(HazardManager).GetField("_time", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_hazards);

            int preferred = 0; // -1 left, 1 right
            foreach (var g in _level.gates)
            {
                if (g.z < cz || g.z > cz + 30f) continue;
                int l = g.left.Apply(_crowd.Count), r = g.right.Apply(_crowd.Count);
                preferred = r > l ? 1 : l > r ? -1 : 0;
                break;
            }

            float bestX = 0f, bestCost = float.MaxValue;
            foreach (float x in Candidates)
            {
                float cost = Mathf.Abs(x - _crowd.Center.x) * 0.15f;
                if (preferred != 0)
                {
                    if (x == 0f) cost += 15f;
                    else if (Mathf.Sign(x) != preferred) cost += 30f;
                }

                foreach (var s in _level.saws)
                {
                    float dz = s.z - cz;
                    if (dz < -1.5f || dz > 16f) continue;
                    float sx = s.x;
                    if (s.patrolAmplitude > 0f) sx += Mathf.Sin((time + dz / 9f) * s.patrolSpeed) * s.patrolAmplitude;
                    float gap = Mathf.Abs(x - sx) - (radius * 0.9f + 1.5f);
                    if (gap < 0f) cost += 500f - gap * 20f;
                }
                if (cost < bestCost) { bestCost = cost; bestX = x; }
            }
            _crowd.SetTargetX(bestX);
        }
    }
}
#endif
