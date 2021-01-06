using System;

namespace OLS.Casy.IO.SQLite.Entities
{
    public class UserEntity
    {
        public UserEntity()
        {
            CreatedAt = DateTime.MinValue;
            LastModifiedAt = DateTime.MinValue;
            DeletedAt = DateTime.MinValue;
        }
        public int UserEntityId { get; set; }
        public string Username { get; set; }

        public string Password { get; set; }
        
        public int UserRoleEntityId { get; set; }

        //[ForeignKey("UserRoleEntityId")]
        public UserRoleEntity UserRoleEntity { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string JobTitle { get; set; }

        public string CountryRegionName { get; set; }

        public string KeyboardCountryRegionName { get; set; }

        public string EmailAddress { get; set; }

        public byte[] Image { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public string LastModifiedBy { get; set; }

        public DateTime LastModifiedAt { get; set; }

        public bool IsDelete { get; set; }

        public string DeletedBy { get; set; }

        public DateTime DeletedAt { get; set; }

        public bool ForceCreatePassword { get; set; }

        public string RecentMeasureResultIds { get; set; }

        public int LastUsedSetupId { get; set; }

        public bool IsEmergencyUser { get; set; }

        public string RecentTemplateIds { get; set; }

        public string FavoriteTemplateIds { get; set; }
    }
}
