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
  | 'Completed';

export type IntakeChannel = 'Portal' | 'Email' | 'SmsChat' | 'PhoneNote';

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
