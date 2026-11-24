using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdSurge
{
    public enum GateOp { Add, Subtract, Multiply, Divide }

    [Serializable]
    public struct GateEffect
    {
        public GateOp op;
        public int value;

        public GateEffect(GateOp op, int value)
        {
            this.op = op;
            this.value = value;
        }

        public bool IsPositive => op == GateOp.Add || op == GateOp.Multiply;

        public string Label
        {
            get
            {
                switch (op)
                {
                    case GateOp.Add: return "+" + value;
                    case GateOp.Subtract: return "-" + value;
                    case GateOp.Multiply: return "x" + value;
                    default: return "/" + value;
                }
            }
        }

        public int Apply(int count)
        {
            switch (op)
            {
                case GateOp.Add: return count + value;
                case GateOp.Subtract: return count - value;
                case GateOp.Multiply: return count * value;
                default: return Mathf.CeilToInt(count / (float)Mathf.Max(1, value));
            }
        }
    }

    [Serializable]
    public struct GateDef
    {
        public float z;
        public GateEffect left;
        public GateEffect right;
    }

    [Serializable]
    public struct SawDef
    {
        public float z;
        public float x;
        [Tooltip("0 = stationary. Otherwise the blade patrols left/right by this amplitude.")]
        public float patrolAmplitude;
        public float patrolSpeed;
    }

    [Serializable]
    public struct EnemyDef
    {
        public float z;
        public float x;
        public int count;
        public bool isBoss;
    }

    /// <summary>Data-driven description of one level. Edit in the inspector to build new levels.</summary>
    [CreateAssetMenu(menuName = "CrowdSurge/Level Config", fileName = "Level01")]
    public sealed class LevelConfig : ScriptableObject
    {
        public string levelName = "EASY";
        public int startCount = 10;
        public float finishZ = 300f;
        [Tooltip("Remaining crowd needed at the finish for 2 and 3 stars.")]
        public int star2Count = 20;
        public int star3Count = 50;

        [Header("Look")]
        public Color skyColor = new Color(0.36f, 0.67f, 1f);
        public Color roadLight = new Color(0.78f, 0.8f, 0.86f);
        public Color roadDark = new Color(0.58f, 0.6f, 0.68f);

        [Header("Content")]
        public List<GateDef> gates = new List<GateDef>();
        public List<SawDef> saws = new List<SawDef>();
        public List<EnemyDef> enemies = new List<EnemyDef>();

        public int StarsFor(int remaining) => remaining >= star3Count ? 3 : remaining >= star2Count ? 2 : 1;

        private static GateDef Gate(float z, GateEffect left, GateEffect right) => new GateDef { z = z, left = left, right = right };
        private static GateEffect Add(int v) => new GateEffect(GateOp.Add, v);
        private static GateEffect Sub(int v) => new GateEffect(GateOp.Subtract, v);
        private static GateEffect Mul(int v) => new GateEffect(GateOp.Multiply, v);
        private static GateEffect Div(int v) => new GateEffect(GateOp.Divide, v);
        private static SawDef Saw(float z, float x, float amp = 0f, float speed = 0f) => new SawDef { z = z, x = x, patrolAmplitude = amp, patrolSpeed = speed };
        private static EnemyDef Enemy(float z, int count, bool boss = false) => new EnemyDef { z = z, x = 0f, count = count, isBoss = boss };

        /// <summary>Fills this config with one of the three hand-tuned levels (0 = easy, 1 = medium, 2 = hard).</summary>
        public void Build(int index)
        {
            switch (index)
            {
                case 0: BuildEasy(); break;
                case 1: BuildMedium(); break;
                default: BuildHard(); break;
            }
        }

        private void BuildEasy()
        {
            levelName = "EASY";
            startCount = 10;
            finishZ = 270f;
            star2Count = 25;
            star3Count = 50;
            skyColor = new Color(0.36f, 0.67f, 1f);
            roadLight = new Color(0.78f, 0.8f, 0.86f);
            roadDark = new Color(0.58f, 0.6f, 0.68f);

            gates = new List<GateDef>
            {
                Gate(30, Mul(2), Add(8)),
                Gate(85, Add(15), Mul(2)),
                Gate(145, Mul(2), Add(10)),
                Gate(195, Add(20), Sub(5)),
            };
            saws = new List<SawDef> { Saw(60, -2f), Saw(170, 2.5f) };
            enemies = new List<EnemyDef> { Enemy(115, 8), Enemy(232, 25, true) };
        }

        private void BuildMedium()
        {
            levelName = "MEDIUM";
            startCount = 12;
            finishZ = 380f;
            star2Count = 30;
            star3Count = 70;
            skyColor = new Color(1f, 0.62f, 0.42f);
            roadLight = new Color(0.86f, 0.76f, 0.72f);
            roadDark = new Color(0.66f, 0.54f, 0.54f);

            gates = new List<GateDef>
            {
                Gate(30, Add(10), Mul(2)),
                Gate(90, Mul(2), Sub(8)),
                Gate(150, Mul(3), Add(15)),
                Gate(205, Sub(10), Mul(2)),
                Gate(265, Add(30), Mul(2)),
            };
            saws = new List<SawDef>
            {
                Saw(55, -2.5f), Saw(68, 2.5f),
                Saw(125, 0f, 3f, 1.5f),
                Saw(175, -3f), Saw(190, 3f),
                Saw(240, -3f),
            };
            enemies = new List<EnemyDef> { Enemy(118, 20), Enemy(228, 55), Enemy(330, 100, true) };
        }

        private void BuildHard()
        {
            levelName = "HARD";
            startCount = 15;
            finishZ = 500f;
            star2Count = 25;
            star3Count = 60;
            skyColor = new Color(0.2f, 0.16f, 0.42f);
            roadLight = new Color(0.52f, 0.52f, 0.72f);
            roadDark = new Color(0.36f, 0.36f, 0.56f);

            gates = new List<GateDef>
            {
                Gate(30, Mul(2), Add(10)),
                Gate(85, Mul(2), Sub(12)),
                Gate(145, Mul(3), Add(20)),
                Gate(205, Div(2), Add(40)),
                Gate(265, Mul(2), Sub(25)),
                Gate(335, Add(60), Mul(2)),
                Gate(395, Mul(3), Div(2)),
            };
            saws = new List<SawDef>
            {
                Saw(50, -2.5f), Saw(64, 2.5f),
                Saw(110, 0f, 3.5f, 1.8f),
                Saw(170, -3f), Saw(184, 3f),
                Saw(240, 0f, 3.5f, 2f),
                Saw(365, 0f, 4f, 2.5f),
            };
            enemies = new List<EnemyDef> { Enemy(118, 22), Enemy(225, 35), Enemy(300, 45), Enemy(450, 150, true) };
        }
    }
}
