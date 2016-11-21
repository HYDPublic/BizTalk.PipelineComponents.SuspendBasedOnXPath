using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CodeDom.Compiler;
using System.Reflection;
using Microsoft.CSharp;
using System.ComponentModel;
using System.IO;
using System.Xml;

namespace Shared.PipelineComponents
{
    public static class ScriptExpressionHelper
    {

        public static Boolean ValidateExpression(string value, string expression)
        {

            if (String.IsNullOrEmpty(value) || String.IsNullOrEmpty(expression))
                return false;

            string body = GetScriptBody(value, expression);
            var instance = GetScriptInstance(body);

            return  instance.Evaluate;
        }

        private static string GetScriptBody(string value, string expression)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System;");

            sb.AppendLine();
            sb.AppendLine("namespace BizTalkComponents");
            sb.AppendLine("{");

            sb.AppendLine("      public class GenericHelper");
            sb.AppendLine("      {");

            sb.AppendFormat("              public Boolean Evaluate = ({0} {1});", value, expression);
            sb.AppendLine("      }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static dynamic GetScriptInstance(string script)
        {


          
            CSharpCodeProvider provider = new CSharpCodeProvider(new Dictionary<string, string>
              {
                 { "CompilerVersion", "v4.0" }
              });

            CompilerParameters parameters = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
                WarningLevel = 3,
                CompilerOptions = "/optimize",
                TreatWarningsAsErrors = false,

            };

            Assembly a = Assembly.GetExecutingAssembly();
           
            foreach (var referencedAssembly in a.GetReferencedAssemblies())
            {
                var asm = Assembly.Load(referencedAssembly);
                parameters.ReferencedAssemblies.Add(asm.Location);
            }
           // parameters.ReferencedAssemblies.Add("System.Xml.dll");

            CompilerResults results = provider.CompileAssemblyFromSource(parameters, script);

            StringBuilder errors = new StringBuilder();

            if (results.Errors.Count != 0)
            {
                foreach (var error in results.Errors)
                {
                    errors.AppendLine(error.ToString());
                }
            }

            if (errors.Length > 0)
            {
                throw new Exception(errors.ToString());
            }

            dynamic instance = results.CompiledAssembly.CreateInstance("BizTalkComponents.GenericHelper");

            return instance;
        }
    }

    
}
