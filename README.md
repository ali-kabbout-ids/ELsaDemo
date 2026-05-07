# Elsa Solution — Purchase Order Demo

Two projects, one solution.

| Project | Port | Purpose |
|---|---|---|
| `PurchaseOrderApi` | 5000 | Backend API + Elsa runtime + Swagger |
| `PurchaseOrderApi.Studio` | 5001 | Elsa Studio visual designer (Blazor WASM) |

---

## Prerequisites
- .NET 8 SDK: https://dotnet.microsoft.com/download
- Visual Studio 2022 17.8+ OR VS Code with C# DevKit

---

## Open in Visual Studio
Double-click `ElsaSolution.sln` — both projects load automatically.

---

## Run both projects

### Option A — Visual Studio (recommended)
1. Right-click the Solution → **Set Startup Projects…**
2. Select **Multiple startup projects**
3. Set both `PurchaseOrderApi` and `PurchaseOrderApi.Studio` to **Start**
4. Press **F5**

### Option B — Two terminals
```bash
# Terminal 1 — API
cd PurchaseOrderApi
dotnet run
# → http://localhost:5000/swagger

# Terminal 2 — Studio
cd PurchaseOrderApi.Studio
dotnet run
# → http://localhost:5001
```

---

## What you'll see

**Swagger UI** (`http://localhost:5000/swagger`)  
All your custom API endpoints for creating POs, submitting, and approving/rejecting.

**Elsa Studio** (`http://localhost:5001`)  
The visual drag-and-drop workflow designer. It connects to `http://localhost:5000`
and shows you:
- All registered workflow definitions
- The visual graph of `PurchaseOrderApprovalWorkflow`
- Live workflow instances and their status (Running / Suspended / Finished / Faulted)
- Active bookmarks (suspension points)
- Activity execution history

---

## The bug that was fixed
The original file `Workflows/PurchaseOrderApprovalWorkflow.cs` had:
```csharp
using Elsa.Workflows.Contracts;  // ❌ this namespace does NOT exist in Elsa 3.x
```
It was removed. `IWorkflowBuilder` lives in `Elsa.Workflows` which was already imported.

---

## Test Sequence (use Swagger at localhost:5000/swagger)

1. `POST /api/orders`  — create a PO  
2. `POST /api/orders/1/submit`  — start the workflow → SUSPENDS  
3. `GET  /api/debug/instances`  — see Status="Suspended"  
4. `GET  /api/debug/bookmarks`  — see the WaitForApproval bookmark  
5. `POST /api/orders/1/decide`  — `{"decision":"approved"}` → RESUMES → FINISHES  
6. `GET  /api/debug/instances/{id}` — Status="Finished", Bookmarks=[]  

Then watch the same workflow in Elsa Studio while running the steps above.

---

## Elsa features covered

| Feature | Location |
|---|---|
| `WorkflowBase` + `IWorkflowBuilder` | `Workflows/PurchaseOrderApprovalWorkflow.cs` |
| `Variable<T>` + `SetVariable` | Workflow definition |
| `CodeActivity` + `Input<T>` / `Output<T>` | All activities |
| `Activity` base class (non-CodeActivity) | `WaitForApprovalActivity` |
| `ctx.CreateBookmark()` — **suspend** | `WaitForApprovalActivity.ExecuteAsync` |
| `OnResumedAsync` — **resume callback** | `WaitForApprovalActivity` |
| `Sequence` + `If/Else` branching | Workflow definition |
| `ctx.GetRequiredService<T>()` — DI in activities | `ValidateOrderActivity` etc. |
| `IWorkflowRuntime.StartWorkflowAsync` | `PurchaseOrdersController.Submit` |
| `IWorkflowRuntime.ResumeWorkflowAsync` | `PurchaseOrdersController.Decide` |
| `IWorkflowInstanceStore` | Debug controller + Decide |
| `IBookmarkStore` | Debug controller + Decide |
| `CorrelationId` | Submit → links PO to its workflow instance |
| `elsa.UseWorkflowsApi()` | `Program.cs` — exposes REST for Studio |
| In-memory runtime (no DB) | `Program.cs` |
| `WorkflowStatus` enum | Debug controller |
| Elsa Studio visual designer | `PurchaseOrderApi.Studio` project |
