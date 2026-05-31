using UnityEngine;

namespace AdaptiveDifficulty.Runtime
{
    /// <summary>
    /// Отладочный оверлей системы адаптивной сложности.
    ///
    /// Отображает в реальном времени:
    ///   • D — текущую сложность с направлением изменения
    ///   • I (Performance) и S_safety с прогресс-барами
    ///   • Интенсивность боя (для саундтрека)
    ///   • EMA-сглаженные метрики: убийства/с, урон, комбо, точность, расходники
    ///   • Сырую статистику: комбо, всего убийств
    ///
    /// Управляется из AdaptiveDifficultyDebugSystem.
    /// Переключается клавишей F1 (настраивается через ToggleKey).
    /// Окно можно перетаскивать мышью.
    /// </summary>
    public sealed class AdaptiveDifficultyDebugView : MonoBehaviour
    {
        // Настройки (задаются из системы после создания компонента)
        public KeyCode ToggleKey  = KeyCode.F1;
        public float   UiScale    = 1f;
        public bool    Visible    = true;

        // Данные, обновляемые каждый тик из AdaptiveDifficultyDebugSystem
        private AdaptiveDifficultySnapshot _diffSnap;
        private AdaptiveStatsSnapshot      _statsSnap;
        private int   _currentCombo;
        private int   _totalKills;
        private string _musicStateName = "Ambient";
        private float _prevDifficulty;
        private float _difficultyDelta;

        // Состояние окна
        private Rect    _windowRect  = new Rect(10f, 10f, 430f, 0f);
        private bool    _isDragging;
        private Vector2 _dragOffset;

        // Стили GUI (инициализируются лениво в OnGUI)
        private GUIStyle _windowBg;
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _hintStyle;
        private bool     _stylesReady;

        // Цветовая палитра
        private static readonly Color ColGood    = new Color(0.30f, 1.00f, 0.45f);
        private static readonly Color ColBad     = new Color(1.00f, 0.35f, 0.30f);
        private static readonly Color ColNeutral = new Color(0.90f, 0.90f, 0.90f);
        private static readonly Color ColHeader  = new Color(0.40f, 0.70f, 1.00f);
        private static readonly Color ColSection = new Color(0.60f, 0.82f, 1.00f);
        private static readonly Color ColBarBg   = new Color(0.12f, 0.12f, 0.18f);
        private static readonly Color ColWinBg   = new Color(0.05f, 0.05f, 0.10f, 0.93f);

        private const float BAR_W  = 190f;
        private const float BAR_H  = 11f;
        private const float MARGIN = 8f;

        // ─── Обновление данных ────────────────────────────────────────────────

        public void SetData(
            AdaptiveDifficultySnapshot diffSnap,
            AdaptiveStatsSnapshot      statsSnap,
            int currentCombo,
            int totalKills,
            string musicStateName = "Ambient")
        {
            _difficultyDelta = diffSnap.Difficulty - _prevDifficulty;
            _prevDifficulty  = diffSnap.Difficulty;
            _diffSnap        = diffSnap;
            _statsSnap       = statsSnap;
            _currentCombo    = currentCombo;
            _totalKills      = totalKills;
            _musicStateName = musicStateName;
        }

        // ─── Unity ────────────────────────────────────────────────────────────

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
                Visible = !Visible;
        }

        private void OnGUI()
        {
            // Подсказка о горячей клавише — всегда видна
            EnsureStyles();
            GUI.Label(new Rect(10f, 200, 260f, 20f),
                      $"[{ToggleKey}] Дебаг сложности", _hintStyle);

            if (!Visible)
                return;

            Matrix4x4 prevMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(UiScale, UiScale, 1f));

            // Рисуем фон окна
            float scaledX = _windowRect.x / UiScale;
            float scaledY = _windowRect.y / UiScale;

            // Вычислим высоту содержимого через Layout
            Rect contentRect = new Rect(scaledX + MARGIN, scaledY + MARGIN,
                                        _windowRect.width - MARGIN * 2f, 9999f);

            // Фон
            DrawWindowBackground(new Rect(scaledX, scaledY, _windowRect.width, GetWindowHeight()));

            // Содержимое
            GUILayout.BeginArea(contentRect);
            DrawContent();
            GUILayout.EndArea();

            GUI.matrix = prevMatrix;

            HandleDrag();
        }

        // ─── Отрисовка содержимого ────────────────────────────────────────────

        private void DrawContent()
        {
            // Заголовок
            GUILayout.Label("⚙  ADAPTIVE DIFFICULTY  DEBUG", _headerStyle);
            DrawHLine();
            GUILayout.Space(2f);

            // ── Глобальная сложность D ────────────────────────────────────────
            DrawSection("ГЛОБАЛЬНАЯ СЛОЖНОСТЬ  D");

            float d      = _diffSnap.Difficulty;
            string delta = _difficultyDelta >= 0f
                ? $"+{_difficultyDelta:F4} ▲"
                : $"{_difficultyDelta:F4} ▼";
            Color deltaCol = _difficultyDelta >= 0f ? ColBad : ColGood;

            DrawLabelBar("  D:", $"{d:F4}", d, Color.Lerp(ColGood, ColBad, d));
            DrawLabelValue("  Изменение:", delta, deltaCol);
            GUILayout.Space(4f);

            // ── Показатели I и S_safety ───────────────────────────────────────
            DrawSection("ПОКАЗАТЕЛИ АДАПТАЦИИ");

            float I = _diffSnap.PerformanceScore;
            float S = _diffSnap.SafetyScore;
            float balance = I - S;

            DrawLabelBar("  I (эффект.):", $"{I:F3}", I, ColGood);
            DrawLabelBar("  S (уязв.):",   $"{S:F3}", S, ColBad);

            string balStr  = balance >= 0f ? $"+{balance:F3}  ↑ D растёт" : $"{balance:F3}  ↓ D снижается";
            Color  balCol  = balance >= 0f ? ColBad : ColGood;
            DrawLabelValue("  I − S:", balStr, balCol);
            GUILayout.Space(4f);

            // ── Интенсивность боя ─────────────────────────────────────────────
            DrawSection("ИНТЕНСИВНОСТЬ БОЯ  (саундтрек)");

            float intensity = _diffSnap.RuntimeIntensity;
            Color intCol = Color.Lerp(new Color(0.3f, 0.85f, 0.3f), new Color(1f, 0.3f, 0.1f), intensity);
            DrawLabelBar("  Intensity:", $"{intensity:F3}", intensity, intCol);
            DrawLabelValue("  musicState:", _musicStateName, ColNeutral);
            GUILayout.Space(4f);
            GUILayout.Space(4f);

            // ── EMA-метрики ───────────────────────────────────────────────────
            DrawSection("EMA-СГЛАЖЕННЫЕ МЕТРИКИ");

            float maxKills = 1f; // нормализованы уже в snapshot к kills/s
            DrawMetricBar("  Убийства/с:", $"{_statsSnap.KillsPerSecond:F3}", _statsSnap.KillsPerSecond, 1f, ColGood);
            DrawMetricBar("  Урон нан./с:", $"{_statsSnap.DamageDealtPerSecond:F1}", _statsSnap.DamageDealtPerSecond, 100f, ColGood);
            DrawMetricBar("  Комбо (EMA):", $"{_statsSnap.Combo:F2}", _statsSnap.Combo, 15f, ColGood);
            DrawMetricBar("  Урон пол./с:", $"{_statsSnap.DamageTakenPerSecond:F1}", _statsSnap.DamageTakenPerSecond, 50f, ColBad);

            float acc    = _statsSnap.Accuracy;
            Color accCol = acc >= 0.6f ? ColGood : (acc >= 0.3f ? Color.yellow : ColBad);
            DrawMetricBar("  Точность:", $"{acc * 100f:F0}%", acc, 1f, accCol);
            DrawMetricBar("  Патр.подбор/с:", $"{_statsSnap.AmmoPickupRate:F3}", _statsSnap.AmmoPickupRate, 1f, ColBad);
            DrawMetricBar("  Хил.подбор/с:", $"{_statsSnap.HealthPickupRate:F3}", _statsSnap.HealthPickupRate, 1f, ColBad);
            GUILayout.Space(4f);

            // ── Сырая статистика ──────────────────────────────────────────────
            DrawSection("ТЕКУЩАЯ СЕССИЯ (raw)");

            DrawLabelValue("  Убийств всего:", $"{_totalKills}", ColNeutral);
            Color comboCol = _currentCombo >= 5 ? ColGood : (_currentCombo >= 2 ? Color.yellow : ColNeutral);
            DrawLabelValue("  Активное комбо:", $"{_currentCombo}", comboCol);
            GUILayout.Space(4f);
        }

        // ─── Утилиты рисования ────────────────────────────────────────────────

        private void DrawWindowBackground(Rect rect)
        {
            Color prev = GUI.color;
            GUI.color = ColWinBg;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private void DrawHLine()
        {
            Color prev = GUI.color;
            GUI.color = new Color(0.35f, 0.40f, 0.60f, 0.7f);
            GUILayout.Box("", GUILayout.Height(1f), GUILayout.ExpandWidth(true));
            GUI.color = prev;
            GUILayout.Space(2f);
        }

        private void DrawSection(string title)
        {
            GUILayout.Label(title, _sectionStyle);
        }

        private void DrawLabelValue(string label, string value, Color valueColor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(140f));
            Color prev = GUI.color;
            GUI.color = valueColor;
            GUILayout.Label(value, _valueStyle);
            GUI.color = prev;
            GUILayout.EndHorizontal();
        }

        private void DrawLabelBar(string label, string valueText, float value01, Color barColor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(140f));

            Color prev = GUI.color;
            GUI.color = barColor;
            GUILayout.Label(valueText, _valueStyle, GUILayout.Width(52f));
            GUI.color = prev;

            Rect r = GUILayoutUtility.GetRect(BAR_W, BAR_H, GUILayout.Width(BAR_W));
            DrawBar(r, Mathf.Clamp01(value01), barColor);

            GUILayout.EndHorizontal();
            GUILayout.Space(2f);
        }

        private void DrawMetricBar(string label, string valueText, float value, float maxValue, Color barColor)
        {
            float norm = maxValue > 0f ? Mathf.Clamp01(value / maxValue) : 0f;
            DrawLabelBar(label, valueText, norm, barColor);
        }

        private void DrawBar(Rect r, float fill, Color fillColor)
        {
            Color prev = GUI.color;

            GUI.color = ColBarBg;
            GUI.DrawTexture(r, Texture2D.whiteTexture);

            if (fill > 0.001f)
            {
                GUI.color = fillColor;
                GUI.DrawTexture(new Rect(r.x, r.y, r.width * fill, r.height), Texture2D.whiteTexture);
            }

            GUI.color = prev;
        }

        // ─── Перетаскивание окна ──────────────────────────────────────────────

        private void HandleDrag()
        {
            Event e = Event.current;

            // Масштабированный titlebar (верхние 20px)
            float scaledX = _windowRect.x / UiScale;
            float scaledY = _windowRect.y / UiScale;
            Rect titleBar = new Rect(scaledX * UiScale, scaledY * UiScale,
                                     _windowRect.width * UiScale, 20f * UiScale);

            if (e.type == EventType.MouseDown && titleBar.Contains(e.mousePosition))
            {
                _isDragging = true;
                _dragOffset = e.mousePosition - new Vector2(_windowRect.x, _windowRect.y);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _isDragging)
            {
                _windowRect.x = Mathf.Clamp(e.mousePosition.x - _dragOffset.x, 0f, Screen.width  - _windowRect.width);
                _windowRect.y = Mathf.Clamp(e.mousePosition.y - _dragOffset.y, 0f, Screen.height - 50f);
                e.Use();
            }
            else if (e.type == EventType.MouseUp)
            {
                _isDragging = false;
            }
        }

        // ─── Вспомогательные ─────────────────────────────────────────────────

        private float GetWindowHeight()
        {
            // Подсчёт высоты по строкам контента:
            //   Header(24) + HLine(7) + Space(2)
            //   Секция D:          label(18) + bar(20) + value(18) + space(4)        = 60
            //   Секция Адаптация:  label(18) + bar(20)*2 + value(18) + space(4)      = 80
            //   Секция Интенс.:    label(18) + bar(20) + space(4)                    = 42
            //   Секция EMA:        label(18) + bar(20)*7 + space(4)                  = 162
            //   Секция Сессия:     label(18) + value(18)*2 + space(4)                = 58
            //   Отступы окна (MARGIN*2)                                              = 16
            // Итого: ~451, берём с запасом
            return 480f;
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _headerStyle.normal.textColor = ColHeader;

            _sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _sectionStyle.normal.textColor = ColSection;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 10,
                alignment = TextAnchor.MiddleLeft
            };
            _labelStyle.normal.textColor = new Color(0.75f, 0.75f, 0.80f);

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _valueStyle.normal.textColor = ColNeutral;

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 9,
                alignment = TextAnchor.LowerLeft
            };
            _hintStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);

            _windowBg = new GUIStyle(GUI.skin.box);
        }
    }
}
