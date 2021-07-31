using Microsoft.Xrm.Sdk.Metadata;

namespace MsCrmTools.AccessChecker
{
    public class EntityInfo
    {
        public EntityInfo(EntityMetadata emd)
        {
            Metadata = emd;

            LogicalName = emd.LogicalName;
            DisplayName = emd.DisplayName?.UserLocalizedLabel?.Label ?? "N/A";
            PrimaryAttribute = emd.PrimaryNameAttribute;
        }

        public EntityInfo(string logicalName, string displayName, string primaryAttribute)
        {
            LogicalName = logicalName;
            DisplayName = displayName;
            PrimaryAttribute = primaryAttribute;
        }

        public string DisplayName { get; private set; }
        public string LogicalName { get; private set; }
        public string PrimaryAttribute { get; private set; }

        public EntityMetadata Metadata { get;  }

        public override string ToString()
        {
            return string.Format("{0} ({1})", DisplayName, LogicalName);
        }
    }
}