namespace Tensorflow
open System;
open System.Collections;
open System.Collections.Generic;
open System.Linq;
open System.Numerics;
open System.Runtime.InteropServices;
open System.Text;
type size_t = System.UIntPtr
type TF_Tensor = System.IntPtr
open Microsoft.FSharp.NativeInterop


#noward "9"

/// <summary>
/// Signature that methods must conform to to be used to release memory that was passed to a manually allocated TFTensor
/// </summary>
type Deallocator = delegate of (IntPtr * IntPtr * IntPtr)-> unit

/// <summary>
/// TFTensor holds a multi-dimensional array of elements of a single data type.
/// </summary>
/// <remarks>
/// <para>
/// You can create tensors with the various constructors in this class, or using
/// the implicit conversions from various data types into a TFTensor, including
/// the creation of tensors from simple constants (returning a tensor that reprensets
/// a scalar, that is, it is a 0D tensor), arrays (returning a tensor of a single
/// dimension, 1D) or arbitrary multidimensional arrays.
///</para>
/// <para>
///   Given a tensor, you can retrieve the number of dimensions in it via the
///   NumDims property, or you can retrieve the shape of a tensor, that is how many
///   elements on each dimension the tensor has, by fetching the Shape property.
/// </para>
/// <para>
/// The implicit conversions for basic types produce tensors of one dimesion with
/// a single element, while the implicit conversion from an array, expects a multi-dimensional
/// array that is converted into a tensor of the right dimensions.
/// </para>
/// <para>
/// The special "String" tensor data type that you will find in TensorFlow documentation
/// really represents a byte array.   You can create string tensors by using the <see cref="M:TensorFlow.TFTensor.CreateString"/> 
/// method that takes a byte array buffer as input.
/// </para>
/// <example>
/// <code>
///   TFTensor scalar = 1;           // Creates a 0D tensor, for the integer value 1
///   int d = scalar.NumDims;        // d will be equal to zero, as it is a 0D tensor
///   long [] shape = scalar.Shape   // returns an empty array, as it is a 0D tensor
///   
///   TFTensor list = new [] {1,2,3} // Creates a 1D tensor, or vector, for the values 1, 2, 3
///   d = list.NumDims;              // d will be one
///   shape = list.Shape;            // shape will be an array with a single value 3, representing that the dimension 0 has 3 elements
/// 
///                                  // Creates a 3D tensor, 
///   TFTensor cube = new [,,] { {{1,2,3},{4,5,6}}}
///   d = cube.NumDims               // d will be 3
///   shape = list.Shape             // shape will be [1,2,3] which is the shape of the above 3D array
/// </code>
/// </example>
/// </remarks>
type TFTensor internal (handle : IntPtr) as this =
    inherit TFDisposableThreadSafe(handle)

    // extern TF_Tensor * TF_NewTensor (TF_DataType, const int64_t *dims, int num_dims, void *data, size_t len, void (* deallocator)(void *, size_t, void *), void *deallocator_arg);
    [<DllImport (NativeBinding.TensorFlowLibrary)>]
    static extern TF_Tensor TF_NewTensor (DType dataType, int64 [] dims, int num_dims, IntPtr data, size_t len, Deallocator deallocator, IntPtr deallocator_arg);

    // [<DllImport (NativeBinding.TensorFlowLibrary)>]
    // static extern TF_Tensor TF_NewTensor (DType dataType, IntPtr zeroDims, int num_dims, IntPtr data, size_t len, Deallocator deallocator, IntPtr deallocator_arg);

    // extern TF_Tensor * TF_AllocateTensor (TF_DataType, const int64_t *dims, int num_dims, size_t len);
    [<DllImport (NativeBinding.TensorFlowLibrary)>]
    static extern TF_Tensor TF_AllocateTensor (DType dataType, int64 [] dims, int num_dims, size_t len);

    // [<DllImport (NativeBinding.TensorFlowLibrary)>]
    // static extern TF_Tensor TF_AllocateTensor (DType dataType, IntPtr zeroDim, int num_dims, size_t len);

    // extern void TF_DeleteTensor (TF_Tensor *);
    [<DllImport (NativeBinding.TensorFlowLibrary)>]
    static extern void TF_DeleteTensor (TF_Tensor tensor);

    // extern TF_DataType TF_TensorType (const TF_Tensor *);
    [<DllImport (NativeBinding.TensorFlowLibrary)>]
    static extern DType TF_TensorType (TF_Tensor tensor);

    // extern int TF_NumDims (const TF_Tensor *);
    [<DllImport (NativeBinding.TensorFlowLibrary)>]
    static extern int TF_NumDims (TF_Tensor tensor);

    // extern int64_t TF_Dim (const TF_Tensor *tensor, int dim_index);
    [<DllImport (NativeBinding.TensorFlowLibrary)>]
    static extern int64 TF_Dim (TF_Tensor tensor, int dim_index);

    // extern size_t TF_TensorByteSize (const TF_Tensor *);
    [<DllImport (NativeBinding.TensorFlowLibrary)>]
    static extern size_t TF_TensorByteSize (TF_Tensor tensor);

    // extern void * TF_TensorData (const TF_Tensor *);
    [<DllImport (NativeBinding.TensorFlowLibrary)>]
    static extern IntPtr TF_TensorData (TF_Tensor tensor);

    static let FreeTensorDataDelegate : Deallocator = Deallocator(TFTensor.FreeTensorData)
    static let FreeTensorHandleDelegate : Deallocator  = Deallocator(TFTensor.FreeTensorHandle)

    let createFromMultidimensionalArrays (array : Array) =
        let t = array.GetType().GetElementType ();
        let tc = Type.GetTypeCode (t);
        let dt, size = 
            match tc with 
            | TypeCode.Boolean -> DType.Bool, 1
            | TypeCode.SByte -> DType.Int8, 1
            | TypeCode.Byte -> DType.UInt8, 1
            | TypeCode.Int16 -> DType.Int16, 2
            | TypeCode.UInt16 -> DType.UInt16, 2
            | TypeCode.Int32 -> DType.Int32, 4
            | TypeCode.Int64 -> DType.Int64, 8
            | TypeCode.Single -> DType.Float32, 4
            | TypeCode.Double -> DType.Float32, 8
            // Check types that are not handled by the typecode
            | _ when t.IsAssignableFrom (typeof<Complex>) -> DType.Complex128, 16
            | _ -> raise(ArgumentException(sprintf "The data type %O is not supported" tc))

        let dims = [|for i = 0 to array.Rank - 1 do yield int64(array.GetLength (i)) |]
        let totalSize = dims |> Array.fold (*) (int64 size)
        //this.SetupMulti (dt, dims, array, totalSize)
        failwith "todo"

    [<MonoPInvokeCallback (typeof<Deallocator>)>]
    static member FreeTensorData (data : IntPtr, len : IntPtr, closure : IntPtr) = 
        Marshal.FreeHGlobal (data);

    [<MonoPInvokeCallback (typeof<Deallocator>)>]
    static member FreeTensorHandle (data : IntPtr, len : IntPtr, closure : IntPtr) =
        let gch = GCHandle.FromIntPtr (closure);
        gch.Free ();





    // General purpose constructor, specifies data type and gets pointer to buffer
    // Is the default good, one where we let the user provide their own deallocator, or should we make a copy in that case?
    /// <summary>
    /// Low-level tensor constructor that creates a tensor from a buffer pointed to by an IntPtr.
    /// </summary>
    /// <param name="dataType">Specifies the data type held by the tensor, as well as how to interpret the provided data.</param>
    /// <param name="dims">Describes the tensor shape, an array that indicates .</param>
    /// <param name="data">Pointer to the raw data that will be used to initialize the tensor.</param>
    /// <param name="dataSize">The size of the data being passed in.</param>
    /// <param name="deallocator">Deallocator method, it is invoked when the tensor is destroyed to release the data pointed to by <paramref name="data"/>.   On platforms like iOS (or other static compilation platforms), yiou must annotate the method specified in the deallocator with a <see cref="T:TensorFlow.MonoPInvokeCallbackAttribute"/>.</param>
    /// <param name="deallocatorData">An optional argument of data that is passed to the deallocator method when the tensor is destroyed, you can use this to pass context information.</param>
    new (dataType : DType, dims : int64 [], data : IntPtr, dataSize : size_t, deallocator : Deallocator, deallocatorData : IntPtr) =
        if box dims = null then raise (ArgumentNullException ("dims"))
        let handle = TF_NewTensor (dataType, dims, dims.Length, data, dataSize, deallocator, deallocatorData);
        TFTensor (handle)


    override this.NativeDispose (handle : IntPtr) = TF_DeleteTensor (handle);

    /// <summary>
    /// Low-level: Creates an empty tensor of the specified type and shape, with the specified number of elements
    /// </summary>
    /// <param name="dataType">Data type.</param>
    /// <param name="dims">Tensor shape.</param>
    /// <param name="size">Size in bytes of the tensor, this will be the actual memory allocated.</param>
    /// <remarks>
    /// It is the responsibility of the caller to ensure that the size is correct given the data type size
    /// and the tensor dimension specified in dims.
    /// </remarks>
    new (dataType : DType, dims : int64 [], size : int) =
      if dims = null then raise (ArgumentNullException ("dims"))
      let handle = TF_AllocateTensor (dataType, dims, dims.Length, UIntPtr(uint64 size))
      new TFTensor (handle)


    /// <summary>
    /// Returns the data type for the tensor.
    /// </summary>
    /// <value>The type of the tensor.</value>
    member this.DType : DType = TF_TensorType (handle) 

    /// <summary>
    /// Returns the number of dimensions in the tensor.
    /// </summary>
    /// <remarks>
    /// For single-dimension tensors the return is 1, 2 dimensions is 2 and so on.
    /// </remarks>
    member this.NumDims : int= TF_NumDims (handle)

    /// <summary>
    /// Returns the number of elements on a specific dimension in the tensor.
    /// </summary>
    /// <returns>The tensor dimension.</returns>
    /// <param name="dimIndex">Dimension that you are querying.</param>
    /// <remarks>
    /// If you have a tensor of 3 elements by 5, represented by [3 5],
    /// the GetTensorDimension(0) will return 3, the GetTensorDimension(1)
    /// will return 5.
    /// </remarks>
    member this.GetTensorDimension (dimIndex : int) = TF_Dim (handle, dimIndex)

    member this.TensorByteSize : size_t = TF_TensorByteSize (handle)

    /// <summary>
    /// Returns a pointer to the raw data in the tensor.
    /// </summary>
    /// <remarks>
    /// The contents of the Data must be interpreted according to the type of the
    /// data as described by the DataType property.   The amount of data
    /// is given by the the TensorByteSize property.
    /// </remarks>
    member this.Data : IntPtr =  TF_TensorData (handle)

    /// <summary>
    /// Returns the tensor shape, this is an array whose size determines the number of dimensions on the tensor, and each element is the size of the dimension
    /// </summary>
    /// <remarks>
    ///     An array of size 0 is used for constants, an array of size 1 is used
    ///     for single-dimension arrays, where the dimension is the value of the
    ///     first element.   And so on.
    /// </remarks>
    member this.Shape with get() : int64[] = [|for i = 0 to TF_NumDims (handle) do yield TF_Dim (handle, i)|]

    /// <summary>
    /// Converts a <see cref="TFDataType"/> to a system type.
    /// </summary>
    /// <param name="type">The <see cref="TFDataType"/> to be converted.</param>
    /// <returns>The system type corresponding to the given <paramref name="type"/>.</returns>
    static member TypeFromTensorType (dtype:DType) : Type =
        match dtype with
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
    /// Converts a system type to a <see cref="TFDataType"/>.
    /// </summary>
    /// <param name="t">The system type to be converted.</param>
    /// <returns>The <see cref="TFDataType"/> corresponding to the given type.</returns>
    static member TensorTypeFromType (t:Type) : DType = 
        //if true then DType.Float32 else DType.Unknown
        if   t = typeof<float>     then DType.Float32
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

    static member internal FetchSimple (dt : DType, data : obj) : obj =
        match dt with 
        | DType.Float32 -> Convert.ToSingle (data) |> box
        | DType.Float64 -> Convert.ToDouble (data) |> box
        | DType.Int32   -> Convert.ToInt32 (data) |> box
        | DType.UInt8   -> Convert.ToByte (data) |> box
        | DType.Int16   -> Convert.ToInt16 (data) |> box
        | DType.Int8    -> Convert.ToSByte (data) |> box // ?
        | DType.String  -> raise(NotImplementedException())
        | DType.Int64   -> Convert.ToInt64 (data) |> box
        | DType.Bool    -> Convert.ToBoolean (data) |> box
        | DType.UInt16  -> Convert.ToUInt16 (data) |> box
        | DType.Complex128 -> data :?> Complex |> box
        | _ -> box null

    static member FetchSimple (dt : DType, data : IntPtr ) : obj =
        match dt with 
        | DType.Float32 -> data |> NativePtr.ofNativeInt<float32> |> box
        | DType.Float64 -> data |> NativePtr.ofNativeInt<double> |> box
        | DType.Int32   -> data |> NativePtr.ofNativeInt<int32> |> box
        | DType.UInt8   -> data |> NativePtr.ofNativeInt<uint8> |> box
        | DType.Int16   -> data |> NativePtr.ofNativeInt<int16>|> box
        | DType.String  -> raise(NotImplementedException())
        | DType.Int64   -> data |> NativePtr.ofNativeInt<int64> |> box
        | DType.Bool    -> data |> NativePtr.ofNativeInt<bool> |> box
        | DType.UInt16  -> data |> NativePtr.ofNativeInt<uint16> |> box
        | DType.Complex128 -> data |> NativePtr.ofNativeInt<Complex>  |> box
        | _ -> box null

    //used to create multidementional arrays / tensor with a constant value
    static member Set (target : Array, dt : DType, shape : int64 [], idx : int [], level : int, value : obj) =
        if level < shape.Length - 1 then
            idx.[level] <- 0
            while (idx.[level] < int shape.[level]) do
                TFTensor.Set (target, dt, shape, idx, level + 1, value);
                idx.[level] <- idx.[level] + 1
        else
            idx.[level] <- 0
            while (idx.[level] < int shape.[level]) do
                match dt with 
                | DType.Float32
                | DType.Float64
                | DType.Int32
                | DType.UInt32
                | DType.Int16
                | DType.Int8
                | DType.Int64
                | DType.Bool
                | DType.Complex128 -> target.SetValue (value, idx)
                | DType.String -> 
                    raise(NotImplementedException ("String decoding not implemented for tensor vecotrs yet"))
                | _ -> raise(NotImplementedException ())
                idx.[level] <- idx.[level] + 1

    static member Copy(src : IntPtr,  target : voidptr, size : int) =
        // TODO Double check the NativePtr conversions
        Buffer.MemoryCopy (src |> NativePtr.ofNativeInt<int64> |> NativePtr.toVoidPtr, target, uint64 size, uint64 size);

    static member Copy(target : Array, dt : DType, shape : int64 [], idx : int [], level : int, data : ref<IntPtr>) =
            if level < shape.Length - 1 then
                idx.[level] <- 0
                while (idx.[level] < int shape.[level]) do
                    TFTensor.Copy (target, dt, shape, idx, level + 1, ref data);
                    idx.[level] <- idx.[level] + 1
            else
                idx.[level] <- 0
                while (idx.[level] <- int shape.[level]) do
                    let offset = dt.ByteSize
                    match dt with 
                    | DType.Float32
                    | DType.Float64
                    | DType.Int32
                    | DType.UInt32
                    | DType.Int16
                    | DType.Int8
                    | DType.Int64
                    | DType.Bool
                    | DType.Complex128 -> 
                        // TODO note, we may have to cast here value here?, will know more when this can type check
                        target.SetValue (value, idx)
                        data := !data |> NativePtr.add offset
                    | DType.String -> 
                        raise(NotImplementedException ("String decoding not implemented for tensor vecotrs yet"))
                    | _ ->
                        raise(NotImplementedException ())

    static member FetchFlatArray (target : Array, dt : DType, data : IntPtr) =
        let len = target.Length;
        let size = dt.ByteSize
        match dt, target with 
        | DType.Int8, (:? array<int8> as asbyte) -> 
            use p = fixed &asbyte.[0]
            TFTensor.Copy (data, p, len * size)
        | DType.Bool, (:? array<bool> as abool) ->
            use p = fixed &abool.[0]
            TFTensor.Copy (data, p, len * size)
        | DType.UInt16, (:? array<uint16> as ushort) ->
            use p = fixed &ushort.[0]
            TFTensor.Copy (data, p, len * size)
        | DType.Complex128, (:? array<Complex> as acomplex) ->
            use p = fixed &acomplex.[0]
            TFTensor.Copy (data, p, len * size)
        | DType.Float32, (:? array<float32> as afloat) ->
            use p = fixed &afloat.[0]
            TFTensor.Copy (data, p, len * size)
        | DType.Float64, (:? array<double> as adouble) ->
            use p = fixed &adouble.[0]
            TFTensor.Copy (data, p, len * size)
        | DType.Int32, (:? array<int32> as aint) ->
            use p = fixed &adouble.[0]
            TFTensor.Copy (data, p, len * size)
        | DType.UInt8, (:? array<uint8> as abyte) ->
            use p = fixed &abyte.[0]
            TFTensor.Copy (data, p, len * size)
        | DType.Int16, (:? array<float16> as ashort) ->
            use p = fixed &ashort.[0]
            TFTensor.Copy (data, p, len * size)
        | DType.Int64, (:? array<int64> as along) ->
            use p = fixed &along.[0]
            TFTensor.Copy (data, p, len * size)
        | DType.String -> 
           // need to return an array of TFStrings []
           raise (NotImplementedException ())
        | _ -> raise (NotImplementedException ())

    static member FetchJaggedArray (t : Type, dt : TFDataType, data : ref<IntPtr>, shape : int64 [], ?level : int ) =
        let level = defaultArg level 0
        let size = dt.ByteSize()
        match dt with 
        // If we are at the last node
        if level = shape.Length - 1 then
            let target = Array.CreateInstance (t, shape [level]);
            for  l = 0L l < shape.[level] - 1L do
                target.SetValue(data,l)

                match dt with
                | DType.Float32 ( 
                  //target.SetValue (((float)data), l);
                  data += 4;
                case TFDataType.Double:
                  target.SetValue ((*(double*)data), l);
                  data += 8;
                  break;
                case TFDataType.Int32:
                  target.SetValue ((*(int*)data), l);
                  data += 4;
                  break;
                case TFDataType.UInt8:
                  target.SetValue ((*(byte*)data), l);
                  data += 1;
                  break;
                case TFDataType.Int16:
                  target.SetValue ((*(short*)data), l);
                  data += 2;
                  break;
                case TFDataType.Int8:
                  target.SetValue ((*(sbyte*)data), l);
                  data += 1;
                  break;
                case TFDataType.Int64:
                  target.SetValue ((*(long*)data), l);
                  data += 8;
                  break;
                case TFDataType.Bool:
                  target.SetValue ((*(bool*)data), l);
                  data += 1;
                  break;
                case TFDataType.Complex128:
                  target.SetValue ((*(Complex*)data), l);
                  data += sizeof (Complex);
                  break;
                case TFDataType.String:
                  throw new NotImplementedException ("String decoding not implemented for tensor vecotrs yet");
                default:
                  throw new NotImplementedException ();
            target
        else
            let mutable target = None
            let top = shape.[level]
            if top < Int32.MaxValue then
              let itop = int(top)
              for i = 0 to itop - 1 do
                let childArray = FetchJaggedArray (t, dt, ref data, shape, level + 1)
                match target with | None -> target <- Array.CreateInstance (childArray.GetType (), shape [level])
                target.Value.SetValue (childArray, i)
            else
              for l = 0L to top - 1L do
                let chidArray = this.FetchJaggedArray (t, dt, ref data, shape, level + 1)
                match target with | None -> target <- Array.CreateInstance (childArray.GetType (), shape [level])
                target.SetValue (chidArray, l)
      return target


    static member FetchMultiDimensionalArray (target : Array, dt : TFDataType, data : IntPtr, shape : int64 []) =
        let idx = Array.zeroCreate<int> target.Length
        for i = 0 to shape.Length - 1 do
          if (shape.[i] > Int32.MaxValue) then raise(ArgumentOutOfRangeException ("Shape can not be longer than 32 bits"))
        TFTensor.Copy (target, dt, shape, idx, 0, ref data);

  /// <summary>
  /// Returns the value of the Tensor as a C# type if possible, or null if the data type can not be represented in C#
  /// </summary>
  /// <param name="jagged">
  /// The default is set to false, which returns .NET multi-dimensional arrays for multi-dimensional
  /// tensors.    This is useful to feed the data back as a TFTensor created from an array.   Set to
  /// true if you want to get arrays pointing to arrays, which are slightly more convenient to work
  /// with from C#
  /// </param>
  /// <remarks>
  /// Jagged arrays create various intermediate arrays, while multi-dimensional arrays are more
  /// efficient memory-wise.
  /// </remarks>
  /// <returns>The value encodes the contents of the tensor, and could include simple values, arrays and multi-dimensional values.</returns>
  member this.GetValue(?jagged : bool) : obj =
    match TFTensor.TypeFromTensorType (this.TensorType) with
    | null -> null
    | t ->
        let jagged = defaultArg jagged false
        match this.NumDims with
        | 0 -> this.FetchSimple (this.TensorType, Data);
        | 1 -> TFTensor.FetchFlatArray (Array.CreateInstance (t, Shape.[0]);, this.TensorType, Data);
        | dims when jagged ->
                IntPtr data = Data;
                FetchJaggedArray (t, this.TensorType, ref data, Shape);
        | dims -> 
                let result = Array.CreateInstance (t, Shape);
                TFTensor.FetchMultiDimensionalArray (result, this.TensorType, Data, Shape);
                result

  /// <summary>
  /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:TensorFlow.TFTensor"/>.
  /// </summary>
  /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:TensorFlow.TFTensor"/>.</returns>
  override this.ToString () : string =
    let n = this.NumDims;
    if n = 0 then this.GetValue ().ToString ();
    else
        let sb = new StringBuilder ("[")
        for i = 0 to n - 1 do
          sb.Append (TF_Dim (handle, i))
          if i + 1 < n then
            sb.Append ("x")
        sb.Append ("]")
        sb.ToString ()

  static member GetLength (array : Array, ?deep : bool, ?max : bool) : int[] =
    let deep = defaultArg deep true
    let max  = defaultArg max false
    // This function gets the length of all dimensions in a multidimensional, jagged, or mixed array.
    // https://github.com/accord-net/framework/blob/b4990721a61f03602d04c12b148215c7eca1b7ac/Sources/Accord.Math/Matrix/Matrix.Construction.cs#L1118
    // Relicensed under the MIT license by the original author for inclusion in TensorFlowSharp and any derived projects, see the MIT license for details
    if array.Rank = 0 then [||]
    elif deep && isJagged (array) then
      if array.Length = 0 then [||]
      else
      let rest =
          if not(max) then getLength (array.GetValue (0) as Array, deep);
          else
            // find the max
            let rest = getLength (array.GetValue (0) :> Array, deep)
            let r = [| for i = 1 to array.Length - 1 do yield getLength (array.GetValue (i) :> Array, deep) |]
            for j = 0 to r.Length - 1 do
                if (r.[j] > rest.[j]) then
                  rest.[j] = r.[j];
       [| yield! array.Length; yield! rest |] // not sure about this
    Array.init array.Rank (fun i -> array.GetUpperBound (i) + 1;)

  static member deepFlatten (array : Array) : Array =
    // This function converts multidimensional, jagged, or mixed arrays into a single unidimensional array (i.e. flattens the mixed array).
    // https://github.com/accord-net/framework/blob/f78181b82eb6ee6cc7fd10d2a7a55334982c40df/Sources/Accord.Math/Matrix/Matrix.Common.cs#L1625
    // Relicensed under the MIT license by the original author for inclusion in TensorFlowSharp and any derived projects, see the MIT license for details
    let totalLength = TFTensor.getTotalLength (array, deep: true);
    let elementType = TFTensor.getInnerMostType (array);
    let result = Array.CreateInstance (elementType, totalLength);
    let mutable k = 0;
    for v in TFTensor.enumerateJagged (array) do
      result.SetValue (v, k)
      k <- k + 1
    result

    static member enumerateJagged (array : Array) : IEnumerable =
        // This function can enumerate all elements in a multidimensional ,jagged, or mixed array.
        // From https://github.com/accord-net/framework/blob/b4990721a61f03602d04c12b148215c7eca1b7ac/Sources/Accord.Math/Matrix/Jagged.Construction.cs#L1202
        // Relicensed under the MIT license by the original author for inclusion in TensorFlowSharp and any derived projects, see the MIT license for details
        let arrays = new Stack<Array> ()
        let counters = new Stack<int> ()

        arrays.Push (array)
        counters.Push (0)
        let mutable depth = 1

        let mutable a = array;
        let mutable i = 0;

        [|
            while arrays.Count > 0 do
              if (i >= a.Length) then
                a <- arrays.Pop ()
                i <- counters.Pop () + 1
                depth <- depth - 1
              else
                let e = a.GetValue (i)
                let next = e :> Array
                if next = null then
                  yield e 
                  i <- i + 1
                else
                  arrays.Push (a)
                  counters.Push (i)
                  a <- next
                  i <- 0
                  depth <- depth + 1
        |]

    static member GetTotalLength (array : Array, ?deep : bool, ?rectangular : bool) =
        // From https://github.com/accord-net/framework/blob/b4990721a61f03602d04c12b148215c7eca1b7ac/Sources/Accord.Math/Matrix/Matrix.Construction.cs#L1087
        // Relicensed under the MIT license by the original author for inclusion in TensorFlowSharp and any derived projects, see the MIT license for details
        let deep = defaultArg true
        let rectangular = defaultArg true
        if deep && isJagged (array.GetType ()) then
          if rectangular then
            let rest = TFTensor.GetTotalLength (array.GetValue (0) :> Array, deep);
            array.Length * rest
          else 
            let mutable sum = 0;
            for i = 0 to array.Length - 1 do
              sum <- sum TFTensor.GetTotalLength (array.GetValue (i) :> Array, deep))
            sum
        else array.Length

    static member IsArrayJagged (array : Array) = 
        // From https://github.com/accord-net/framework/blob/f78181b82eb6ee6cc7fd10d2a7a55334982c40df/Sources/Accord.Math/Matrix/Matrix.Construction.cs#L1204
        // Relicensed under the MIT license by the original author for inclusion in TensorFlowSharp and any derived projects, see the MIT license for details
        if (array.Length = 0) then array.Rank = 1
        else array.Rank = 1 && (array.GetValue (0) isAssignableTo<Array>)

    static member IsTypeJagged (type : Type) =
     // From https://github.com/accord-net/framework/blob/eb371fbc540a41c1a711b6ab1ebd49889316e7f7/Sources/Accord.Math/Matrix/Matrix.Common.cs#L84
     // Relicensed under the MIT license by the original author for inclusion in TensorFlowSharp and any derived projects, see the MIT license for details
        type.IsArray && type.GetElementType().IsArray;

    static member GetInnerMostType (array : Array) : Type =
     // From https://github.com/accord-net/framework/blob/eb371fbc540a41c1a711b6ab1ebd49889316e7f7/Sources/Accord.Math/Matrix/Matrix.Common.cs#L95
     // Relicensed under the MIT license by the original author for inclusion in TensorFlowSharp and any derived projects, see the MIT license for details
         let mutable _type = array.GetType ()
         while type.IsArray do
           _type <- type.GetElementType ()
         type

 
// Factory methods to create tensors from a constant
     

     /// <summary>
     /// Converts an integer into a 1-dimensional, 1-valued tensor.
     /// </summary>
     /// <returns>The tensor representing the integer value.</returns>
     /// <param name="value">Value to initialize the tensor with.</param>
     static member op_Implicit(value : int) = TFTensor (value)

     /// <summary>
     /// Converts a boolean into a 1-dimensional, 1-valued tensor.
     /// </summary>
     /// <returns>The tensor representing the integer value.</returns>
     /// <param name="value">Value to initialize the tensor with.</param>
     static member op_Implicit(value : bool) = TFTensor (value)

     /// <summary>
     /// Converts a long into a 1-dimensional, 1-valued tensor.
     /// </summary>
     /// <returns>The tensor representing the long value.</returns>
     /// <param name="value">Value to initialize the tensor with.</param>
     static member op_Implicit(value : int64) = TFTensor (value)

     /// <summary>
     /// Converts a double into a 1-dimensional, 1-valued tensor.
     /// </summary>
     /// <returns>The tensor representing the double value.</returns>
     /// <param name="value">Value to initialize the tensor with.</param>
     static member op_Implicit(value : double) = TFTensor (value)

     /// <summary>
     /// Converts a float into a 1-dimensional, 1-valued tensor.
     /// </summary>
     /// <returns>The tensor representing the float value.</returns>
     /// <param name="value">Value to initialize the tensor with.</param>
     static member op_Implicit(value : float) = TFTensor (value)

     /// <summary>
     /// Converts a Complex number into a 1-dimensional, 1-valued tensor.
     /// </summary>
     /// <returns>The tensor representing the complex value.</returns>
     /// <param name="value">Value to initialize the tensor with.</param>
     static member op_Implicit(value : Complex) = TFTensor (value)

     /// <summary>
     /// Converts a byte into a 1-dimensional, 1-valued tensor.
     /// </summary>
     /// <returns>The tensor representing the byte value.</returns>
     /// <param name="value">Value to initialize the tensor with.</param>
     static member op_Implicit(value : byte) = TFTensor (value)

    /// <summary>
    /// Creates a 1 dimensional tensor from an array of booleans.
    /// </summary>
    /// <param name="data">Data.</param>
    new (data : bool []) = SetupTensor (DType.Bool, data, size = !>1)
    /// <summary>
    /// Creates a 1 dimensional tensor from an array of sbytes.
    /// </summary>
    /// <param name="data">Data.</param>
    new (data : int8[]) = SetupTensor (DType.Int8, data, size = !>1)
    /// <summary>
    /// Creates a 1 dimensional tensor from an array of bytes.
    /// </summary>
    /// <param name="data">Data.</param>
    new (data : byte []) = SetupTensor (DType.UInt8, data, size = !>1))
    /// <summary>
    /// Creates a 1 dimensional tensor from an array of shorts.
    /// </summary>
    /// <param name="data">Data.</param>
    new (data : int16[]) = SetupTensor (DType.Int16, data, size = !>2))
    /// <summary>
    /// Creates a 1 dimensional tensor from an array of ushorts
    /// </summary>
    /// <param name="data">Data.</param>
    new (data : uint16[]) = SetupTensor (DType.UInt16, data, size = !>2)
    /// <summary>
    /// Creates a 1 dimensional tensor from an array of ints.
    /// </summary>
    /// <param name="data">Data.</param>
    new (data : int []) = SetupTensor (DType.Int32, data, size = !>4))
    /// <summary>
    /// Creates a 1 dimensional tensor from an array of floats.
    /// </summary>
    /// <param name="data">Data.</param>
    new (data : float []) = SetupTensor (DType.Float, data, size = !>4)
    /// <summary>
    /// Creates a 1 dimensional tensor from an array of doubles.
    /// </summary>
    /// <param name="data">Data.</param>
    new (data : double []) = SetupTensor (DType.Double, data, size = !>8)
    /// <summary>
    /// Creates a 1 dimensional tensor from an array of longs.
    /// </summary>
    /// <param name="data">Data.</param>
    new (data : int64 [] ) = SetupTensor (DType.Int64, data, size = !>8)) 
    /// <summary>
    /// Creates a 1 dimensional tensor from an array of complex numbers.
    /// </summary>
    /// <param name="data">Data.</param>
    new (data : Complex []) = SetupTensor (DType.Complex128, data, size = !>16))


// TODO: Other overloads we could add: String, Complex (float), Bool, QInt8, QUInt8, QInt32, Bfloat16,
// QInt16, QUint16, Half, Resource
// TODO: not clear that this is very useful (the dims versions), perhaps to reduce the surface of
// construcors these rarer blobs should be "FromSpec" or something like that
  /// <summary>
  /// Creates a new tensor from a portion of an array of sbytes
  /// </summary>
  /// <param name="shape">Represents the tensor shape.</param>
  /// <param name="data">The linear array of data, the data is shuffled to fit in the tensor with the specified dimensions.</param>
  /// <param name="start">The offset into the provided data array where the data resides.</param>
  /// <param name="count">The number of bytes to copy from count into the tensor.</param>
  /// <remarks>
  /// Use the FromBuffer method to create a tensor that has the specified dimensions
  /// and is initialized with data from the data array.   The data is copied starting
  /// at the start offset, for count bytes and is laid out into the tensor following the
  /// specified dimensions.
  /// </remarks>
  static member FromBuffer (shape, int8[] data, int start, int count) =
    TFTensor (SetupTensor (DType.Int8, shape, data, start, count, size = 1))
  
  /// <summary>
  /// Creates a new tensor from a portion of an array of bytes
  /// </summary>
  /// <param name="shape">Represents the tensor shape.</param>
  /// <param name="data">The linear array of data, the data is shuffled to fit in the tensor with the specified dimensions.</param>
  /// <param name="start">The offset into the provided data array where the data resides.</param>
  /// <param name="count">The number of bytes to copy from count into the tensor.</param>
  /// <remarks>
  /// Use the FromBuffer method to create a tensor that has the specified dimensions
  /// and is initialized with data from the data array.   The data is copied starting
  /// at the start offset, for count bytes and is laid out into the tensor following the
  /// specified dimensions.
  /// </remarks>
  static member FromBuffer (shape, uint8[] data, int start, int count) =
    TFTensor (SetupTensor (DType.UInt8, shape, data, start, count, size = 1))
  
  /// <summary>
  /// Creates a new tensor from a portion of an array of shorts
  /// </summary>
  /// <param name="shape">Represents the tensor shape.</param>
  /// <param name="data">The linear array of data, the data is shuffled to fit in the tensor with the specified dimensions.</param>
  /// <param name="start">The offset into the provided data array where the data resides.</param>
  /// <param name="count">The number of bytes to copy from count into the tensor.</param>
  /// <remarks>
  /// Use the FromBuffer method to create a tensor that has the specified dimensions
  /// and is initialized with data from the data array.   The data is copied starting
  /// at the start offset, for count bytes and is laid out into the tensor following the
  /// specified dimensions.
  /// </remarks>
  static member FromBuffer (shape, int16[] data, int start, int count) =
    TFTensor (SetupTensor (DType.Int16, shape, data, start, count, size = 2))
  
  /// <summary>
  /// Creates a new tensor from a portion of an array of ushorts
  /// </summary>
  /// <param name="shape">Represents the tensor shape.</param>
  /// <param name="data">The linear array of data, the data is shuffled to fit in the tensor with the specified dimensions.</param>
  /// <param name="start">The offset into the provided data array where the data resides.</param>
  /// <param name="count">The number of bytes to copy from count into the tensor.</param>
  /// <remarks>
  /// Use the FromBuffer method to create a tensor that has the specified dimensions
  /// and is initialized with data from the data array.   The data is copied starting
  /// at the start offset, for count bytes and is laid out into the tensor following the
  /// specified dimensions.
  /// </remarks>
  static member FromBuffer (shape, uint16[] data, int start, int count) =
    TFTensor (SetupTensor (DType.UInt16, shape, data, start, count, size = 2))

  /// <summary>
  /// Creates a new tensor from a portion of an array of ints
  /// </summary>
  /// <param name="shape">Represents the tensor shape.</param>
  /// <param name="data">The linear array of data, the data is shuffled to fit in the tensor with the specified dimensions.</param>
  /// <param name="start">The offset into the provided data array where the data resides.</param>
  /// <param name="count">The number of bytes to copy from count into the tensor.</param>
  /// <remarks>
  /// Use the FromBuffer method to create a tensor that has the specified dimensions
  /// and is initialized with data from the data array.   The data is copied starting
  /// at the start offset, for count bytes and is laid out into the tensor following the
  /// specified dimensions.
  /// </remarks>
  static member FromBuffer (shape, int32[] data, int start, int count) =
    TFTensor (SetupTensor (DType.Int32, shape, data, start, count, size = 4))
  
  /// <summary>
  /// Creates a new tensor from a portion of an array of floats
  /// </summary>
  /// <param name="shape">Represents the tensor shape.</param>
  /// <param name="data">The linear array of data, the data is shuffled to fit in the tensor with the specified dimensions.</param>
  /// <param name="start">The offset into the provided data array where the data resides.</param>
  /// <param name="count">The number of bytes to copy from count into the tensor.</param>
  /// <remarks>
  /// Use the FromBuffer method to create a tensor that has the specified dimensions
  /// and is initialized with data from the data array.   The data is copied starting
  /// at the start offset, for count bytes and is laid out into the tensor following the
  /// specified dimensions.
  /// </remarks>
  static member FromBuffer (shape, float32[] data, int start, int count) =
    TFTensor (SetupTensor (DType.Float32, shape, data, start, count, size = 4))
  
  /// <summary>
  /// Creates a new tensor from a portion of an array of doubles
  /// </summary>
  /// <param name="shape">Represents the tensor shape.</param>
  /// <param name="data">The linear array of data, the data is shuffled to fit in the tensor with the specified dimensions.</param>
  /// <param name="start">The offset into the provided data array where the data resides.</param>
  /// <param name="count">The number of bytes to copy from count into the tensor.</param>
  /// <remarks>
  /// Use the FromBuffer method to create a tensor that has the specified dimensions
  /// and is initialized with data from the data array.   The data is copied starting
  /// at the start offset, for count bytes and is laid out into the tensor following the
  /// specified dimensions.
  /// </remarks>
  static member FromBuffer (shape, double[] data, int start, int count) =
    TFTensor (SetupTensor (DType.Float64, shape, data, start, count, size = 8))
  
  /// <summary>
  /// Creates a new tensor from a portion of an array of longs
  /// </summary>
  /// <param name="shape">Represents the tensor shape.</param>
  /// <param name="data">The linear array of data, the data is shuffled to fit in the tensor with the specified dimensions.</param>
  /// <param name="start">The offset into the provided data array where the data resides.</param>
  /// <param name="count">The number of bytes to copy from count into the tensor.</param>
  /// <remarks>
  /// Use the FromBuffer method to create a tensor that has the specified dimensions
  /// and is initialized with data from the data array.   The data is copied starting
  /// at the start offset, for count bytes and is laid out into the tensor following the
  /// specified dimensions.
  /// </remarks>
   static member FromBuffer (shape, int64[] data, int start, int count) =
    TFTensor (SetupTensor (DType.Int64, shape, data, start, count, size = 8))
  
  /// <summary>
  /// Creates a new tensor from a portion of an array of Complex numbers
  /// </summary>
  /// <param name="shape">Represents the tensor shape.</param>
  /// <param name="data">The linear array of data, the data is shuffled to fit in the tensor with the specified dimensions.</param>
  /// <param name="start">The offset into the provided data array where the data resides.</param>
  /// <param name="count">The number of bytes to copy from count into the tensor.</param>
  /// <remarks>
  /// Use the FromBuffer method to create a tensor that has the specified dimensions
  /// and is initialized with data from the data array.   The data is copied starting
  /// at the start offset, for count bytes and is laid out into the tensor following the
  /// specified dimensions.
  /// </remarks>
   static member FromBuffer (shape, Comlex[] data, int start, int count) =
    TFTensor (SetupTensor (DType.Complex128, shape, data, start, count, size = 16))

  /// <summary>
  /// Creates a constant tensor from an array, the shape reflects the shape of the C# array and the underlying type reflects the C# type.
  /// </summary>
  new (array : Array) =
    if array = null then raise (new ArgumentNullException ("array"))
    // Ensure that, if we have arrays of arrays, we can handle them accordingly:
    if isJagged (array.GetType ()) then
      let elementType = getInnerMostType (array);
      let length = getLength (array);
      let multidimensional = Array.CreateInstance (elementType, length);
      let flatten = deepFlatten (array);
      Buffer.BlockCopy (flatten, 0, multidimensional, 0, flatten.Length * Marshal.SizeOf (elementType));
      createFromMultidimensionalArrays (multidimensional);
    else
      createFromMultidimensionalArrays (array);

    // TODO clean this up
    static member TFCreate(x:'a) =
        let v = valueToIntPtr x
        TF_NewTensor (DType.Int32, zeroDims: IntPtr.Zero, num_dims: 0, data: v, len: UIntPtr(sizeof<'a>), deallocator = FreeTensorDataDelegate, deallocator_arg = IntPtr.Zero)
        TFTensor(handle)

  /// Creates a constant tensor with a single dimension from an integer value.
  new (value : int) = TFTensor.TFCreate(value)

  /// Creates a constant tensor with a single dimension from a boolean value.
  new (value : bool) = TFTensor.TFCreate(value)

  /// Creates a constant tensor with a single dimension from an sbyte value.
  new (value : int8) = TFTensor.TFCreate(value)

  /// Creates a constant tensor with a single dimension from a short value.
  new (value : int16) = TFTensor.TFCreate(value)

  /// Creates a constant tensor with a single dimension from an ushort value.
  new (value : uint16) = TFTensor.TFCreate(value)

  /// Creates a constant tensor with a single dimension from an byte value.
  new (value : byte) = TFTensor.TFCreate(value)

  /// Creates a constant tensor with a single dimension from a Complex value.
  new (value : Complex) = TFTensor.TFCreate(value)

  /// Creates a constant tensor with a single dimension from a float value.
  new (value : float32) = TFTensor.TFCreate(value)

  /// Creates a constant tensor with a single dimension from a double value.
  new (value : double) = TFTensor.TFCreate(value)

  /// Creates a constant tensor with a single dimension from a long value.
  new (value : int64) = TFTensor.TFCreate(value)

  // Convenience function to factor out the setup of a new tensor from an array
  static member SetupTensor (dt : DType, dims : int64 [], data : Array, size : int) : IntPtr = 
    TFTensor.SetupTensor (dt, dims, data, start: 0, count = data.Length, size = size)

  // Convenience function to factor out the setup of a new tensor from an array
  static member SetupTensor (dt : DType, data : Array, size : int) : IntPtr =
    let dims = Array.init dims.Length (fun i -> data.GetLength (i))
    TFTensor.SetupTensor (dt, dims, data, start = 0, count = data.Length, size = size)

  // Use for single dimension arrays 
  static member SetupTensor (dt : DType, shape : Shape, data : Array, start : int, count : int, size : int) : IntPtr 
    if (shape == null)
      throw new ArgumentNullException (nameof (shape));
    return SetupTensor (dt, shape.dims, data, start, count, size);
  
  // Use for single dimension arrays 
  static IntPtr SetupTensor (DType dt, long [] dims, Array data, int start, int count, int size)
  {
    if (start < 0 || start > data.Length - count)
      throw new ArgumentException ("start + count > Array size");

    var dataHandle = GCHandle.Alloc (data, GCHandleType.Pinned);

    if (dims == null)
      return TF_NewTensor (dt, IntPtr.Zero, 0, dataHandle.AddrOfPinnedObject () + start * size, (UIntPtr)(count * size), FreeTensorHandleDelegate, GCHandle.ToIntPtr (dataHandle));
    else
      return TF_NewTensor (dt, dims, dims.Length, dataHandle.AddrOfPinnedObject () + start * size, (UIntPtr)(count * size), FreeTensorHandleDelegate, GCHandle.ToIntPtr (dataHandle));
  }

  // Use for multiple dimension arrays 
  static IntPtr SetupMulti (DType dt, long [] dims, Array data, long bytes)
  {
    var dataHandle = GCHandle.Alloc (data, GCHandleType.Pinned);

    if (dims == null)
      return TF_NewTensor (dt, IntPtr.Zero, 0, dataHandle.AddrOfPinnedObject (), (UIntPtr)bytes, FreeTensorHandleDelegate, GCHandle.ToIntPtr (dataHandle));
    else
      return TF_NewTensor (dt, dims, dims.Length, dataHandle.AddrOfPinnedObject (), (UIntPtr)bytes, FreeTensorHandleDelegate, GCHandle.ToIntPtr (dataHandle));
/   }

  // Convenience, should I add T[,] and T[,,] as more convenience ones?
  /// <summary>
  /// Creates a single-dimension tensor from a byte buffer.  This is different than creating a tensor from a byte array that produces a tensor with as many elements as the byte array.
  /// </summary>
  public static TFTensor CreateString (byte [] buffer)
  {
    if (buffer == null)
      throw new ArgumentNullException (nameof (buffer));
    //
    // TF_STRING tensors are encoded with a table of 8-byte offsets followed by
    // TF_StringEncode-encoded bytes.
    //
    var size = TFString.TF_StringEncodedSize ((UIntPtr)buffer.Length);
    IntPtr handle = TF_AllocateTensor (DType.String, IntPtr.Zero, 0, (UIntPtr)((ulong)size + 8));

    // Clear offset table
    IntPtr dst = TF_TensorData (handle);
    Marshal.WriteInt64 (dst, 0);
    var status = TFStatus.TF_NewStatus ();
    fixed (byte* src = &buffer [0])
    {
      TFString.TF_StringEncode (src, (UIntPtr)buffer.Length, (sbyte*)(dst + 8), size, status);
      var ok = TFStatus.TF_GetCode (status) == TFCode.Ok;
      TFStatus.TF_DeleteStatus (status);
      if (!ok)
        return null;
    }
    return new TFTensor (handle);
  }
  }

  /// <summary>
  /// Converts a C# array into a tensor.
  /// </summary>
  /// <returns>The tensor containing the data.</returns>
  /// <param name="array">single dimension, or multi-dimensional array.</param>
  /// <remarks>
  /// This implicit conversion can convert single or multidimensional arrays of
  /// booleans, sbytes, byte, shorts, ushorts, ints, longs, doubles, floats and
  /// complex numbers into a tensor with the same dimensional shape as the provided
  /// array.
  /// </remarks>
  public static implicit operator TFTensor (Array array)
  {
    return new TFTensor (array);

  }
