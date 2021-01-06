namespace OLS.Casy.Ui.Base
{
    public class NavigationArgs
    {
        public NavigationArgs(NavigationCategory navigationCategory)
        {
            this.NavigationCategory = navigationCategory;
        }
        public NavigationCategory NavigationCategory { get; private set; }
        public object Parameter { get; set; }
    }
}
