namespace OLS.Casy.Models
{
    public class UserRole
    {
        public static UserRole None = new UserRole() { Name = "None", Priority = 0 };

        public int Id { get; set; }
        
        public int Priority { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
