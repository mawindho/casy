using OLS.Casy.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OLS.Casy.Ui.Authorization.Converters
{
    public class CanDeleteUserConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var users = values[0] as IList<UserModel>;
            var currentUser = values[1] as UserModel;
            var loggedInUser = values[2] as User;

            return currentUser.UserName != loggedInUser.Identity.Name && (currentUser.UserRole != "Supervisor" || users.Where(user => user.UserRole == "Supervisor").Count() > 1);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
