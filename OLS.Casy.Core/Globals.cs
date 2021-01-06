using System;
using System.Collections.Generic;
using System.Text;

namespace OLS.Casy.Core
{
    public static  class Globals
    {
        public static Action<object> ShowSplashCustomDialogDelegate { get; set; }
        public static Action<object> ShowSplashMessageDialogDelegate { get; set; }
        public static Action<object> ShowSplashProgressDialogDelegate { get; set; }
        public static IProgress<string> SplashProgress { get; set; }
    }
}
