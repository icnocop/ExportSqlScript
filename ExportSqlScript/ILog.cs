namespace ExportSqlScript
{
    public interface ILog
    {
        void Information(string message);

        void Warning(string message);

        void Debug(string message);
    }
}
