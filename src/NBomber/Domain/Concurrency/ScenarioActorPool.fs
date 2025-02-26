module internal NBomber.Domain.Concurrency.ScenarioActorPool

open NBomber.Domain
open NBomber.Domain.Concurrency.ScenarioActor
open NBomber.Domain.ScenarioContext

[<Struct>]
type ActorPoolResult = {
    ActorsFromPool: ScenarioActor[]
    NewActors: ScenarioActor[]
}

let createActors (scnCtx: ScenarioContextArgs) count fromIndex =
    let scenario = scnCtx.Scenario
    Array.init count (fun i ->
        let actorIndex = fromIndex + i
        let scenarioInfo = Scenario.createScenarioInfo(scenario.ScenarioName, scenario.PlanedDuration, actorIndex, scnCtx.ScenarioOperation)
        ScenarioActor(scnCtx, scenarioInfo)
    )

// todo: add tests
let inline rentActors
    (createActors: int -> int -> ScenarioActor[]) // count -> fromIndex
    (actorPool: ResizeArray<ScenarioActor>)
    (actorCount: int) =

    let notWorkingActors =
        actorPool
        |> Seq.filter(fun x -> not x.Working)
        |> Seq.truncate actorCount
        |> Seq.toArray

    let notWorkingCount = Array.length notWorkingActors

    if notWorkingCount < actorCount then
        let createCount = actorCount - notWorkingCount
        let fromIndex = actorPool.Count
        let newActors = createActors createCount fromIndex
        { ActorsFromPool = notWorkingActors; NewActors = newActors }
    else
        { ActorsFromPool = notWorkingActors; NewActors = Array.empty }

let inline askToStop (actorPool: ScenarioActor seq) =
    actorPool |> Seq.iter(fun x -> x.AskToStop())

let inline getWorkingActors (actorPool: ScenarioActor seq) =
    actorPool |> Seq.filter(fun x -> x.Working)

let inline updatePool (currentPool: ResizeArray<ScenarioActor>) (newActors: ScenarioActor[]) =
    currentPool.AddRange newActors
    currentPool

module Test =

    let getWorkingActors actorPool = getWorkingActors actorPool
