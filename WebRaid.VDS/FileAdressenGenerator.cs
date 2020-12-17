namespace WebRaid.VDS
{
    internal class FileAdressenGenerator:IFileAdressenGenerator
    {
        public string GetNew()
        {
            return GetNew(4, "/");
        }

        public string GetNew(int blockGröße)
        {
            return GetNew(blockGröße, "/");
        }

        public string GetNew(string separator)
        {
            return GetNew(4, separator);
        }
        public string GetNew(int blockGröße, string separator)
        {
            var gui = System.Guid.NewGuid().ToString("N");
            for (var i = (gui.Length-1) / blockGröße; i > 0; i--)
            {
                gui = gui.Insert(i * blockGröße, separator);
            }

            return gui;
        }
    }
}