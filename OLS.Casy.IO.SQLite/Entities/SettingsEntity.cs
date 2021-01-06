namespace OLS.Casy.IO.SQLite.Entities
{
    public class SettingsEntity
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public byte[] BlobValue { get; set; }
    }
}
