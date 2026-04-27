export type MaintenanceRequestCategory =
  | 'Plumbing'
  | 'Electrical'
  | 'HVAC'
  | 'Appliances'
  | 'Security'
  | 'General';

export type MaintenanceRequestPriority = 'Low' | 'Normal' | 'High' | 'Emergency';

export type MaintenanceRequestStatus =
  | 'New'
  | 'InReview'
  | 'Scheduled'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled';
export type MaintenanceDispatchOutcome =
  | 'Completed'
  | 'Escalated'
  | 'Duplicate'
  | 'Cancelled'
  | 'NoAccess'
  | 'VendorUnavailable'
  | 'TenantResolved'
  | 'NotMaintenance';

export type IntakeChannel = 'Portal' | 'Email' | 'SmsChat' | 'PhoneNote';
export type ConnectorChannel = Exclude<IntakeChannel, 'Portal'>;

export interface MaintenanceRequest {
  id: string;
  referenceNumber: string;
  submitterName: string;
  emailAddress: string;
  phoneNumber: string;
  propertyName: string;
  unitNumber: string;
  description: string;
  internalSummary: string;
  assignedTeam: string;
  category: MaintenanceRequestCategory;
  priority: MaintenanceRequestPriority;
  status: MaintenanceRequestStatus;
  channel: IntakeChannel;
  submittedAtUtc: string;
  targetResponseByUtc: string;
}

export interface DashboardOverview {
  openRequests: number;
  urgentRequests: number;
  todaySubmissions: number;
  averageResponseHours: number;
  recentRequests: MaintenanceRequest[];
}

export interface StandardizedIntakePayload {
  channel: ConnectorChannel;
  sourceReference: string;
  receivedAtUtc: string;
  submitterName: string;
  tenantName: string;
  emailAddress: string;
  phoneNumber: string;
  propertyName: string;
  unitNumber: string;
  subject: string;
  rawContent: string;
  normalizedContent: string;
  category: MaintenanceRequestCategory;
  priority: MaintenanceRequestPriority;
  isAfterHours: boolean;
  metadataMatched: boolean;
}

export interface IntakeSubmission {
  id: string;
  standardizedPayload: StandardizedIntakePayload;
  maintenanceRequestId: string;
  maintenanceRequestReferenceNumber: string;
}

export interface IntakeIngestionResult {
  submission: IntakeSubmission;
  maintenanceRequest: MaintenanceRequest;
}

export interface CreateMaintenanceRequestPayload {
  submitterName: string;
  emailAddress: string;
  phoneNumber: string;
  propertyName: string;
  unitNumber: string;
  description: string;
  category: MaintenanceRequestCategory;
  priority: MaintenanceRequestPriority;
  channel: IntakeChannel;
}

export interface MaintenanceTriageOutputContract {
  category: MaintenanceRequestCategory;
  priority: MaintenanceRequestPriority;
  vendorType: string;
  dispatchDecision: string;
  internalSummary: string;
  tenantResponseDraft: string;
}

export interface MaintenanceTriageGuardrailIssue {
  code: string;
  severity: 'Warning' | 'Blocking';
  message: string;
}

export interface MaintenanceTriageGuardrailResult {
  schemaValid: boolean;
  policyPassed: boolean;
  emergencyKeywordCheckPassed: boolean;
  confidenceScore: number;
  confidenceThreshold: number;
  requiresHumanReview: boolean;
  fallbackApplied: boolean;
  issues: MaintenanceTriageGuardrailIssue[];
}

export interface MaintenanceTriageInferenceResult {
  rulesVersion: string;
  outputContract: MaintenanceTriageOutputContract;
  guardrails: MaintenanceTriageGuardrailResult;
  inferenceMetadata: {
    providerMode: string;
    modelName: string;
  };
}

export interface SubmitMaintenanceTriageReviewPayload {
  aiOutput: MaintenanceTriageOutputContract;
  guardrails: MaintenanceTriageGuardrailResult;
  category: MaintenanceRequestCategory;
  priority: MaintenanceRequestPriority;
  vendorType: string;
  dispatchDecision: string;
  internalSummary: string;
  tenantResponseDraft: string;
}

export interface MaintenanceTriageReview {
  id: string;
  maintenanceRequestId: string;
  aiCategory: MaintenanceRequestCategory;
  aiPriority: MaintenanceRequestPriority;
  aiVendorType: string;
  aiDispatchDecision: string;
  aiInternalSummary: string;
  aiTenantResponseDraft: string;
  finalCategory: MaintenanceRequestCategory;
  finalPriority: MaintenanceRequestPriority;
  finalVendorType: string;
  finalDispatchDecision: string;
  finalInternalSummary: string;
  finalTenantResponseDraft: string;
  guardrailRequiresHumanReview: boolean;
  guardrailSummary: string;
  status: 'Approved' | 'Edited';
  reviewedBy: string;
  reviewedAtUtc: string;
}

export interface MaintenanceOperationalAction {
  id: string;
  maintenanceRequestId: string;
  actionType: 'WorkOrderCreated' | 'VendorAssigned' | 'TenantNotified' | 'InternalNoteLogged';
  detail: string;
  externalReference: string;
  createdBy: string;
  createdAtUtc: string;
}

export interface MaintenanceResolutionFeedback {
  id: string;
  maintenanceRequestId: string;
  maintenanceTriageReviewId: string | null;
  finalResolution: string;
  correctedCategory: MaintenanceRequestCategory;
  correctedPriority: MaintenanceRequestPriority;
  finalTenantResponse: string;
  dispatchOutcome: MaintenanceDispatchOutcome;
  resolutionNotes: string;
  excludeFromTraining: boolean;
  exclusionReason: string;
  resolvedBy: string;
  resolvedAtUtc: string;
}

export interface MaintenanceOperationsDetail {
  request: MaintenanceRequest;
  latestReview: MaintenanceTriageReview | null;
  latestFeedback: MaintenanceResolutionFeedback | null;
  actions: MaintenanceOperationalAction[];
}

export interface SubmitMaintenanceResolutionFeedbackPayload {
  finalResolution: string;
  correctedCategory: MaintenanceRequestCategory;
  correctedPriority: MaintenanceRequestPriority;
  finalTenantResponse: string;
  dispatchOutcome: MaintenanceDispatchOutcome;
  resolutionNotes: string;
  excludeFromTraining: boolean;
  exclusionReason: string;
}

export interface FineTuningExampleCandidate {
  id: string;
  maintenanceRequestId: string;
  maintenanceResolutionFeedbackId: string;
  status: 'Candidate' | 'Approved' | 'Excluded';
  inputSnapshotJson: string;
  outputSnapshotJson: string;
  metadataSnapshotJson: string;
  exclusionReason: string;
  createdAtUtc: string;
}

export interface FineTuningDatasetExport {
  exampleCount: number;
  jsonl: string;
}

export interface IngestEmailPayload {
  submitterName: string;
  emailAddress: string;
  subject: string;
  messageBody: string;
  phoneNumber: string;
  propertyHint: string;
  unitHint: string;
  sourceReference: string;
  receivedAtUtc: string | null;
}

export interface IngestSmsChatPayload {
  submitterName: string;
  phoneNumber: string;
  messageBody: string;
  emailAddress: string;
  propertyHint: string;
  unitHint: string;
  sourceReference: string;
  receivedAtUtc: string | null;
}

export interface IngestPhoneNotePayload {
  submitterName: string;
  phoneNumber: string;
  emailAddress: string;
  note: string;
  propertyHint: string;
  unitHint: string;
  sourceReference: string;
  receivedAtUtc: string | null;
}

export interface LoginPayload {
  email: string;
  password: string;
}

export interface PortalUser {
  id: string;
  fullName: string;
  email: string;
  role: string;
}

export interface PortalSession {
  accessToken: string;
  expiresAtUtc: string;
  user: PortalUser;
}

export type PortalRole =
  | 'PropertyManager'
  | 'Dispatcher'
  | 'Tenant'
  | 'PropertyOwner'
  | 'Vendor';
