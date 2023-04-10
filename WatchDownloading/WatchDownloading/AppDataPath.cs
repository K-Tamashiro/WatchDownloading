using System;
using System.Reflection;

namespace WatchDownloading
{
	public class AppdataSave
	{

		public static string LocalAppData
		{
			get
			{
				Func<string, bool> nameHasForbiddenChar = (str) => {
					foreach (var each in "\\/:*?\"<>|")
					{
						if (str.Contains(each + ""))
						{
							return true;
						}
					}
					return false;
				};
				Assembly asm = Assembly.GetExecutingAssembly();
				string asmName = asm.GetName().Name;
				Attribute customAttr = Attribute.GetCustomAttribute(asm, typeof(AssemblyCompanyAttribute));
				string company = (customAttr as AssemblyCompanyAttribute).Company;
				if (company.Length == 0 || nameHasForbiddenChar(company))
				{
					company = "Default";
				}
				string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				return path + @"\" + company + @"\" + asmName;
			}
		}
	}
}
