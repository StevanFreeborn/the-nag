# The Nag

This is an experimental exploration of using AI to optimize AI prompts through iterative evaluation and refinement.

## Overview

This project explores a question: *What if we used LLMs to automatically improve prompts instead of manually iterating?*

The Nag is a proof-of-concept that demonstrates one possible approach to this problem:

1. Running an initial (possibly poorly-written) prompt against an AI model
2. Evaluating the output against a known "ground truth" result  
3. Using AI to analyze what went wrong and generate an improved prompt
4. Repeating until the target accuracy is achieved or maximum iterations are reached

The experiment persists detailed session reports showing how prompts evolve and whether accuracy improves across iterations.

## How It Works

### Scenario-Based Architecture

The Nag organizes experiments around **scenarios** - self-contained units that bundle everything needed for an optimization run:

- **IScenario<TResult>**: Encapsulates all components of an experiment:
  - `InitialPrompt`: The starting (possibly poor) prompt
  - `Context`: Domain-specific information (policy document, data, etc.)
  - `GroundTruth`: The expected/correct result for evaluation
  - `GetJudge()`: Returns a custom evaluator for this scenario
  - `TargetScore`: Accuracy threshold to achieve
  - `History`: Tracks iterations for reporting

### Core Components

- **Optimizer**: Orchestrates the iterative optimization loop
- **GeminiService**: Wrapper for Google Gemini API interactions
  - `GetStructuredResponseAsync()`: Gets JSON responses based on a schema
  - `RefinePromptAsync()`: Meta-prompts the AI to improve the prompt based on errors
- **IJudge<TResult>**: Custom evaluation interface for scoring AI outputs against ground truth
- **ITaskContext**: Provides context information to the AI model

### Optimization Loop

```txt
Scenario
   ├─ Initial Prompt → AI Response → Judge Evaluation → Score
   ├─ Context            ↑                                  ↓
   ├─ Ground Truth       └───── Refined Prompt ←────────────┘
   └─ Judge
```

For each iteration:

1. Send the current prompt + context to Gemini
2. Parse the structured JSON response matching the result schema
3. Evaluate against ground truth using the scenario's Judge
4. If score < target, use Gemini to refine the prompt based on errors
5. Add iteration to scenario history
6. Track best-performing prompt throughout the process

## What This Explores

- Can an LLM effectively critique and improve its own prompts?
- Does prompt quality consistently improve over iterations?
- Using JSON schemas to enforce predictable output formats
- Can this pattern work across different domains and result types?
- Tracking prompt evolution through detailed session reports

## Session Reports

After each optimization run, a session report is saved to:

```txt
bin/Debug/net11.0/sessions/YYYYMMDD_HHMMSS/report.md
```

Reports include:

- Final success status
- Total iterations performed
- For each iteration:
  - Score achieved
  - Prompt used
  - Issues identified by the Judge
  - Raw AI response

## Potential Applications

This approach might be interesting for scenarios where:

- You have a clear ground truth or expected output to evaluate against
- You want to experiment with automated prompt engineering
- You need structured, schema-compliant responses from AI models
- You're curious about how prompts can evolve to improve accuracy

The scenario pattern could apply to:

- Data extraction and classification tasks
- Policy interpretation and analysis
- Structured information retrieval
- Any domain-specific AI task with measurable outputs

## Observations & Learnings

Some interesting questions this experiment raises:

- How effective is AI at critiquing its own prompt quality?
- Does this approach generalize across different task types?
- At what point do diminishing returns set in?
- Is automated refinement faster/better than human prompt engineering?

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.md) file for details.
