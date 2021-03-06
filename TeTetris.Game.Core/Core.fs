module TeTetris.Game.Core

open TeTetris.Utils
open TeTetris.Game.Types

// Predefined

let WorldWidth = 10
let WorldHeight = 22

let startPoint = { x=WorldWidth-5; y=WorldHeight }

let initTetramino = function
    | Cube ->
        { shape=Cube;
          block={color="green"}
          coords=
          {
            a=startPoint;
            b={ startPoint with x = startPoint.x+1 };
            c={ startPoint with y = startPoint.y+1 };
            d={ startPoint with y = startPoint.y+1; x = startPoint.x + 1 };
          }
        }
        
    | Palka ->
        { shape=Palka;
          block={color="green"};
          coords=
          {
            a=startPoint;
            b={ startPoint with y = startPoint.y+1 };
            c={ startPoint with y = startPoint.y+2 };
            d={ startPoint with y = startPoint.y+3 };               
          }       
        }
    | L ->
        { shape=L;
          block={color="yellow"};
          coords=
          {
            a=startPoint;
            b={ startPoint with x = startPoint.x+1 };
            c={ startPoint with y = startPoint.y+1 };
            d={ startPoint with y = startPoint.y+2 };       
          }
        }
    | J ->
        { shape=L;
          block={color="yellow"};
          coords=
          {
            a=startPoint;
            b={ startPoint with x = startPoint.x-1 };
            c={ startPoint with y = startPoint.y+1 };
            d={ startPoint with y = startPoint.y+2 };       
          }
        }
    | S ->
        { shape=L;
          block={color="yellow"};
          coords=
          {
            a=startPoint;
            b={ startPoint with x = startPoint.x-1 };
            c={ startPoint with y = startPoint.y+1 };
            d={ startPoint with y = startPoint.y+1; x = startPoint.x + 1 };   
          }
        }
    | Z ->
        { shape=L;
          block={color="yellow"};
          coords=
          {
            a=startPoint;
            b={ startPoint with x = startPoint.x+1 };
            c={ startPoint with y = startPoint.y+1 };
            d={ startPoint with y = startPoint.y+1; x = startPoint.x - 1 };   
          }
        }
    | R ->
        { shape=L;
          block={color="yellow"};
          coords=
          {
            a=startPoint;
            b={ startPoint with y = startPoint.y+1; x = startPoint.x - 1 };   
            c={ startPoint with y = startPoint.y+1 };
            d={ startPoint with y = startPoint.y+1; x = startPoint.x + 1 };   
          }
        }

let emptyGrid = [for i in 0 .. WorldHeight + 4 do
                   yield  i, [for j in 0 .. WorldWidth - 1 do yield j, None] |> Map.ofList 
                ] |> Map.ofList

let emptyState (x::xs) = { tetraminoQueue= Seq.repeat xs; activeTetramino=initTetramino x; blocks=emptyGrid }

// Game logic

let moveTetramino xo yo (t: TetraminoCoords) = 
    let movePoint p = {p with x = p.x + xo; y = p.y + yo}
    {t with 
        a = movePoint t.a
        b = movePoint t.b
        c = movePoint t.c
        d = movePoint t.d
    }

let isLandConflict (landed: Map<int, Map<int, Block option>>) (t: TetraminoCoords) = 
    let isPointConflict p = p.x < 0 || p.x >= WorldWidth || p.y < 0 || landed.[p.y].[p.x] |> Option.isSome
    isPointConflict t.a || isPointConflict t.b || isPointConflict t.c || isPointConflict t.d

let runClearing (bl: Map<int, Map<int, Block option>>) =
    let isLineFull _ el = el |> Map.forall (fun _ -> Option.isSome) 
    let fullLines = bl |> Map.filter isLineFull |> Seq.map (fun kvp -> kvp.Key)
    printfn "%A" bl.[0]
    let clearLine (m: Map<int, Map<int, Block option>>, offset) i = 
        let shift m i = m |> Map.add i m.[i+1]
        [ i - offset .. WorldHeight] |> List.fold shift m, offset + 1
    
    fullLines |> Seq.fold clearLine (bl, 0) |> fst

let landTetramino state = 
     let addPoint p (blocks: Map<int, Map<int, Block option>>) = blocks.Add(p.y, (blocks.[p.y].Add( p.x, { color="black" } |> Some)))     
     let t = state.activeTetramino.coords
     let landedBlocks = state.blocks |> addPoint t.a |> addPoint t.b |> addPoint t.c |> addPoint t.d |> runClearing
     let h,hs = deattachHead state.tetraminoQueue

     { state with 
         tetraminoQueue = hs
         activeTetramino = h |> initTetramino
         blocks = landedBlocks
     }

let gameTick (state: State)=
    let potentialTetraminoPos = moveTetramino 0 (-1) state.activeTetramino.coords
    if isLandConflict state.blocks potentialTetraminoPos
        then landTetramino state
        else {state with activeTetramino = {state.activeTetramino with coords = potentialTetraminoPos }}

let move x y state = 
    let potentialPos = moveTetramino x y state.activeTetramino.coords
    if isLandConflict state.blocks potentialPos
        then state
        else {state with activeTetramino = {state.activeTetramino with coords = potentialPos }}

open TeTetris.Game.Rotation

let potentialShifts coords =
    let move' xo yo = 
        seq {yield moveTetramino xo yo coords; yield moveTetramino (-xo) (-yo) coords}    
    seq { for shift in 0 .. WorldHeight do
            yield! move' shift 0    
            yield! move' shift shift    
            yield! move' 0 shift    
            yield! move' -shift shift    
        }


let resolveConflicts (t: TetraminoCoords) (landed: Map<int, Map<int, Block option>>) =     
    Seq.find (isLandConflict landed >> not) (potentialShifts t)
let rotate state = 
    let defaultRotation state =

        let potentialPos = tryRotate state.activeTetramino.coords
        let newCoords = resolveConflicts potentialPos state.blocks
        {state with activeTetramino = {state.activeTetramino with coords = newCoords }}

    match state.activeTetramino.shape with
        | Cube -> state
        | _ -> defaultRotation state
    
let commandHandler command state =    
    match command with
        | Tick -> gameTick state
        | MoveLeft  -> move (-1) 0 state
        | MoveRight -> move (+1) 0 state
        | ShiftDown -> move 0 (-1) state        
        | Rotate -> rotate state
        | _ -> state

