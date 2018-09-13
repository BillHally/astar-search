module AStar

type Config<'a> = 
    {
        /// <summary>
        /// A method that, given a source, will return its neighbours
        /// </summary>
        neighbours: 'a -> seq<'a>
        /// <summary>
        /// Given two nodes that are next to each other, return the g cost between them
        /// The g cost is the cost of moving from one to the other directly
        /// </summary>
        gCost: 'a -> 'a -> float
        /// <summary>
        /// Given two nodes, return the f cost between them. This is a heuristic, and is used from a given node to the goal
        /// Line of site distance is an example of how this might be expressed
        /// </summary>
        fCost: 'a -> 'a -> float
    }

let search<'a> start goal config =

    let rec reconstructPath cameFrom current =
        seq {
            yield current
            match Map.tryFind current cameFrom with
            | None -> ()
            | Some next -> yield! reconstructPath cameFrom next
        }

    let rec crawler closedSet (openSet, gScores, fScores, cameFrom) =
        match List.sortBy (fun n -> Map.find n fScores) openSet with
        | current::_ when current = goal -> Some <| reconstructPath cameFrom current 
        | current::rest ->
            let gScore = Map.find current gScores
            config.neighbours current 
            |> Seq.filter (fun n -> closedSet |> Set.contains n |> not)
            |> Seq.fold (fun (openSet, gScores, fScores, cameFrom) neighbour ->
                let tentativeGScore = gScore + config.gCost current neighbour
                if List.contains neighbour openSet && tentativeGScore >= Map.find neighbour gScores 
                then (openSet, gScores, fScores, cameFrom)
                else
                    let newOpenSet = if List.contains neighbour openSet then openSet else neighbour::openSet
                    let newGScores = Map.add neighbour tentativeGScore gScores
                    let newFScores = Map.add neighbour (tentativeGScore + config.fCost neighbour goal) fScores
                    let newCameFrom = Map.add neighbour current cameFrom
                    newOpenSet, newGScores, newFScores, newCameFrom
                ) (rest, gScores, fScores, cameFrom)
            |> crawler (Set.add current closedSet)
        | _ -> None

    let gScores = Map.ofList [start, 0.]
    let fScores = Map.ofList [start, config.fCost start goal]
    crawler Set.empty ([start], gScores, fScores, Map.empty)