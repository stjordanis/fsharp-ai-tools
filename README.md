This repo contains archival material about "F# for AI Models". 

Contents: 

* FM: An F# DSL for AI Models with separated shape checking and tooling

  **FM was a prototype F# eDSL for writing numeric models.  It has now been subsumed by [DiffSharp 1.0](https://diffsharp.github.io).**

* The TensorFlow API for F# 

  **This is now archived. We recommend TensorFlow.NET or [DiffSharp 1.0](https://diffsharp.github.io).**

* Live Checking Tooling for AI models

  **This is now being merged to [DiffSharp 1.0](https://github.com/DiffSharp/DiffSharp/pull/207).**

* fsx2nb now part of [the fsdocs tool](https://fsprojects.github.io/FSharp.Formatting/commandline.html).**

# ARCHIVAL MATERIAL: FM: An F# DSL for AI Models 


Models written in FM can be passed to 
optimization and training algorithms utilising automatic differentiation without
any change to modelling code, and can be executed on GPUs and TPUs using TensorFlow.

There is also experimental tooling for interactive tensor shape-checking, inference, tooltips and other nice things. 

This is a POC that it is possible to configure F# to be suitable for authoring AI models. We
execute them as real, full-speed TensorFlow graphs, achieving cohabitation and win-win with the TF ecosystem.
Live trajectory execution tooling gives added correctness guarantees and developer productivity interactively.

FM is implemented in the FSAI.Tools package built in this repo.

The aim of FM is to support the authoring of numeric functions and AI models - including
neural networks - in F# code. For example:

```fsharp
/// A numeric function of two parameters, returning a scalar, see
/// https://en.wikipedia.org/wiki/Gradient_descent
let f (xs: DT<double>) = 
    sin (v 0.5 * sqr xs.[0] - v 0.25 * sqr xs.[1] + v 3.0) * -cos (v 2.0 * xs.[0] + v 1.0 - exp xs.[1])
```

These functions and models can then be passed to optimization algorithms that utilise gradients, e.g.

```fsharp
// Pass this Define a numeric function of two parameters, returning a scalar
let train numSteps = GradientDescent.train f (vec [ -0.3; 0.3 ]) numSteps

let results = train 200 |> Seq.last
```

FM supports the live "trajectory" checking of key correctness properties of your numeric code,
including vector, matrix and tensor size checking, and tooling to interactively report the sizes.  To active
this tooling you need to specify a `LiveCheck` that is interactively executed by the experimental tooling
described further below.

```fsharp
[<LiveCheck>] 
let check1 = train 4 |> Seq.last 
```
When using live-checks, underlying tensors are not actually populated with data - instead only their
shapes are analyzed.  Arrays and raw numerics values are computed as normal.

Typically each model is equipped with one `LiveCheck` that instantiates the model on training data.


### ARCHIVAL MATERIAL: Optimization algorithms utilising gradients

The aim of FM is to allow the clean description of numeric code and yet still allow this code to be
either executed using TensorFlow and - in the future - other tensor fabrics such as Torch (TorchSharp)
and DiffSharp.  These fabrics automatically compute the gradients of your models and functions with respect to
model parameters and/or function inputs.  Gradients are usually computed inside an optimization
algorithm.

For example, a naive version of Gradient Descent is shown below:

```fsharp
module GradientDescent =

    // Note, the rate in this example is constant. Many practical optimizers use variable
    // update (rate) - often reducing.
    let rate = 0.005

    // Gradient descent
    let step f xs =   
        // Get the partial derivatives of the function
        let df xs =  fm.diff f xs  
        printfn "xs = %A" xs
        let dzx = df xs 
        // evaluate to output values 
        xs - v rate * dzx |> fm.eval

    let train f initial steps = 
        initial |> Seq.unfold (fun pos -> Some (pos, step f pos)) |> Seq.truncate steps 
```

Note the call is `fm.diff` - FM allows optimizers to derive the gradients of FM
functions and models in a way inspired by the design of `DiffSharp`. For example:

```fsharp
// Define a function which will be executed using TensorFlow
let f x = x * x + v 4.0 * x 

// Get the derivative of the function. This computes "x*2 + 4.0"
let df x = fm.diff f x  

// Run the derivative 
df (v 3.0) |> fm.RunScalar // returns 6.0 + 4.0 = 10.0
```

To differentiate a scalar function with multiple input variables:

```fsharp
// Define a function which will be executed using TensorFlow
// computes [ x1*x1*x3 + x2*x2*x2 + x3*x3*x1 + x1*x1 ]
let f (xs: DT<'T>) = sum (xs * xs * fm.Reverse xs)

// Get the partial derivatives of the scalar function
// computes [ 2*x1*x3 + x3*x3; 3*x2*x2; 2*x3*x1 + x1*x1 ]
let df xs = fm.diff f xs   

// Run the derivative 
df (vec [ 3.0; 4.0; 5.0 ]) |> fm.RunArray // returns [ 55.0; 48.0; 39.0 ]
```

### ARCHIVAL MATERIAL: A Larger Example

Below we show fitting a linear model to training data, by differentiating a loss function w.r.t. coefficients, and optimizing
using gradient descent (200 data points generated by linear  function, 10 parameters, linear model).

```fsharp
module ModelExample =

    let modelSize = 10

    let checkSize = 5

    let trainSize = 500

    let validationSize = 100

    let rnd = Random()

    let noise eps = (rnd.NextDouble() - 0.5) * eps 

    /// The true function we use to generate the training data (also a linear model plus some noise)
    let trueCoeffs = [| for i in 1 .. modelSize -> double i |]

    let trueFunction (xs: double[]) = 
        Array.sum [| for i in 0 .. modelSize - 1 -> trueCoeffs.[i] * xs.[i]  |] + noise 0.5

    let makeData size = 
        [| for i in 1 .. size -> 
            let xs = [| for i in 0 .. modelSize - 1 -> rnd.NextDouble() |]
            xs, trueFunction xs |]
         
    /// Make the data used to symbolically check the model
    let checkData = makeData checkSize

    /// Make the training data
    let trainData = makeData trainSize

    /// Make the validation data
    let validationData = makeData validationSize
 
    let prepare data = 
        let xs, y = Array.unzip data
        let xs = batchOfVecs xs
        let y = batchOfScalars y
        (xs, y)

    /// evaluate the model for input and coefficients
    let model (xs: DT<double>, coeffs: DT<double>) = 
        fm.Sum (xs * coeffs, axis= [| 1 |])
           
    let meanSquareError (z: DT<double>) tgt = 
        let dz = z - tgt 
        fm.Sum (dz * dz) / v (double modelSize) / v (double z.Shape.[0].Value) 

    /// The loss function for the model w.r.t. a true output
    let loss (xs, y) coeffs = 
        let y2 = model (xs, batchExtend coeffs)
        meanSquareError y y2
          
    let validation coeffs = 
        let z = loss (prepare validationData) (vec coeffs)
        z |> fm.eval

    let train inputs steps =
        let initialCoeffs = vec [ for i in 0 .. modelSize - 1 -> rnd.NextDouble()  * double modelSize ]
        let inputs = prepare inputs
        GradientDescent.train (loss inputs) initialCoeffs steps
           
    [<LiveCheck>]
    let check1 = train checkData 1  |> Seq.last

    let learnedCoeffs = train trainData 200 |> Seq.last |> fm.toArray
         // [|1.017181246; 2.039034327; 2.968580146; 3.99544071; 4.935430581;
         //   5.988228378; 7.030374908; 8.013975714; 9.020138699; 9.98575733|]

    validation trueCoeffs

    validation learnedCoeffs
```

More examples/tests are in [dsl-live.fsx](https://github.com/fsprojects/FSAI.Tools/blob/master/examples/dsl/dsl-live.fsx).

The approach scales to the complete expression of deep neural networks 
and full computation graphs. The links below show the implementation of a common DNN sample (the samples may not
yet run, this is wet paint):

* [NeuralStyleTransfer in DSL form](https://github.com/fsprojects/FSAI.Tools/blob/master/examples/dsl/NeuralStyleTransfer-dsl.fsx)

The design is intended to allow alternative execution with Torch or DiffSharp.
DiffSharp may be used once Tensors are available in that library.


### ARCHIVAL MATERIAL: Technical notes:

* `DT` stands for `differentiable tensor` and the one type of `DT<_>` values are used to represent differentiable scalars, vectors, matrices and tensors.
  If you are familiar with the design of `DiffSharp` there are similarities here: DiffSharp defines `D` (differentiable scalar), `DV` (differentiable
  vector), `DM` (differentiable matrix).

* `fm.gradients` is used to get gradients of arbitrary outputs w.r.t. arbitrary inputs

* `fm.diff` is used to differentiate of `R^n -> R` scalar-valued functions (loss functions) w.r.t. multiple input variables. If 
  a scalar input is used, a single total deriative is returned. If a vector of inputs is used, a vector of
  partial derivatives are returned.

* In the prototype, all gradient-based functions are implemented using TensorFlow's `AddGradients`, i.e. the C++ implementation of
  gradients. THis has many limitations.

* `fm.*` is a DSL for expressing differentiable tensors using the TensorFlow fundamental building blocks.  The naming
  of operators in this DSL are currently TensorFLow specific and may change.

* A preliminary pass of shape inference is performed _before_ any TensorFlow operations are performed.  This
  allows you to check the shapes of your differentiable code independently of TensorFlow's shape computations.
  A shape inference system is used which allows for many shapes to be inferred and is akin to F# type inference.
  It also means not all TensorFlow automatic shape transformations are applied during shape inference.

# ARCHIVAL MATERIAL: The TensorFlow API for F# 

See `FSAI.Tools`.  This API is designed in a similar way to `TensorFlowSharp`, but is implemented directly in F# and
contains some additional functionality.

# Live Checking Tooling for AI models

ARCHIVAL MATERIAL

There is some tooling to do "live trajectory execution" of models and training on limited training sets,
reporting tensor sizes and performing tensor size checking.

LiveCheck for a vector addition:

<p align="center">
  <img src="https://user-images.githubusercontent.com/7204669/52524060-90eee980-2c90-11e9-9b0e-2752480dbe7d.gif" width="512"  title="LiveCheck example for vectors">
</p>


LiveCheck for a DNN:

<p align="center">
  <img src="https://user-images.githubusercontent.com/7204669/52758231-6c33a280-2fff-11e9-9098-2c47b60f71fe.gif" width="512"  title="LiveCheck example for vectors">
</p>


1. Clone the necessary repos

       git clone http://github.com/dotnet/fsharp
       git clone http://github.com/fsprojects/FSharp.Compiler.PortaCode
       git clone http://github.com/fsprojects/fsharp-ai-tools

2. Build the VS tooling with the extensibility "hack" to allow 3rd party tools to add checking and tooltips

       cd fsharp
       git fetch https://github.com/dsyme/fsharp livecheck
       git checkout livecheck
       .\build.cmd
       cd ..

3. Compile the extra tool
	
       dotnet build FSharp.Compiler.PortaCode

4. Compile this repo

       dotnet build fsharp-ai-tools

5. Start the tool and edit using experimental VS instance

       cd fsharp-ai-tools\examples
       devenv.exe /rootsuffix RoslynDev
       ..\..\..\FSharp.Compiler.PortaCode\FsLive.Cli\bin\Debug\net471\FsLive.Cli.exe --eval --writeinfo --watch --vshack --livechecksonly  --define:LIVECHECK dsl-live.fsx

       (open dsl-live.fsx)

# ARCHIVAL MATERIAL: fsx2nb

There is a separate tool `fsx2nb` in the repo to convert F# scripts to F# Jupyter notebooks:

    dotnet fsi tools\fsx2nb.fsx -i script\sample.fsx

These scripts use the following elements:


    (**markdown 
    
    *)

    (**cell *)   -- delimits between two code cells
    
    (**ydec xyz *)   -- this adds 'xyz' to a code cell for use fo producing visual outputs
    
    #if INTERACTIVE   -- this is removed in a code block
    ...
    #endif

    #if COMPILED   -- this is removed in a code block
    ...
    #endif
    
    
    #if NOTEBOOK   -- this is kept and the #if are removed
    ...
    #endif



# Building

    dotnet build
    dotnet test
    dotnet pack

