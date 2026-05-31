#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// ВАЖНО: этот файл находится в Plugins/AdaptiveDifficulty/Editor/ и компилируется в
// Assembly-CSharp-Editor-firstpass. Из этой сборки НЕЛЬЗЯ ссылаться на классы
// из _Project/ (они компилируются позже в Assembly-CSharp). Поэтому здесь используются
// только типы из Plugins/AdaptiveDifficulty/Runtime/ (той же firstpass-сборки).

namespace AdaptiveDifficulty.Editor
{
    /// <summary>
    /// Кастомный инспектор для AdaptiveDifficultySettings.
    /// Разбивает параметры на 9 сворачиваемых групп с HelpBox-описаниями и
    /// предупреждениями о некорректных значениях.
    /// Не требует Odin Inspector.
    /// </summary>
    [CustomEditor(typeof(Runtime.AdaptiveDifficultySettings))]
    public sealed class AdaptiveDifficultySettingsEditor : UnityEditor.Editor
    {
        private static readonly Color HeaderColor = new Color(0.40f, 0.75f, 1.00f);

        // Foldout-состояния (сохраняем в EditorPrefs между сессиями)
        private const string P = "ADS2_";
        private bool _foldEma, _foldNorm, _foldPerf, _foldSafety,
                     _foldDiff, _foldRuntime, _foldCombo, _foldInteg, _foldDebug;

        private GUIStyle _titleStyle;
        private GUIStyle _formulaStyle;
        private bool     _stylesReady;

        private void OnEnable()
        {
            _foldEma     = EditorPrefs.GetBool(P + "ema",     true);
            _foldNorm    = EditorPrefs.GetBool(P + "norm",    true);
            _foldPerf    = EditorPrefs.GetBool(P + "perf",    true);
            _foldSafety  = EditorPrefs.GetBool(P + "safety",  true);
            _foldDiff    = EditorPrefs.GetBool(P + "diff",    true);
            _foldRuntime = EditorPrefs.GetBool(P + "runtime", true);
            _foldCombo   = EditorPrefs.GetBool(P + "combo",   true);
            _foldInteg   = EditorPrefs.GetBool(P + "integ",   false);
            _foldDebug   = EditorPrefs.GetBool(P + "debug",   true);
        }

        private void OnDisable()
        {
            EditorPrefs.SetBool(P + "ema",     _foldEma);
            EditorPrefs.SetBool(P + "norm",    _foldNorm);
            EditorPrefs.SetBool(P + "perf",    _foldPerf);
            EditorPrefs.SetBool(P + "safety",  _foldSafety);
            EditorPrefs.SetBool(P + "diff",    _foldDiff);
            EditorPrefs.SetBool(P + "runtime", _foldRuntime);
            EditorPrefs.SetBool(P + "combo",   _foldCombo);
            EditorPrefs.SetBool(P + "integ",   _foldInteg);
            EditorPrefs.SetBool(P + "debug",   _foldDebug);
        }

        public override void OnInspectorGUI()
        {
            EnsureStyles();
            serializedObject.Update();

            var s = (Runtime.AdaptiveDifficultySettings)target;

            // ── Заголовок ────────────────────────────────────────────────────
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("⚙  Adaptive Difficulty Settings", _titleStyle);
            EditorGUILayout.HelpBox(
                "Контур обратной связи DDA:\n" +
                "действия игрока → EMA-сглаживание → I и S_safety\n" +
                "→ D_new = D + β · (I − S_safety) → параметры уровня + музыка\n\n" +
                "Дебаг-оверлей в Play Mode: клавиша F1",
                MessageType.None);
            EditorGUILayout.Space(6f);

            // ── 1. EMA ───────────────────────────────────────────────────────
            _foldEma = Fold(_foldEma, "1.  Сглаживание метрик (EMA)");
            if (_foldEma)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "Временные константы τ (сек): α = dt / (τ + dt)\n" +
                    "Большее τ → медленнее реагирует, устойчивее оценка.",
                    MessageType.Info);
                Prop("TauKills");
                Prop("TauDamageDealt");
                Prop("TauDamageTaken");
                Prop("TauAccuracy");
                Prop("TauCombo");
                Prop("TauPickups");
                EditorGUI.indentLevel--;
            }
            EndFold();
            EditorGUILayout.Space(3f);

            // ── 2. Нормализация ───────────────────────────────────────────────
            _foldNorm = Fold(_foldNorm, "2.  Нормализация метрик");
            if (_foldNorm)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "Опорные значения для приведения метрик к [0, 1].\n" +
                    "Пример: NormalizedKillsPerSecond = 0.33 → 1 убийство за ~3 сек = вклад 1.0 в I.",
                    MessageType.Info);
                Prop("NormalizedKillsPerSecond");
                Prop("NormalizedDamageDealtPerSecond");
                Prop("NormalizedDamageTakenPerSecond");
                Prop("NormalizedCombo");
                Prop("NormalizedPickupsPerSecond");
                EditorGUI.indentLevel--;
            }
            EndFold();
            EditorGUILayout.Space(3f);

            // ── 3. Показатель I ───────────────────────────────────────────────
            _foldPerf = Fold(_foldPerf, "3.  Показатель эффективности  I");
            if (_foldPerf)
            {
                EditorGUI.indentLevel++;
                DrawFormula("I = KillWeight·kills + ComboWeight·combo + DamageWeight·damage\n" +
                            "→ clamp01( I ^ PerformanceGamma )");
                Prop("KillWeight");
                Prop("ComboWeight");
                Prop("DamageWeight");

                float w = s.KillWeight + s.ComboWeight + s.DamageWeight;
                if (Mathf.Abs(w - 1f) > 0.1f)
                    EditorGUILayout.HelpBox($"⚠  Сумма весов I = {w:F2} (рекомендуется ≈ 1.0)", MessageType.Warning);

                EditorGUILayout.Space(2f);
                Prop("PerformanceGamma");
                EditorGUI.indentLevel--;
            }
            EndFold();
            EditorGUILayout.Space(3f);

            // ── 4. Показатель S_safety ────────────────────────────────────────
            _foldSafety = Fold(_foldSafety, "4.  Показатель уязвимости  S_safety");
            if (_foldSafety)
            {
                EditorGUI.indentLevel++;
                DrawFormula("S = DamageTakenWeight · dmgTaken\n" +
                            "  + AccuracyPenaltyWeight · (1 − acc)\n" +
                            "  + PickupsWeight · pickups");
                Prop("DamageTakenWeight");
                Prop("AccuracyPenaltyWeight");
                Prop("PickupsWeight");

                float ws = s.DamageTakenWeight + s.AccuracyPenaltyWeight + s.PickupsWeight;
                if (Mathf.Abs(ws - 1f) > 0.1f)
                    EditorGUILayout.HelpBox($"⚠  Сумма весов S = {ws:F2} (рекомендуется ≈ 1.0)", MessageType.Warning);

                EditorGUI.indentLevel--;
            }
            EndFold();
            EditorGUILayout.Space(3f);

            // ── 5. Обновление D ───────────────────────────────────────────────
            _foldDiff = Fold(_foldDiff, "5.  Обновление сложности  D");
            if (_foldDiff)
            {
                EditorGUI.indentLevel++;
                DrawFormula("D_new = clamp01( D + β · (I − S_safety) )\n" +
                            "β = BetaUp если I ≥ S,  иначе BetaDown");
                EditorGUILayout.HelpBox(
                    "D обновляется один раз при CommitLevel() — смена уровня.\n" +
                    "MaxDifficultyDelta ограничивает максимальный шаг за один уровень.",
                    MessageType.Info);
                Prop("InitialDifficulty");
                Prop("BetaUp");
                Prop("BetaDown");
                Prop("MaxDifficultyDelta");

                if (s.BetaUp <= 0f || s.BetaDown <= 0f)
                    EditorGUILayout.HelpBox("⚠  BetaUp/BetaDown должны быть > 0.", MessageType.Warning);

                EditorGUI.indentLevel--;
            }
            EndFold();
            EditorGUILayout.Space(3f);

            // ── 6. Интенсивность (саундтрек) ──────────────────────────────────
            _foldRuntime = Fold(_foldRuntime, "6.  Интенсивность в реальном времени  (саундтрек)");
            if (_foldRuntime)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "RuntimeIntensity обновляется каждый кадр и управляет слоями DynamicSoundtrackView.\n" +
                    "RuntimeTau — скорость нарастания интенсивности.\n" +
                    "AudioTau — дополнительное сглаживание перед кроссфейдом.\n" +
                    "Рекомендуется: AudioTau < RuntimeTau.",
                    MessageType.Info);
                Prop("RuntimeTau");
                Prop("AudioTau");

                if (s.AudioTau >= s.RuntimeTau)
                    EditorGUILayout.HelpBox("⚠  AudioTau ≥ RuntimeTau — музыка будет реагировать медленнее ожидаемого.", MessageType.Warning);

                EditorGUI.indentLevel--;
            }
            EndFold();
            EditorGUILayout.Space(3f);

            // ── 7. Комбо ──────────────────────────────────────────────────────
            _foldCombo = Fold(_foldCombo, "7.  Комбо");
            if (_foldCombo)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "Комбо растёт при убийствах и сбрасывается,\n" +
                    "если пауза без убийства ≥ ComboResetDelay секунд.",
                    MessageType.Info);
                Prop("ComboResetDelay");
                EditorGUI.indentLevel--;
            }
            EndFold();
            EditorGUILayout.Space(3f);

            // ── 8. Интеграция ─────────────────────────────────────────────────
            _foldInteg = Fold(_foldInteg, "8.  Интеграция с проектом  (обычно не требует изменений)");
            if (_foldInteg)
            {
                EditorGUI.indentLevel++;
                Prop("DefaultPlayerMaxHealth");
                Prop("DefaultPlayerArmor");
                Prop("DefaultProjectileDamage");
                Prop("ProjectileOwnerSearchRadius");
                Prop("CountAnyEnemyHealthLossAsPlayerDamage");
                Prop("CountEnemyDespawnAsKill");
                EditorGUI.indentLevel--;
            }
            EndFold();
            EditorGUILayout.Space(3f);

            // ── 9. Отладка ────────────────────────────────────────────────────
            _foldDebug = Fold(_foldDebug, "9.  Отладка");
            if (_foldDebug)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "EnableDebugLogs → D, I, S, Intensity в Console каждые DebugLogInterval сек.\n" +
                    "ShowDebugHud → OnGUI-оверлей в Play Mode (переключить: F1).\n" +
                    "Отключите оба в релизе.",
                    MessageType.Info);
                Prop("EnableDebugLogs");
                Prop("DebugLogInterval");
                Prop("ShowDebugHud");
                EditorGUI.indentLevel--;
            }
            EndFold();

            EditorGUILayout.Space(4f);
            serializedObject.ApplyModifiedProperties();
        }

        // ─── Вспомогательные методы ───────────────────────────────────────────

        private bool Fold(bool state, string label)
            => EditorGUILayout.BeginFoldoutHeaderGroup(state, label);

        private static void EndFold()
            => EditorGUILayout.EndFoldoutHeaderGroup();

        private void DrawFormula(string text)
        {
            EditorGUILayout.LabelField(text, _formulaStyle);
            EditorGUILayout.Space(2f);
        }

        private void Prop(string propName)
        {
            var prop = serializedObject.FindProperty(propName);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, true);
            else
                EditorGUILayout.HelpBox($"[!] Свойство '{propName}' не найдено.", MessageType.Error);
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 13,
                alignment = TextAnchor.MiddleCenter
            };
            _titleStyle.normal.textColor = HeaderColor;

            _formulaStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 10,
                fontStyle = FontStyle.Italic,
                wordWrap  = true
            };
        }
    }
}
#endif
