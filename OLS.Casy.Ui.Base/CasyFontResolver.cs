using System;
using PdfSharp.Fonts;
using System.IO;
using System.Windows;

namespace OLS.Casy.Ui.Base
{
    public class CasyFontResolver : IFontResolver
    {
        public byte[] GetFont(string faceName)
        {
            switch (faceName)
            {
                case "Dosis#Regular":
                    return FontHelper.DosisRegular;
                case "Dosis#Light":
                    return FontHelper.DosisLight;
                case "Dosis#Bold":
                    return FontHelper.DosisBold;
            }

            return null;
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            var name = familyName.ToLower();

            switch (name)
            {
                case "dosis-regular":
                    return new FontResolverInfo("Dosis#Regular");
                case "dosis-light":
                    if (isBold)
                    {
                        return new FontResolverInfo("Dosis#Light");
                    }
                    else
                    { 
                        return new FontResolverInfo("Dosis#Bold");
                    }
                    //if (isBold)
                    //{
                    //    if (isItalic)
                    //        return new FontResolverInfo("Ubuntu#bi");
                    //    return new FontResolverInfo("Ubuntu#b");
                    //}
                    //if (isItalic)
                    //    return new FontResolverInfo("Ubuntu#i");

            }

            return PlatformFontResolver.ResolveTypeface(familyName, isBold, isItalic);
        }

        public static class FontHelper
        {
            public static byte[] DosisRegular
            {
                get { return LoadFontData("pack://application:,,,/OLS.Casy.Ui.Base;component/Resources/Dosis-Regular.ttf"); }
            }

            public static byte[] DosisLight
            {
                get { return LoadFontData("pack://application:,,,/OLS.Casy.Ui.Base;component/Resources/Dosis-Light.ttf"); }
            }

            public static byte[] DosisBold
            {
                get { return LoadFontData("pack://application:,,,/OLS.Casy.Ui.Base;component/Resources/Dosis-Bold.ttf"); }
            }

            /// <summary>
            /// Returns the specified font from an embedded resource.
            /// </summary>
            static byte[] LoadFontData(string name)
            {
                //var assembly = Assembly.GetExecutingAssembly();


                using (Stream stream = Application.GetResourceStream(new Uri(name)).Stream)//assembly.GetManifestResourceStream(name))
                {
                    if (stream == null)
                        throw new ArgumentException("No resource with name " + name);

                    int count = (int)stream.Length;
                    byte[] data = new byte[count];
                    stream.Read(data, 0, count);
                    return data;
                }
            }
        }
    }
}
