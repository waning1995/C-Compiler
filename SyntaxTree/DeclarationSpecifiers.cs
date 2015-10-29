﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SyntaxTree {

    /// <summary>
    /// storage-class-specifier
    ///   : auto | register | static | extern | typedef
    /// </summary>
    public enum StorageClsSpec {
        NULL,
        AUTO,
        REGISTER,
        STATIC,
        EXTERN,
        TYPEDEF
    }

    /// <summary>
    /// type-specifier
    ///   : void      --+
    ///   | char        |
    ///   | short       |
    ///   | int         |
    ///   | long        +--> Basic type specifier
    ///   | float       |
    ///   | double      |
    ///   | signed      |
    ///   | unsigned  --+
    ///   | struct-or-union-specifier
    ///   | enum-specifier
    ///   | typedef-name
    /// </summary>
    public class TypeSpec : PTNode {
        public enum Kind {
            NON_BASIC,
            VOID,
            CHAR,
            SHORT,
            INT,
            LONG,
            FLOAT,
            DOUBLE,
            SIGNED,
            UNSIGNED
        }

        public TypeSpec() {
            kind = Kind.NON_BASIC;
        }

        public TypeSpec(Kind spec) {
            kind = spec;
        }

        // GetExprType
        // ===========
        // input: env
        // output: tuple<ExprType, Environment>
        // 
        public virtual Tuple<AST.Env, AST.ExprType> GetExprTypeEnv(AST.Env env, Boolean is_const, Boolean is_volatile) {
            throw new NotImplementedException();
        }

        public readonly Kind kind;
    }

    /// <summary>
    /// typedef-name
    ///   : identifier
    /// </summary>
    public class TypedefName : TypeSpec {
        public TypedefName(String name) {
            this.Name = name;
        }

        public override Tuple<AST.Env, AST.ExprType> GetExprTypeEnv(AST.Env env, Boolean is_const, Boolean is_volatile) {

            Option<AST.Env.Entry> entry_opt = env.Find(Name);

            if (entry_opt.IsNone) {
                throw new InvalidOperationException($"Cannot find name \"{Name}\".");
            }

            AST.Env.Entry entry = entry_opt.Value;

            if (entry.kind != AST.Env.EntryKind.TYPEDEF) {
                throw new InvalidOperationException($"\"{Name}\" is not a typedef.");
            }

            return Tuple.Create(env, entry.type.GetQualifiedType(is_const, is_volatile));
        }


        public String Name { get; }
    }

    /// <summary>
    /// type-qualifier
    ///   : const | volatile
    /// </summary>
    public enum TypeQual {
        NULL,
        CONST,
        VOLATILE
    }

    /// <summary>
    /// specifier-qualifier-list
    ///   : [ type-specifier | type-qualifier ]+
    /// </summary>
    public class SpecQualList : PTNode {
        protected SpecQualList(ImmutableList<TypeSpec> typeSpecs, ImmutableList<TypeQual> typeQuals) {
            this.TypeSpecs = typeSpecs;
            this.TypeQuals = typeQuals;
        }

        public static SpecQualList Create(ImmutableList<TypeSpec> typeSpecs, ImmutableList<TypeQual> typeQuals) =>
            new SpecQualList(typeSpecs, typeQuals);

        public static SpecQualList Create() =>
            Create(ImmutableList<TypeSpec>.Empty, ImmutableList<TypeQual>.Empty);

        public static SpecQualList Add(SpecQualList list, TypeSpec typeSpec) =>
            Create(list.TypeSpecs.Add(typeSpec), list.TypeQuals);

        public static SpecQualList Add(SpecQualList list, TypeQual typeQual) =>
            Create(list.TypeSpecs, list.TypeQuals.Add(typeQual));

        public ImmutableList<TypeSpec> TypeSpecs { get; }
        public ImmutableList<TypeQual> TypeQuals { get; }
        private static ImmutableDictionary<ImmutableSortedSet<TypeSpec.Kind>, AST.ExprType> basicTypeSpecLookupTable { get; }

        static SpecQualList() {

            basicTypeSpecLookupTable = ImmutableDictionary<ImmutableSortedSet<TypeSpec.Kind>, AST.ExprType>.Empty
            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.VOID),                                             new AST.TVoid())

            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.CHAR),                                             new AST.TChar())
            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.CHAR, TypeSpec.Kind.SIGNED),                       new AST.TChar())

            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.CHAR, TypeSpec.Kind.UNSIGNED),                     new AST.TUChar())

            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.SHORT),                                            new AST.TShort())
            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.SHORT, TypeSpec.Kind.SIGNED),                      new AST.TShort())
            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.SHORT, TypeSpec.Kind.INT),                         new AST.TShort())
            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.SHORT, TypeSpec.Kind.INT, TypeSpec.Kind.SIGNED),   new AST.TShort())

            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.SHORT, TypeSpec.Kind.UNSIGNED),                    new AST.TUShort())
            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.SHORT, TypeSpec.Kind.INT, TypeSpec.Kind.UNSIGNED), new AST.TUShort())

            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.INT),                                              new AST.TLong())
            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.INT, TypeSpec.Kind.SIGNED),                        new AST.TLong())
            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.INT, TypeSpec.Kind.LONG),                          new AST.TLong())
            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.INT, TypeSpec.Kind.SIGNED, TypeSpec.Kind.LONG),    new AST.TLong())
            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.SIGNED),                                           new AST.TLong())
            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.SIGNED, TypeSpec.Kind.LONG),                       new AST.TLong())
            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.LONG),                                             new AST.TLong())

            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.UNSIGNED),                                         new AST.TULong())
            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.UNSIGNED, TypeSpec.Kind.INT),                      new AST.TULong())
            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.UNSIGNED, TypeSpec.Kind.LONG),                     new AST.TULong())
            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.UNSIGNED, TypeSpec.Kind.INT, TypeSpec.Kind.LONG),  new AST.TULong())

            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.FLOAT),                                            new AST.TFloat())

            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.DOUBLE),                                           new AST.TDouble())
            .Add(ImmutableSortedSet.Create(TypeSpec.Kind.DOUBLE, TypeSpec.Kind.LONG),                       new AST.TDouble())
            ;
        }

        /// <summary>
        /// Get qualified type, based on type specifiers & type qualifiers.
        /// </summary>
        public ISemantReturn<AST.ExprType> GetExprType(AST.Env env) {
            Boolean isConst = TypeQuals.Contains(TypeQual.CONST);
            Boolean isVolatile = TypeQuals.Contains(TypeQual.VOLATILE);

            // If no type specifier is given, assume long type.
            if (this.TypeSpecs.IsEmpty) {
                return SemantReturn.Create(env, new AST.TLong(isConst, isVolatile));
            }

            // If every type specifier is basic, go to the lookup table.
            if (!this.TypeSpecs.Any(typeSpec => typeSpec.kind == TypeSpec.Kind.NON_BASIC)) {
                var basicTypeSpecKinds =
                    this.TypeSpecs
                    .ConvertAll(typeSpec => typeSpec.kind)
                    .Distinct()
                    .ToImmutableSortedSet();

                if (basicTypeSpecLookupTable.ContainsKey(basicTypeSpecKinds)) {
                    return SemantReturn.Create(env, basicTypeSpecLookupTable[basicTypeSpecKinds]);
                } else {
                    throw new InvalidOperationException("Invalid type specifier set.");
                }
            }

            // If there is a non-basic type specifier, semant it.
            if (this.TypeSpecs.Count == 1) {
                var _ = this.TypeSpecs[0].GetExprTypeEnv(env, isConst, isVolatile);
                return SemantReturn.Create(_.Item1, _.Item2);
            } else {
                throw new InvalidOperationException("Invalid type specifier set.");
            }
        }
    }

    /// <summary>
    /// declaration-specifiers
    ///   : [ storage-class-specifier | type-specifier | type-qualifier ]+
    /// </summary>
    public class DeclnSpecs : SpecQualList {
        protected DeclnSpecs(ImmutableList<StorageClsSpec> storageClsSpecs, ImmutableList<TypeSpec> typeSpecs, ImmutableList<TypeQual> typeQuals)
            : base(typeSpecs, typeQuals) {
            this.StorageClsSpecs = storageClsSpecs;
        }

        public static DeclnSpecs Create(ImmutableList<StorageClsSpec> storageClsSpecs, ImmutableList<TypeSpec> typeSpecs, ImmutableList<TypeQual> typeQuals) =>
            new DeclnSpecs(storageClsSpecs, typeSpecs, typeQuals);

        public static new DeclnSpecs Create() =>
            Create(ImmutableList<StorageClsSpec>.Empty, ImmutableList<TypeSpec>.Empty, ImmutableList<TypeQual>.Empty);

        public static DeclnSpecs Add(DeclnSpecs declnSpecs, StorageClsSpec storageClsSpec) =>
            Create(declnSpecs.StorageClsSpecs.Add(storageClsSpec), declnSpecs.TypeSpecs, declnSpecs.TypeQuals);

        public static DeclnSpecs Add(DeclnSpecs declnSpecs, TypeSpec typeSpec) =>
            Create(declnSpecs.StorageClsSpecs, declnSpecs.TypeSpecs.Add(typeSpec), declnSpecs.TypeQuals);

        public static DeclnSpecs Add(DeclnSpecs declnSpecs, TypeQual typeQual) =>
            Create(declnSpecs.StorageClsSpecs, declnSpecs.TypeSpecs, declnSpecs.TypeQuals.Add(typeQual));

        public ImmutableList<StorageClsSpec> StorageClsSpecs { get; }

        /// <summary>
        /// Get storage class specifier and type.
        /// </summary>
        public Tuple<AST.Env, AST.Decln.SCS, AST.ExprType> GetSCSType(AST.Env env) {
            Tuple<AST.Env, AST.ExprType> r_type = GetExprTypeEnv(env);
            env = r_type.Item1;
            AST.ExprType type = r_type.Item2;
            AST.Decln.SCS scs = GetSCS();
            return Tuple.Create(env, scs, type);
        }

        /// <summary>
        /// Get the type and the modified environment.
        /// </summary>
        public Tuple<AST.Env, AST.ExprType> GetExprTypeEnv(AST.Env env) {
            var _ = GetExprType(env);
            return Tuple.Create(_.Env, _.Value);
            //Boolean is_const = TypeQuals.Contains(TypeQual.CONST);
            //Boolean is_volatile = TypeQuals.Contains(TypeQual.VOLATILE);

            //// 1. if no type specifier => long
            //if (TypeSpecs.Count == 0) {
            //    return new Tuple<AST.Env, AST.ExprType>(env, new AST.TLong(is_const, is_volatile));
            //}

            //// 2. now let's analyse type specs
            //if (TypeSpecs.All(spec => spec.kind != TypeSpec.Kind.NON_BASIC)) {

            //    var basic_specs = TypeSpecs.Select(spec => spec.kind);

            //    var basic_type = GetBasicType(basic_specs);

            //    switch (basic_type) {
            //        case AST.ExprType.Kind.VOID:
            //            return Tuple.Create(env, (AST.ExprType)new AST.TVoid(is_const, is_volatile));

            //        case AST.ExprType.Kind.CHAR:
            //            return new Tuple<AST.Env, AST.ExprType>(env, new AST.TChar(is_const, is_volatile));

            //        case AST.ExprType.Kind.UCHAR:
            //            return new Tuple<AST.Env, AST.ExprType>(env, new AST.TUChar(is_const, is_volatile));

            //        case AST.ExprType.Kind.SHORT:
            //            return new Tuple<AST.Env, AST.ExprType>(env, new AST.TShort(is_const, is_volatile));

            //        case AST.ExprType.Kind.USHORT:
            //            return new Tuple<AST.Env, AST.ExprType>(env, new AST.TUShort(is_const, is_volatile));

            //        case AST.ExprType.Kind.LONG:
            //            return new Tuple<AST.Env, AST.ExprType>(env, new AST.TLong(is_const, is_volatile));

            //        case AST.ExprType.Kind.ULONG:
            //            return new Tuple<AST.Env, AST.ExprType>(env, new AST.TULong(is_const, is_volatile));

            //        case AST.ExprType.Kind.FLOAT:
            //            return new Tuple<AST.Env, AST.ExprType>(env, new AST.TFloat(is_const, is_volatile));

            //        case AST.ExprType.Kind.DOUBLE:
            //            return new Tuple<AST.Env, AST.ExprType>(env, new AST.TDouble(is_const, is_volatile));

            //        default:
            //            throw new Exception("Can't match type specifier.");
            //    }

            //} else if (TypeSpecs.Count == 1) {
            //    // now we can only match for struct, union, function...
            //    return TypeSpecs[0].GetExprTypeEnv(env, is_const, is_volatile);

            //} else {
            //    throw new InvalidOperationException("Can't match type specifier.");
            //}
        }

        /// <summary>
        /// Only used by the parser.
        /// </summary>
        [Obsolete]
        public bool IsTypedef() => StorageClsSpecs.Contains(StorageClsSpec.TYPEDEF);

        private AST.Decln.SCS GetSCS() {
            if (StorageClsSpecs.Count == 0) {
                return AST.Decln.SCS.AUTO;
            }
            if (StorageClsSpecs.Count == 1) {
                switch (StorageClsSpecs[0]) {
                    case StorageClsSpec.AUTO:
                    case StorageClsSpec.NULL:
                    case StorageClsSpec.REGISTER:
                        return AST.Decln.SCS.AUTO;
                    case StorageClsSpec.EXTERN:
                        return AST.Decln.SCS.EXTERN;
                    case StorageClsSpec.STATIC:
                        return AST.Decln.SCS.STATIC;
                    case StorageClsSpec.TYPEDEF:
                        return AST.Decln.SCS.TYPEDEF;
                    default:
                        throw new InvalidOperationException();
                }
            }
            throw new InvalidOperationException("Multiple storage class specifiers.");
        }
    }

    /// <summary>
    /// struct-or-union
    ///   : struct | union
    /// </summary>
    public enum StructOrUnion {
        STRUCT,
        UNION
    }

    /// <summary>
    /// struct-or-union-specifier
    /// </summary>
    public class StructOrUnionSpec : TypeSpec {
        protected StructOrUnionSpec(StructOrUnion structOrUnion, Option<String> name, Option<ImmutableList<StructDecln>> memberDeclns) {
            this.Name = name;
            this.MemberDeclns = memberDeclns;
        }

        [Obsolete]
        public static StructOrUnionSpec Create(StructOrUnion structOrUnion, Option<String> name, Option<ImmutableList<StructDecln>> memberDeclns) =>
            new StructOrUnionSpec(structOrUnion, name, memberDeclns);

        public static StructOrUnionSpec Create(StructOrUnion structOrUnion, Option<String> name, ImmutableList<StructDecln> memberDeclns) =>
            new StructOrUnionSpec(structOrUnion, name, Option.Some(memberDeclns));

        public static StructOrUnionSpec Create(StructOrUnion structOrUnion, String name) =>
            new StructOrUnionSpec(structOrUnion, Option.Some(name), Option<ImmutableList<StructDecln>>.None);

        public StructOrUnion StructOrUnion { get; }
        public Option<String> Name { get; }
        public Option<ImmutableList<StructDecln>> MemberDeclns { get; }

        // TODO: directly get value?
        public Tuple<AST.Env, List<Tuple<String, AST.ExprType>>> GetAttribs(AST.Env env) {
            List<Tuple<String, AST.ExprType>> attribs = new List<Tuple<String, AST.ExprType>>();
            foreach (StructDecln decln in MemberDeclns.Value) {
                Tuple<AST.Env, List<Tuple<String, AST.ExprType>>> r_decln = decln.GetDeclns(env);
                env = r_decln.Item1;
                attribs.AddRange(r_decln.Item2);
            }
            return Tuple.Create(env, attribs);
        }

        public override Tuple<AST.Env, AST.ExprType> GetExprTypeEnv(AST.Env env, Boolean isConst, Boolean isVolatile) {
            
            // If no name is supplied, this must be a new type.
            // Members must be supplied.
            if (this.Name.IsNone) {
                if (MemberDeclns.IsNone) {
                    throw new InvalidProgramException();
                }

                Tuple<AST.Env, List<Tuple<String, AST.ExprType>>> r_attribs = GetAttribs(env);
                env = r_attribs.Item1;

                if (this.StructOrUnion == StructOrUnion.STRUCT) {
                    return new Tuple<AST.Env, AST.ExprType>(env, AST.TStructOrUnion.CreateStruct("<anonymous>", r_attribs.Item2, isConst, isVolatile));
                } else {
                    return new Tuple<AST.Env, AST.ExprType>(env, AST.TStructOrUnion.CreateUnion("<anonymous>", r_attribs.Item2, isConst, isVolatile));
                }

            } else {
                // If a name is supplied, split into 2 cases.

                String typeName = (this.StructOrUnion == StructOrUnion.STRUCT) ? $"struct {this.Name.Value}" : $"union {this.Name.Value}";

                if (MemberDeclns.IsNone) {
                    // Case 1: If no attribute list supplied, then we are either
                    //       1) mentioning an already-existed struct/union
                    //    or 2) creating an incomplete struct/union

                    Option<AST.Env.Entry> entry_opt = env.Find(typeName);

                    if (entry_opt.IsNone) {
                        // If the struct/union is not in the current environment,
                        // then add an incomplete struct/union into the environment
                        AST.ExprType type =
                            (this.StructOrUnion == StructOrUnion.STRUCT)
                            ? AST.TStructOrUnion.CreateIncompleteStruct(this.Name.Value, isConst, isVolatile)
                            : AST.TStructOrUnion.CreateIncompleteUnion(this.Name.Value, isConst, isVolatile);

                        env = env.PushEntry(AST.Env.EntryKind.TYPEDEF, typeName, type);
                        return Tuple.Create(env, type);
                    }

                    if (entry_opt.Value.kind != AST.Env.EntryKind.TYPEDEF) {
                        throw new InvalidProgramException(typeName + " is not a type? This should be my fault.");
                    }

                    // If the struct/union is found, return it.
                    return Tuple.Create(env, entry_opt.Value.type);

                } else {
                    // Case 2: If an attribute list is supplied.

                    // 1) Make sure there is no complete struct/union in the current environment.
                    Option<AST.Env.Entry> entry_opt = env.Find(typeName);
                    if (entry_opt.IsSome && entry_opt.Value.type.kind == AST.ExprType.Kind.STRUCT_OR_UNION && ((AST.TStructOrUnion)entry_opt.Value.type).IsComplete) {
                        throw new InvalidOperationException($"Redefining {typeName}");
                    }

                    // 2) Add an incomplete struct/union into the environment.
                    AST.TStructOrUnion type =
                        (this.StructOrUnion == StructOrUnion.STRUCT)
                        ? AST.TStructOrUnion.CreateIncompleteStruct(this.Name.Value, isConst, isVolatile)
                        : AST.TStructOrUnion.CreateIncompleteUnion(this.Name.Value, isConst, isVolatile);
                    env = env.PushEntry(AST.Env.EntryKind.TYPEDEF, typeName, type);

                    // 3) Iterate over the attributes.
                    Tuple<AST.Env, List<Tuple<String, AST.ExprType>>> r_attribs = GetAttribs(env);
                    env = r_attribs.Item1;

                    // 4) Make the type complete. This would also change the entry inside env.
                    if (this.StructOrUnion == StructOrUnion.STRUCT) {
                        type.DefineStruct(r_attribs.Item2);
                    } else {
                        type.DefineUnion(r_attribs.Item2);
                    }

                    return new Tuple<AST.Env, AST.ExprType>(env, type);
                }
            }
        }
    }

    /// <summary>
    /// enum-specifier
    ///   : enum [identifier]? '{' enumerator-list '}'
    ///   | enum identifier
    /// </summary>
    public class EnumSpec : TypeSpec {
        [Obsolete]
        public EnumSpec(String name, IReadOnlyList<Enumr> enums)
            : this(Option.Some(name), Option.Some(enums.ToImmutableList())) { }

        protected EnumSpec(Option<String> name, Option<ImmutableList<Enumr>> enumrs) {
            this.Name = name;
            this.Enumrs = enumrs;
        }

        protected static EnumSpec Create(Option<String> name, Option<ImmutableList<Enumr>> enumrs) =>
            new EnumSpec(name, enumrs);

        public static EnumSpec Create(Option<String> name, ImmutableList<Enumr> enumrs) =>
            Create(name, Option.Some(enumrs));

        public override Tuple<AST.Env, AST.ExprType> GetExprTypeEnv(AST.Env env, Boolean is_const, Boolean is_volatile) {
            if (Enumrs == null) {
                // if there is no content in this enum type, we must find it's definition in the environment
                Option<AST.Env.Entry> entry_opt = env.Find($"enum {Name}");
                if (entry_opt.IsNone || entry_opt.Value.kind != AST.Env.EntryKind.TYPEDEF) {
                    throw new InvalidOperationException($"Type 'enum {Name}' has not been defined.");
                }
            } else {
                // so there are something in this enum type, we need to put this type into the environment
                Int32 idx = 0;
                foreach (Enumr elem in Enumrs) {
                    Tuple<AST.Env, String, Int32> r_enum = elem.GetEnumerator(env, idx);
                    env = r_enum.Item1;
                    String name = r_enum.Item2;
                    idx = r_enum.Item3;
                    env = env.PushEnum(name, new AST.TLong(), idx);
                    idx++;
                }
                env = env.PushEntry(AST.Env.EntryKind.TYPEDEF, "enum " + Name, new AST.TLong());
            }

            return new Tuple<AST.Env, AST.ExprType>(env, new AST.TLong(is_const, is_volatile));
        }

        public Option<String> Name { get; }
        public Option<ImmutableList<Enumr>> Enumrs { get; }
    }
}