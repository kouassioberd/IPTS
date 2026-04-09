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