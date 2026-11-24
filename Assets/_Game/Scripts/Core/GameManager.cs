using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrowdSurge
{
    public enum GameState { Menu, Ready, Running, Battle, Paused, Won, Lost }

    /// <summary>Owns the state machine (menu, ready, run, battle, pause, win, lose) and ticks every system in order.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        private const float ResultDelay = 1.2f;

        private CrowdController _crowd;
        private GateManager _gates;
        private HazardManager _hazards;
        private EnemyManager _enemies;
        private CameraRig _camera;
        private UIManager _ui;
        private ParticleFx _fx;
        private LevelConfig _level;
        private int _levelIndex;
        private readonly PointerSteering _steering = new PointerSteering();

        private float _time;
        private float _hitStopRemaining;
        private float _confettiTimer, _confettiBurstTimer;
        private float _resultTimer;
        private GameState _stateBeforePause;

        public GameState State { get; private set; } = GameState.Menu;
        public int LevelIndex => _levelIndex;

        public void Init(LevelConfig level, int levelIndex, CrowdController crowd, GateManager gates, HazardManager hazards,
            EnemyManager enemies, CameraRig camera, UIManager ui, ParticleFx fx)
        {
            _level = level;
            _levelIndex = levelIndex;
            _crowd = crowd;
            _gates = gates;
            _hazards = hazards;
            _enemies = enemies;
            _camera = camera;
            _ui = ui;
            _fx = fx;
            Time.timeScale = 1f;

            _enemies.BattleStarted += OnBattleStarted;
            _enemies.BattleEnded += OnBattleEnded;
            _ui.PlayPressed += OnPlayPressed;
            _ui.LevelChosen += LoadLevel;
            _ui.TapToStart += StartRun;
            _ui.PausePressed += Pause;
            _ui.ResumePressed += Resume;
            _ui.RestartPressed += () => LoadLevel(_levelIndex);
            _ui.MenuPressed += GoToMenu;
            _ui.NextPressed += OnNextPressed;

            AudioManager.Instance?.SetMusicScale(1f);
            AudioManager.Instance?.PlayMusic();
            if (GameSession.SkipMenu)
            {
                GameSession.SkipMenu = false;
                EnterReady();
            }
            else
            {
                _crowd.SetIdle(true);
                _camera.SetMenuMode(true, true);
                _ui.ShowMenu();
            }
        }

        private void OnDestroy()
        {
            if (_enemies == null) return;
            _enemies.BattleStarted -= OnBattleStarted;
            _enemies.BattleEnded -= OnBattleEnded;
        }

        // ---------------------------------------------------------------- flow
        private void EnterReady()
        {
            State = GameState.Ready;
            _crowd.SetIdle(true);
            _ui.ShowReady(_levelIndex, _level.levelName, !SaveData.TutorialSeen);
        }

        public void StartRun()
        {
            if (State != GameState.Ready) return;
            State = GameState.Running;
            SaveData.TutorialSeen = true;
            SaveData.TotalRuns++;
            _crowd.SetIdle(false);
            _ui.ShowHud(_levelIndex);
        }

        private void OnPlayPressed() => LoadLevel(SaveData.UnlockedLevels - 1);

        private void SpawnVictoryConfetti(float dt)
        {
            if (_confettiTimer <= 0f) return;
            _confettiTimer -= dt;
            _confettiBurstTimer -= dt;
            if (_confettiBurstTimer > 0f) return;
            _confettiBurstTimer = 0.45f;
            _fx.Confetti(_crowd.Center + Vector3.up * 2f, 70);
        }

        private void OnNextPressed()
        {
            if (_levelIndex + 1 < SaveData.LevelCount) LoadLevel(_levelIndex + 1);
            else LoadLevel(_levelIndex);
        }

        private void LoadLevel(int index)
        {
            GameSession.SelectedLevel = Mathf.Clamp(index, 0, SaveData.LevelCount - 1);
            GameSession.SkipMenu = true;
            Reload();
        }

        private void GoToMenu()
        {
            GameSession.SkipMenu = false;
            Reload();
        }

        private void Reload()
        {
            SaveData.Save();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Pause()
        {
            if (State != GameState.Running && State != GameState.Battle) return;
            _stateBeforePause = State;
            State = GameState.Paused;
            Time.timeScale = 0f;
            _ui.ShowPause();
            AudioManager.Instance?.SetMusicScale(0.35f);
        }

        private void Resume()
        {
            if (State != GameState.Paused) return;
            State = _stateBeforePause;
            Time.timeScale = 1f;
            _ui.HidePause();
            AudioManager.Instance?.SetMusicScale(1f);
        }

        private void OnApplicationPause(bool paused)
        {
#if !UNITY_EDITOR
            if (paused) Pause(); // phone call, home button, app switcher
#endif
        }

        // ---------------------------------------------------------------- loop
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State == GameState.Running || State == GameState.Battle) Pause();
                else if (State == GameState.Paused) Resume();
            }

            if (_hitStopRemaining > 0f)
            {
                _hitStopRemaining -= Time.unscaledDeltaTime;
                if (_hitStopRemaining <= 0f && State != GameState.Paused) Time.timeScale = 1f;
            }

            if (State == GameState.Paused) return;

            _crowd.SetLabelVisible(State != GameState.Menu && State != GameState.Won);
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            _time += dt;
            _steering.Update();

            bool running = State == GameState.Running;
            _crowd.Tick(dt, running, _steering.DeltaX);

            if (running) _gates.Tick(dt);
            _hazards.Tick(dt, running);
            _enemies.Tick(dt, _time);
            _crowd.Refresh(dt, _time);

            if (State == GameState.Running || State == GameState.Battle)
            {
                _ui.SetProgress(_crowd.Center.z / _level.finishZ);
                _ui.SetCrowd(_crowd.Count);
            }
            SpawnVictoryConfetti(dt);

            switch (State)
            {
                case GameState.Running:
                    if (_crowd.Count <= 0) Lose();
                    else if (_crowd.Center.z >= _level.finishZ) Win();
                    break;
                case GameState.Won:
                case GameState.Lost:
                    UpdateResultTimer(dt);
                    break;
            }
        }

        private void OnBattleStarted()
        {
            if (State == GameState.Running) State = GameState.Battle;
        }

        private void OnBattleEnded(bool playerWon, bool wasBoss)
        {
            if (State != GameState.Battle) return;
            if (playerWon)
            {
                State = GameState.Running;
                if (wasBoss) HitStop(0.35f);
                else HitStop(0.09f);
            }
            else Lose();
        }

        /// <summary>Brief slow-motion freeze that makes big hits feel heavy.</summary>
        private void HitStop(float seconds)
        {
            _hitStopRemaining = seconds;
            Time.timeScale = 0.15f;
        }

        private void Win()
        {
            State = GameState.Won;
            _resultTimer = ResultDelay;
            _camera.Celebrate();
            _crowd.SetIdle(true);
            _crowd.SetHop(0.35f);
            _fx.Confetti(_crowd.Center + Vector3.up * 2f, 160);
            _fx.Ring(_crowd.Center + Vector3.up * 0.3f, Color.white, 40, 11f, 0.34f);
            _confettiTimer = 3f;
            SaveData.TotalWins++;
            SaveData.BestCrowd = Mathf.Max(SaveData.BestCrowd, _crowd.Count);
            AudioManager.Sound(Sfx.Win, 0.9f, 0f, 0f);
            Haptics.Light();
            _ui.SetProgress(1f);

            int stars = _level.StarsFor(_crowd.Count);
            SaveData.SetStars(_levelIndex, stars);
            if (_levelIndex + 1 < SaveData.LevelCount && SaveData.UnlockedLevels < _levelIndex + 2)
                SaveData.UnlockedLevels = _levelIndex + 2;
            SaveData.Save();
        }

        private void Lose()
        {
            State = GameState.Lost;
            _resultTimer = ResultDelay;
            _camera.Shake(0.35f, 0.4f);
            AudioManager.Sound(Sfx.Lose, 0.85f, 0f, 0f);
            AudioManager.Instance?.SetMusicScale(0.35f);
            Haptics.Heavy();
        }

        private void UpdateResultTimer(float dt)
        {
            if (_resultTimer <= 0f) return;
            _resultTimer -= dt;
            if (_resultTimer > 0f) return;

            if (State == GameState.Won)
            {
                bool hasNext = _levelIndex + 1 < SaveData.LevelCount;
                _ui.ShowWin(_levelIndex, _level.StarsFor(_crowd.Count), _crowd.Count, hasNext, !hasNext);
            }
            else
            {
                _ui.ShowLose();
            }
        }
    }
}
