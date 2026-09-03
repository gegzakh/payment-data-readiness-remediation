import { apiGet, apiPost, apiPut } from './client';
import type { PagedResult } from './releases';

export type ScenarioMode = 'Current' | 'Future' | 'Remediated';
export type ScenarioStatus = 'Draft' | 'Locked' | 'Archived';
export type RunStatus = 'Running' | 'Completed' | 'Failed';
export type BreakdownDimension = 'Scheme' | 'Source' | 'Country' | 'PartyRole' | 'Issue';
export type TestRisk = 'Low' | 'Medium' | 'High' | 'Critical';
export type TestExecutionStatus = 'NotRun' | 'Passed' | 'Failed' | 'Blocked';
export type PlanStatus = 'Draft' | 'Active' | 'Closed';
export type UatOutcome = 'NotCompared' | 'Match' | 'Mismatch';
export type CriterionKind = 'Entry' | 'Exit';
export type CriterionStatus = 'Pending' | 'Met' | 'Waived' | 'Failed';
export type ApprovalDecision = 'Approved' | 'Rejected';
export type GoNoGoRecommendation = 'Go' | 'GoWithConditions' | 'NoGo';

export const scenarioModes: ScenarioMode[] = ['Current', 'Future', 'Remediated'];
export const testRisks: TestRisk[] = ['Low', 'Medium', 'High', 'Critical'];
export const executionStatuses: TestExecutionStatus[] = ['NotRun', 'Passed', 'Failed', 'Blocked'];
export const criterionStatuses: CriterionStatus[] = ['Pending', 'Met', 'Waived', 'Failed'];

export interface ScenarioDto {
  id: string;
  code: string;
  name: string;
  mode: ScenarioMode;
  asOf: string;
  schemeCodes?: string | null;
  sourceCodes?: string | null;
  countries?: string | null;
  partyRoles?: string | null;
  exclusions?: string | null;
  rulesetVersion?: string | null;
  description?: string | null;
  status: ScenarioStatus;
  runCount: number;
  lastRunAtUtc?: string | null;
}

export interface SimulationBreakdownDto {
  dimension: BreakdownDimension;
  key: string;
  recordCount: number;
  rejectedCount: number;
  warningCount: number;
  paymentsAtRisk: number;
  readinessPercent: number;
}

export interface SimulationRunDto {
  id: string;
  scenarioId: string;
  scenarioCode: string;
  mode: ScenarioMode;
  asOf: string;
  runKey: string;
  requestedBy: string;
  status: RunStatus;
  populationCount: number;
  assessedCount: number;
  excludedCount: number;
  unableToAssessCount: number;
  rejectedCount: number;
  warningCount: number;
  paymentsAtRisk: number;
  readinessPercent: number;
  reconciles: boolean;
  rulesetVersion?: string | null;
  failureReason?: string | null;
  startedAtUtc: string;
  completedAtUtc?: string | null;
  breakdown: SimulationBreakdownDto[];
}

export interface ComparisonRowDto {
  dimension: BreakdownDimension;
  key: string;
  baselineRejected: number;
  candidateRejected: number;
  rejectedDelta: number;
}

export interface RunComparisonDto {
  baseline: SimulationRunDto;
  candidate: SimulationRunDto;
  sameRunKey: boolean;
  rejectedDelta: number;
  paymentsAtRiskDelta: number;
  readinessDelta: number;
  rows: ComparisonRowDto[];
}

export interface CreateScenarioRequest {
  code: string;
  name: string;
  mode: ScenarioMode;
  asOf: string;
  schemeCodes?: string | null;
  sourceCodes?: string | null;
  countries?: string | null;
  partyRoles?: string | null;
  exclusions?: string | null;
  rulesetVersion?: string | null;
  description?: string | null;
}

export const getScenarios = () => apiGet<ScenarioDto[]>('/api/v1/simulation/scenarios');
export const createScenario = (request: CreateScenarioRequest) =>
  apiPost<ScenarioDto>('/api/v1/simulation/scenarios', request);
export const lockScenario = (code: string) =>
  apiPost<ScenarioDto>(`/api/v1/simulation/scenarios/${code}/lock`);
export const archiveScenario = (code: string) =>
  apiPost<ScenarioDto>(`/api/v1/simulation/scenarios/${code}/archive`);
export const runScenario = (code: string) =>
  apiPost<SimulationRunDto>(`/api/v1/simulation/scenarios/${code}/run`);

export const getRuns = (scenarioCode: string | undefined, page: number) =>
  apiGet<PagedResult<SimulationRunDto>>(
    `/api/v1/simulation/runs?page=${page}${scenarioCode ? `&scenarioCode=${encodeURIComponent(scenarioCode)}` : ''}`,
  );
export const getRun = (id: string) => apiGet<SimulationRunDto>(`/api/v1/simulation/runs/${id}`);
export const compareRuns = (baselineId: string, candidateId: string) =>
  apiGet<RunComparisonDto>(
    `/api/v1/simulation/runs/compare?baselineId=${baselineId}&candidateId=${candidateId}`,
  );

export interface TestCaseDto {
  id: string;
  reference: string;
  title: string;
  risk: TestRisk;
  scenarioCode?: string | null;
  sampleReference?: string | null;
  expectedResult: string;
  status: TestExecutionStatus;
  actualResult?: string | null;
  evidenceReference?: string | null;
  defectReference?: string | null;
  executedBy?: string | null;
  executedAtUtc?: string | null;
  executionCount: number;
  isRetested: boolean;
  uatOutcome: UatOutcome;
  engineOutcome?: string | null;
  platformOutcome?: string | null;
  uatExplanation?: string | null;
  reconciledAtUtc?: string | null;
}

export interface TestPlanDto {
  id: string;
  code: string;
  name: string;
  owner: string;
  scope?: string | null;
  description?: string | null;
  status: PlanStatus;
  caseCount: number;
  passedCount: number;
  failedCount: number;
  blockedCount: number;
  notRunCount: number;
  openDefectCount: number;
  uatMismatchCount: number;
  riskWeightedCoveragePercent: number;
  cases: TestCaseDto[];
}

export const getTestPlans = () => apiGet<TestPlanDto[]>('/api/v1/simulation/test-plans');
export const getTestPlan = (code: string) => apiGet<TestPlanDto>(`/api/v1/simulation/test-plans/${code}`);
export const createTestPlan = (request: {
  code: string;
  name: string;
  owner: string;
  scope?: string | null;
  description?: string | null;
}) => apiPost<TestPlanDto>('/api/v1/simulation/test-plans', request);
export const addTestCase = (
  code: string,
  request: {
    reference: string;
    title: string;
    risk: TestRisk;
    scenarioCode?: string | null;
    sampleReference?: string | null;
    expectedResult: string;
  },
) => apiPost<TestPlanDto>(`/api/v1/simulation/test-plans/${code}/cases`, request);
export const activateTestPlan = (code: string) =>
  apiPost<TestPlanDto>(`/api/v1/simulation/test-plans/${code}/activate`);
export const closeTestPlan = (code: string) =>
  apiPost<TestPlanDto>(`/api/v1/simulation/test-plans/${code}/close`);
export const recordExecution = (
  code: string,
  reference: string,
  request: {
    status: TestExecutionStatus;
    actualResult: string;
    evidenceReference?: string | null;
    defectReference?: string | null;
  },
) => apiPost<TestPlanDto>(`/api/v1/simulation/test-plans/${code}/cases/${reference}/execution`, request);
export const recordUat = (
  code: string,
  reference: string,
  request: { engineOutcome: string; platformOutcome: string; explanation?: string | null },
) => apiPost<TestPlanDto>(`/api/v1/simulation/test-plans/${code}/cases/${reference}/uat`, request);

export interface CriterionDto {
  id: string;
  reference: string;
  kind: CriterionKind;
  description: string;
  owner: string;
  isBlocking: boolean;
  status: CriterionStatus;
  evidenceReference?: string | null;
  rationale?: string | null;
  recordedBy?: string | null;
  recordedAtUtc?: string | null;
}

export interface ApprovalDto {
  id: string;
  role: string;
  approver: string;
  decision: ApprovalDecision;
  rationale: string;
  recommendationAtSignOff: GoNoGoRecommendation;
  decidedAtUtc: string;
}

export interface CutoverPlanDto {
  id: string;
  code: string;
  name: string;
  cutoverDate: string;
  owner: string;
  freezeFrom?: string | null;
  freezeTo?: string | null;
  isFrozen: boolean;
  fallbackPlan?: string | null;
  supportModel?: string | null;
  criteria: CriterionDto[];
  approvals: ApprovalDto[];
}

export interface GoNoGoPackDto {
  plan: CutoverPlanDto;
  recommendation: GoNoGoRecommendation;
  residualExposure: number;
  residualExposureTolerance: number;
  paymentsAtRisk: number;
  openCases: number;
  expiredExceptions: number;
  openDefects: number;
  testCoveragePercent: number;
  uatMismatches: number;
  entryCriteriaOutstanding: number;
  exitCriteriaOutstanding: number;
  waivedCriteria: number;
  basedOnRunId?: string | null;
  basedOnRunAtUtc?: string | null;
  generatedAtUtc: string;
}

export const getCutoverPlans = () => apiGet<CutoverPlanDto[]>('/api/v1/simulation/cutover');
export const getGoNoGoPack = (code: string) =>
  apiGet<GoNoGoPackDto>(`/api/v1/simulation/cutover/${code}/go-no-go`);
export const createCutoverPlan = (request: {
  code: string;
  name: string;
  cutoverDate: string;
  owner: string;
}) => apiPost<CutoverPlanDto>('/api/v1/simulation/cutover', request);
export const setOperationalPlan = (
  code: string,
  request: {
    freezeFrom?: string | null;
    freezeTo?: string | null;
    fallbackPlan?: string | null;
    supportModel?: string | null;
  },
) => apiPut<CutoverPlanDto>(`/api/v1/simulation/cutover/${code}/operations`, request);
export const addCriterion = (
  code: string,
  request: {
    reference: string;
    kind: CriterionKind;
    description: string;
    owner: string;
    isBlocking: boolean;
  },
) => apiPost<CutoverPlanDto>(`/api/v1/simulation/cutover/${code}/criteria`, request);
export const recordCriterion = (
  code: string,
  reference: string,
  request: { status: CriterionStatus; evidenceReference?: string | null; rationale?: string | null },
) => apiPost<CutoverPlanDto>(`/api/v1/simulation/cutover/${code}/criteria/${reference}/status`, request);
export const approveCutover = (
  code: string,
  request: { role: string; decision: ApprovalDecision; rationale: string },
) => apiPost<CutoverPlanDto>(`/api/v1/simulation/cutover/${code}/approvals`, request);
