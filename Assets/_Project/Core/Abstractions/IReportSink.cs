namespace PovuciPotegni.Core
{
    /// <summary>Receives questions the player reported as wrong.</summary>
    public interface IReportSink
    {
        void ReportQuestion(string questionId);
    }
}
