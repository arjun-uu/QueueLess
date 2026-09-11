

### V2-only flow

```text
RecommendationsV2Async(input)
        │
        ▼
InitializeGurobiModel()
        │
        │  Creates Gurobi environment + model
        ▼
GetDecisionVariables()
        │
        │  Creates logical decision variables
        │  for every RecommendationSlot
        ▼
BuildExpression()
        │
        │  Converts decision variables into
        │  actual Gurobi variables
        │  + builds objective expression
        ▼
BuildConstraintExpressions()
        │
        ├──────────────► VisitVolumeSource
        │                 GetVisitDataAsync()
        │
        ├──────────────► Generate 30-min time slots
        │
        ├──────────────► BuildConstraintExpressionsForSlots()
        │
        ├──────────────► GetConstraintValueByVisitType()
        │                 ├─ Provider demand
        │                 └─ Miscellaneous demand
        │
        └──────────────► BuildRemainingStaffTerms()
        │
        ▼
AddConstraintsToModel()
        │
        │  Converts collected terms into
        │  actual Gurobi constraints
        ▼
SolveModel()
        │
        │  MINIMIZE objective
        │  + Optimize
        ▼
HandleOptimizationResult()
        │
        ├── OPTIMAL / TIME_LIMIT → continue
        ├── INFEASIBLE → IIS / ignored constraints
        └── UNBOUNDED → exception
        │
        ▼
MapDecisionVariablesToRecommendationItems()
        │
        │  Gurobi X values → RecommendationItemDto
        ▼
Calculate computedFTE
        │
        ▼
Return RecommendationDto
```

The V2 entry point is `RecommendationsV2Async`, and unlike the old method it delegates most of the model construction to helper methods such as `GetDecisionVariables`, `BuildExpression`, and `BuildConstraintExpressions`. 

## 1. `RecommendationsV2Async` — orchestrator

**Why?**
This is the main V2 workflow. It does not perform all the calculations itself; it coordinates the private methods.

**What it performs:**

1. Creates Gurobi model.
2. Creates decision variables.
3. Builds objective expression.
4. Builds constraint expressions.
5. Adds constraints to Gurobi.
6. Solves optimization.
7. Handles optimization result.
8. Maps solver variables into recommendations.
9. Calculates FTE/deviation.
10. Returns `RecommendationDto`.

So conceptually:

```text
Input
 ↓
Prepare model
 ↓
Prepare variables
 ↓
Prepare objective
 ↓
Prepare constraints
 ↓
Optimize
 ↓
Read solution
 ↓
RecommendationDto
```

---

# 2. `InitializeGurobiModel()`

```text
RecommendationsV2Async
        │
        ▼
InitializeGurobiModel()
```

**Why?**
Gurobi cannot optimize until its environment and model exist.

**What it performs:**

* Creates `GRBEnv`
* Configures Gurobi Compute Server
* Sets priority
* Sets application name
* Sets thread count
* Starts environment
* Creates `GRBModel`
* Tracks initialization time through telemetry

This is purely **solver/model initialization**, not recommendation logic. 

---

# 3. `GetDecisionVariables()`

```text
input.Slots
    │
    ▼
GetDecisionVariables()
    │
    ├── Normal slot
    │      ↓
    │   Decision Variable
    │
    └── Non-Visit Work
           ↓
       Decision Variable
           +
       Remaining Staff Variable
```

**Why?**
The optimizer needs a variable representing **how much staff to assign to each available slot**.

For each `RecommendationSlotDto`, it creates a `TeamBuilderDecisionVariable`.

It also creates an additional:

```text
{slotKey}-REM
```

variable for **Non-Visit Work** slots.



### Important distinction

Normal visit slot:

```text
Slot
 ↓
Integer decision variable
```

Non-visit-work slot:

```text
Slot
 ↓
Continuous decision variable
 +
Remaining-staff variable
```

---

# 4. `BuildExpression()`

```text
DecisionVariables
       │
       ▼
BuildExpression()
       │
       ├── Add variables to Gurobi
       ├── Build objective
       └── Build ghost-shift expression
```

**Why?**
`GetDecisionVariables()` creates the **description** of the variables.

`BuildExpression()` actually creates the corresponding `GRBVar` inside Gurobi.

For non-visit work, the variable is **not added to the main objective**. For normal work, its coefficient is added to the objective. Ghost variables are also collected into the ghost-shift expression. 

So:

```text
TeamBuilderDecisionVariable
          ↓
      GRBVar
          ↓
    Gurobi Model
```

---

# 5. `BuildConstraintExpressions()` — main V2 calculation

This is the **most important private method in V2**.

```text
BuildConstraintExpressions()
        │
        ├── Get VisitVolumeSource
        │
        ├── Get historical/visit data
        │
        ├── Generate 30-min slots
        │
        ├── Prepare Non-Visit Work constraints
        │
        └── For each time range
                │
                ├── Build slot expressions
                ├── Find assigned providers
                ├── Calculate visit volume
                ├── Calculate provider demand
                ├── Calculate miscellaneous demand
                ├── Apply minimum rule
                ├── Apply assigned rule
                ├── subtract existing staff
                ├── calculate remaining staff
                └── create constraint
```

The method obtains the visit-volume source based on `input.RecommendationType`, retrieves visit data, generates 30-minute ranges, and builds constraint data for every range. 

### Why?

Because the optimizer needs to know:

> **For every 30-minute period, how many people are actually required?**

It calculates that requirement from:

```text
Visit data
+
Provider assignments
+
Ratios
+
Rules
+
Existing staff
+
Non-visit work
```

---

# 6. `BuildConstraintExpressionsForSlots()`

Inside each 30-minute range:

```text
30-min range
     │
     ▼
Find matching slots
     │
     ▼
BuildConstraintExpressionsForSlots()
```

**Why?**

It determines which recommendation slots can actually contribute to that particular time period.

For every matching slot it checks:

```text
Is NonVisitWork?
       │
       ├── YES → add non-visit-work terms
       │
       └── NO
            │
            ├── Ghost?
            │      └── add ghost constraint
            │
            └── Valid working slot?
                   └── add to staffing constraint
```

It also connects non-visit-work slots to the corresponding **remaining staff variable**. 

---

# 7. `GetConstraintValueByVisitType()` — V2 version

There are **two methods with this name** in the file.

For your V2 flow, use the one that accepts:

```csharp
RecommendationInputDto input
```

i.e. the method starting around line 1064.

**Do NOT use the old static version around line 1209 when documenting V2.**

V2 calls:

```csharp
GetConstraintValueByVisitType(
    range,
    assignedProviderDto,
    currentAverages,
    VisitDataType.Provider,
    input.Ratios,
    input.Rules,
    input
);
```

and again with:

```text
VisitDataType.Miscellaneous
```



### What does it do?

It calculates demand grouped by provider specialty.

```text
Providers
   │
   ▼
Group by ProviderSpecialityId
   │
   ▼
Find specialty ratio
   │
   ├── Specialty ratio exists
   │
   └── otherwise office ratio
   │
   ▼
Determine ModelType
   │
   ├── Dynamic
   │      ↓
   │   use visit data
   │
   └── Fixed
          ↓
      ratio × provider count
   │
   ▼
Apply specialty Minimum rule
   │
   ▼
Apply specialty Assigned rule
   │
   ▼
Return:
   Demands
   Assignments
   RemainingStaff
```

The V2 implementation also has a special path for `input.IsAnalyticRun`: dynamic demand can come directly from `input.GetDemand(...)`; otherwise it uses the visit-volume data. 

---

# 8. Remaining-staff calculation

This is one of the important V2 changes.

Inside `BuildConstraintExpressions()`:

```csharp
remainingStaff =
    Math.Max(value, officeMinimum) - value;
```

Meaning:

```text
minimum required
       -
calculated demand
       =
remaining staff available
```

Example:

```text
Demand = 3
Office minimum = 5

Remaining staff = 5 - 3
                = 2
```

Those 2 people can potentially be used for **Non-Visit Work**.

Then:

```text
BuildRemainingStaffTerms()
```

finds the remaining-staff variables associated with non-visit-work slots. 

---

# 9. `BuildRemainingStaffTerms()`

```text
Non-Visit Work Slots
        │
        ▼
BuildRemainingStaffTerms()
        │
        ▼
Find {slotKey}-REM variables
        │
        ▼
Create terms
```

**Why?**

It creates the expression that says:

> The amount of non-visit work assigned in this time period cannot exceed the staff remaining from the minimum staffing requirement.



---

# 10. `AddConstraintsToModel()`

Until this point, V2 is mostly building **data structures representing constraints**.

Then:

```text
constraintExpressions
        │
        ▼
AddConstraintsToModel()
        │
        ▼
GRBLinExpr
        │
        ▼
model.AddConstr(...)
```

It takes each `TeamBuilderTerm`, creates a Gurobi linear expression, and finally adds the constraint to the actual model. 

V2 adds four groups:

```text
1. constraintExpressions
2. ghostConstraintExpressions
3. nonVisitWorkConstraintExpressions
4. remainingStaffConstraintExpressions
```

---

# 11. `SolveModel()`

Now the model is ready:

```text
Variables
   +
Objective
   +
Constraints
   │
   ▼
SolveModel()
```

It performs:

```text
Set Objective = MINIMIZE
        ↓
Set automatic Gurobi method
        ↓
Set timeout
        ↓
Set MIP gap
        ↓
model.Update()
        ↓
model.Optimize()
```

The objective is therefore essentially:

> **Find the minimum staffing assignment that satisfies the constraints.**



---

# 12. `HandleOptimizationResult()`

After Gurobi finishes:

```text
model.Optimize()
       │
       ▼
HandleOptimizationResult()
```

Possible paths:

```text
OPTIMAL
   ↓
Continue

TIME_LIMIT
   ↓
Continue

INFEASIBLE
   ↓
Compute IIS
   ↓
Return problematic constraints

INF_OR_UNBD
   ↓
Compute IIS
   ↓
Return problematic constraints

UNBOUNDED
   ↓
Throw exception

Other status
   ↓
Throw exception
```



---

# 13. `MapDecisionVariablesToRecommendationItems()`

Once Gurobi has solved:

```text
GRBVar.X
   │
   ▼
MapDecisionVariablesToRecommendationItems()
   │
   ▼
RecommendationItemDto
```

This converts the mathematical solution into the API's business object.

For example conceptually:

```text
Slot 09:00-09:30
Gurobi result = 2
        ↓
RecommendationItemDto
{
    AvailabilityRange = 09:00-09:30,
    Result = 2
}
```

It uses `Parallel.ForEach` to perform the mapping and rounds non-visit work differently from normal slots. 

---

## Final V2 architecture

If you're creating a technical flow/documentation, I'd show **only this**:

```text
                 ┌───────────────────────────┐
                 │ RecommendationsV2Async    │
                 └─────────────┬─────────────┘
                               │
                               ▼
                 ┌───────────────────────────┐
                 │ InitializeGurobiModel     │
                 │ Create Env + Model        │
                 └─────────────┬─────────────┘
                               │
                               ▼
                 ┌───────────────────────────┐
                 │ GetDecisionVariables      │
                 │ Create logical variables  │
                 └─────────────┬─────────────┘
                               │
                               ▼
                 ┌───────────────────────────┐
                 │ BuildExpression            │
                 │ Variables + Objective      │
                 └─────────────┬─────────────┘
                               │
                               ▼
              ┌────────────────────────────────┐
              │ BuildConstraintExpressions      │
              └───────────────┬────────────────┘
                              │
             ┌────────────────┼────────────────┐
             ▼                ▼                ▼
     Visit Volume       Slot Constraints   Demand Calculation
     Source             │                  │
                        │                  └── Provider
                        │                  └── Miscellaneous
                        │                  └── Ratios
                        │                  └── Rules
                        │
                        └── BuildConstraintExpressionsForSlots
                                      │
                                      ▼
                             BuildRemainingStaffTerms
                             
                             
              ┌────────────────────────────────┐
              │ AddConstraintsToModel          │
              └───────────────┬────────────────┘
                              │
                              ▼
                    ┌───────────────────┐
                    │    SolveModel      │
                    │    MINIMIZE        │
                    └─────────┬─────────┘
                              │
                              ▼
                 ┌───────────────────────────┐
                 │ HandleOptimizationResult │
                 └─────────────┬─────────────┘
                               │
                               ▼
             ┌─────────────────────────────────┐
             │ MapDecisionVariablesTo...       │
             │ Gurobi solution → DTO            │
             └───────────────┬─────────────────┘
                             │
                             ▼
                   ┌────────────────────┐
                   │ RecommendationDto  │
                   │ FTE + Recommendations
                   │ Status + Gap        │
                   └────────────────────┘
```

### In one sentence

**V2 takes provider/slot/visit/rule information → creates optimization variables → calculates staffing constraints for every 30-minute period → gives those constraints to Gurobi → minimizes required staffing → converts Gurobi's solution back into recommendations.**

For your documentation, I would **exclude the old `RecommendationsAsync` and the old static `GetConstraintValueByVisitType` completely**, because they are part of the V1 path, not the V2 execution flow.
