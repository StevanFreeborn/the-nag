using TheNag.Terminal.Evaluation;

namespace TheNag.Terminal.Examples.ControlMapping;

internal sealed class ControlMappingScenario : IScenario<MappingResult>
{
  public string Name => nameof(ControlMappingScenario);
  public string InitialPrompt => "yo heres a policy. I need to know if it helps me become iso compliant.";
  public double TargetScore => 95.0;
  public int MaxIterations => 5;

  public string GetMetaPrompt(string currentPrompt, string errorLog) => $@"
    You are an expert Prompt Engineer specializing in high-precision compliance analysis.
    
    CRITICAL: Make the prompt GENERIC and TRANSFERABLE across different policies and compliance frameworks.
    - Do NOT mention specific control IDs, policy names, or content details
    - Do NOT create checklists for individual controls  
    - Focus on improving the REASONING PROCESS and EVIDENCE REQUIREMENTS
    - The improved prompt must work for ANY policy against ANY compliance framework
    
    GOAL:
    Analyze the error logs from multiple policy evaluations and improve the prompt to eliminate common patterns of errors.
    
    RULES FOR REFINEMENT:
    1. DO NOT include specific answers or reference specific policy content
    2. Focus on improving Instructions, Constraints, and Thinking Process
    3. If errors show missing data, improve instructions on systematic evidence search
    4. If errors show hallucinations, strengthen requirements for verbatim evidence
    5. Keep the prompt concise and avoid over-specification

    CURRENT PROMPT TO IMPROVE:
    ""{currentPrompt}""

    ERROR LOGS FROM MULTIPLE POLICIES:
    {errorLog}

    Output ONLY the text of the new, improved prompt. Do not include conversational filler.";

  public IReadOnlyList<ITestCase<MappingResult>> TrainingCases { get; } = [
    new TestCase<MappingResult>(
      name: "Access Control Policy",
      context: new PolicyContext
      {
        PolicyName = "Access Control Policy: ACP-2026-v1",
        Content = @"
          1. Purpose: Establish requirements for granting, reviewing, and Revoking access. Ensures Least Privilege and Need to Know.
          2. Scope: All employees, contractors, and third-party vendors.
          3. Policy Statements:
            3.1 User Registration and De-registration:
              - Formal request via IT Ticketing System and Dept Head approval.
              - HR notifies IT of terminations within 4 hours; IT disables accounts immediately.
              - Guest accounts restricted to 24 hours with CISO authorization.
            3.2 Authentication Management:
              - MFA mandatory for all remote and administrative accounts.
              - Passwords: 14+ characters, 3/4 complexity rules.
              - Admin passwords rotate every 90 days.
            3.3 Access Provisioning:
                - RBAC assigned by job function.
                - Privileged access (root) requires PAM tool and session logging.
            3.4 Access Review:
              - Quarterly Audit: Dept heads review subordinates every 90 days.
              - Stale Accounts: Disabled automatically after 60 days.
            3.5 Physical Access:
              - Server room restricted via biometrics.
              - Visitors escorted at all times and must sign a visitor log."
      },
      groundTruth: new MappingResult
      {
        Evaluations = [
          new() { ControlId = "A.5.15", Status = ComplianceStatus.Compliant, Quote = "Section 3.1: Formal request via IT Ticketing and Dept Head approval." },
          new() { ControlId = "A.5.16", Status = ComplianceStatus.Compliant, Quote = "Section 3.1: Covers registration, de-registration (4hr window), and guest accounts." },
          new() { ControlId = "A.5.17", Status = ComplianceStatus.Compliant, Quote = "Section 3.2: Specifically defines password complexity (14 chars) and MFA." },
          new() { ControlId = "A.5.18", Status = ComplianceStatus.Compliant, Quote = "Section 3.4: Mandates quarterly (90-day) reviews by department heads." },
          new() { ControlId = "A.5.33", Status = ComplianceStatus.Partial, Quote = "Section 3.5: Mentions visitor logs, but lacks broad record retention or protection rules." },
          new() { ControlId = "A.7.2", Status = ComplianceStatus.Gap, Quote = "No mention. The policy does not address pre-employment screening or background checks." },
          new() { ControlId = "A.8.2", Status = ComplianceStatus.Compliant, Quote = "Section 3.3: Requires PAM tool for production/root and mandates session logging." },
          new() { ControlId = "A.8.3", Status = ComplianceStatus.Compliant, Quote = "Section 1 & 3.3: Explicitly cites 'Least Privilege' and 'Need to Know' principles." },
          new() { ControlId = "A.8.5", Status = ComplianceStatus.Compliant, Quote = "Section 3.2: Mandates MFA for remote and administrative accounts." },
          new() { ControlId = "A.8.10", Status = ComplianceStatus.Gap, Quote = "No mention. The policy covers disabling accounts but is silent on the deletion of data/media." }
        ]
      }
    )
  ];

  public IReadOnlyList<ITestCase<MappingResult>> ValidationCases { get; } = [
    new TestCase<MappingResult>(
      name: "Encryption Policy",
      context: new PolicyContext
      {
        PolicyName = "Encryption Policy: ENC-2026-v2",
        Content = @"
          1. Purpose: Define encryption requirements for data at rest and in transit.
          2. Scope: All systems, applications, and data repositories.
          3. Policy Statements:
            3.1 Data at Rest:
              - All sensitive data must be encrypted using AES-256 or stronger.
              - Encryption keys managed via dedicated Key Management Service (KMS).
              - Database encryption mandatory for PII and financial records.
            3.2 Data in Transit:
              - TLS 1.2 or higher required for all external communications.
              - Internal network traffic between data centers encrypted via IPsec.
            3.3 Key Management:
              - Keys rotated annually.
              - Access to KMS restricted to authorized personnel with MFA.
              - Key backup stored in secure offline vault."
      },
      groundTruth: new MappingResult
      {
        Evaluations = [
          new() { ControlId = "A.5.15", Status = ComplianceStatus.Gap, Quote = "No mention. Policy does not address identity provisioning processes." },
          new() { ControlId = "A.5.16", Status = ComplianceStatus.Gap, Quote = "No mention. Policy focuses on encryption, not identity lifecycle." },
          new() { ControlId = "A.5.17", Status = ComplianceStatus.Partial, Quote = "Section 3.3: Requires MFA for KMS access, but no general authentication requirements." },
          new() { ControlId = "A.5.18", Status = ComplianceStatus.Gap, Quote = "No mention. No access review procedures defined." },
          new() { ControlId = "A.5.33", Status = ComplianceStatus.Gap, Quote = "No mention. No information records management addressed." },
          new() { ControlId = "A.7.2", Status = ComplianceStatus.Gap, Quote = "No mention. Policy does not address HR screening." },
          new() { ControlId = "A.8.2", Status = ComplianceStatus.Partial, Quote = "Section 3.3: Restricts KMS access to authorized personnel, but lacks broader privileged access controls." },
          new() { ControlId = "A.8.3", Status = ComplianceStatus.Gap, Quote = "No mention. Least privilege principle not explicitly stated." },
          new() { ControlId = "A.8.5", Status = ComplianceStatus.Partial, Quote = "Section 3.3: MFA required for KMS, but not mandated system-wide." },
          new() { ControlId = "A.8.10", Status = ComplianceStatus.Gap, Quote = "No mention. Policy covers encryption keys but not general information deletion." }
        ]
      }
    )
  ];

  public IJudge<MappingResult> GetJudge() => new ComplianceJudge();

  private readonly List<Iteration<MappingResult>> _history = [];
  public IReadOnlyList<Iteration<MappingResult>> History => _history;
  public void AddIteration(Iteration<MappingResult> iteration) => _history.Add(iteration);

  private ControlMappingScenario()
  {
  }

  public static ControlMappingScenario New() => new();
}