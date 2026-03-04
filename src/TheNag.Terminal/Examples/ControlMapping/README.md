# Control Mapping Example

This example demonstrates using `The Nag` to optimize prompts for **compliance control mapping** - specifically, mapping an organization's access control policy to ISO 27001 controls.

## The Problem

Given an internal policy document, a compliance analyst needs to:

1. Identify which ISO 27001 controls the policy addresses
2. Determine the compliance status for each control (Compliant, Partial, or Gap)
3. Provide verbatim evidence from the policy to support each determination

This is tedious, requires domain expertise, and is prone to inconsistency. But what if we could use AI to do this mapping? And what if we could optimize the prompt to get more accurate results?

## The Experiment

This example starts with an intentionally vague prompt:

```txt
"yo heres a policy. I need to know if it helps me become iso compliant."
```

Then watches as TheNag iteratively refines it to produce more accurate control mappings.

## The Domain Model

### PolicyContext

Provides the policy document to analyze:

- `PolicyName`: The name/identifier of the policy
- `Content`: The full text of the policy being evaluated

### MappingResult

The structured output containing:

- `Evaluations`: A list of control evaluations

### ControlEvaluation

Each evaluation assesses one control:

- `ControlId`: The ISO control identifier (e.g., "A.5.15")
- `Status`: One of three values:
  - `"Compliant"`: Policy fully addresses the control
  - `"Partial"`: Policy partially addresses the control
  - `"Gap"`: Policy does not address the control
- `Quote`: Verbatim text from the policy supporting the assessment
- `Reasoning`: Explanation of why this status was assigned

### ComplianceJudge

Evaluates AI output against the ground truth using a scoring rubric:

**Per control (10 points each):**

- **7 points**: Correct compliance status (Compliant/Partial/Gap)
- **3 points**: Provided a meaningful verbatim quote (>10 characters)
- **0 points**: Control was not analyzed at all

**Target score**: 95% (95 out of 100 points across 10 controls)

## Example Ground Truth

The demo uses an Access Control Policy and evaluates against 10 ISO 27001 controls:

| Control | Status    | Section Referenced                            |
|---------|-----------|-----------------------------------------------|
| A.5.15  | Compliant | User registration via formal approval process |
| A.5.16  | Compliant | De-registration process (4hr window)          |
| A.5.17  | Compliant | Password complexity (14+ chars, MFA)          |
| A.5.18  | Compliant | Quarterly access reviews (90 days)            |
| A.5.33  | Partial   | Visitor logs exist, but no retention rules    |
| A.7.2   | Gap       | No pre-employment screening mentioned         |
| A.8.2   | Compliant | PAM tool and session logging required         |
| A.8.3   | Compliant | Least Privilege principle explicitly stated   |
| A.8.5   | Compliant | MFA for remote/admin accounts                 |
| A.8.10  | Gap       | No data/media deletion procedures             |

## What Gets Optimized?

The prompt refinement aims to teach the AI to:

- **Systematically analyze** each control rather than skipping some
- **Correctly classify** compliance status based on policy content
- **Provide evidence** by quoting directly from the source document
- **Recognize gaps** when the policy is silent on a control
- **Distinguish** between full compliance and partial coverage

## Interesting Questions

- Does the optimized prompt work for other policies and control frameworks?
- How much does the quality of the "ground truth" affect refinement?
- Could this approach scale to hundreds of controls?
- Would a domain expert write the final prompt differently?
- Is this faster/more accurate than manual prompt engineering?

## Running This Example

The example runs automatically when you start the application. It will:

1. Load the Access Control Policy as context
2. Start with the vague initial prompt
3. Iterate up to 5 times or until 95% accuracy is achieved
4. Output the optimized prompt and save a detailed session report

Check `bin/Debug/net11.0/sessions/[timestamp]/report.md` to see how the prompt evolved.
