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
        public static int stringFixed, failedStrings, removedNeg, fixedMathCalls, failedMathCalls;
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
            catch { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(" Failed to load app! (DOS Header might have been stripped out)"); goto end; }
            path = args[0];
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(" Obfuscator Version Used: " + getLogicVersion());
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" Sorting out methods...");
            CleanMethods();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" Cleaning up Various Junk...");
            removeJunk();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" Cleaning up Math Obfuscation...");
            fixMath();
            Console.WriteLine();
            Console.WriteLine(" Results:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  |- " + stringFixed + " Strings Fixed! ");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("(" + failedStrings + " failed)");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("  |- " + fixedMathCalls + " Math Calls Fixed! ");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("(" + failedMathCalls + " failed, ");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(removedNeg + " Neg instruction removed)");
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
                            case Code.Neg:
                                methods.Body.Instructions.RemoveAt(x_);
                                removedNeg++;
                                x_--;
                                continue;
                            case Code.Callvirt:
                                if (inst.Operand.ToString().Contains("System.Text.StringBuilder::Append") && methods.Body.Instructions[x_ - 1].OpCode.Equals(OpCodes.Conv_U2)) { methods.Name = "StringDecryption"; }
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

        public static void objectUpdatedPrint(string frontText, string from, string to, string type = "")
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(frontText + " ('");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(from);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("'");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" -> ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("'");
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.Write(to);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("')");
            if (type != "") { Console.WriteLine(type); } else { Console.WriteLine(); }
        }

        static void removeJunk()
        {

            objectUpdatedPrint(" Module Renamed!", asm.Name, Path.GetFileName(path));
            asm.Name = Path.GetFileName(path);
            objectUpdatedPrint(" Entrypoint Renamed!", asm.EntryPoint.DeclaringType.Name + "::" + asm.EntryPoint.Name, "Entrypoint::Main");
            asm.EntryPoint.DeclaringType.Name = "Entrypoint";
            asm.EntryPoint.Name = "Main";

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
                                try
                                {
                                    string og_string = inst.Operand.ToString();
                                    Instruction secondPart = methods.Body.Instructions[x_ + 2];
                                    //for (int x_inst_temp = x_; x_inst_temp < methods.Body.Instructions.Count; x_inst_temp++)
                                    //{
                                    //    Instruction inst_ = methods.Body.Instructions[x_inst_temp];
                                    //    if (inst_.OpCode.Equals(OpCodes.Call))
                                    //    {
                                    //        if (inst_.Operand.ToString().Contains("StringDecryption"))
                                    //        {
                                    //            secondPart = methods.Body.Instructions[x_inst_temp-4];
                                    //            break;
                                    //        }
                                    //    }
                                    //}
                                    //if (secondPart == null) { }
                                    string dec_String = StringDecryptor(og_string, secondPart.GetLdcI4Value());
                                    inst.Operand = dec_String;
                                    stringFixed++;
                                    //objectUpdatedPrint(" Fixed String!", og_string, dec_String);
                                    //methods.Body.Instructions.RemoveAt(temp_inst_x);
                                    //for (int temp_inst_x = x_+1; temp_inst_x < methods.Body.Instructions.IndexOf(secondPart)+2; temp_inst_x++)
                                    //{
                                    //    methods.Body.Instructions.RemoveAt(temp_inst_x);
                                    //}
                                    break; 
                                }
                                catch
                                { //Console.ForegroundColor = Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(" Coudn't fix string");
                                    failedStrings++;
                                }
                                break; 
                            case Code.Call:
                                if (inst.Operand.ToString().Contains("AntiTamper")) { methods.Body.Instructions.Remove(inst); x_--; }
                                break;
                            case Code.Ldc_I4:
                                if (methods.Body.Instructions.Count == 2) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine(" Removed Junk Method ('" + methods.Name + "')!"); type.Methods.Remove(methods); x--; }
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

        public static void fixMath()
        {
            foreach (TypeDef type in asm.Types)
            {
                for (int x = 0; x < type.Methods.Count; x++)
                {
                    MethodDef methods = type.Methods[x];
                    methods.Body.Instructions.OptimizeBranches();
                    methods.Body.Instructions.SimplifyBranches();
                    //while (stillHasMath(methods))
                    //{
                        for (int x_ = 0; x_ < methods.Body.Instructions.Count; x_++)
                        {
                            Instruction inst = methods.Body.Instructions[x_];
                            switch (inst.OpCode.Code)
                            {
                                case Code.Call:
                                    if (inst.Operand.ToString().Contains("System.Math::"))
                                    {
                                        try
                                        {
                                            switch (((MemberRef)inst.Operand).Name)
                                            {
                                                case "Abs":
                                                    //if (!methods.Body.Instructions[x_ - 1].OpCode.ToString().Contains("ldc.i4")) { break; }
                                                    methods.Body.Instructions[x_ - 1].Operand = Math.Abs(Convert.ToInt32(methods.Body.Instructions[x_ - 1].Operand.ToString()));
                                                    methods.Body.Instructions.RemoveAt(x_);
                                                    x_--;
                                                    fixedMathCalls++;
                                                    break;
                                                case "Min":
                                                    //if (!methods.Body.Instructions[x_ - 1].OpCode.ToString().Contains("ldc.i4") || !methods.Body.Instructions[x_ - 2].OpCode.ToString().Contains("ldc.i4")) { break; }
                                                    int l = 0; int r = 0;
                                                    if (methods.Body.Instructions[x_ - 1].ToString() == int.MaxValue.ToString()) { l = int.MaxValue; } else { Convert.ToInt32(methods.Body.Instructions[x_ - 1].GetLdcI4Value()); }
                                                    if (methods.Body.Instructions[x_ - 2].ToString() == int.MaxValue.ToString()) { r = int.MaxValue; } else { Convert.ToInt32(methods.Body.Instructions[x_ - 2].GetLdcI4Value()); }
                                                    methods.Body.Instructions[x_ - 2].Operand = Math.Min(l, r);
                                                    methods.Body.Instructions.RemoveAt(x_ - 1);
                                                    methods.Body.Instructions.RemoveAt(x_ - 1);
                                                    //x_ -= 2;
                                                    fixedMathCalls++;
                                                    break;
                                            }
                                        }
                                        catch { failedMathCalls++; }
                                    }
                                    break;
                            }
                        }
                    //}
                }
            }
        }

        public static bool stillHasMath(MethodDef methods)
        {
            for (int x_ = 0; x_ < methods.Body.Instructions.Count; x_++)
            {
                Instruction inst = methods.Body.Instructions[x_];
                switch (inst.OpCode.Code)
                {
                    case Code.Call:
                        if (inst.Operand.ToString().Contains("System.Math::"))
                        {
                            return true;
                        }
                        break;
                }
            }
            return false;
        }

        public static string StringDecryptor(string data, int key)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in data.ToCharArray())
            {
                stringBuilder.Append((char)((int)c - key));
            }
            return stringBuilder.ToString();
        }

    }
}