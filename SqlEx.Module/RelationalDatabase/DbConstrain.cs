namespace SqlEx.Module.RelationalDatabase
{
    public class DbConstrain
    {
        public string Table { get; set; }

        public string Name { get; set; }

        public string Column { get; set; }

        public ConstrainType Type { get; set; }

        public enum ConstrainType
        {
            Index,
            PrimaryKey,
            DefaultConstraint,
        }
    }
}
