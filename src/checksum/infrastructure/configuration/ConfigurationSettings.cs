namespace checksum.infrastructure.configuration
{
    public class ConfigurationSettings
    {
        public string FilePath { get; set; }
        public string HashType { get; set; }
        public string HashToCheck { get; set; }

        /// <summary>
        /// Output type for the hash (b64 or hex)
        /// </summary>
        public string OutputType { get; set; }
    }
}