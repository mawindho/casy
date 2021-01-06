namespace OLS.Casy.Models
{
    public enum AccessMode
    {
        Read,
        Write
    }

    public class MeasureResultAccessMapping
    {
        public int MeasureResultAccessMappingId { get; set; }
        public MeasureResult MeasureResult { get; set; }
        public int? UserId { get; set; }
        public User User { get; set; }
        public int? UserGroupId { get; set; }
        public UserGroup UserGroup { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }

        public AccessMode AccessMode
        {
            get { return CanWrite ? AccessMode.Write: AccessMode.Read; }
            set
            {
                switch(value)
                {
                    case AccessMode.Read:
                        this.CanWrite = false;
                        this.CanRead = true;
                        break;
                    case AccessMode.Write:
                        this.CanWrite = true;
                        this.CanRead = false;
                        break;
                }
            }
        }

        public string Name
        {
            get { return this.UserGroupId == null ? this.User.Name : this.UserGroup.Name; }
        }
    }
}
