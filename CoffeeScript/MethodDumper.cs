using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace CoffeeScript
{
    class MethodDumper
    {
        public static void Dump(MethodInfo m)
        {
            var dm = ILReaderFactory.GetDynamicMethod(m);
            var il = dm.GetILGenerator();
            var reader = ILReaderFactory.Create(m);
            //reader.InstructionBytes
            var asmFileName = "temp.dll";

            var myAsmName = new AssemblyName {Name = "MyDynamicAssembly"};

            AssemblyBuilder myAsmBldr = AppDomain.CurrentDomain.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.RunAndSave);
            
            // We've created a dynamic assembly space - now, we need to create a module 
            // within it to reflect the type Point into.

            ModuleBuilder myModuleBldr = myAsmBldr.DefineDynamicModule(asmFileName, asmFileName);
            
            TypeBuilder myTypeBldr = myModuleBldr.DefineType("TempClass");

            // Build the constructor.
            

            MethodBuilder methodBldr = myTypeBldr.DefineMethod(dm.Name, MethodAttributes.Public | MethodAttributes.Static);
            var parameters = dm.GetParameters();
            methodBldr.SetReturnType(dm.ReturnType);
            methodBldr.SetParameters(parameters.Select(p=> p.ParameterType).ToArray());
            
            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                methodBldr.DefineParameter(i + 1, p.Attributes, p.Name ?? "p" + i);
            }

            methodBldr.CreateMethodBody(reader.InstructionBytes, reader.InstructionBytes.Length);
            
            myTypeBldr.CreateType();

            myAsmBldr.Save(asmFileName);
        }


        public class ILReaderFactory
        {
            public static ILReader Create(object obj)
            {
                var dm = GetDynamicMethod(obj);

                if(dm != null)
                    return new ILReader(new DynamicMethodILProvider(dm), new DynamicScopeTokenResolver(dm));

                Type type = obj.GetType();

                if (type == s_runtimeMethodInfoType || type == s_runtimeConstructorInfoType)
                {
                    MethodBase method = obj as MethodBase;
                    return new ILReader(method);
                }

                throw new NotSupportedException(string.Format("Reading IL from type {0} is currently not supported", type));
            }

            public static DynamicMethod GetDynamicMethod(object obj)
            {
                Type type = obj.GetType();


                DynamicMethod dm = null;
                if (type == s_dynamicMethodType || type == s_rtDynamicMethodType)
                {
                    if (type == s_rtDynamicMethodType)
                    {
                        //
                        // if the target is RTDynamicMethod, get the value of 
                        // RTDynamicMethod.m_owner instead
                        //
                        dm = (DynamicMethod) s_fiOwner.GetValue(obj);
                    }
                    else
                    {
                        dm = obj as DynamicMethod;
                    }
                }
                return dm;
            }

            private static Type s_dynamicMethodType = Type.GetType("System.Reflection.Emit.DynamicMethod");
            private static Type s_runtimeMethodInfoType = Type.GetType("System.Reflection.RuntimeMethodInfo");
            private static Type s_runtimeConstructorInfoType = Type.GetType("System.Reflection.RuntimeConstructorInfo");

            private static Type s_rtDynamicMethodType = Type.GetType("System.Reflection.Emit.DynamicMethod+RTDynamicMethod");
            private static FieldInfo s_fiOwner = s_rtDynamicMethodType.GetField("m_owner", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public sealed class ILReader 
        {
            #region Static members
            static Type s_runtimeMethodInfoType = Type.GetType("System.Reflection.RuntimeMethodInfo");
            static Type s_runtimeConstructorInfoType = Type.GetType("System.Reflection.RuntimeConstructorInfo");

            static OpCode[] s_OneByteOpCodes;
            static OpCode[] s_TwoByteOpCodes;

            static ILReader()
            {
                s_OneByteOpCodes = new OpCode[0x100];
                s_TwoByteOpCodes = new OpCode[0x100];

                foreach (FieldInfo fi in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    OpCode opCode = (OpCode)fi.GetValue(null);
                    UInt16 value = (UInt16)opCode.Value;
                    if (value < 0x100)
                    {
                        s_OneByteOpCodes[value] = opCode;
                    }
                    else if ((value & 0xff00) == 0xfe00)
                    {
                        s_TwoByteOpCodes[value & 0xff] = opCode;
                    }
                }
            }
            #endregion

            Int32 m_position;
            ITokenResolver m_resolver;
            IILProvider m_ilProvider;
            public byte[] InstructionBytes { get; private set; }

            public ILReader(MethodBase method)
            {
                if (method == null)
                {
                    throw new ArgumentNullException("method");
                }

                Type rtType = method.GetType();
                if (rtType != s_runtimeMethodInfoType && rtType != s_runtimeConstructorInfoType)
                {
                    throw new ArgumentException("method must be RuntimeMethodInfo or RuntimeConstructorInfo for this constructor.");
                }

                m_ilProvider = new MethodBaseILProvider(method);
                m_resolver = new ModuleScopeTokenResolver(method);
                InstructionBytes = m_ilProvider.GetByteArray();
                m_position = 0;
            }

            public ILReader(IILProvider ilProvider, ITokenResolver tokenResolver)
            {
                if (ilProvider == null)
                {
                    throw new ArgumentNullException("ilProvider");
                }

                m_resolver = tokenResolver;
                m_ilProvider = ilProvider;
                InstructionBytes = m_ilProvider.GetByteArray();
                m_position = 0;
            }

            //public IEnumerator<ILInstruction> GetEnumerator()
            //{
            //    while (m_position < m_byteArray.Length)
            //        yield return Next();

            //    m_position = 0;
            //    yield break;
            //}

            //IEnumerator IEnumerable.GetEnumerator()
            //{
            //    return this.GetEnumerator();
            //}

            //ILInstruction Next()
            //{
            //    Int32 offset = m_position;
            //    OpCode opCode = OpCodes.Nop;
            //    Int32 token = 0;

            //    // read first 1 or 2 bytes as opCode
            //    Byte code = ReadByte();
            //    if (code != 0xFE)
            //    {
            //        opCode = s_OneByteOpCodes[code];
            //    }
            //    else
            //    {
            //        code = ReadByte();
            //        opCode = s_TwoByteOpCodes[code];
            //    }

            //    switch (opCode.OperandType)
            //    {
            //        case OperandType.InlineNone:
            //            return new InlineNoneInstruction(offset, opCode);

            //        //The operand is an 8-bit integer branch target.
            //        case OperandType.ShortInlineBrTarget:
            //            SByte shortDelta = ReadSByte();
            //            return new ShortInlineBrTargetInstruction(offset, opCode, shortDelta);

            //        //The operand is a 32-bit integer branch target.
            //        case OperandType.InlineBrTarget:
            //            Int32 delta = ReadInt32();
            //            return new InlineBrTargetInstruction(offset, opCode, delta);

            //        //The operand is an 8-bit integer: 001F  ldc.i4.s, FE12  unaligned.
            //        case OperandType.ShortInlineI:
            //            Byte int8 = ReadByte();
            //            return new ShortInlineIInstruction(offset, opCode, int8);

            //        //The operand is a 32-bit integer.
            //        case OperandType.InlineI:
            //            Int32 int32 = ReadInt32();
            //            return new InlineIInstruction(offset, opCode, int32);

            //        //The operand is a 64-bit integer.
            //        case OperandType.InlineI8:
            //            Int64 int64 = ReadInt64();
            //            return new InlineI8Instruction(offset, opCode, int64);

            //        //The operand is a 32-bit IEEE floating point number.
            //        case OperandType.ShortInlineR:
            //            Single float32 = ReadSingle();
            //            return new ShortInlineRInstruction(offset, opCode, float32);

            //        //The operand is a 64-bit IEEE floating point number.
            //        case OperandType.InlineR:
            //            Double float64 = ReadDouble();
            //            return new InlineRInstruction(offset, opCode, float64);

            //        //The operand is an 8-bit integer containing the ordinal of a local variable or an argument
            //        case OperandType.ShortInlineVar:
            //            Byte index8 = ReadByte();
            //            return new ShortInlineVarInstruction(offset, opCode, index8);

            //        //The operand is 16-bit integer containing the ordinal of a local variable or an argument.
            //        case OperandType.InlineVar:
            //            UInt16 index16 = ReadUInt16();
            //            return new InlineVarInstruction(offset, opCode, index16);

            //        //The operand is a 32-bit metadata string token.
            //        case OperandType.InlineString:
            //            token = ReadInt32();
            //            return new InlineStringInstruction(offset, opCode, token, m_resolver);

            //        //The operand is a 32-bit metadata signature token.
            //        case OperandType.InlineSig:
            //            token = ReadInt32();
            //            return new InlineSigInstruction(offset, opCode, token, m_resolver);

            //        //The operand is a 32-bit metadata token.
            //        case OperandType.InlineMethod:
            //            token = ReadInt32();
            //            return new InlineMethodInstruction(offset, opCode, token, m_resolver);

            //        //The operand is a 32-bit metadata token.
            //        case OperandType.InlineField:
            //            token = ReadInt32();
            //            return new InlineFieldInstruction(m_resolver, offset, opCode, token);

            //        //The operand is a 32-bit metadata token.
            //        case OperandType.InlineType:
            //            token = ReadInt32();
            //            return new InlineTypeInstruction(offset, opCode, token, m_resolver);

            //        //The operand is a FieldRef, MethodRef, or TypeRef token.
            //        case OperandType.InlineTok:
            //            token = ReadInt32();
            //            return new InlineTokInstruction(offset, opCode, token, m_resolver);

            //        //The operand is the 32-bit integer argument to a switch instruction.
            //        case OperandType.InlineSwitch:
            //            Int32 cases = ReadInt32();
            //            Int32[] deltas = new Int32[cases];
            //            for (Int32 i = 0; i < cases; i++)
            //                deltas[i] = ReadInt32();
            //            return new InlineSwitchInstruction(offset, opCode, deltas);

            //        default:
            //            throw new BadImageFormatException("unexpected OperandType " + opCode.OperandType);
            //    }
            //}

            //public void Accept(ILInstructionVisitor visitor)
            //{
            //    if (visitor == null)
            //        throw new ArgumentNullException("argument 'visitor' can not be null");

            //    foreach (ILInstruction instruction in this)
            //    {
            //        instruction.Accept(visitor);
            //    }
            //}

            #region read in operands
            Byte ReadByte()
            {
                return (Byte)InstructionBytes[m_position++];
            }

            SByte ReadSByte()
            {
                return (SByte)ReadByte();
            }

            UInt16 ReadUInt16()
            {
                int pos = m_position;
                m_position += 2;
                return BitConverter.ToUInt16(InstructionBytes, pos);
            }

            UInt32 ReadUInt32()
            {
                int pos = m_position;
                m_position += 4;
                return BitConverter.ToUInt32(InstructionBytes, pos);
            }
            UInt64 ReadUInt64()
            {
                int pos = m_position;
                m_position += 8;
                return BitConverter.ToUInt64(InstructionBytes, pos);
            }

            Int32 ReadInt32()
            {
                int pos = m_position;
                m_position += 4;
                return BitConverter.ToInt32(InstructionBytes, pos);
            }
            Int64 ReadInt64()
            {
                int pos = m_position;
                m_position += 8;
                return BitConverter.ToInt64(InstructionBytes, pos);
            }

            Single ReadSingle()
            {
                int pos = m_position;
                m_position += 4;
                return BitConverter.ToSingle(InstructionBytes, pos);
            }
            Double ReadDouble()
            {
                int pos = m_position;
                m_position += 8;
                return BitConverter.ToDouble(InstructionBytes, pos);
            }
            #endregion
        }

        public interface ITokenResolver
        {
            MethodBase AsMethod(int token);
            FieldInfo AsField(int token);
            Type AsType(int token);
            String AsString(int token);
            MemberInfo AsMember(int token);
            byte[] AsSignature(int token);
        }

        public class ModuleScopeTokenResolver : ITokenResolver
        {
            private Module m_module;
            private MethodBase m_enclosingMethod;
            private Type[] m_methodContext;
            private Type[] m_typeContext;

            public ModuleScopeTokenResolver(MethodBase method)
            {
                m_enclosingMethod = method;
                m_module = method.Module;
                m_methodContext = (method is ConstructorInfo) ? null : method.GetGenericArguments();
                m_typeContext = (method.DeclaringType == null) ? null : method.DeclaringType.GetGenericArguments();
            }

            public MethodBase AsMethod(int token)
            {
                return m_module.ResolveMethod(token, m_typeContext, m_methodContext);
            }

            public FieldInfo AsField(int token)
            {
                return m_module.ResolveField(token, m_typeContext, m_methodContext);
            }

            public Type AsType(int token)
            {
                return m_module.ResolveType(token, m_typeContext, m_methodContext);
            }

            public MemberInfo AsMember(int token)
            {
                return m_module.ResolveMember(token, m_typeContext, m_methodContext);
            }

            public string AsString(int token)
            {
                return m_module.ResolveString(token);
            }

            public byte[] AsSignature(int token)
            {
                return m_module.ResolveSignature(token);
            }
        }

        public interface IILProvider
        {
            Byte[] GetByteArray();
        }

        public class MethodBaseILProvider : IILProvider
        {
            MethodBase m_method;
            byte[] m_byteArray;

            public MethodBaseILProvider(MethodBase method)
            {
                m_method = method;
            }

            public byte[] GetByteArray()
            {
                if (m_byteArray == null)
                {
                    MethodBody methodBody = m_method.GetMethodBody();
                    m_byteArray = (methodBody == null) ? new Byte[0] : methodBody.GetILAsByteArray();
                }
                return m_byteArray;
            }
        }

        public class DynamicMethodILProvider : IILProvider
        {
            static FieldInfo s_fiLen = typeof(ILGenerator).GetField("m_length", BindingFlags.NonPublic | BindingFlags.Instance);
            static FieldInfo s_fiStream = typeof(ILGenerator).GetField("m_ILStream", BindingFlags.NonPublic | BindingFlags.Instance);
            static MethodInfo s_miBakeByteArray = typeof(ILGenerator).GetMethod("BakeByteArray", BindingFlags.NonPublic | BindingFlags.Instance);

            DynamicMethod m_method;
            byte[] m_byteArray;

            public DynamicMethodILProvider(DynamicMethod method)
            {
                m_method = method;
            }

            public byte[] GetByteArray()
            {
                if (m_byteArray == null)
                {
                    ILGenerator ilgen = m_method.GetILGenerator();
                    
                    try
                    {
                        m_byteArray = (byte[])s_miBakeByteArray.Invoke(ilgen, null);
                        if (m_byteArray == null) m_byteArray = new byte[0];
                    }
                    catch (TargetInvocationException)
                    {
                        int length = (int)s_fiLen.GetValue(ilgen);
                        m_byteArray = new byte[length];
                        Array.Copy((byte[])s_fiStream.GetValue(ilgen), m_byteArray, length);
                    }
                }
                return m_byteArray;
            }

        }

        internal class DynamicScopeTokenResolver : ITokenResolver
        {
            #region Static stuffs
            private static PropertyInfo s_indexer;
            private static FieldInfo s_scopeFi;

            private static Type s_genMethodInfoType;
            private static FieldInfo s_genmethFi1, s_genmethFi2;

            private static Type s_varArgMethodType;
            private static FieldInfo s_varargFi1, s_varargFi2;

            private static Type s_genFieldInfoType;
            private static FieldInfo s_genfieldFi1, s_genfieldFi2;

            static DynamicScopeTokenResolver()
            {
                BindingFlags s_bfInternal = BindingFlags.NonPublic | BindingFlags.Instance;
                s_indexer = Type.GetType("System.Reflection.Emit.DynamicScope").GetProperty("Item", s_bfInternal);
                s_scopeFi = Type.GetType("System.Reflection.Emit.DynamicILGenerator").GetField("m_scope", s_bfInternal);

                s_varArgMethodType = Type.GetType("System.Reflection.Emit.VarArgMethod");
                s_varargFi1 = s_varArgMethodType.GetField("m_method", s_bfInternal);
                s_varargFi2 = s_varArgMethodType.GetField("m_signature", s_bfInternal);

                s_genMethodInfoType = Type.GetType("System.Reflection.Emit.GenericMethodInfo");
                s_genmethFi1 = s_genMethodInfoType.GetField("m_methodHandle", s_bfInternal);
                s_genmethFi2 = s_genMethodInfoType.GetField("m_context", s_bfInternal);

                s_genFieldInfoType = Type.GetType("System.Reflection.Emit.GenericFieldInfo", false);
                if (s_genFieldInfoType != null)
                {
                    s_genfieldFi1 = s_genFieldInfoType.GetField("m_fieldHandle", s_bfInternal);
                    s_genfieldFi2 = s_genFieldInfoType.GetField("m_context", s_bfInternal);
                }
                else
                {
                    s_genfieldFi1 = s_genfieldFi2 = null;
                }
            }
            #endregion

            object m_scope = null;
            internal object this[int token]
            {
                get
                {
                    return s_indexer.GetValue(m_scope, new object[] { token });
                }
            }

            public DynamicScopeTokenResolver(DynamicMethod dm)
            {
                m_scope = s_scopeFi.GetValue(dm.GetILGenerator());
            }

            public String AsString(int token)
            {
                return this[token] as string;
            }

            public FieldInfo AsField(int token)
            {
                if (this[token] is RuntimeFieldHandle)
                    return FieldInfo.GetFieldFromHandle((RuntimeFieldHandle)this[token]);

                if (this[token].GetType() == s_genFieldInfoType)
                {
                    return FieldInfo.GetFieldFromHandle(
                            (RuntimeFieldHandle)s_genfieldFi1.GetValue(this[token]),
                            (RuntimeTypeHandle)s_genfieldFi2.GetValue(this[token]));
                }

                Debug.Assert(false, string.Format("unexpected type: {0}", this[token].GetType()));
                return null;
            }

            public Type AsType(int token)
            {
                return Type.GetTypeFromHandle((RuntimeTypeHandle)this[token]);
            }

            public MethodBase AsMethod(int token)
            {
                if (this[token] is DynamicMethod)
                    return this[token] as DynamicMethod;

                if (this[token] is RuntimeMethodHandle)
                    return MethodBase.GetMethodFromHandle((RuntimeMethodHandle)this[token]);

                if (this[token].GetType() == s_genMethodInfoType)
                    return MethodBase.GetMethodFromHandle(
                        (RuntimeMethodHandle)s_genmethFi1.GetValue(this[token]),
                        (RuntimeTypeHandle)s_genmethFi2.GetValue(this[token]));

                if (this[token].GetType() == s_varArgMethodType)
                    return (MethodInfo)s_varargFi1.GetValue(this[token]);

                Debug.Assert(false, string.Format("unexpected type: {0}", this[token].GetType()));
                return null;
            }

            public MemberInfo AsMember(int token)
            {
                if ((token & 0x02000000) == 0x02000000)
                    return this.AsType(token);
                if ((token & 0x06000000) == 0x06000000)
                    return this.AsMethod(token);
                if ((token & 0x04000000) == 0x04000000)
                    return this.AsField(token);

                Debug.Assert(false, string.Format("unexpected token type: {0:x8}", token));
                return null;
            }

            public byte[] AsSignature(int token)
            {
                return this[token] as byte[];
            }
        }
    }
}
