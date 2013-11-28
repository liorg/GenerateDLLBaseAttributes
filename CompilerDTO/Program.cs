using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.IO;
namespace CompilerDTO
{
    /// <summary> 
    /// "$(SolutionDir)\SharedDLL\CRM\CompilerMetaDataExcute.exe"  "$(TargetPath) " "$(SolutionDir) +"\SharedDLL\CRM"
    /// </summary>
    class Program
    {
        static void RemoveReadOnly(string output)
        {
            FileAttributes attributes = File.GetAttributes(output);

            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                // Make the file RW
                attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                File.SetAttributes(output, attributes);
                Console.WriteLine("The {0} file is no longer RO.", output);
            }
        }

        private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        static void Main(string[] args)
        {
            var codeProvider = CodeDomProvider.CreateProvider("CSharp");
            var outputPath = "";// @"F:\Guardian Projects\State Comptroller Menta\CRM\Guardian.Menta\SharedDLL\CRM";
            var assemblyPath = "";// @"F:\Guardian Projects\State Comptroller Menta\CRM\Guardian.Menta\Common\DTO\bin\Debug\Guardian.Menta.Common.DTO.dll";
            var assemblyCode = "";

            if (args.Length >= 2)
            {
                Console.WriteLine("has arguments");
                assemblyPath = args[0];
                outputPath = args[1];
            }


            Console.WriteLine(assemblyPath);
            Console.WriteLine(outputPath);

            string output = outputPath + "\\BaseCrmMetaData.dll";
            RemoveReadOnly(output);

            var parameters = new CompilerParameters();
            //Make sure we generate an EXE, not a DLL
            parameters.GenerateExecutable = false;
            parameters.OutputAssembly = output;

            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            Type crmEntityAttribute = assembly.GetType("Guardian.Menta.Common.DTO.CrmEntityAttribute");
            Type crmAttribute = assembly.GetType("Guardian.Menta.Common.DTO.CrmPropertyAttribute");

            var assemblyCodeBuilder = new StringBuilder();
            assemblyCodeBuilder.Append("namespace Guardian.MetaData { ");
            assemblyCodeBuilder.Append("public class MetaDataBaseCRM {");
            foreach (Type type in assembly.GetTypes())
            {
                var attributes = System.Attribute.GetCustomAttributes(type);
                foreach (Attribute attribute in attributes)
                {
                    assemblyCodeBuilder.Append(System.Environment.NewLine);
                    if (attribute.GetType() == crmEntityAttribute)
                    {
                        assemblyCodeBuilder.AppendFormat("public partial class {0} {{", type.Name);
                        if (attribute != null)
                        {
                            dynamic att = attribute;
                            assemblyCodeBuilder.AppendFormat("public const string  EntityName=\"{0}\" ;", att.EntityName);
                        }

                        assemblyCodeBuilder.Append(System.Environment.NewLine);
                        List<string> hasPropery = new List<string>();
                        foreach (var propery in type.GetProperties())
                        {

                            if (propery.IsDefined(crmAttribute, true))
                            {
                                if (hasPropery.Contains(propery.Name))
                                    continue;
                                hasPropery.Add(propery.Name);

                                object[] crmTypes = propery.GetCustomAttributes(crmAttribute, false);
                                if (crmTypes != null && crmTypes.Length > 0)
                                {
                                    if (crmTypes[0] != null)
                                    {
                                        dynamic crmType = crmTypes[0];
                                        assemblyCodeBuilder.AppendFormat("public const string  {0}=\"{1}\" ;", propery.Name, crmType.FieldName);
                                        assemblyCodeBuilder.Append(System.Environment.NewLine);
                                    }
                                }

                            }
                        }
                        assemblyCodeBuilder.Append("}");
                    }
                }
            }
            assemblyCodeBuilder.Append("}");
            assemblyCodeBuilder.Append("}");
            assemblyCode = assemblyCodeBuilder.ToString();
            CompilerResults results = codeProvider.CompileAssemblyFromSource(parameters, new string[] { assemblyCode });
            //  CompilerResults results = codeProvider.CompileAssemblyFromSource(parameters, textBox1.Text);

            if (results.Errors.Count > 0)
            {
                foreach (CompilerError CompErr in results.Errors)
                    Console.WriteLine(CompErr);
                Console.WriteLine("build UnSuccessfully !!!");
            }
            else

                Console.WriteLine("build Successfully");

        }
    }
}

