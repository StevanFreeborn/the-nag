# The Nag

This is an experimental exploration of using AI to optimize AI prompts through iterative evaluation and refinement.

## Overview

This project explores a question: *What if we used LLMs to automatically improve prompts instead of manually iterating?*

The Nag is a proof-of-concept that demonstrates one possible approach to this problem:

1. Start with an intentionally bad prompt
2. Run it against multiple test cases, each with its own context and ground truth
3. Score the AI output using a domain-specific judge
4. Feed the error logs into a meta-prompt that rewrites the prompt
5. Repeat until the target accuracy is achieved or maximum iterations are reached

The system uses a **training/validation split** — training cases drive prompt refinement while validation cases measure whether improvements generalize to unseen inputs. Detailed session reports are persisted after each run showing how prompts evolve across iterations.

## How It Works

### Scenario-Based Architecture

The Nag organizes experiments around **scenarios** — self-contained units that bundle everything needed for an optimization run:

- **IScenario<TResult>**: Encapsulates all components of an experiment:
  - `InitialPrompt`: The starting (intentionally poor) prompt
  - `TrainingCases`: Test cases used to drive prompt refinement
  - `ValidationCases`: Held-out test cases to measure generalization
  - `GetJudge()`: Returns a custom evaluator for this scenario
  - `GetMetaPrompt()`: Builds the refinement prompt from the current prompt and error logs
  - `TargetScore`: Accuracy threshold to achieve
  - `MaxIterations`: Upper bound on refinement cycles
  - `History`: Tracks all iterations for reporting

- **ITestCase<TResult>**: Each test case carries its own:
  - `Name`: Descriptive label
  - `Context`: Domain-specific input (e.g. a policy document)
  - `GroundTruth`: The expected result for scoring

### Core Components

- **Optimizer**: Orchestrates the iterative optimization loop across training and validation sets
- **GeminiService**: Wrapper for Google Gemini API interactions
  - `GetStructuredResponseAsync()`: Gets structured JSON responses using `gemini-2.5-flash`
  - `RefinePromptAsync()`: Meta-prompts `gemini-2.5-pro` to improve the prompt based on error logs
- **IJudge<TResult>**: Domain-specific evaluation interface for scoring AI outputs against ground truth. Automatically derives a JSON schema from `TResult` to constrain AI output.
- **ITaskContext**: Provides context information to the AI model

### Optimization Loop

```txt
                    ┌─────────────────────────────────────────────────────────┐
                    │            For each iteration:                          │
                    │                                                         │
  Training Cases ──▶│  Prompt + Context ──▶ AI ──▶ Judge ──▶ Score            │
                    │                                    │                    │
Validation Cases ──▶│  Prompt + Context ──▶ AI ──▶ Judge ──▶ Score            │
                    │                                    │                    │
                    │  Combined Error Logs ──▶ Meta-Prompt ──▶ Refined Prompt │
                    │                                                         │
                    └─────────────────────────────────────────────────────────┘
```

For each iteration:

1. Evaluate all **training cases** — send prompt + each case's context to Gemini, score against ground truth
2. Evaluate all **validation cases** — same process, but results don't influence refinement
3. If training score < target, combine error logs from all training cases and pass them to the **meta-prompt**
4. The meta-prompt (powered by Gemini Pro) analyzes error patterns and rewrites the prompt
5. Track the best-performing prompt across all iterations

## Session Reports

After each optimization run, a session report is saved to:

```txt
bin/Debug/net11.0/sessions/YYYYMMDD_HHMMSS/report.md
```

Reports include:

- Final success/incomplete status
- Total iterations and best training/validation scores
- Initial and final optimized prompts
- For each iteration:
  - Training and validation scores
  - Prompt used
  - Per-test-case results with issues and raw AI responses

## What This Explores

- Can an LLM effectively critique and improve its own prompts?
- Does a training/validation split help detect overfitting to specific test cases?
- Does prompt quality consistently improve over iterations?
- Can this pattern generalize across different domains and result types?
- Using JSON schemas to enforce predictable output formats

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.md) file for details.
