namespace WebRaid.VDS
{
    public interface IFileAdressenGenerator
    {
        string GetNew();
        string GetNew(int blockGröße);
        string GetNew(string separator);
        string GetNew(int blockGröße, string separator);
    }
}