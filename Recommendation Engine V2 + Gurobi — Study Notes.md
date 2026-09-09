

---
## 1. Big Picture

The V2 recommendation engine converts a staffing/scheduling problem into a **Gurobi optimization problem**.

```text
RecommendationInputDto
        │
        ├── Slots
        ├── ProviderShifts
        ├── StaffShifts
        ├── Ratios
        ├── Rules
        ├── NonVisitWorks
        ├── Break
        └── Other configuration
        │
        ▼
Create Decision Variables
        │
        ▼
Build Objective Function
        │
        ▼
Build Constraints
        │
        ▼
Add Constraints to Gurobi
        │
        ▼
Optimize
        │
        ▼
Read variable.X
        │
        ▼
RecommendationDto
```

The important distinction is:

> **Your C# code prepares the optimization problem; Gurobi finds the best values for the decision variables.**

---

# 2. Optimization Fundamentals

## 2.1 Optimization Problem

An optimization problem contains:

```text
Decision Variables
        +
Objective Function
        +
Constraints
```

Example:

> Decide how many staff members should be assigned to each time slot while satisfying staffing requirements and minimizing total staffing effort.

Mathematically:

min⁡f(x)\min f(x)

subject to:

gi(x)≥/≤/=big_i(x) \geq / \leq / = b_i

---

# 3. Decision Variables

A **decision variable** represents something Gurobi is allowed to choose.

For the recommendation engine:

```text
08:00–08:30 → x1
08:30–09:00 → x2
09:00–09:30 → x3
```

Gurobi chooses the values:

```text
x1 = 2
x2 = 3
x3 = 1
```

These values become the recommended staffing numbers.

---

## 3.1 V2 Decision Variable Object

V2 first creates application-level decision variables:

```csharp
GetDecisionVariables(input.Slots, input.Break)
```

This creates:

```csharp
TeamBuilderDecisionVariable
```

for every recommendation slot.

Then `BuildExpression()` converts those application objects into actual Gurobi variables:

```csharp
model.AddVar(...)
```

So:

```text
TeamBuilderDecisionVariable
            ↓
       GRBModel.AddVar()
            ↓
          GRBVar
```

---

# 4. Variable Types

V2 uses two important Gurobi variable types.

## INTEGER

```csharp
GRB.INTEGER
```

Example:

```text
x = 0
x = 1
x = 2
x = 3
```

Not:

```text
x = 2.5
```

Normal visit staffing uses integer variables.

The code gets this from:

```csharp
GetSlotType(slot)
```

which returns `GRB.INTEGER` unless the slot is non-visit work.

---

## CONTINUOUS

```csharp
GRB.CONTINUOUS
```

Allows fractional values:

```text
x = 1.5
x = 2.75
x = 3.25
```

V2 uses continuous variables for **NonVisitWork** slots.

```csharp
if (slot.IsNonVisitWorkSlot)
    return GRB.CONTINUOUS;
```

### Why?

Non-visit work may require a fractional staffing amount.

Example:

```text
Required effort = 45 minutes
Task unit = 30 minutes

45 / 30 = 1.5 personnel
```

So a continuous variable can represent `1.5`.

---

# 5. Variable Bounds

A variable can have:

```text
Lower Bound ≤ Variable ≤ Upper Bound
```

Example:

0≤x≤100 \leq x \leq 10

In V2:

```csharp
model.AddVar(
    decisionVariable.Lb,
    decisionVariable.Ub,
    ...
);
```

The bounds are part of the decision variable definition.

---

# 6. Objective Function

The **objective function** tells Gurobi what "best" means.

Here the recommendation engine wants to **minimize staffing assignment**.

Conceptually:

min⁡∑icixi\min \sum_i c_i x_i

where:

- xix_i = number of staff assigned
    
- cic_i = objective weight
    

---

## 6.1 Main Expression

V2 creates:

```csharp
GRBLinExpr mainExpression = 0;
```

Then `BuildExpression()` adds terms:

```csharp
mainExpression.AddTerm(
    decisionVariable.Coeff,
    grbDecVar
);
```

Conceptually:

```text
mainExpression

= coefficient₁ × x₁
+ coefficient₂ × x₂
+ coefficient₃ × x₃
+ ...
```

---

# 7. Objective Weight

The coefficient comes from:

```csharp
GetObjectiveWeight(slot, breakDto)
```

The method calculates:

```text
shift length
        ↓
break duration
        ↓
working hours
        ↓
objective weight
```

The formula is:

workingHours=length−breakMinutes60\text{workingHours} = \text{length} - \frac{\text{breakMinutes}}{60}

and:

weight=workingHours8−0.0001(workingHours)2\text{weight} = \frac{\text{workingHours}}{8} - 0.0001(\text{workingHours})^2

### Important idea

The objective isn't simply:

```text
minimize number of staff
```

It is effectively:

```text
minimize weighted staffing
```

The weight depends on the length of the availability range.

The code comments explain that longer blocks are slightly preferred over equivalent shorter blocks.

---

# 8. Why NonVisitWork Is Excluded From Main Objective

Inside `BuildExpression()`:

```csharp
if (!decisionVariable.IsNonVisitWork)
{
    mainExpression.AddTerm(
        decisionVariable.Coeff,
        grbDecVar
    );
}
```

Therefore:

```text
Visit Work
    ↓
included in objective

NonVisit Work
    ↓
not included in main objective
```

Non-visit work is handled through its own constraints.

---

# 9. Constraints

A **constraint** restricts what solutions are allowed.

Example:

```text
At least 3 staff are required.
```

Mathematically:

x≥3x \geq 3

Another example:

```text
Maximum 2 ghost shifts.
```

ghost1+ghost2+ghost3≤2ghost_1 + ghost_2 + ghost_3 \leq 2

---

# 10. Gurobi Linear Expression

`GRBLinExpr` represents a linear mathematical expression.

Example:

```csharp
GRBLinExpr expression = 0;

expression.AddTerm(1, x);
expression.AddTerm(2, y);
```

means:

x+2yx + 2y

You can then turn it into a constraint:

```csharp
model.AddConstr(
    expression,
    GRB.GREATER_EQUAL,
    5,
    "Constraint"
);
```

which means:

x+2y≥5x + 2y \geq 5

---

# 11. Constraint Direction

V2 primarily uses:

### Greater than or equal

```csharp
GRB.GREATER_EQUAL
```

Mathematically:

LHS≥RHSLHS \geq RHS

Used when the model requires **at least** a certain amount.

Example:

```text
Required staff = 5

x1 + x2 >= 5
```

---

### Less than or equal

```csharp
GRB.LESS_EQUAL
```

Mathematically:

LHS≤RHSLHS \leq RHS

Used for limits.

Example:

```text
Maximum ghost shifts = 2

ghost1 + ghost2 <= 2
```

---

# 12. V2 Main Method

The V2 method:

```csharp
RecommendationsV2Async(...)
```

is primarily an **orchestrator**.

It does not contain all the optimization logic itself.

Its job is:

```text
Initialize
→ Create variables
→ Build objective
→ Build constraints
→ Add constraints
→ Solve
→ Read result
→ Map result
```

---

# 13. Step 1 — Initialize Gurobi

```csharp
InitializeGurobiModel(
    ref env,
    ref model,
    runningDateTime,
    input.IsAnalyticRun
);
```

This creates:

```text
GRBEnv
   ↓
GRBModel
```

---

# 14. Gurobi Environment

`GRBEnv` represents the Gurobi environment.

It handles things such as:

- configuration
- logging
- compute server
- threads
- priority

The method creates:

```csharp
env = new GRBEnv(true);
```

and configures the Compute Server.

---

# 15. Gurobi Model

The model represents the actual optimization problem.

```csharp
model = new GRBModel(env)
{
    ModelName = "TeamBuilder_Optimization"
};
```

Think:

```text
GRBEnv
  = execution environment

GRBModel
  = mathematical optimization problem
```

---

# 16. Step 2 — Create Decision Variables

V2:

```csharp
ConcurrentDictionary<string, TeamBuilderDecisionVariable>
    decisionVariables =
        GetDecisionVariables(input.Slots, input.Break);
```

---

# 17. GetDecisionVariables()

This method loops through all slots.

```csharp
Parallel.ForEach(
    slots,
    slot =>
```

For each slot:

```csharp
string slotKey = slot.ToString();
```

Then creates:

```csharp
new(
    GetObjectiveWeight(slot, breakDto),
    GetSlotType(slot),
    slotKey,
    slot.IsGhostSlot,
    slot.IsNonVisitWorkSlot
)
```

So each decision variable contains information such as:

```text
Name
Coefficient
Type
Ghost?
NonVisitWork?
Bounds
GRBVar
```

---

# 18. Special Non-Visit Variable

For a NonVisitWork slot, V2 creates an additional variable:

```text
slotKey
```

and:

```text
slotKey-REM
```

because:

```csharp
RemainingStaffPostfix = "-REM";
```

Example:

```text
09 → normal variable
09-REM → remaining staff variable
```

This is important for understanding the V2 model.

---

# 19. Why Remaining Staff Exists

Suppose the minimum staffing requirement is:

```text
5 staff
```

but calculated demand is:

```text
3 staff
```

Then:

```text
remaining staff = 5 - 3
                 = 2
```

Those additional 2 staff can potentially be used for **NonVisitWork**.

V2 calculates:

```csharp
remainingStaff =
    Math.Max(value, officeMinimum) - value;
```

So:

```text
Minimum requirement
        -
Actual demand
        =
Remaining staff capacity
```

---

# 20. Step 3 — BuildExpression()

```csharp
BuildExpression(
    model,
    mainExpression,
    ghostSlotConstraintExpression,
    decisionVariables
);
```

This method does two major things:

### 1. Adds variables to Gurobi

```csharp
model.AddVar(...)
```

### 2. Builds the objective

```csharp
mainExpression.AddTerm(...)
```

### 3. Builds ghost-shift expression

```csharp
ghostSlotConstraintExpression.AddTerm(...)
```

---

# 21. Ghost Shift Constraint

V2 creates:

```csharp
GRBLinExpr ghostSlotConstraintExpression = 0;
```

Then ghost variables are added to it.

Example:

```text
ghost1 + ghost2 + ghost3
```

Then:

```csharp
model.AddConstr(
    ghostSlotConstraintExpression,
    GRB.LESS_EQUAL,
    input.GhostShiftBoundPerDay,
    "Constraint_Ghost"
);
```

Mathematically:

∑GhostVariables≤GhostShiftBoundPerDay\sum GhostVariables \leq GhostShiftBoundPerDay

---

# 22. Step 4 — BuildConstraintExpressions()

This is one of the **most important V2 methods**.

```csharp
BuildConstraintExpressions(...)
```

Its responsibility is to calculate the constraints that will later be added to Gurobi.

It prepares four groups:

```text
constraintExpressions
ghostConstraintExpressions
nonVisitWorkConstraintExpressions
remainingStaffConstraintExpressions
```

Think:

```text
Input
 ↓
Calculate demand
 ↓
Calculate minimums
 ↓
Calculate assignments
 ↓
Calculate existing staff
 ↓
Calculate remaining staff
 ↓
Build mathematical expressions
```

---

# 23. Visit Volume Data

V2 creates a visit-volume source:

```csharp
var visitVolumeSource =
    _visitVolumeSourceFactory.CreateSource(
        input.RecommendationType
    );
```

Then retrieves visit data:

```csharp
await visitVolumeSource.GetVisitDataAsync(...)
```

This data contributes to determining staffing demand.

---

# 24. Generate Time Slots

V2 generates the evaluation ranges:

```csharp
TimeSpanHelper.GenerateSlots(
    ScheduleConstants.TimeSpanBase,
    ScheduleConstants.TimeSpanTop,
    ShiftDurationOffset
);
```

The shift duration offset is:

```csharp
30 minutes
```

So conceptually:

```text
08:00–08:30
08:30–09:00
09:00–09:30
09:30–10:00
...
```

---

# 25. NonVisitWork Constraint

For every non-visit work item:

```csharp
requiredPersonnelForNonVisitWork =
    nonVisitWork.EffortMinutes / 30
```

Example:

```text
Effort = 90 minutes

90 / 30 = 3
```

Therefore:

NonVisitWorkVariable≥3NonVisitWorkVariable \geq 3

---

# 26. Parallel Constraint Construction

V2 uses:

```csharp
Parallel.ForEach(
    timeSlots,
    range =>
```

For every time range it independently builds constraint information.

Conceptually:

```text
08:00–08:30 ──→ constraint
08:30–09:00 ──→ constraint
09:00–09:30 ──→ constraint
09:30–10:00 ──→ constraint
```

This is why V2 uses thread-safe structures such as:

```csharp
ConcurrentDictionary
ConcurrentBag
```

---

# 27. BuildConstraintExpressionsForSlots()

This helper examines the recommendation slots matching the current time range.

```csharp
BuildConstraintExpressionsForSlots(...)
```

Its job is to add variables to:

```text
normal staffing constraint
ghost constraint
non-visit-work constraint
```

---

# 28. Matching Slots

V2 determines which slots cover the current 30-minute range:

```csharp
input.Slots.Where(a =>
    a.TimeBeginTimeSpan <= range &&
    rangeEnd <= a.TimeEndTimeSpan
);
```

Example:

```text
Slot: 08:00–12:00

Current range:
09:00–09:30

Does slot contain range?
YES
```

Therefore its variable participates in the 09:00–09:30 constraint.

---

# 29. NonVisitWork Mathematical Trick

For NonVisitWork:

```csharp
constraintExpression.Add(
    new(-1, slotVar.GRBVar, key)
);
```

So the variable is added with coefficient `-1`.

Then its own NonVisitWork expression receives:

```text
+ slot variable
+ remaining staff variable
```

This allows the model to reason about staff being allocated between:

```text
Visit work
        +
Non-visit work
```

within the available staffing capacity.

---

# 30. Break Handling

V2 calls:

```csharp
CheckPossibleBreakSlot(...)
```

to determine whether a particular availability can be used during the current range.

The method calculates:

```text
Shift length
      ↓
Is long break required?
      ↓
Is short break required?
      ↓
Calculate break window
      ↓
Does current range fall inside break?
```

If the current range is a break:

```text
don't count the normal staffing variable
```

unless it is a ghost slot.

---

# 31. Provider Shifts

For each time range V2 finds providers whose shifts overlap that range:

```csharp
TimeSpanHelper.Overlaps(...)
```

Example:

```text
Provider A → 08:00–16:00
Provider B → 10:00–18:00

Current range → 10:00–10:30

Both overlap.
```

---

# 32. Provider Speciality

Providers are grouped by:

```csharp
ProviderSpecialityId
```

inside:

```csharp
GetConstraintValueByVisitType(...)
```

Conceptually:

```text
Providers
   │
   ├── Specialty A
   │     ├── Provider 1
   │     └── Provider 2
   │
   └── Specialty B
         ├── Provider 3
         └── Provider 4
```

The model can therefore calculate demand independently for each specialty.

---

# 33. Ratios

V2 uses ratios to determine staffing demand.

There are two important model types:

```text
Dynamic
Fixed
```

---

## Dynamic Ratio

Dynamic means demand comes from visit-volume data.

Conceptually:

StaffNeeded=f(VisitVolume,Ratio)StaffNeeded = f(VisitVolume, Ratio)

V2 calls:

```csharp
x.StaffNeeded(...)
```

or:

```csharp
x.StaffNeeded(...)
```

depending on the data source path.

---

## Fixed Ratio

Fixed ratio simply uses:

```text
ratio × number of providers
```

V2:

```csharp
ratioForCurrentSpeciality.Value * item.Count()
```

Example:

```text
Fixed ratio = 2 staff/provider

Providers = 3

Demand = 2 × 3 = 6
```

---

# 34. Ratio Fallback

V2 first looks for a specialty-specific ratio.

If none exists:

```text
Specialty ratio
      ↓ unavailable
Office/default ratio
      ↓ unavailable
System default
```

The logic is:

```csharp
specialityRatioDto ?? defaultRatioDto
```

This is an important concept to remember.

---

# 35. Minimum Rule

After calculating demand:

```csharp
Math.Max(
    demandForSpeciality,
    minimumValue
);
```

Meaning:

RequiredStaff=max⁡(Demand,MinimumRule)RequiredStaff = \max(Demand, MinimumRule)

Example:

```text
Demand = 3
Minimum = 5

Required = 5
```

Another:

```text
Demand = 7
Minimum = 5

Required = 7
```

---

# 36. Assigned Rule

V2 also adds assigned staff requirements.

```csharp
GetSpecialityRuleValueByType(
    rules,
    RuleType.Assigned,
    ...
)
```

So the overall requirement can contain:

```text
Demand
+
Minimum rule
+
Assigned rule
```

---

# 37. Existing Staff

V2 checks staff already assigned:

```csharp
int assignedStaff =
    input.StaffShifts.Count(x =>
        x.TimeBeginTimeSpan <= range &&
        range < x.TimeEndTimeSpan
    );
```

Then:

```csharp
value -= assignedStaff;
```

Meaning:

AdditionalRequiredStaff=RequiredStaff−AlreadyAssignedStaffAdditionalRequiredStaff = RequiredStaff - AlreadyAssignedStaff

Example:

```text
Required = 7
Already assigned = 3

Additional required = 4
```

---

# 38. Remaining Staff

This is different from the previous calculation.

V2 calculates:

```csharp
remainingStaff =
    Math.Max(value, officeMinimum) - value;
```

This represents staff capacity created by the minimum staffing rule that may be available for NonVisitWork.

Example:

```text
Demand = 3
Minimum = 5

Remaining = 5 - 3
          = 2
```

---

# 39. Main Staffing Constraint

After all calculations, V2 stores:

```csharp
constraintExpressions.TryAdd(
    slotKey,
    new(
        constraintExpression,
        GRB.GREATER_EQUAL,
        value
    )
);
```

Mathematically:

∑matching staffing variables≥required value\sum \text{matching staffing variables} \geq \text{required value}

This is one of the most important equations in the entire recommendation engine.

---

# 40. Ghost Constraint Per Slot

For every time range:

```csharp
ghostConstraintExpressions.TryAdd(
    $"Ghost_{slotKey}",
    new(
        ghostConstraintExpression,
        GRB.LESS_EQUAL,
        input.GhostShiftBoundPerSlot
    )
);
```

Therefore:

∑GhostVariablesslot≤GhostShiftBoundPerSlot\sum GhostVariables_{slot} \leq GhostShiftBoundPerSlot

There are therefore two ghost limits:

```text
Per day
+
Per time slot
```

---

# 41. Remaining Staff Constraint

V2 creates:

```csharp
remainingStaffConstraintExpressions
```

using:

```csharp
BuildRemainingStaffTerms(...)
```

The resulting constraint is:

∑RemainingStaffVariables≤RemainingStaff\sum RemainingStaffVariables \leq RemainingStaff

This prevents NonVisitWork from consuming more remaining staffing capacity than is available.

---

# 42. BuildRemainingStaffTerms()

This method only deals with NonVisitWork slots.

It:

```text
Find NonVisitWork slots
        ↓
Find corresponding -REM variables
        ↓
Add them to expression
```

Example:

```text
09-REM
10-REM
11-REM
```

could produce:

x09−REM+x10−REM+x11−REM≤2x_{09-REM}+x_{10-REM}+x_{11-REM} \leq 2

---

# 43. Step 5 — AddConstraintsToModel()

Once all expressions are prepared, V2 calls:

```csharp
AddConstraintsToModel(...)
```

four times:

```csharp
AddConstraintsToModel(model, constraintExpressions);
AddConstraintsToModel(model, ghostConstraintExpressions);
AddConstraintsToModel(model, nonVisitWorkConstraintExpressions);
AddConstraintsToModel(model, remainingStaffConstraintExpressions);
```

---

# 44. Why Separate Expression Building From Adding?

This is an important V2 architectural improvement.

Instead of immediately modifying the Gurobi model while processing every slot:

```text
Calculate expressions
        ↓
Store expressions
        ↓
Add expressions to model
```

This makes the parallel construction possible and keeps model mutation centralized.

---

# 45. AddConstraintsToModel()

The method converts stored terms into a real `GRBLinExpr`.

```csharp
GRBLinExpr constraintExpression = 0;
```

Then:

```csharp
constraintExpression.AddTerm(
    term.Coeff,
    term.GrbVar
);
```

Finally:

```csharp
model.AddConstr(
    constraintExpression,
    constraint.Value.Item2,
    (double)constraint.Value.Item3,
    constraint.Key
);
```

So:

```text
TeamBuilderTerm
       ↓
GRBLinExpr
       ↓
model.AddConstr()
       ↓
Gurobi constraint
```

---

# 46. Step 6 — SolveModel()

V2 calls:

```csharp
SolveModel(model, mainExpression);
```

This is where the mathematical optimization actually happens.

---

# 47. Set Objective

Inside `SolveModel()`:

```csharp
model.SetObjective(
    expression,
    GRB.MINIMIZE
);
```

Meaning:

min⁡MainExpression\min MainExpression

---

# 48. MIP

Because normal staffing variables are integers, this is a **Mixed Integer Programming-style optimization model** when continuous NonVisitWork variables are present.

```text
INTEGER variables
       +
CONTINUOUS variables
       +
LINEAR objective
       +
LINEAR constraints
       ↓
MIP
```

This is one of the core Gurobi concepts you should understand.

---

# 49. Time Limit

V2 configures:

```csharp
model.Set(
    GRB.DoubleParam.TimeLimit,
    timeout
);
```

Default:

```text
3 seconds
```

according to the code.

This means Gurobi doesn't necessarily have unlimited time to search for the mathematically perfect solution.

---

# 50. MIP Gap

V2 also sets:

```csharp
model.Set(
    GRB.DoubleParam.MIPGap,
    mipGap
);
```

The **MIP gap** tells Gurobi how close the current solution must be to the proven optimal solution before it can stop.

Conceptually:

```text
Best known solution
        vs
Best possible bound
        ↓
Gap
```

Smaller gap generally means greater optimality certainty but potentially more computation.

---

# 51. Optimize

Finally:

```csharp
model.Optimize();
```

This is the moment Gurobi solves the optimization problem.

Before this:

```text
C# is constructing the problem
```

After this:

```text
Gurobi has calculated variable values
```

---

# 52. Gurobi Variable `.X`

After optimization:

```csharp
dvarPair.Value.GRBVar.X
```

contains the solution value.

Example:

```text
Variable: 08-09
X = 3
```

means:

```text
Recommendation = 3 staff
```

V2 maps these values in:

```csharp
MapDecisionVariablesToRecommendationItems()
```

---

# 53. Step 7 — HandleOptimizationResult()

After optimization:

```csharp
List<string> ignoredConstraints =
    HandleOptimizationResult(model);
```

This method checks:

```text
OPTIMAL
TIME_LIMIT
UNBOUNDED
INF_OR_UNBD
INFEASIBLE
other
```

---

# 54. OPTIMAL

```csharp
GRB.Status.OPTIMAL
```

means Gurobi found an optimal solution.

V2 returns an empty ignored-constraints list.

---

# 55. TIME_LIMIT

```csharp
GRB.Status.TIME_LIMIT
```

is also accepted by the application.

The method returns an empty list.

This means the application accepts the best solution available when the timeout occurs.

---

# 56. INFEASIBLE

If the model cannot satisfy all constraints:

```csharp
GRB.Status.INFEASIBLE
```

V2 calls:

```csharp
model.ComputeIIS();
```

---

# 57. IIS

**IIS = Irreducible Inconsistent Subsystem**

You should understand this concept well.

Suppose the model contains:

```text
x >= 10
x <= 5
```

Impossible.

Gurobi can identify constraints responsible for the infeasibility.

The code checks:

```csharp
c.IISConstr == 1
```

and stores those constraint names.

Therefore the application can return:

```text
IgnoredConstraints
```

to identify problematic constraints.

---

# 58. UNBOUNDED

```csharp
GRB.Status.UNBOUNDED
```

means the objective can improve indefinitely without a limiting bound.

V2 throws:

```csharp
UnboundedModelException
```

---

# 59. Step 8 — Map Results

V2 calls:

```csharp
MapDecisionVariablesToRecommendationItems(
    decisionVariables,
    input.Slots
);
```

This converts:

```text
Gurobi variables
      ↓
RecommendationItemDto
```

---

# 60. Mapping Logic

For each decision variable:

```csharp
dvarPair.Value.GRBVar.X
```

is read.

Then:

```csharp
new RecommendationItemDto
{
    AvailabilityRange = resultSlot,
    Result = ...
}
```

is created.

So:

```text
Gurobi:

08 → X = 2
09 → X = 3
10 → X = 1

        ↓

API:

08 → Result = 2
09 → Result = 3
10 → Result = 1
```

---

# 61. Result FTE

V2 obtains:

```csharp
model.ObjVal
```

and stores it as:

```csharp
resultFTE
```

`ObjVal` is the objective value of the solution.

Conceptually:

FTE=∑icixiFTE = \sum_i c_i x_i

---

# 62. Computed FTE

V2 independently calculates:

```csharp
computedFTE =
    recommendations
        .Where(...)
        .Sum(
            x => x.Result *
                 GetObjectiveWeight(...)
        );
```

This gives an independent calculation from the returned recommendation values.

Then:

```csharp
ResultFTEDeviation =
    resultFTE - computedFTE
```

---

# 63. Final V2 Output

The method returns:

```csharp
RecommendationDto
```

containing:

```text
Recommendations
ResultFTE
ResultFTEDeviation
RunDateTime
IgnoredConstraints
OfficeId
Status
GapPercentage
RecommendationType
```

---

# 64. Complete Mathematical Picture

A simplified version of the V2 model looks like:

### Decision variables

x1,x2,…,xnx_1,x_2,\dots,x_n

where each xix_i represents recommended staff for a slot.

---

### Objective

min⁡∑icixi\min \sum_i c_i x_i

where:

ci=GetObjectiveWeight(sloti)c_i = GetObjectiveWeight(slot_i)

---

### Staffing constraints

For each time range tt:

∑i∈txi≥RequiredStafft\sum_{i \in t}x_i \geq RequiredStaff_t

---

### Ghost constraints

Per slot:

∑GhostVariablest≤GhostBoundt\sum GhostVariables_t \leq GhostBound_t

Per day:

∑GhostVariables≤GhostDailyBound\sum GhostVariables \leq GhostDailyBound

---

### NonVisitWork constraints

∑NonVisitWorkVariables≥RequiredNonVisitWork\sum NonVisitWorkVariables \geq RequiredNonVisitWork

---

### Remaining staff

∑RemainingStaffVariablest≤RemainingStafft\sum RemainingStaffVariables_t \leq RemainingStaff_t

---

### Variable domains

Normal staffing:

xi∈Z≥0x_i \in \mathbb{Z}_{\geq0}

NonVisitWork:

xi∈R≥0x_i \in \mathbb{R}_{\geq0}

---

# 65. V2 Method Dependency Map

Study the methods in this order:

```text
RecommendationsV2Async()
│
├── InitializeGurobiModel()
│
├── GetDecisionVariables()
│   ├── GetObjectiveWeight()
│   └── GetSlotType()
│
├── BuildExpression()
│
├── BuildConstraintExpressions()
│   │
│   ├── GetConstraintValueByVisitType()
│   │   └── GetSpecialityRuleValueByType()
│   │
│   ├── BuildConstraintExpressionsForSlots()
│   │   └── CheckPossibleBreakSlot()
│   │
│   └── BuildRemainingStaffTerms()
│
├── AddConstraintsToModel()
│
├── SolveModel()
│
├── HandleOptimizationResult()
│
└── MapDecisionVariablesToRecommendationItems()
```

---
