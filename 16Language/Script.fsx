﻿> seq {for i in 0 .. 3 -> (i, i * i)};;
//val it : seq<int * int> = seq [(0, 0); (1, 1); (2, 4); (3, 9)]

type Agent<'T> = MailboxProcessor<'T>

let counter =
    new Agent<_>(fun inbox ->
        let rec loop n =
            async {printfn "n = %d, waiting..." n
                   let! msg = inbox.Receive()
                   return! loop (n + msg)}
        loop 0)
//type Agent<'T> = MailboxProcessor<'T>
//val counter : Agent<int>

type Attempt<'T> = (unit -> 'T option)
//type Attempt<'T> = unit -> 'T option

let succeed x = (fun () -> Some(x)) : Attempt<'T>
let fail = (fun () -> None) : Attempt<'T>
let runAttempt (a : Attempt<'T>) = a()
//val succeed : x:'T -> unitVar0:unit -> 'T option
//val fail : unit -> 'T option
//val runAttempt : a:Attempt<'T> -> 'T option

type AttemptBuilder =
    member Bind : Attempt<'T> * ('T -> Attempt<'U>) -> Attempt<'U>
    member Delay : (unit -> Attempt<'T>) -> Attempt<'T>
    member Return : 'T -> Attempt<'T>
    member ReturnFrom : Attempt<'T> -> Attempt<'T>
//type AttemptBuilder =
//  class
//    new : unit -> AttemptBuilder
//    member
//      Bind : p:Attempt<'d> * rest:('d -> unit -> 'e option) ->
//               (unit -> 'e option)
//    member Combine : p1:Attempt<'T> * p2:Attempt<'T> -> (unit -> 'T option)
//    member Delay : f:(unit -> Attempt<'c>) -> (unit -> 'c option)
//    member Return : x:'b -> (unit -> 'b option)
//    member ReturnFrom : x:Attempt<'T> -> Attempt<'T>
//    member Zero : unit -> (unit -> 'a option)
//  end

let attempt = new AttemptBuilder()
//val attempt : AttemptBuilder

> let alwaysOne = attempt {return 1};;
//val alwaysOne : Attempt<int>
//val alwaysOne : (unit -> int option)

> let alwaysPair = attempt {return (1, "two")};;
//val alwaysPair : Attempt<int * string>
//val alwaysPair : (unit -> (int * string) option)

> runAttempt alwaysOne;;
//val it : int option = Some 1

> runAttempt alwaysPair;;
//val it : (int * string) option = Some (1, "two")

> let failIfBig n = attempt {if n > 1000 then return! fail else return n};;
//val failIfBig : int -> Attempt<int>
//val failIfBig : n:int -> (unit -> int option)

> runAttempt (failIfBig 999);;
//val it : int option = Some 999

> runAttempt (failIfBig 1001);;
//val it : int option = None

> let failIfEitherBig (inp1, inp2) = attempt {
        let! n1 = failIfBig inp1
        let! n2 = failIfBig inp2
        return (n1, n2)};;
//val failIfEitherBig: int * int -> Attempt<int * int>
//val failIfEitherBig : inp1:int * inp2:int -> (unit -> (int * int) option)


> runAttempt (failIfEitherBig (999, 998));;
//val it : (int * int) option = Some(999,998)

> runAttempt (failIfEitherBig (1003, 998));;
//val it : (int * int) option = None

> runAttempt (failIfEitherBig (999, 1001));;
//val it : (int * int) option = None

let sumIfBothSmall (inp1, inp2) = attempt {
    let! n1 = failIfBig inp1
    let! n2 = failIfBig inp2
    let sum = n1 + n2
    return sum}
//val sumIfBothSmall : inp1:int * inp2:int -> (unit -> int option)

let succeed x = (fun () -> Some(x))
let fail = (fun () -> None)
let runAttempt (a : Attempt<'T>) = a()
let bind p rest = match runAttempt p with None -> fail | Some r -> (rest r)
let delay f = (fun () -> runAttempt (f()))
let combine p1 p2 = (fun () -> match p1() with None -> p2() | res -> res)
//val succeed : x:'a -> unitVar0:unit -> 'a option
//val fail : unit -> 'a option
//val runAttempt : a:Attempt<'T> -> 'T option
//val bind :
//  p:Attempt<'a> -> rest:('a -> unit -> 'b option) -> (unit -> 'b option)
//val delay : f:(unit -> Attempt<'a>) -> unitVar0:unit -> 'a option
//val combine :
//  p1:(unit -> 'a option) ->
//    p2:(unit -> 'a option) -> unitVar0:unit -> 'a option

type AttemptBuilder() =

    /// Used to de-sugar uses of 'let!' inside computation expressions.
    member b.Bind(p, rest) = bind p rest

    /// Delays the construction of an attempt until just before it is executed
    member b.Delay(f) = delay f

    /// Used to de-sugar uses of 'return' inside computation expressions.
    member b.Return(x) = succeed x

    /// Used to de-sugar uses of 'return!' inside computation expressions.
    member b.ReturnFrom(x : Attempt<'T>) = x

    /// Used to de-sugar uses of 'c1; c2' inside computation expressions.
    member b.Combine(p1 : Attempt<'T>, p2 : Attempt<'T>) = combine p1 p2

    /// Used to de-sugar uses of 'if .. then ..' inside computation expressions.
    member b.Zero() = fail
//type AttemptBuilder =
//  class
//    new : unit -> AttemptBuilder
//    member
//      Bind : p:Attempt<'d> * rest:('d -> unit -> 'e option) ->
//               (unit -> 'e option)
//    member Combine : p1:Attempt<'T> * p2:Attempt<'T> -> (unit -> 'T option)
//    member Delay : f:(unit -> Attempt<'c>) -> (unit -> 'c option)
//    member Return : x:'b -> (unit -> 'b option)
//    member ReturnFrom : x:Attempt<'T> -> Attempt<'T>
//    member Zero : unit -> (unit -> 'a option)
//  end

//type AttemptBuilder =
//  class
//    new : unit -> AttemptBuilder
//    member Bind : p:Attempt<'T> * rest:('T -> Attempt<'U>) -> Attempt<'U>
//    member Combine : p1:Attempt<'T> * p2:Attempt<'T> -> Attempt<'T>
//    member Delay : f:(unit -> Attempt<'T>) -> Attempt<'T>
//    member Return : x:'T -> Attempt<'T>
//    member ReturnFrom : x:Attempt<'T> -> Attempt<'T>
//    member Zero : unit -> Attempt<'T>
//  end

let attempt = new AttemptBuilder()
//val attempt : AttemptBuilder

let f inp1 inp2 =
    attempt {
        let! n1 = failIfBig inp1
        let! n2 = failIfBig inp2
        let sum = n1 + n2
        return sum}
//val f : inp1:int -> inp2:int -> (unit -> int option)

let f inp1 inp2 =
    attempt.Bind(failIfBig inp1, (fun n1 ->
        attempt.Bind(failIfBig inp2, (fun n2 ->
            let sum = n1 + n2
            attempt.Return sum))))
//val f : inp1:int -> inp2:int -> (unit -> int option)


//val bindM : M<'T> -> ('T -> M<'U>) -> M<'U>
//val returnM : 'T -> M<'T>

let delayM f = bindM (returnM()) f

type MBuilder() =
    member b.Return(x) = returnM x
    member b.Bind(v, f) = bindM v f
    member b.Delay(f) = delayM f

let sumIfBothSmall (inp1, inp2) = attempt {
    let! n1 = failIfBig inp1
    printfn "Hey, n1 was small!"
    let! n2 = failIfBig inp2
    printfn "n2 was also small!"
    let sum = n1 + n2
    return sum}

> runAttempt(sumIfBothSmall (999, 999));;
//Hey, n1 was small!
//n2 was also small!
//val it : int option = Some 1998

> runAttempt(sumIfBothSmall (999, 1003));;
//Hey, n1 was small!
//val it : int option = None

let sumIfBothSmall (inp1, inp2) = attempt {
    let sum = ref 0
    let! n1 = failIfBig inp1
    sum := sum.Value + n1
    let! n2 = failIfBig inp2
    sum := sum.Value + n2
    return sum.Value}
//val sumIfBothSmall : inp1:int * inp2:int -> (unit -> int option)

let printThenSeven = attempt {printf "starting..."; return 3 + 4}
//val printThenSeven : (unit -> int option)

let printThenSeven = attempt.Delay(fun () -> printf "starting..."; attempt.Return(3 + 4))
//val printThenSeven : (unit -> int option)

type Attempt<'T> = (unit -> 'T option)
let succeed x = (fun () -> Some(x))
let fail = (fun () -> None)
let runAttempt (a : Attempt<'T>) = a()
let bind p rest = match runAttempt p with None -> fail | Some r -> (rest r)
let delay f = (fun () -> runAttempt (f()))
let condition p guard = (fun () -> 
    match p() with 
    | Some x when guard x -> Some x 
    | _ -> None)

type AttemptBuilder() =
    member b.Return(x) = succeed x
    member b.Bind(p, rest) = bind p rest
    member b.Delay(f) = delay f

    [<CustomOperation("condition",MaintainsVariableSpaceUsingBind = true)>]
    member x.Condition(p, [<ProjectionParameter>] b) = condition p b

let attempt = new AttemptBuilder()
//type Attempt<'T> = unit -> 'T option
//val succeed : x:'a -> unitVar0:unit -> 'a option
//val fail : unit -> 'a option
//val runAttempt : a:Attempt<'T> -> 'T option
//val bind :
//  p:Attempt<'a> -> rest:('a -> unit -> 'b option) -> (unit -> 'b option)
//val delay : f:(unit -> Attempt<'a>) -> unitVar0:unit -> 'a option
//val condition :
//  p:(unit -> 'a option) -> guard:('a -> bool) -> unitVar0:unit -> 'a option
//type AttemptBuilder =
//  class
//    new : unit -> AttemptBuilder
//    member
//      Bind : p:Attempt<'c> * rest:('c -> unit -> 'd option) ->
//               (unit -> 'd option)
//    member
//      Condition : p:(unit -> 'a option) * b:('a -> bool) ->
//                    (unit -> 'a option)
//    member Delay : f:(unit -> Attempt<'b>) -> (unit -> 'b option)
//    member Return : x:'e -> (unit -> 'e option)
//  end
//val attempt : AttemptBuilder

let random = new System.Random()
let rand() = random.NextDouble()

let randomNumberInCircle = attempt {
    let x, y = rand(), rand()
    condition (x * x + y * y < 1.0)
    return (x, y)}
//val random : System.Random
//val rand : unit -> float
//val randomNumberInCircle : (unit -> (float * float) option)
                   
type Distribution<'T when 'T : comparison> =
    abstract Sample : 'T
    abstract Support : Set<'T>
    abstract Expectation: ('T -> float) -> float

let always x =
    {new Distribution<'T> with
         member d.Sample = x
         member d.Support = Set.singleton x
         member d.Expectation(H) = H(x)}

let rnd = System.Random()

let coinFlip (p : float) (d1 : Distribution<'T>) (d2 : Distribution<'T>) =
    if p < 0.0 || p > 1.0 then failwith "invalid probability in coinFlip"
    {new Distribution<'T> with
         member d.Sample =
             if rnd.NextDouble() < p then d1.Sample else d2.Sample
         member d.Support = Set.union d1.Support d2.Support
         member d.Expectation(H) =
             p * d1.Expectation(H) + (1.0 - p) * d2.Expectation(H)}
//type Distribution<'T when 'T : comparison> =
//  interface
//    abstract member Expectation : ('T -> float) -> float
//    abstract member Sample : 'T
//    abstract member Support : Set<'T>
//  end
//val always : x:'T -> Distribution<'T> when 'T : comparison
//val rnd : System.Random
//val coinFlip :
//  p:float -> d1:Distribution<'T> -> d2:Distribution<'T> -> Distribution<'T>
//    when 'T : comparison

let bind (dist : Distribution<'T>) (k : 'T -> Distribution<'U>) =
    {new Distribution<'U> with
         member d.Sample = 
             (k dist.Sample).Sample
         member d.Support =
             Set.unionMany (dist.Support |> Set.map (fun d -> (k d).Support))
         member d.Expectation H = 
             dist.Expectation(fun x -> (k x).Expectation H)}

type DistributionBuilder() =
    member x.Delay f = bind (always ()) f
    member x.Bind(d, f) = bind d f
    member x.Return v = always v
    member x.ReturnFrom vs = vs

let dist = new DistributionBuilder()
//val bind :
//  dist:Distribution<'T> -> k:('T -> Distribution<'U>) -> Distribution<'U>
//    when 'T : comparison and 'U : comparison
//type DistributionBuilder =
//  class
//    new : unit -> DistributionBuilder
//    member
//      Bind : d:Distribution<'c> * f:('c -> Distribution<'d>) ->
//               Distribution<'d> when 'c : comparison and 'd : comparison
//    member
//      Delay : f:(unit -> Distribution<'e>) -> Distribution<'e>
//                when 'e : comparison
//    member Return : v:'b -> Distribution<'b> when 'b : comparison
//    member ReturnFrom : vs:'a -> 'a
//  end
//val dist : DistributionBuilder

let weightedCases (inp : ('T * float) list) =
    let rec coinFlips w l =
        match l with
        | [] -> failwith "no coinFlips"
        | [(d, _)] -> always d
        | (d, p) :: rest -> coinFlip (p / (1.0 - w)) (always d) (coinFlips (w + p) rest)
    coinFlips 0.0 inp

let countedCases inp =
    let total = Seq.sumBy (fun (_, v) -> v) inp
    weightedCases (inp |> List.map (fun (x, v) -> (x, float v / float total)))
//val weightedCases :
//  inp:('T * float) list -> Distribution<'T> when 'T : comparison
//val countedCases :
//  inp:('a * int) list -> Distribution<'a> when 'a : comparison

type Outcome = Even | Odd | Zero
let roulette = countedCases [Even, 18; Odd, 18; Zero, 1]
//type Outcome =
//  | Even
//  | Odd
//  | Zero
//val roulette : Distribution<Outcome>

> roulette.Sample;;
//val it : Outcome = Even

> roulette.Sample;;
//val it : Outcome = Odd

> roulette.Expectation (function Even -> 10.0 | Odd -> 0.0 | Zero -> 0.0);;
//val it : float = 4.864864865

type Light =
    | Red
    | Green
    | Yellow

let trafficLightD = weightedCases [Red, 0.50; Yellow, 0.10; Green, 0.40]
//type Light =
//  | Red
//  | Green
//  | Yellow
//val trafficLightD : Distribution<Light>

type Action = Stop | Drive

let cautiousDriver light = dist {
    match light with
    | Red -> return Stop
    | Yellow -> return! weightedCases [Stop, 0.9; Drive, 0.1]
    | Green -> return Drive}
//type Action =
//  | Stop
//  | Drive
//val cautiousDriver : light:Light -> Distribution<Action>

let aggressiveDriver light = dist {
    match light with
    | Red -> return! weightedCases [Stop, 0.9; Drive, 0.1]
    | Yellow -> return! weightedCases [Stop, 0.1; Drive, 0.9]
    | Green -> return Drive}
//val aggressiveDriver : light:Light -> Distribution<Action>

let otherLight light =
    match light with
    | Red -> Green
    | Yellow -> Red
    | Green -> Red
//val otherLight : light:Light -> Light

type CrashResult = Crash | NoCrash

// Where the suffix D means distribution
let crash (driverOneD, driverTwoD, lightD) = dist {
    // Sample from the traffic light
    let! light = lightD

    // Sample the first driver's behavior given the traffic light
    let! driverOne = driverOneD light

    // Sample the second driver's behavior given the traffic light
    let! driverTwo = driverTwoD (otherLight light)

    // Work out the probability of a crash
    match driverOne, driverTwo with
    | Drive, Drive -> return! weightedCases [Crash, 0.9; NoCrash, 0.1]
    | _ -> return NoCrash}
//type CrashResult =
//  | Crash
//  | NoCrash
//val crash :
//  driverOneD:(Light -> #Distribution<Action>) *
//  driverTwoD:(Light -> #Distribution<Action>) * lightD:Distribution<Light> ->
//    Distribution<CrashResult>

> let model = crash (cautiousDriver, aggressiveDriver, trafficLightD);;
//val model : Distribution<CrashResult>

> model.Sample;;
//val it : CrashResult = NoCrash
...
> model.Sample;;
//val it : CrashResult = Crash

> model.Expectation (function Crash -> 1.0 | NoCrash -> 0.0);;
//val it : float = 0.0369

let linesOfFile(fileName) = seq {
    use textReader = System.IO.File.OpenText(fileName)
    while not textReader.EndOfStream do
        yield textReader.ReadLine()}

let rnd = System.Random()

let rec randomWalk k = seq {
    yield k
    yield! randomWalk (k + rnd.NextDouble() - 0.5)}
//val rnd : System.Random
//val randomWalk : k:float -> seq<float>

> randomWalk 10.0;;
//val it : seq<float> = seq [10.0; 10.44456912; 10.52486359; 10.07400056; ...]

> randomWalk 10.0;;
//val it : seq<float> = seq [10.0; 10.03566833; 10.12441613; 9.922847582; ...]

> let intType = typeof<int>;;
//val intType : System.Type = System.Int32

> intType.FullName;;
//val it : string = "System.Int32"

> intType.AssemblyQualifiedName;;
//val it : string =
//  "System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"

> let intListType = typeof<int list>;;
//val intListType : System.Type =
//  Microsoft.FSharp.Collections.FSharpList`1[System.Int32]


> intListType.FullName;;
//val it : string =
//  "Microsoft.FSharp.Collections.FSharpList`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"

open System
open System.IO
open System.Globalization
open Microsoft.FSharp.Reflection

/// An attribute to be added to fields of a schema record type to indicate the
/// column used in the data format for the schema.
type ColumnAttribute(col : int) =
    inherit Attribute()
    member x.Column = col

/// SchemaReader builds an object that automatically transforms lines of text
/// files in comma-separated form into instances of the given type 'Schema.
/// 'Schema must be an F# record type where each field is attributed with a
/// ColumnAttribute attribute, indicating which column of the data the record
/// field is drawn from. This simple version of the reader understands
/// integer, string and DateTime values in the CSV format.
type SchemaReader<'Schema>() =

    // Grab the object for the type that describes the schema
    let schemaType = typeof<'Schema>

    // Grab the fields from that type
    let fields = FSharpType.GetRecordFields(schemaType) 

    // For each field find the ColumnAttribute and compute a function
    // to build a value for the field
    let schema =
        fields |> Array.mapi (fun fldIdx fld ->
            let fieldInfo = schemaType.GetProperty(fld.Name)
            let fieldConverter =
                match fld.PropertyType with
                | ty when ty = typeof<string> -> (fun (s : string) -> box s)
                | ty when ty = typeof<int> -> (System.Int32.Parse >> box)
                | ty when ty = typeof<DateTime> ->
                    (fun s -> box (DateTime.Parse(s, CultureInfo.InvariantCulture)))
                | ty -> failwithf "Unknown primitive type %A" ty

            let attrib =
                match fieldInfo.GetCustomAttributes(typeof<ColumnAttribute>, false) with
                | [|(:? ColumnAttribute as attrib)|] -> attrib
                | _ -> failwithf "No column attribute found on field %s" fld.Name
            (fldIdx, fld.Name, attrib.Column, fieldConverter))

    // Compute the permutation defined by the ColumnAttribute indexes
    let columnToFldIdxPermutation c =
        schema |> Array.pick (fun (fldIdx, _, colIdx,_) ->
            if colIdx = c then Some fldIdx else None)

    // Drop the parts of the schema we don't need
    let schema =
        schema |> Array.map (fun (_, fldName, _, fldConv) -> (fldName, fldConv))

    // Compute a function to build instances of the schema type. This uses an
    // F# library function.
    let objectBuilder = FSharpValue.PreComputeRecordConstructor(schemaType)

    // OK, now we're ready to implement a line reader
    member reader.ReadLine(textReader : TextReader) =
        let line = textReader.ReadLine()
        let words = line.Split([|','|]) |> Array.map(fun s -> s.Trim())
        if words.Length <> schema.Length then
            failwith "unexpected number of columns in line %s" line
        let words = words |> Array.permute columnToFldIdxPermutation

        let convertColumn colText (fieldName, fieldConverter) =
           try fieldConverter colText
           with e ->
               failwithf "error converting '%s' to field '%s'" colText fieldName

        let obj = objectBuilder (Array.map2 convertColumn words schema)

        // OK, now we know we've dynamically built an object of the right type
        unbox<'Schema>(obj)

    /// This reads an entire file
    member reader.ReadFile(file) = seq {
        use textReader = File.OpenText(file)
        while not textReader.EndOfStream do
            yield reader.ReadLine(textReader)}
type ColumnAttribute =
  class
    inherit System.Attribute
    new : col:int -> ColumnAttribute
    member Column : int
  end
//type SchemaReader<'Schema> =
//  class
//    new : unit -> SchemaReader<'Schema>
//    member ReadFile : file:string -> seq<'Schema>
//    member ReadLine : textReader:System.IO.TextReader -> 'Schema
//  end

Steve, 12 March 2010, Cheddar
Sally, 18 Feb 2010, Brie
...

type CheeseClub =
    {
        [<Column(0)>] Name : string
        [<Column(2)>] FavouriteCheese : string
        [<Column(1)>] LastAttendance : System.DateTime
    }
//type CheeseClub =
//  {Name: string;
//   FavouriteCheese: string;
//   LastAttendance: DateTime;}

> let reader = new SchemaReader<CheeseClub>();;
//val reader : SchemaReader<CheeseClub>

> fsi.AddPrinter(fun (c : System.DateTime) -> c.ToString());;

> System.IO.File.WriteAllLines("data.txt",
    [|"Steve, 12 March 2010, Cheddar"; "Sally, 18 Feb 2010, Brie"|]);;

> reader.ReadFile("data.txt");;
//val it : seq<CheeseClub> =
//  seq
//    [{Name = "Steve";
//      FavouriteCheese = "Cheddar";
//      LastAttendance = 12/03/2010 12:00:00 AM;};
//     {Name = "Sally";
//      FavouriteCheese = "Brie";
//      LastAttendance = 18/02/2010 12:00:00 AM;}]

open System.Reflection

let (?) (obj : obj) (nm : string) : 'T = 
    obj.GetType().InvokeMember(nm, BindingFlags.GetProperty, null, obj, [||]) 
    |> unbox<'T>

let (?<-) (obj : obj) (nm : string) (v : obj) : unit = 
    obj.GetType().InvokeMember(nm, BindingFlags.SetProperty, null, obj, [|v|]) 
    |> ignore
//val ( ? ) : obj:obj -> nm:string -> 'T
//val ( ?<- ) : obj:obj -> nm:string -> v:obj -> unit

type Record1 = {Length : int; mutable Values : int list}

let obj1 = box [1; 2; 3]
let obj2 = box {Length = 4; Values = [3; 4; 5; 7]}

let n1 : int = obj1?Length 
let n2 : int = obj2?Length
let valuesOld : int list = obj2?Values

obj2?Values <- [7; 8; 9]

let valuesNew : int list = obj2?Values
//val valuesNew : int list = [7; 8; 9]

> open Microsoft.FSharp.Quotations;;

> let oneExpr = <@ 1 @>;;
//val oneExpr : Expr<int> = Value (1)

> oneExpr;;
//val it : Expr<int> = <@ (Int32 1) @>

> let plusExpr = <@ 1 + 1 @>;;
//val it : Expr<int> =
//  Value (1)
//    {CustomAttributes = [NewTuple (Value ("DebugRange"),
//          NewTuple (Value ("C:\dev\apress\f-3.0code\16Language\Script.fsx"),
//                    Value (635), Value (17), Value (635), Value (18)))];
//     Raw = ...;
//     Type = System.Int32;}

> plusExpr;;
//val it : Expr<int> =
//  Call (None, op_Addition, [Value (1), Value (1)])
//    {CustomAttributes = [NewTuple (Value ("DebugRange"),
//          NewTuple (Value ("C:\dev\apress\f-3.0code\16Language\Script.fsx"),
//                    Value (641), Value (18), Value (641), Value (23)))];
//     Raw = ...;
//     Type = System.Int32;}

open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns

type Error = Err of float

let rec errorEstimateAux (e : Expr) (env : Map<Var, _>) =
    match e with
    | SpecificCall <@@ (+) @@> (tyargs, _, [xt; yt]) ->
        let x, Err(xerr) = errorEstimateAux xt env
        let y, Err(yerr) = errorEstimateAux yt env
        (x + y, Err(xerr + yerr))

    | SpecificCall <@@ (-) @@> (tyargs, _, [xt; yt]) ->
        let x, Err(xerr) = errorEstimateAux xt env
        let y, Err(yerr) = errorEstimateAux yt env
        (x - y, Err(xerr + yerr))

    | SpecificCall <@@ ( * ) @@> (tyargs, _, [xt; yt]) ->
        let x, Err(xerr) = errorEstimateAux xt env
        let y, Err(yerr) = errorEstimateAux yt env
        (x * y, Err(xerr * abs(y) + yerr * abs(x) + xerr * yerr))

    | SpecificCall <@@ abs @@> (tyargs, _, [xt]) ->
        let x, Err(xerr) = errorEstimateAux xt env
        (abs(x), Err(xerr))

    | Let(var, vet, bodyt) ->
        let varv, verr = errorEstimateAux vet env
        errorEstimateAux bodyt (env.Add(var, (varv, verr)))

    | Call(None, MethodWithReflectedDefinition(Lambda(v, body)), [arg]) ->
        errorEstimateAux (Expr.Let(v, arg, body)) env

    | Var(x) -> env.[x]

    | Double(n) -> (n, Err(0.0))

    | _ -> failwithf "unrecognized term: %A" e

let rec errorEstimateRaw (t : Expr) =
    match t with
    | Lambda(x, t) ->
        (fun xv -> errorEstimateAux t (Map.ofSeq [(x, xv)]))
    | PropertyGet(None, PropertyGetterWithReflectedDefinition(body), []) ->
        errorEstimateRaw body
    | _ -> failwithf "unrecognized term: %A - expected a lambda" t

let errorEstimate (t : Expr<float -> float>) = errorEstimateRaw t
//type Error = | Err of float
//val errorEstimateAux : e:Expr -> env:Map<Var,(float * Error)> -> float * Error
//val errorEstimateRaw : t:Expr -> (float * Error -> float * Error)
//val errorEstimate :
//  t:Expr<(float -> float)> -> (float * Error -> float * Error)

> let err x = Err x;;
//val err : float -> Error

> fsi.AddPrinter (fun (x : float, Err v) -> sprintf "%g±%g" x v);;
//val it : unit = ()

> errorEstimate <@ fun x -> x + 2.0 * x + 3.0 * x * x @> (1.0, err 0.1);;
//val it : float * Error = 6±0.93

> errorEstimate <@ fun x -> let y = x + x in y * y + 2.0 @> (1.0, err 0.1);;
//val it : float * Error = 6±0.84

[<ReflectedDefinition>]
let poly x = x + 2.0 * x + 3.0 / (x * x)
//val poly : x:float -> float

> errorEstimate <@ poly @> (3.0, err 0.1);;
//System.Exception: unrecognized term: Call (None, op_Division, [Value (3.0), Call (None, op_Multiply, [x, x])])
//   at FSI_0145.errorEstimateAux@697.Invoke(String message) in C:\dev\apress\f-3.0code\16Language\Script.fsx:line 697
//   at Microsoft.FSharp.Core.PrintfImpl.go@523-3[b,c,d](String fmt, Int32 len, FSharpFunc`2 outputChar, FSharpFunc`2 outa, b os, FSharpFunc`2 finalize, FSharpList`1 args, Int32 i)
//   at Microsoft.FSharp.Core.PrintfImpl.run@521[b,c,d](FSharpFunc`2 initialize, String fmt, Int32 len, FSharpList`1 args)
//   at Microsoft.FSharp.Core.PrintfImpl.capture@540[b,c,d](FSharpFunc`2 initialize, String fmt, Int32 len, FSharpList`1 args, Type ty, Int32 i)
//   at <StartupCode$FSharp-Core>.$Reflect.Invoke@720-4.Invoke(T1 inp)
//   at FSI_0145.errorEstimateAux(FSharpExpr e, FSharpMap`2 env) in C:\dev\apress\f-3.0code\16Language\Script.fsx:line 669
//   at <StartupCode$FSI_0157>.$FSI_0157.main@()
//Stopped due to error

> errorEstimate <@ poly @> (30271.3, err 0.0001);;
//System.Exception: unrecognized term: Call (None, op_Division, [Value (3.0), Call (None, op_Multiply, [x, x])])
//   at FSI_0145.errorEstimateAux@697.Invoke(String message) in C:\dev\apress\f-3.0code\16Language\Script.fsx:line 697
//   at Microsoft.FSharp.Core.PrintfImpl.go@523-3[b,c,d](String fmt, Int32 len, FSharpFunc`2 outputChar, FSharpFunc`2 outa, b os, FSharpFunc`2 finalize, FSharpList`1 args, Int32 i)
//   at Microsoft.FSharp.Core.PrintfImpl.run@521[b,c,d](FSharpFunc`2 initialize, String fmt, Int32 len, FSharpList`1 args)
//   at Microsoft.FSharp.Core.PrintfImpl.capture@540[b,c,d](FSharpFunc`2 initialize, String fmt, Int32 len, FSharpList`1 args, Type ty, Int32 i)
//   at <StartupCode$FSharp-Core>.$Reflect.Invoke@720-4.Invoke(T1 inp)
//   at FSI_0145.errorEstimateAux(FSharpExpr e, FSharpMap`2 env) in C:\dev\apress\f-3.0code\16Language\Script.fsx:line 669
//   at <StartupCode$FSI_0158>.$FSI_0158.main@()
//Stopped due to error

