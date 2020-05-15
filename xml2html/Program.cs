using System.Globalization;
using System.Xml.Xsl;

namespace xml2html
{
    class Program
    {
        static void Main(string[] args)
        {
            var xslt = new XslCompiledTransform();
            xslt.Load("uclick.xslt");
            var filename = "output.html";
            if (args.Length > 1) {
                filename = args[1];
                if (!filename.EndsWith(".html", true, CultureInfo.InvariantCulture)) {
                    filename += ".html";
                }
            }
            xslt.Transform(args[0], filename);
        }
    }
}
