namespace Tensorflow

open System
open System.Numerics

/// <summary>
/// The data type for a specific tensor.
/// </summary>
/// <remarks>
/// Tensors have uniform data types, all the elements of the tensor are of this
/// type and they dictate how TensorFlow will treat the data stored.   
/// </remarks>
[<RequireQualifiedAccess>]
type DType = 
    | Unknown       = 0u
    | Float32       = 1u
    | Float64       = 2u
    | Int32         = 3u
    | UInt8         = 4u
    | Int16         = 5u
    | Int8          = 6u
    | String        = 7u
    | Complex64     = 8u
    | Complex       = 8u
    | Int64         = 9u
    | Bool          = 10u
    | QInt8         = 11u
    | QUInt8        = 12u
    | QInt32        = 13u
    | BFloat16      = 14u
    | QInt16        = 15u
    | QUInt16       = 16u
    | UInt16        = 17u
    | Complex128    = 18u
    | Float16       = 19u
    | Resource      = 20u
    | Variant       = 21u
    | UInt32        = 22u
    | UInt64        = 23u

[<AutoOpen>]
module DTypeExtensions =
    let private toEnum = LanguagePrimitives.EnumOfValue
    let private ofEnum = LanguagePrimitives.EnumToValue

    type DType with    
        member this.IsComplex =
            match this with
            | DType.Complex | DType.Complex64 | DType.Complex128 -> true
            | _ -> false

        member this.IsFloat =
            match this with
            | DType.Float16 | DType.Float32 | DType.Float64 | DType.BFloat16 -> true
            | _ -> false

        member this.IsInteger =
            match this with
            | DType.Int8 | DType.Int16 | DType.Int32 | DType.Int64 -> true
            | _ -> false
        
        member this.Name =
            match this with
            | DType.Float16 -> "float16"
            | DType.Float32 -> "float32"
            | DType.Float64 -> "float64"
            | DType.Int32 -> "int32"
            | DType.UInt8 -> "uint8"
            | DType.UInt16 -> "uint16"
            | DType.UInt32 -> "uint32"
            | DType.UInt64 -> "uint64"
            | DType.Int16 -> "int16"
            | DType.Int8 -> "int8"
            | DType.String -> "string"
            | DType.Complex64 -> "complex64"
            | DType.Complex128 -> "complex128"
            | DType.Int64 -> "int64"
            | DType.Bool -> "bool"
            // | DType.QInt8 -> "qint8"
            // | DType.QUInt8 -> "quint8"
            // | DType.QInt16 -> "qint16"
            // | DType.QUInt16 -> "quint16"
            // | DType.QInt32 -> "qint32"
            | DType.BFloat16 -> "bfloat16"
            | DType.Resource -> "resource"
            | DType.Variant -> "variant"
            | _ -> String.Empty
        
        /// Returns `True` if this `DType` represents a reference type.
        member this.IsRefDtype = 
            ofEnum(this) > 100u

        /// Returns a reference `DType` based on this `DType`.
        member this.AsRef = 
            if this.IsRefDtype then this else  (toEnum(ofEnum(this) + 100u))

        /// Returns a non reference `DType` based on this `DType`.
        member this.BaseType = 
            if this.IsRefDtype then (toEnum(ofEnum(this) - 100u)) else this

        member this.ByteSize =
            match this with
            | DType.Unknown       -> -1 
            | DType.Float32       -> 4
            | DType.Float64       -> 8
            | DType.Int32         -> 4
            | DType.UInt8         -> 1
            | DType.Int16         -> 2
            | DType.Int8          -> 1
            | DType.String        -> -1
            | DType.Complex64     -> 16
            //| DType.Complex       -> 16, this will never be used because it's equal to Complex64
            | DType.Int64         -> 8
            | DType.Bool          -> 1
            | DType.QInt8         -> 1
            | DType.QUInt8        -> 1
            | DType.QInt32        -> 4
            | DType.BFloat16      -> 2
            | DType.QInt16        -> 2
            | DType.QUInt16       -> 2
            | DType.UInt16        -> 2
            | DType.Complex128    -> 32
            | DType.Float16       -> 2
            | DType.Resource      -> -1
            | DType.Variant       -> -1
            | DType.UInt32        -> 4
            | DType.UInt64        -> 8
            | _                   -> -1

        /// <summary>
        /// Converts a <see cref="DType"/> to a system type.
        /// </summary>
        /// <param name="type">The <see cref="DType"/> to be converted.</param>
        /// <returns>The system type corresponding to the given <paramref name="type"/>.</returns>
        member this.ToType =
            match this with
            | DType.Float32 -> typeof<single>
            | DType.Float64 -> typeof<double>
            | DType.Int32 -> typeof<int32>
            | DType.UInt8 -> typeof<uint8>
            | DType.UInt16 -> typeof<uint16>
            | DType.UInt32 -> typeof<uint32>
            | DType.UInt64 -> typeof<uint64>
            | DType.Int16 -> typeof<int16>
            | DType.Int8 -> typeof<int8> // sbyte?
            | DType.String -> typeof<string> // TFString?
            | DType.Complex128 -> typeof<Complex>
            | DType.Int64 -> typeof<int64>
            | DType.Bool -> typeof<bool>
            | _ -> box null :?> Type

        /// <summary>
        /// Converts a system type to a <see cref="DType"/>.
        /// </summary>
        /// <param name="t">The system type to be converted.</param>
        /// <returns>The <see cref="DType"/> corresponding to the given type.</returns>
        static member FromType (t:Type) : DType = 
            //if true then DType.Float32 else DType.Unknown
            if   t = typeof<float32>     then DType.Float32
            elif t = typeof<double>    then DType.Float64
            elif t = typeof<int>       then DType.Int32 
            elif t = typeof<byte>      then DType.UInt8
            elif t = typeof<int16>     then DType.Int16
            elif t = typeof<sbyte>     then DType.Int8
            elif t = typeof<string>    then DType.String
            elif t = typeof<int64>     then DType.Int64
            elif t = typeof<bool>      then DType.Bool
            elif t = typeof<uint16>    then DType.UInt16
            elif t = typeof<Complex>   then DType.Complex128
            else raise(ArgumentOutOfRangeException ("t", sprintf "The given type could not be mapped to an existing DType."))