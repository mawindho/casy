using System.Collections.Generic;

namespace OLS.Casy.IO.SQLite.Entities
{
    public class UserRoleEntity
    {
        private ICollection<UserEntity> _userEntities;

        public UserRoleEntity()
        {
            this._userEntities = new List<UserEntity>();
        }

        public int UserRoleEntityId { get; set; }
        public string Name { get; set; }
        public int Priority { get; set; }

        public virtual ICollection<UserEntity> Users
        {
            get { return _userEntities; }
            set { this._userEntities = value; }
        }
    }
}
