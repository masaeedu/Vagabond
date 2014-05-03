﻿namespace Nessos.Vagrant

    open System
    open System.Reflection

    open Nessos.FsPickler

    /// unique identifier for assembly
    [<StructuralComparison>]
    [<StructuralEquality>]
    type AssemblyId =
        {
            /// assembly qualified name
            FullName : string
            /// digest of the raw assembly image
            ImageHash : byte []
        }
    with
        member id.GetName() = new AssemblyName(id.FullName)

    /// static initialization data for portable assembly
    [<NoEquality; NoComparison>]
    type StaticInitializer =
        {
            /// Generation of given static initializer
            Generation : int

            /// Static initialization data
            Data : byte []

            /// Is partial static initialization data
            IsPartial : bool
        }

    /// Contains information necessary for the exportation of an assembly
    [<NoEquality; NoComparison>] 
    type PortableAssembly =
        {
            // Assembly Metadata
            Id : AssemblyId

            /// Raw image of the assembly
            Image : byte [] option

            /// Symbols file
            Symbols : byte [] option

            /// Static initialization data
            StaticInitializer : StaticInitializer option
        }
    with
        member pa.FullName = pa.Id.FullName
        member pa.GetName() = pa.Id.GetName()
        static member Empty (id : AssemblyId) =
            { Id = id ; Image = None ; Symbols = None ; StaticInitializer = None }

    /// Static initialization metadata
    [<NoEquality; NoComparison>] 
    type StaticInitializationInfo =
        {
            /// Generation of given static initializer
            Generation : int

            /// Is partial static initialization data
            IsPartial : bool

            /// Static initialization errors
            Errors : (FieldInfo * exn) []
        }

    /// Assembly load information
    type AssemblyLoadInfo =
        | NotLoaded of AssemblyId
        | LoadFault of AssemblyId * exn
        | Loaded of AssemblyId
        | LoadedWithStaticIntialization of AssemblyId * StaticInitializationInfo
    with
        member info.Id = 
            match info with
            | NotLoaded id
            | LoadFault (id,_)
            | Loaded id -> id
            | LoadedWithStaticIntialization(id,_) -> id

    type IRemoteAssemblyReceiver =
        abstract GetLoadedAssemblyInfo : AssemblyId list -> Async<AssemblyLoadInfo list>
        abstract PushAssemblies : PortableAssembly list -> Async<AssemblyLoadInfo list>

    type IRemoteAssemblyPublisher =
        abstract GetRequiredAssemblyInfo : unit -> Async<AssemblyLoadInfo list>
        abstract PullAssemblies : AssemblyId list -> Async<PortableAssembly list>

    /// customizes slicing behaviour on given dynamic assembly
    type IDynamicAssemblyProfile =

        /// identifies dynamic assemblies that match this profile
        abstract IsMatch : Assembly -> bool

        /// a short description of the profile
        abstract Description : string
        
        /// Specifies if type is to be included in every iteration of the slice
        abstract AlwaysIncludeType: Type -> bool

        /// Specifies if type is to be erased from slices
        abstract EraseType : Type -> bool

        /// Specifies if static constructor is to be erased
        abstract EraseStaticConstructor : Type -> bool

        /// Specifies if static field is to be pickled
        abstract PickleStaticField : FieldInfo * isErasedCtor : bool -> bool

        /// Decides if given slices requires fresh evaluation of assemblies
        abstract IsPartiallyEvaluatedSlice : sliceResolver : (Type -> Assembly option) -> Assembly -> bool


    type VagrantException (message : string, ?inner : exn) =
        inherit Exception(sprintf "Vagrant error: %s"  message, defaultArg inner null)