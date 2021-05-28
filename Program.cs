using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace LoGeeK
{
    class Program
    {
        public static ModuleDefMD asm;
        public static string path;

        static void Main(string[] args)
        {
            Console.Title = "LoGeek";
            Console.WriteLine();
            Console.WriteLine(" LoGeek | LoGiC.NET Deobfuscator by misonothx");
            Console.WriteLine("  |- https://github.com/miso-xyz/LoGeek/");
            Console.WriteLine();
            try
            {
                asm = ModuleDefMD.Load(args[0]);
            }
            catch { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(" Failed to load app!"); goto end; }
            path = args[0];
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(" Obfuscator Version Used: " + getLogicVersion());
            Console.WriteLine();
            CleanMethods();
            removeJunk();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" Now saving...");
            ModuleWriterOptions moduleWriterOptions = new ModuleWriterOptions(asm);
            moduleWriterOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
            moduleWriterOptions.Logger = DummyLogger.NoThrowInstance;
            NativeModuleWriterOptions nativeModuleWriterOptions = new NativeModuleWriterOptions(asm, true);
            nativeModuleWriterOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
            nativeModuleWriterOptions.Logger = DummyLogger.NoThrowInstance;
            if (asm.IsILOnly) { asm.Write(Path.GetFileNameWithoutExtension(path) + "-LoGeek" + Path.GetExtension(path)); }
            else { asm.NativeWrite(Path.GetFileNameWithoutExtension(path) + "-LoGeek" + Path.GetExtension(path)); }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" Successfully cleaned! (saved as '" + Path.GetFileNameWithoutExtension(path) + "-LoGeek" + Path.GetExtension(path) + "')");
            end:
            Console.ResetColor();
            Console.Write(" Press any key to close...");
            Console.ReadKey();
        }

        static void CleanMethods()
        {
            foreach (TypeDef type in asm.Types)
            {
                for (int x = 0; x < type.Methods.Count; x++)
                {
                    MethodDef methods = type.Methods[x];
                    for (int x_ = 0; x_ < methods.Body.Instructions.Count; x_++)
                    {
                        Instruction inst = methods.Body.Instructions[x_];
                        switch (inst.OpCode.Code)
                        {
                            case Code.Newobj:
                                if (inst.Operand.ToString().Contains("System.EntryPointNotFoundException")) { methods.Name = "AntiTamper"; }
                                break;
                            case Code.Ldstr:
                                if (inst.Operand.ToString() == "*$,;:!ù^*&é\"'(-è_çà)") { methods.Name = "StringDecrypt"; }
                                break;
                        }
                    }
                }
            }
        }

        static string getLogicVersion()
        {
            string result = null;
            for (int x = 0; x < asm.CustomAttributes.Count; x++)
            {
                CustomAttribute CA = asm.CustomAttributes[x];
                foreach (CAArgument constrArgs in CA.ConstructorArguments)
                {
                    if (constrArgs.Value.ToString().Contains("LoGiC.NET")) { asm.CustomAttributes.RemoveAt(x); x--; result = constrArgs.Value.ToString().Replace("Obfuscated with ", null).Replace("version ", "v");}
                }
            }
            return result.Remove(result.Length-1);
        }

        static void removeJunk()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" Fixed Module Name! ('");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(asm.Name);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("'");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" -> ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("'");
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.Write(Path.GetFileName(path));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("')");
            asm.Name = Path.GetFileName(path);
            for (int x_type = 0; x_type < asm.Types.Count; x_type++)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                TypeDef type = asm.Types[x_type];
                if (type.BaseType != null) { if (type.BaseType.Name == "Attribute") { Console.WriteLine(" Removed AntiDe4dot (" + type.Name + ")!"); asm.Types.Remove(type); x_type--; continue; } }
                for (int x = 0; x < type.Methods.Count; x++)
                {
                    MethodDef methods = type.Methods[x];
                    methods.Body.KeepOldMaxStack = true;
                    for (int x_ = 0; x_ < methods.Body.Instructions.Count; x_++)
                    {
                        Instruction inst = methods.Body.Instructions[x_];
                        switch (inst.OpCode.Code)
                        {
                            case Code.Ldstr:
                                if (methods.Body.Instructions.Count == 3 || methods.Body.Instructions.Count == 5) { type.Methods.Remove(methods); x--; break; }
                                if (methods.Body.Instructions[x_ + 1].OpCode.Equals(OpCodes.Call))
                                {
                                    string og_string = inst.Operand.ToString();
                                    string dec_String = StringDecryptor(inst.Operand.ToString());
                                    inst.Operand = dec_String;
                                    if (!methods.Body.Instructions[x_ + 2].OpCode.Equals(OpCodes.Call)) { Console.ForegroundColor = ConsoleColor.Cyan; Console.Write(" Fixed String! ('"); Console.ForegroundColor = ConsoleColor.DarkYellow; Console.Write(og_string); Console.ForegroundColor = ConsoleColor.Cyan; Console.Write("'"); Console.ForegroundColor = ConsoleColor.White; Console.Write(" -> "); Console.ForegroundColor = ConsoleColor.Cyan; Console.Write("'"); Console.ForegroundColor = ConsoleColor.DarkMagenta; Console.Write(dec_String); Console.ForegroundColor = ConsoleColor.Cyan; Console.WriteLine("')"); methods.Body.Instructions.RemoveAt(x_ + 1); break; }
                                    if (((MemberRef)methods.Body.Instructions[x_ + 2].Operand).Name != "get_Length") { methods.Body.Instructions.RemoveAt(x_ + 1); break; }
                                    inst.OpCode = OpCodes.Ldc_I4;
                                    inst.Operand = Math.Abs(dec_String.Length);
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.Write(" Fixed Int! ('");
                                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                                    Console.Write(og_string);
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.Write("'");
                                    Console.ForegroundColor = ConsoleColor.White;
                                    Console.Write(" -> ");
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.Write("'");
                                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                                    Console.Write(dec_String);
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("')");
                                    methods.Body.Instructions.RemoveAt(x_ + 1);
                                    methods.Body.Instructions.RemoveAt(x_ + 1);
                                    methods.Body.Instructions.RemoveAt(x_ + 1);
                                }
                                break;
                            case Code.Call:
                                if (inst.Operand.ToString().Contains("AntiTamper")) { methods.Body.Instructions.Remove(inst); x_--; }
                                break;
                            case Code.Ldc_I4:
                                if (methods.Body.Instructions.Count == 2) { Console.WriteLine(" Removed Junk Method ('" + methods.Name + "')!"); type.Methods.Remove(methods); x--; }
                                break;
                        }
                    }
                }
            }
            foreach (TypeDef type in asm.Types)
            {
                for (int x = 0; x < type.Methods.Count; x++)
                {
                    MethodDef methods = type.Methods[x];
                    if (methods.Name == "AntiTamper") { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine(" Removed AntiTamper Protection!"); type.Methods.RemoveAt(x); x--; continue; }
                }
            }
        }

        public static string StringDecryptor(string data)
        {
            char[] array = "*$,;:!ù^*&é\"'(-è_çà)".ToCharArray();
            foreach (char c in array)
            {
                data = data.Replace(c.ToString(), string.Empty);
            }
            return Encoding.UTF32.GetString(Convert.FromBase64String(data));
        }

    }
}