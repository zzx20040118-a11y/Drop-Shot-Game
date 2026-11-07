using System.Collections.Generic;
using UnityEngine;

namespace FractureTool.Demo
{
    /// <summary>
    /// Timed shooting range: spawns breakable targets, keeps score and shows a small HUD.
    /// Put it on a trigger collider that covers the firing area.
    /// </summary>
    public class ShooterGame : MonoBehaviour
    {
        [Header("References")]
        public SimplePlayerController player;
        public ShooterWeapon weapon;
        public List<ShooterTarget> targetTemplates = new List<ShooterTarget>();
        public List<Transform> spawnPoints = new List<Transform>();

        [Header("Rules")]
        public float roundDuration = 60f;
        public int maxActiveTargets = 6;
        public float spawnInterval = 0.7f;
        [Range(0f, 1f)] public float movingTargetChance = 0.3f;
        public KeyCode startKey = KeyCode.E;

        enum State { Idle, Playing, Finished }

        struct Popup
        {
            public Vector3 position;
            public string text;
            public float time;
        }

        const string BestScoreKey = "FractureTool.ShooterBest";

        State state = State.Idle;
        bool playerInside;
        float timeLeft;
        float nextSpawn;
        int shots, hits, best;
        bool newBest;
        readonly List<ShooterTarget> active = new List<ShooterTarget>();
        readonly List<Popup> popups = new List<Popup>();
        GUIStyle panelStyle, bigStyle, popupStyle;

        public int Score { get; private set; }
        public bool IsPlaying => state == State.Playing;

        void Start()
        {
            best = PlayerPrefs.GetInt(BestScoreKey, 0);
            if (weapon != null) weapon.game = this;
            SetWeaponActive(false);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            playerInside = true;
            SetWeaponActive(true);
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            playerInside = false;
            SetWeaponActive(false);
        }

        void SetWeaponActive(bool on)
        {
            if (weapon != null) weapon.gameObject.SetActive(on);
            if (player != null) player.clickToFracture = !on;
        }

        void Update()
        {
            if (playerInside && state != State.Playing && Input.GetKeyDown(startKey))
                StartRound();

            if (state != State.Playing) return;

            active.RemoveAll(t => t == null);
            timeLeft -= Time.deltaTime;

            if (timeLeft <= 0f)
            {
                EndRound();
                return;
            }

            if (Time.time >= nextSpawn && active.Count < maxActiveTargets)
            {
                SpawnTarget();
                nextSpawn = Time.time + spawnInterval * Random.Range(0.6f, 1.4f);
            }
        }

        public void StartRound()
        {
            foreach (ShooterTarget t in active)
                if (t != null) Destroy(t.gameObject);
            active.Clear();

            Score = shots = hits = 0;
            newBest = false;
            timeLeft = roundDuration;
            nextSpawn = Time.time + 0.5f;
            state = State.Playing;
        }

        void EndRound()
        {
            state = State.Finished;

            foreach (ShooterTarget t in active.ToArray())
                if (t != null) t.Expire();
            active.Clear();

            if (Score > best)
            {
                best = Score;
                newBest = true;
                PlayerPrefs.SetInt(BestScoreKey, best);
                PlayerPrefs.Save();
            }
        }

        void SpawnTarget()
        {
            if (targetTemplates.Count == 0 || spawnPoints.Count == 0) return;

            // Pick a free spawn point
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Transform point = spawnPoints[Random.Range(0, spawnPoints.Count)];
                if (IsOccupied(point.position)) continue;

                ShooterTarget template = targetTemplates[Random.Range(0, targetTemplates.Count)];
                ShooterTarget target = Instantiate(template, point.position + Vector3.up * template.baseOffset, point.rotation * template.transform.rotation);
                target.name = template.name;
                target.game = this;
                if (Random.value < movingTargetChance)
                    target.moveAmplitude = Random.Range(0.6f, 1.2f);
                target.gameObject.SetActive(true);
                active.Add(target);
                return;
            }
        }

        bool IsOccupied(Vector3 position)
        {
            foreach (ShooterTarget t in active)
            {
                if (t == null) continue;
                Vector3 d = t.transform.position - position;
                d.y = 0f;
                if (d.sqrMagnitude < 2.5f * 2.5f) return true;
            }
            return false;
        }

        public void OnShotFired()
        {
            if (state == State.Playing) shots++;
        }

        public void OnTargetBroken(ShooterTarget target, Vector3 point)
        {
            active.Remove(target);
            if (state != State.Playing) return;

            hits++;
            Score += target.points;
            popups.Add(new Popup { position = point, text = "+" + target.points, time = Time.time });
        }

        public void OnTargetExpired(ShooterTarget target)
        {
            active.Remove(target);
        }

        void OnGUI()
        {
            if (!playerInside && state != State.Playing) return;

            if (panelStyle == null)
            {
                panelStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 16, richText = true, padding = new RectOffset(14, 14, 10, 10) };
                bigStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 28, richText = true };
                bigStyle.normal.textColor = Color.white;
                popupStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold };
            }

            int accuracy = shots > 0 ? Mathf.RoundToInt(100f * hits / shots) : 0;
            string panel =
                "<b>SHOOTING RANGE</b>\n" +
                $"Score      <b>{Score}</b>\n" +
                $"Time       {Mathf.CeilToInt(Mathf.Max(0f, state == State.Playing ? timeLeft : 0f))} s\n" +
                $"Accuracy   {accuracy}%\n" +
                $"Best       {best}";
            GUI.Box(new Rect(Screen.width - 232, 12, 220, 124), panel, panelStyle);

            Rect center = new Rect(0, Screen.height * 0.28f, Screen.width, 80);
            if (state == State.Idle)
                GUI.Label(center, $"Press <b>{startKey}</b> to start  -  {Mathf.RoundToInt(roundDuration)} second round", bigStyle);
            else if (state == State.Finished)
                GUI.Label(center, $"Time!  Score <b>{Score}</b>{(newBest ? "  -  NEW BEST!" : "")}\nPress <b>{startKey}</b> to play again", bigStyle);

            // Floating score popups
            Camera cam = Camera.main;
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                float age = Time.time - popups[i].time;
                if (age > 0.9f || cam == null)
                {
                    popups.RemoveAt(i);
                    continue;
                }

                Vector3 screen = cam.WorldToScreenPoint(popups[i].position + Vector3.up * age * 0.8f);
                if (screen.z < 0f) continue;

                popupStyle.normal.textColor = new Color(1f, 0.6f, 0.15f, 1f - age / 0.9f);
                GUI.Label(new Rect(screen.x - 50, Screen.height - screen.y - 20, 100, 40), popups[i].text, popupStyle);
            }
        }
    }
}
