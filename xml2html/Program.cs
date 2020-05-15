using System.Xml;
using System.Xml.Xsl;

namespace xml2html
{
    class Program
    {
        static void Main(string[] args)
        {
            var xslt = new XslCompiledTransform();
            var settings = new XsltSettings(true, true);
            xslt.Load("uclick.xslt", settings, new XmlUrlResolver());
            xslt.Transform("usaon200510-data.xml", "output.html");
        }
    }
}
