namespace _ExampleProject.Code.Features._Core.Audio
{
    /// <summary>
    /// Дискретные стадии динамического саундтрека.
    /// Значения соответствуют числовому параметру musicState в проекте FMOD:
    ///   0 — Intro    (вступление, стартует при загрузке уровня)
    ///   1 — Ambient  (фоновый слой, низкая интенсивность)
    ///   2 — Lead     (ведущая мелодия, средняя интенсивность)
    ///   3 — DrumsBass(ударные и бас, высокая интенсивность)
    ///
    /// Порядок значений важен: он совпадает с числовым значением параметра FMOD,
    /// которое передаётся через EventInstance.setParameterByName("musicState", (float)state).
    /// </summary>
    public enum MusicState
    {
        Intro     = 0,
        Ambient   = 1,
        Lead      = 2,
        DrumsBass = 3
    }
}
