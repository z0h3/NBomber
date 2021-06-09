module internal NBomber.DomainServices.NBomberRunner

open FsToolkit.ErrorHandling

open NBomber
open NBomber.Contracts
open NBomber.Contracts.Stats
open NBomber.Domain.Stats
open NBomber.Errors
open NBomber.Infra
open NBomber.Infra.Dependency
open NBomber.DomainServices.Reports
open NBomber.DomainServices.TestHost

let runSession (testInfo: TestInfo) (nodeInfo: NodeInfo) (context: NBomberContext) (dep: IGlobalDependency) =
    taskResult {
        Constants.Logo |> Console.addLogo |> Console.render

        dep.Logger.Information(Constants.NBomberWelcomeText, nodeInfo.NBomberVersion, testInfo.SessionId)
        dep.Logger.Information("NBomber started as single node.")

        let! sessionArgs  = context |> NBomberContext.createSessionArgs testInfo
        let! scenarios    = context |> NBomberContext.createScenarios
        use testHost      = new TestHost(dep, scenarios, sessionArgs, ScenarioStatsActor.create)
        let! result       = testHost.RunSession()

        let finalStats =
            Report.build dep.Logger result testHost.TargetScenarios
            |> Report.save dep context result.FinalStats

        return { result with FinalStats = finalStats }
    }

let run (context: NBomberContext) =

    let testInfo = {
        SessionId = Dependency.createSessionId()
        TestSuite = NBomberContext.getTestSuite(context)
        TestName = NBomberContext.getTestName(context)
    }

    let nodeInfo = NodeInfo.init()
    let appType = context.ApplicationType |> Option.defaultValue(NodeInfo.getApplicationType())
    let reportFolder = NBomberContext.getReportFolder(context)

    Dependency.create reportFolder testInfo appType NodeType.SingleNode context
    |> runSession testInfo nodeInfo context
    |> TaskResult.mapError(fun error ->
        error |> AppError.toString |> Serilog.Log.Error
        error
    )
    |> fun task -> task.Result
