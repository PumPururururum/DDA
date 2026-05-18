namespace AdaptiveDifficulty.Runtime
{
    public interface IAdaptiveTelemetrySource
    {
        bool IsReady { get; }

        void Tick(float dt);

        AdaptiveFrameTelemetry ConsumeFrameTelemetry();
    }
}