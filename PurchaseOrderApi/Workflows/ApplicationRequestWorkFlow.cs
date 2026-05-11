

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

namespace PurchaseOrderApi.Workflows;

public class ApplicationRequestWorkFlow : WorkflowBase
{
    public static string DefinitionId => nameof(ApplicationRequestWorkFlow);

    protected override void Build(IWorkflowBuilder builder)
    {
        Variable<int> appIdVar = builder.WithVariable<int>("ApplicationId", 0).WithWorkflowStorage();
        Variable<bool> mo5atabatNeededVar = builder.WithVariable<bool>("Mo5atabatNeeded", false).WithWorkflowStorage();
        Variable<string> i3almDecisionVar = builder.WithVariable<string>("I3almDecision", "").WithWorkflowStorage();
        Variable<string> i3almReasonVar = builder.WithVariable<string>("I3almReason", "").WithWorkflowStorage();
        Variable<string> mo3awenDecisionVar = builder.WithVariable<string>("Mo3awenDecision", "").WithWorkflowStorage();
        Variable<string> mo3awenReasonVar = builder.WithVariable<string>("Mo3awenReason", "").WithWorkflowStorage();
        Variable<string> hasMane3DecisionVar = builder.WithVariable<string>("HasMane3Decision", "").WithWorkflowStorage();
        Variable<string> hasMane3ReasonVar = builder.WithVariable<string>("HasMane3Reason", "").WithWorkflowStorage();

        // ── STEP 0 ───────────────────────────────────────────────────────────
        SetVariable<int> setAppId = new SetVariable<int>
        {
            Id = "SetAppId",
            Variable = appIdVar,
            Value = new Input<int>(ctx =>
                TryGetInt(ctx.GetWorkflowExecutionContext().Input, "applicationId"))
        }.WithLayout(
            x: -70,
            y: 100,
            w: 218.76953125,
            h: 68.4375);

        SetVariable<bool> setMo5atabatNeeded = new SetVariable<bool>
        {
            Id = "SetMo5atabatNeeded",
            Variable = mo5atabatNeededVar,
            Value = new Input<bool>(ctx =>
                TryGetBool(ctx.GetWorkflowExecutionContext().Input, "requiresMo5atabat"))
        }.WithLayout(
            x: 220,
            y: 100,
            w: 236.5234375,
            h: 68.4375);

        // ── STEP 1 ── top row ─────────────────────────────────────────────────
        WaitForI3almKanouniActivity waitI3alm = new WaitForI3almKanouniActivity
        {
            Id = "WaitI3almKanouni",
            ApplicationId = new Input<int>(appIdVar),
            Decision = new Output<string>(i3almDecisionVar),
            Reason = new Output<string>(i3almReasonVar)
        }.WithLayout(
                x: 530,
                y: 100,
                w: 310.25390625,
                h: 68.4375);

        // ── STEP 2 ── top row ─────────────────────────────────────────────────
        WaitForMo3awenCho3baActivity waitMo3awen = new WaitForMo3awenCho3baActivity
        {
            Id = "WaitMo3awenCho3ba",
            ApplicationId = new Input<int>(appIdVar),
            Decision = new Output<string>(mo3awenDecisionVar),
            Reason = new Output<string>(mo3awenReasonVar)
        }.WithLayout(
            x: 910,
            y: 100,
            w: 342.0703125,
            h: 68.4375);

        // ── STEP 3 ── drops below Mo3awen ─────────────────────────────────────
        FlowDecision conditionBothApproved = new FlowDecision(ctx =>
            i3almDecisionVar.Get(ctx) == "approved" &&
            mo3awenDecisionVar.Get(ctx) == "approved")
        {
            Id = "ConditionBothApproved",
            Name = "Both Approved?"
        }.WithLayout(
            x: 690,
            y: 220,
            w: 201.3671875,
            h: 68.4375,
            displayText: "Both Approved?");

        // False branch goes LEFT to IncrementRound, then loops back up to I3alm
        IncrementReviewRoundActivity incrementRound = new IncrementReviewRoundActivity
        {
            Id = "IncrementRound"
        }.WithLayout(
            x: 240,
            y: 220,
            w: 321.015625,
            h: 68.4375);

        // ── STEP 4 ── True branch goes right + down from Condition ────────────
        FlowDecision checkMo5atabat = new FlowDecision
        {
            Id = "CheckMo5atabat",
            Name = "Requires Mo5atabat?",
            Condition = new Input<bool>(ctx => mo5atabatNeededVar.Get(ctx))
        }.WithLayout(
            x: 1040,
            y: 320,
            w: 240.625,
            h: 68.4375,
            displayText: "Requires Mo5atabat?");

        // ── STEP 4b ── True branch drops down from CheckMo5 ──────────────────
        WaitForMo5atabatActivity waitMo5atabat = new WaitForMo5atabatActivity
        {
            Id = "WaitMo5atabat",
            ApplicationId = new Input<int>(appIdVar)
        }.WithLayout(
            x: 1040,
            y: 531.5625,
            w: 285.5859375,
            h: 68.4375);

        // ── STEP 5 ── merge point: both Mo5atabat paths arrive here ──────────
        WaitForFinalMo3awenActivity waitFinalMo3awen = new WaitForFinalMo3awenActivity
        {
            Id = "WaitFinalMo3awen",
            ApplicationId = new Input<int>(appIdVar)
        }.WithLayout(
            x: 1400,
            y: 531.5625,
            w: 316.796875,
            h: 68.4375);

        WaitForHasMane3AnouniActivity waitHasMane3 = new WaitForHasMane3AnouniActivity
        {
            Id = "WaitHasMane3Anouni",
            ApplicationId = new Input<int>(appIdVar),
            Decision = new Output<string>(hasMane3DecisionVar),
            Reason = new Output<string>(hasMane3ReasonVar)
        }.WithLayout(
            x: 1400,
            y: 700,
            w: 343.203125,
            h: 68.4375);

        FlowDecision hasMane3Check = new FlowDecision
        {
            Id = "HasMane3Check",
            Name = "Has Legal Obstacle?",
            Condition = new Input<bool>(ctx =>
                hasMane3DecisionVar.Get(ctx) == "has_obstacle")
        }.WithLayout(
            x: 1400,
            y: 840,
            w: 240.625,
            h: 68.4375,
            displayText: "Has Legal Obstacle?");

        FinalizeApplicationActivity finalizeApproved = new FinalizeApplicationActivity
        {
            Id = "FinalizeApproved",
            ApplicationId = new Input<int>(appIdVar),
            IsApproved = new Input<bool>(_ => true)
        }.WithLayout(
            x: 1640,
            y: 1040,
            w: 283.90625,
            h: 68.4375 , displayText: "Finalize Approved");

        FinalizeApplicationActivity finalizeRejected = new FinalizeApplicationActivity
        {
            Id = "FinalizeRejected",
            ApplicationId = new Input<int>(appIdVar),
            IsApproved = new Input<bool>(_ => false),
            RejectionReason = new Input<string>(hasMane3ReasonVar)
        }.WithLayout(
            x: 1040,
            y: 1040,
            w: 283.90625,
            h: 68.4375 , displayText: "Finalize Rejected");

        builder.Root = new Flowchart
        {
            Activities =
            {
                setAppId, setMo5atabatNeeded,
                waitI3alm, waitMo3awen, conditionBothApproved,
                incrementRound,
                checkMo5atabat, waitMo5atabat,
                waitFinalMo3awen,
                waitHasMane3, hasMane3Check,
                finalizeApproved, finalizeRejected
            },

            // ── CONNECTIONS (arrows on the diagram) ─────────────────────────────
            Connections =
            {
                // Init → top row
                new Connection(new Endpoint(setAppId,           "Done"), new Endpoint(setMo5atabatNeeded)),
                new Connection(new Endpoint(setMo5atabatNeeded, "Done"), new Endpoint(waitI3alm)),

                // Top row
                new Connection(new Endpoint(waitI3alm,   "Done"), new Endpoint(waitMo3awen)),

                // Mo3awen drops DOWN to Condition
                new Connection(new Endpoint(waitMo3awen, "Done"), new Endpoint(conditionBothApproved)),

                // Condition False → LEFT to IncrementRound → loops back UP to I3alm
                new Connection(new Endpoint(conditionBothApproved, "False"), new Endpoint(incrementRound)),
                new Connection(new Endpoint(incrementRound,        "Done"),  new Endpoint(waitI3alm)),

                // Condition True → RIGHT+DOWN to CheckMo5
                new Connection(new Endpoint(conditionBothApproved, "True"), new Endpoint(checkMo5atabat)),

                // CheckMo5 True → DOWN to WaitMo5 → RIGHT to FinalMo3awen
                new Connection(new Endpoint(checkMo5atabat, "True"),  new Endpoint(waitMo5atabat)),
                new Connection(new Endpoint(waitMo5atabat,  "Done"),  new Endpoint(waitFinalMo3awen)),

                // CheckMo5 False → skips RIGHT directly to FinalMo3awen
                new Connection(new Endpoint(checkMo5atabat, "False"), new Endpoint(waitFinalMo3awen)),

                // FinalMo3awen drops DOWN
                new Connection(new Endpoint(waitFinalMo3awen, "Done"), new Endpoint(waitHasMane3)),
                new Connection(new Endpoint(waitHasMane3,     "Done"), new Endpoint(hasMane3Check)),

                // HasMane3 True → LEFT (rejected), False → RIGHT (approved)
                new Connection(new Endpoint(hasMane3Check, "True"),  new Endpoint(finalizeRejected)),
                new Connection(new Endpoint(hasMane3Check, "False"), new Endpoint(finalizeApproved))
            }
        };
    }

    private static int TryGetInt(IDictionary<string, object>? d, string key)
        => d != null && d.TryGetValue(key, out object? v) ? Convert.ToInt32(v) : 0;

    private static bool TryGetBool(IDictionary<string, object>? d, string key)
        => d != null &&
           d.TryGetValue(key, out object? v) &&
           (v is bool b ? b : v?.ToString()?.ToLower() == "true");
}