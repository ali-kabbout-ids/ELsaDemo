

using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using PurchaseOrderApi.Activities;
using PurchaseOrderApi.Activities.ApplicationActivities;
using PurchaseOrderApi.Helpers;
using Endpoint = Elsa.Workflows.Activities.Flowchart.Models.Endpoint;
using static PurchaseOrderApi.Helpers.DictionaryHelper;
using Elsa.Http;
using Elsa.Workflows.Runtime.Activities;
using PurchaseOrderApi.Enums;
using PurchaseOrderApi.Dtos;
using Elsa.Expressions.Models;
using Elsa.Expressions.JavaScript.Models;

namespace PurchaseOrderApi.Workflows;

public class ApplicationRequestWorkFlow : WorkflowBase
{
    public static string DefinitionId => nameof(ApplicationRequestWorkFlow);

    private readonly string ApiBaseUrl;

    public ApplicationRequestWorkFlow(IConfiguration configuration)
    {
        ApiBaseUrl = configuration["Elsa:Http:BaseUrl"] ?? "https://localhost:44306";
    }

    protected override void Build(IWorkflowBuilder builder)
    {
        Variable<int> appIdVar = builder.WithVariable<int>("ApplicationId", 0).WithWorkflowStorage();
        Variable<string> hasMane3DecisionVar = builder.WithVariable<string>("HasMane3Decision", "").WithWorkflowStorage();
        Variable<string> hasMane3ReasonVar = builder.WithVariable<string>("HasMane3Reason", "").WithWorkflowStorage();
        Variable<object?> hasMane3ResponseVar = builder.WithVariable<object?>("HasMane3Response").WithWorkflowStorage();

        Variable<ApprovalResult?> i3lamResultVar = builder
            .WithVariable<ApprovalResult?>("I3almResult", null)
            .WithWorkflowStorage();

        Variable<ApprovalResult?> mo3awenResultVar = builder
            .WithVariable<ApprovalResult?>("Mo3awenResult", null)
            .WithWorkflowStorage();

        Variable<ApprovalResult?> finalMo3awenResultVar = builder
            .WithVariable<ApprovalResult?>("FinalMo3awenResult", null)
            .WithWorkflowStorage();

        Variable<bool> mo5atabatNeededVar = builder
            .WithVariable<bool>("Mo5atabatNeeded", false)
            .WithWorkflowStorage();

        // ── STEP 0 ───────────────────────────────────────────────────────────
        SetVariable<int> setAppId = new SetVariable<int>
        {
            Id = "SetAppId",
            Name = "Set Application ID",
            Variable = appIdVar,
            Value = new Input<int>(ctx =>
                TryGetInt(ctx.GetWorkflowExecutionContext().Input, "applicationId"))
        }.WithLayout(x: -70, y: 100, w: 218, h: 68, displayText: "Set App ID");

        SetVariable<bool> setMo5atabatNeeded = new SetVariable<bool>
        {
            Id = "SetMo5atabatNeeded",
            Name = "Set Mokhatabat Flag",
            Variable = mo5atabatNeededVar,
            Value = new Input<bool>(ctx =>
                TryGetBool(ctx.GetWorkflowExecutionContext().Input, "requiresMo5atabat"))
        }.WithLayout(x: 220, y: 100, w: 236, h: 68, displayText: "Set Mokhatabat Flag");

        // ── STEP 1 ───────────────────────────────────────────────────────────
        WaitForApplicationApprovalActivity waitI3lam = new WaitForApplicationApprovalActivity
        {
            Id = "WaitI3lamKanouni",
            ApplicationId = new Input<int>(appIdVar),
            RequiredRole = new Input<ApprovalRole>(ApprovalRole.I3lamKanouni),
            StepName = new Input<string>("I3lam Kanouni Review"),
            Result = new Output<ApprovalResult?>(i3lamResultVar)
        }.WithLayout(x: 530, y: 100, w: 310, h: 68, displayText: "I3alm Kanouni Review");

        // ── STEP 2 ───────────────────────────────────────────────────────────
        WaitForApplicationApprovalActivity waitMo3awen = new WaitForApplicationApprovalActivity
        {
            Id = "WaitMo3awenCho3ba",
            ApplicationId = new Input<int>(appIdVar),
            RequiredRole = new Input<ApprovalRole>(ApprovalRole.Mo3awenCho3ba),
            StepName = new Input<string>("Mo3awen Cho3ba Review"),

            Result = new Output<ApprovalResult?>(mo3awenResultVar)
        }.WithLayout(x: 910, y: 100, w: 342, h: 68, displayText: "Mo3awen Cho3ba Review");

        // ── STEP 3 ── Both approved? ──────────────────────────────────────────
        FlowDecision conditionBothApproved = new FlowDecision
        {
            Id = "ConditionBothApproved",
            Name = "Both Approved?",
            Condition = new Input<bool>(JavaScriptExpression.Create(
                "variables.I3almResult?.Action == 'approve' && variables.Mo3awenResult?.Action == 'approveWithMo5atabat'"))
        }.WithLayout(x: 690, y: 220, w: 201, h: 68, displayText: "Both Approved?");

        IncrementReviewRoundActivity incrementRound = new IncrementReviewRoundActivity
        {
            Id = "IncrementRound",
            Name = "Increment Review Round"
        }.WithLayout(x: 240, y: 220, w: 321, h: 68, displayText: "Increment Review Round");

        // ── STEP 4 ── Mokhatabat needed? ───────────────────────────────────────
        FlowDecision checkMo5atabat = new FlowDecision
        {
            Id = "CheckMo5atabat",
            Name = "Requires Mokhatabat?",
            Condition = new Input<bool>(JavaScriptExpression.Create(
                "variables.Mo3awenResult?.Extra?.['requiresMo5atabat'] === 'true'"))
        }.WithLayout(x: 1040, y: 320, w: 248, h: 68, displayText: "Requires Mokhatabat?");

        // ── STEP 4b ──────────────────────────────────────────────────────────
        DispatchWorkflow waitMo5atabat = new DispatchWorkflow
        {
            Id = "WaitMo5atabat",
            Name = "SubWorkflow Mokhatabat",
            WorkflowDefinitionId = new Input<string>(_ => MokhatabatWorkflow.DefinitionId),
            WaitForCompletion = new Input<bool>(_ => true),
            CorrelationId = new Input<string?>(ctx => $"mo5-{appIdVar.Get(ctx)}"),
            Input = new Input<IDictionary<string, object>?>(ctx =>
                new Dictionary<string, object> { ["applicationId"] = appIdVar.Get(ctx) })
        }.WithLayout(x: 1040, y: 531, w: 310, h: 68, displayText: "Run Mokhatabat Sub-Workflow");

        // ── STEP 5 ───────────────────────────────────────────────────────────
        WaitForApplicationApprovalActivity waitFinalMo3awen = new WaitForApplicationApprovalActivity
        {
            Id = "WaitFinalMo3awen",
            ApplicationId = new Input<int>(appIdVar),
            RequiredRole = new Input<ApprovalRole>(ApprovalRole.Mo3awenCho3ba),
            StepName = new Input<string>("Final Mo3awen Sign-off"),

            Result = new Output<ApprovalResult?>(finalMo3awenResultVar)
        }.WithLayout(x: 1400, y: 531, w: 316, h: 68, displayText: "Final Mo3awen Sign-off");

        // ── STEP 6 — mark app as pending before the HTTP call ────────────────
        SetHasMane3PendingActivity setHasMane3Pending = new SetHasMane3PendingActivity
        {
            Id = "SetHasMane3Pending",
            Name = "Set Pending HasMane3",
            ApplicationId = new Input<int>(appIdVar)
        }.WithLayout(x: 1880, y: 531, w: 295, h: 68, displayText: "Set Pending Has Mane3");

        SendHttpRequest hasMane3HttpCall = new SendHttpRequest
        {
            Id = "HasMane3HttpCall",
            Name = "Call Has Mane3 Check",
            Url = new Input<Uri?>(ctx =>
                new Uri($"{ApiBaseUrl}/api/applications/{appIdVar.Get(ctx)}/has-mane3")),
            Method = new Input<string>(_ => HttpMethods.Get),
            ParsedContent = new Output<object?>(hasMane3ResponseVar)
        }.WithLayout(x: 1400, y: 700, w: 384, h: 200, displayText: "HTTP GET: Has Mane3 Check");

        // ── STEP 6c ──────────────────────────────────────────────────────────
        SetVariable<string> setHasMane3Decision = new SetVariable<string>
        {
            Id = "SetHasMane3Decision",
            Name = "Extract Decision",
            Variable = hasMane3DecisionVar,
            Value = new Input<string>(ctx =>
            {
                dynamic body = hasMane3ResponseVar.Get(ctx)!;
                return (string?)body.decision ?? "no_obstacle";
            })
        }.WithLayout(x: 1999, y: 772, w: 260, h: 68, displayText: "Extract Decision");

        // ── STEP 6d ──────────────────────────────────────────────────────────
        SetVariable<string> setHasMane3Reason = new SetVariable<string>
        {
            Id = "SetHasMane3Reason",
            Name = "Extract Reason",
            Variable = hasMane3ReasonVar,
            Value = new Input<string>(ctx =>
            {
                dynamic body = hasMane3ResponseVar.Get(ctx)!;
                return (string?)body.reason ?? string.Empty;
            })
        }.WithLayout(x: 2360, y: 772, w: 260, h: 68, displayText: "Extract Reason");

        // ── STEP 6e ──────────────────────────────────────────────────────────
        SaveHasMane3DecisionActivity saveHasMane3 = new SaveHasMane3DecisionActivity
        {
            Id = "SaveHasMane3",
            Name = "Save HasMane3 to DB",
            ApplicationId = new Input<int>(appIdVar),
            Decision = new Input<string>(hasMane3DecisionVar),
            Reason = new Input<string>(hasMane3ReasonVar)
        }.WithLayout(x: 1400, y: 1020, w: 308, h: 68, displayText: "Save Has Mane3 to DB");

        // ── STEP 7 ───────────────────────────────────────────────────────────
        FlowDecision hasMane3Check = new FlowDecision(ctx =>
            hasMane3DecisionVar.Get(ctx) == "has_obstacle")
        {
            Id = "HasMane3Check",
            Name = "Has Legal Obstacle?"
        }.WithLayout(x: 1438, y: 1160, w: 240, h: 68, displayText: "Has Legal Obstacle?");

        // ── STEP 8a ──────────────────────────────────────────────────────────
        FinalizeApplicationActivity finalizeApproved = new FinalizeApplicationActivity
        {
            Id = "FinalizeApproved",
            ApplicationId = new Input<int>(appIdVar),
            IsApproved = new Input<bool>(_ => true)
        }.WithLayout(x: 1784, y: 1320, w: 283, h: 68, displayText: "Finalize Approved ✅");

        // ── STEP 8b ──────────────────────────────────────────────────────────
        FinalizeApplicationActivity finalizeRejected = new FinalizeApplicationActivity
        {
            Id = "FinalizeRejected",
            ApplicationId = new Input<int>(appIdVar),
            IsApproved = new Input<bool>(_ => false),
            RejectionReason = new Input<string>(hasMane3ReasonVar)
        }.WithLayout(x: 1005, y: 1320, w: 283, h: 68, displayText: "Finalize Rejected ❌");

        // ── FLOWCHART ─────────────────────────────────────────────────────────
        builder.Root = new Flowchart
        {
            Activities =
            {
                setAppId, setMo5atabatNeeded,
                waitI3lam, waitMo3awen,
                conditionBothApproved, incrementRound,
                checkMo5atabat, waitMo5atabat,
                waitFinalMo3awen,
                setHasMane3Pending,
                hasMane3HttpCall,
                setHasMane3Decision,
                setHasMane3Reason,
                saveHasMane3,
                hasMane3Check,
                finalizeApproved, finalizeRejected
            },
            Connections =
            {
                new Connection(new Endpoint(setAppId,           "Done"), new Endpoint(setMo5atabatNeeded)),
                new Connection(new Endpoint(setMo5atabatNeeded, "Done"), new Endpoint(waitI3lam)),

                new Connection(new Endpoint(waitI3lam,   "Done"), new Endpoint(waitMo3awen)),
                new Connection(new Endpoint(waitMo3awen, "Done"), new Endpoint(conditionBothApproved)),

                new Connection(new Endpoint(conditionBothApproved, "False"), new Endpoint(incrementRound)),
                new Connection(new Endpoint(incrementRound,        "Done"),  new Endpoint(waitI3lam)),
                new Connection(new Endpoint(conditionBothApproved, "True"),  new Endpoint(checkMo5atabat)),

                new Connection(new Endpoint(checkMo5atabat, "True"),  new Endpoint(waitMo5atabat)),
                new Connection(new Endpoint(waitMo5atabat,  "Done"),  new Endpoint(waitFinalMo3awen)),
                new Connection(new Endpoint(checkMo5atabat, "False"), new Endpoint(waitFinalMo3awen)),

                new Connection(new Endpoint(waitFinalMo3awen,    "Done"), new Endpoint(setHasMane3Pending)),
                new Connection(new Endpoint(setHasMane3Pending,  "Done"), new Endpoint(hasMane3HttpCall)),
                new Connection(new Endpoint(hasMane3HttpCall,    "Done"), new Endpoint(setHasMane3Decision)),
                new Connection(new Endpoint(setHasMane3Decision, "Done"), new Endpoint(setHasMane3Reason)),
                new Connection(new Endpoint(setHasMane3Reason,   "Done"), new Endpoint(saveHasMane3)),
                new Connection(new Endpoint(saveHasMane3,        "Done"), new Endpoint(hasMane3Check)),

                new Connection(new Endpoint(hasMane3Check, "True"),  new Endpoint(finalizeRejected)),
                new Connection(new Endpoint(hasMane3Check, "False"), new Endpoint(finalizeApproved))
            }
        };
    }
}