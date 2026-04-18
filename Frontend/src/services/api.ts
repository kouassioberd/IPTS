// src/services/api.ts
// ── Central API client — every API call goes through here ─────────

const BASE_URL = "https://localhost:7056/api";

// ── TOKEN HELPERS ─────────────────────────────────────────────────

export const getToken = () => localStorage.getItem("accessToken");
export const getUser = (): StoredUser | null => {
    const u = localStorage.getItem("user");
    return u ? JSON.parse(u) : null;
};
export const saveAuth = (data: AuthResponse) => {
    localStorage.setItem("accessToken", data.accessToken);
    localStorage.setItem("refreshToken", data.refreshToken);
    localStorage.setItem("user", JSON.stringify({
        fullName: data.fullName,
        role: data.role,
        hospitalId: data.hospitalId,
    }));
};
export const clearAuth = () => {
    localStorage.removeItem("accessToken");
    localStorage.removeItem("refreshToken");
    localStorage.removeItem("user");
};

// ── BASE FETCH ────────────────────────────────────────────────────

async function apiFetch<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const token = getToken();

    const res = await fetch(`${BASE_URL}${endpoint}`, {
        ...options,
        headers: {
            "Content-Type": "application/json",
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
            ...options.headers,
        },
    });

    if (!res.ok) {
        const text = await res.text();
        throw new Error(text || `HTTP ${res.status}`);
    }

    if (res.status === 204) return null as T;
    return res.json();
}

// ── AUTH ──────────────────────────────────────────────────────────

export const authApi = {
    login: (email: string, password: string) =>
        apiFetch<AuthResponse>("/Auth/login", {
            method: "POST",
            body: JSON.stringify({ email, password }),
        }),
    me: () => apiFetch<MeResponse>("/Auth/me"),
    logout: (refreshToken: string) =>
        apiFetch("/Auth/logout", {
            method: "POST",
            body: JSON.stringify({ refreshToken }),
        }),
};

// ── HOSPITALS ─────────────────────────────────────────────────────

export const hospitalsApi = {
    getAll: () => apiFetch<HospitalSummaryDto[]>("/Hospitals"),
    getById: (id: string) => apiFetch<HospitalDto>(`/Hospitals/${id}`),
    getDashboard: (id: string) => apiFetch<HospitalDashboardDto>(`/Hospitals/${id}/dashboard`),
    create: (data: CreateHospitalRequest) => apiFetch<HospitalDto>("/Hospitals", { method: "POST", body: JSON.stringify(data) }),
    update: (id: string, data: CreateHospitalRequest) => apiFetch<HospitalDto>(`/Hospitals/${id}`, { method: "PUT", body: JSON.stringify(data) }),
    deactivate: (id: string) => apiFetch(`/Hospitals/${id}`, { method: "DELETE" }),
};

// ── WARDS ─────────────────────────────────────────────────────────

export const wardsApi = {
    getByHospital: (hospitalId: string) => apiFetch<WardDetailDto[]>(`/Wards/hospital/${hospitalId}`),
    getById: (wardId: string) => apiFetch<WardDetailDto>(`/Wards/${wardId}`),
    create: (data: CreateWardRequest) => apiFetch<WardDetailDto>("/Wards", { method: "POST", body: JSON.stringify(data) }),
    update: (id: string, data: UpdateWardRequest) => apiFetch<WardDetailDto>(`/Wards/${id}`, { method: "PUT", body: JSON.stringify(data) }),
    delete: (id: string) => apiFetch(`/Wards/${id}`, { method: "DELETE" }),
};

// ── BEDS ──────────────────────────────────────────────────────────

export const bedsApi = {
    updateStatus: (bedId: string, status: number) =>
        apiFetch<BedSummaryDto>(`/Beds/${bedId}/status`, {
            method: "PATCH",
            body: JSON.stringify({ status }),
        }),
    delete: (bedId: string) =>
        apiFetch(`/Beds/${bedId}`, { method: "DELETE" }),
};

// ── STAFF ─────────────────────────────────────────────────────────

export const staffApi = {
    getAll: () => apiFetch<StaffDto[]>("/Staff"),
    getById: (id: string) => apiFetch<StaffDto>(`/Staff/${id}`),
    create: (data: CreateStaffRequest) => apiFetch("/Staff", { method: "POST", body: JSON.stringify(data) }),
    update: (id: string, data: UpdateStaffRequest) => apiFetch<StaffDto>(`/Staff/${id}`, { method: "PUT", body: JSON.stringify(data) }),
    deactivate: (id: string) => apiFetch(`/Staff/${id}/deactivate`, { method: "PATCH" }),
    reactivate: (id: string) => apiFetch(`/Staff/${id}/reactivate`, { method: "PATCH" }),
    changePassword: (id: string, data: ChangePasswordRequest) =>
        apiFetch(`/Staff/${id}/change-password`, { method: "PATCH", body: JSON.stringify(data) }),
};


// ── TRANSFERS ─────────────────────────────────────────────────────

export const transfersApi = {
    // Phase 1: create anonymous broadcast
    createBroadcast: (data: CreateBroadcastRequest) =>
        apiFetch<BroadcastDto>("/Transfers/broadcast", {
            method: "POST",
            body: JSON.stringify(data),
        }),

    // Phase 2: ranked hospitals
    getMatches: (broadcastId: string) =>
        apiFetch<MatchingResultDto>(`/Transfers/broadcast/${broadcastId}/matches`),

    // notify selected hospitals
    notifyHospitals: (data: NotifyHospitalsRequest) =>
        apiFetch<void>("/Transfers/broadcast/notify", {
            method: "POST",
            body: JSON.stringify(data),
        }),

    // full broadcast with responses (waiting room)
    getById: (id: string) =>
        apiFetch<BroadcastDto>(`/Transfers/broadcast/${id}`),

    // sending doctor list
    getMyBroadcasts: () =>
        apiFetch<BroadcastSummaryDto[]>("/Transfers/my-broadcasts"),

    // receiving doctor anonymous requests
    getIncoming: () =>
        apiFetch<BroadcastSummaryDto[]>("/Transfers/incoming"),

    // accept or decline
    respond: (broadcastId: string, data: RespondToBroadcastRequest) =>
        apiFetch<HospitalResponseDto>(`/Transfers/broadcast/${broadcastId}/respond`, {
            method: "POST",
            body: JSON.stringify(data),
        }),
};

// live form preview (no broadcast created)
export const matchingApi = {
    preview: (data: CreateBroadcastRequest) =>
        apiFetch<MatchingResultDto>("/Matching/preview", {
            method: "POST",
            body: JSON.stringify(data),
        }),
};


// ══════════════════════════════════════════════════════════════════
// TYPES
// ══════════════════════════════════════════════════════════════════

export interface AuthResponse {
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
    fullName: string;
    role: string;
    hospitalId: string;
}

export interface MeResponse {
    userId: string;
    email: string;
    name: string;
    role: string;
    hospitalId: string;
}

export interface StoredUser {
    fullName: string;
    role: string;
    hospitalId: string;
}

// Hospital
export interface HospitalSummaryDto {
    id: string;
    name: string;
    address: string;
    phone: string;
    isActive: boolean;
    totalBeds: number;
    availableBeds: number;
}

export interface HospitalDto extends HospitalSummaryDto {
    latitude: number;
    longitude: number;
    acceptedInsuranceTypes: string;
    wards: WardDetailDto[];
}

export interface HospitalDashboardDto {
    hospitalId: string;
    hospitalName: string;
    totalBeds: number;
    availableBeds: number;
    occupiedBeds: number;
    reservedBeds: number;
    maintenanceBeds: number;
    activeTransfersToday: number;
    avgResponseTimeMinutes: number;
    acceptanceRate: number;
    wards: WardDetailDto[];
    ambulances: AmbulanceSummaryDto[];
}

export interface AmbulanceSummaryDto {
    id: string;
    unitNumber: string;
    status: number;
    latitude: number;
    longitude: number;
}

// Ward
export interface WardDetailDto {
    id: string;
    hospitalId: string;
    hospitalName: string;
    name: string;
    type: number;  // 0=ICU,1=ER,2=General,3=Surgery,4=Pediatric,5=Cardiology
    totalBeds: number;
    availableBeds: number;
    occupiedBeds: number;
    reservedBeds: number;
    maintenanceBeds: number;
    beds: BedSummaryDto[];
}

// Bed
export interface BedSummaryDto {
    id: string;
    bedNumber: string;
    status: number;  // 0=Available,1=Occupied,2=Reserved,3=Maintenance
    lastUpdated: string;
}

// Staff
export interface StaffDto {
    id: string;
    fullName: string;
    email: string;
    role: number;  // 0=Admin,1=Doctor,2=Dispatcher
    hospitalId: string;
    hospitalName: string;
    isActive: boolean;
    createdAt: string;
}

// Requests
export interface CreateHospitalRequest {
    name: string;
    address: string;
    latitude: number;
    longitude: number;
    phone: string;
    acceptedInsuranceTypes: string;
}

export interface CreateWardRequest {
    name: string;
    type: number;
    totalBeds: number;
}

export type UpdateWardRequest = CreateWardRequest;

export interface CreateStaffRequest {
    fullName: string;
    email: string;
    password: string;
    role: number;
}

export interface UpdateStaffRequest {
    fullName: string;
    email: string;
    role: number;
}

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
}


// ── TRANSFER TYPES ────────────────────────────────────────────────
// Enums as numbers — matching C# enum order
// UrgencyLevel:    0=Stable, 1=Urgent, 2=Critical
// BroadcastStatus: 0=Active, 1=Matched, 2=Expired, 3=Cancelled
// ResponseType:    0=Pending, 1=Accepted, 2=Declined, 3=NoResponse

export interface CreateBroadcastRequest {
    bedTypeRequired: string;
    equipmentNeeded: string;
    insuranceType: string;
    maxDistanceMiles: number;
    urgency: number; // 0=Stable, 1=Urgent, 2=Critical
}

// Matches BroadcastDto
export interface BroadcastDto {
    id: string;
    sendingHospitalId: string;
    sendingHospitalName: string;
    bedTypeRequired: string;
    equipmentNeeded: string;
    insuranceType: string;
    maxDistanceMiles: number;
    urgency: number;          // UrgencyLevel enum
    status: number;           // BroadcastStatus enum
    createdAt: string;
    totalResponses: number;
    acceptedResponses: number;
    declinedResponses: number;
    responses: HospitalResponseDto[];
}

// Matches BroadcastSummaryDto
export interface BroadcastSummaryDto {
    id: string;
    sendingHospitalName: string;
    bedTypeRequired: string;
    equipmentNeeded: string;
    urgency: number;          // UrgencyLevel enum
    status: number;           // BroadcastStatus enum
    createdAt: string;
    totalResponses: number;
    acceptedResponses: number;
}

// Matches HospitalMatchDto
export interface HospitalMatchDto {
    hospitalId: string;
    hospitalName: string;
    address: string;
    distanceMiles: number;
    availableBeds: number;
    hasRequiredEquipment: boolean;
    acceptsInsurance: boolean;
    score: number;
    distanceScore: number;
    bedScore: number;
    responseRateScore: number;
    avgAcceptTimeScore: number;  // field name in C# is AvgAcceptTimeScore
    avgResponseTimeMinutes: number;
    acceptanceRate: number;
}

// Matches MatchingResultDto
export interface MatchingResultDto {
    broadcastId: string;
    matches: HospitalMatchDto[];
    totalHospitalsChecked: number;
    totalFiltered: number;
    generatedAt: string;
}

// Matches HospitalResponseDto
export interface HospitalResponseDto {
    id: string;
    broadcastId: string;
    receivingHospitalId: string;
    receivingHospitalName: string;
    response: number;         // ResponseType enum: 0=Pending,1=Accepted,2=Declined,3=NoResponse
    declineReason: string | null;
    respondedAt: string | null;
}

// Matches RespondToBroadcastRequest
export interface RespondToBroadcastRequest {
    response: number;         // ResponseType enum: 1=Accepted, 2=Declined
    declineReason: string | null;
}

// Matches NotifyHospitalsRequest
export interface NotifyHospitalsRequest {
    broadcastId: string;
    hospitalIds: string[];
}

// ── DISPLAY HELPERS ───────────────────────────────────────────────

export const URGENCY_LABELS: Record<number, string> = {
    0: "Stable", 1: "Urgent", 2: "Critical",
};
export const URGENCY_COLORS: Record<number, string> = {
    0: "#00D68F", 1: "#FF9A3C", 2: "#FF4D6A",
};
export const STATUS_LABELS: Record<number, string> = {
    0: "Active", 1: "Matched", 2: "Expired", 3: "Cancelled",
};
export const STATUS_COLORS: Record<number, string> = {
    0: "#FF9A3C", 1: "#00D68F", 2: "#8BA3C7", 3: "#8BA3C7",
};
export const RESPONSE_LABELS: Record<number, string> = {
    0: "Pending", 1: "Accepted", 2: "Declined", 3: "No Response",
};
export const RESPONSE_COLORS: Record<number, string> = {
    0: "#FF9A3C", 1: "#00D68F", 2: "#FF4D6A", 3: "#8BA3C7",
};
