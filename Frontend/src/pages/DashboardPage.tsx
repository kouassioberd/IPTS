// src/pages/DashboardPage.tsx
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
    hospitalsApi, wardsApi, bedsApi, staffApi,
    clearAuth, getUser,
} from "../services/api";
import type {
    HospitalDashboardDto, WardDetailDto,
    StaffDto, CreateStaffRequest, CreateWardRequest,
} from "../services/api";

// ── CONSTANTS ─────────────────────────────────────────────────────
const WARD_TYPES = ["ICU", "ER", "General", "Surgery", "Pediatric", "Cardiology"];
const STAFF_ROLES = ["Admin", "Doctor", "Dispatcher"];
const BED_STATUSES = ["Available", "Occupied", "Reserved", "Maintenance"];
const BED_COLORS: Record<number, string> = {
    0: "#00D68F", 1: "#FF4D6A", 2: "#FF9A3C", 3: "#8BA3C7",
};
const AMB_STATUSES = ["Available", "Assigned", "In Transit", "Maintenance"];

type ActiveTab = "beds" | "staff" | "ambulances";

function getErrorMessage(error: unknown): string {
    if (error instanceof Error) return error.message;
    if (typeof error === "string") return error;
    return "An unexpected error occurred";
}

// ══════════════════════════════════════════════════════════════════
// MAIN DASHBOARD PAGE
// ══════════════════════════════════════════════════════════════════

export default function DashboardPage() {
    const navigate = useNavigate();
    const user = getUser();

    const [dashboard, setDashboard] = useState<HospitalDashboardDto | null>(null);
    const [staff, setStaff] = useState<StaffDto[]>([]);
    const [activeTab, setActiveTab] = useState<ActiveTab>("beds");
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    // Modal states
    const [showAddWard, setShowAddWard] = useState(false);
    const [showAddStaff, setShowAddStaff] = useState(false);

    useEffect(() => {
        if (!user) { navigate("/login"); return; }
        loadAll();
    }, []);

    const loadAll = async () => {
        try {
            setLoading(true);
            const [dash, staffList] = await Promise.all([
                hospitalsApi.getDashboard(user!.hospitalId),
                staffApi.getAll(),
            ]);
            setDashboard(dash);
            setStaff(staffList);
        } catch (e: unknown) {
            setError(getErrorMessage(e));
        } finally {
            setLoading(false);
        }
    };

    const handleUpdateBedStatus = async (bedId: string, status: number) => {
        try {
            await bedsApi.updateStatus(bedId, status);
            await loadAll();
        } catch (e: unknown) {
            alert("Failed to update bed: " + getErrorMessage(e));
        }
    };

    const handleAddWard = async (data: CreateWardRequest) => {
        try {
            await wardsApi.create(data);
            setShowAddWard(false);
            await loadAll();
        } catch (e: unknown) {
            alert("Failed to create ward: " + getErrorMessage(e));
        }
    };

    const handleAddStaff = async (data: CreateStaffRequest) => {
        try {
            await staffApi.create(data);
            setShowAddStaff(false);
            await loadAll();
        } catch (e: unknown) {
            alert("Failed to create staff: " + getErrorMessage(e));
        }
    };

    const handleDeactivateStaff = async (id: string) => {
        if (!confirm("Deactivate this staff member?")) return;
        try {
            await staffApi.deactivate(id);
            await loadAll();
        } catch (e: unknown) {
            alert(getErrorMessage(e));
        }
    };

    const handleReactivateStaff = async (id: string) => {
        try {
            await staffApi.reactivate(id);
            await loadAll();
        } catch (e: unknown) {
            alert(getErrorMessage(e));
        }
    };

    const handleDeleteWard = async (wardId: string) => {
        if (!confirm("Delete this ward? This cannot be undone.")) return;
        try {
            await wardsApi.delete(wardId);
            await loadAll();
        } catch (e: unknown) {
            alert(getErrorMessage(e));
        }
    };

    if (loading) return <LoadingScreen />;
    if (error) return <ErrorScreen message={error} onRetry={loadAll} />;
    if (!dashboard) return null;

    const occupancyPct = dashboard.totalBeds > 0
        ? Math.round((dashboard.occupiedBeds / dashboard.totalBeds) * 100) : 0;

    return (
        <div style={{ minHeight: "100vh", background: "#0A1628", fontFamily: "'Inter', sans-serif" }}>

            {/* ── NAVBAR ── */}
            <nav style={{
                background: "#112240", borderBottom: "1px solid rgba(255,255,255,0.08)",
                padding: "0 32px", display: "flex", alignItems: "center",
                justifyContent: "space-between", height: 60, position: "sticky", top: 0, zIndex: 50,
            }}>
                <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                    <span style={{ fontSize: 22 }}>🏥</span>
                    <span style={{ color: "#F0F6FF", fontWeight: 700, fontSize: 17 }}>IPTS</span>
                    <span style={{
                        background: "rgba(0,194,212,0.15)", color: "#00C2D4",
                        padding: "2px 10px", borderRadius: 10, fontSize: 12, fontWeight: 600,
                    }}>Admin</span>
                </div>
                <div style={{ display: "flex", alignItems: "center", gap: 16 }}>
                    <span style={{ color: "#8BA3C7", fontSize: 13 }}>{user?.fullName}</span>
                    <button onClick={() => { clearAuth(); navigate("/login"); }} style={{
                        background: "rgba(255,77,106,0.12)", border: "1px solid rgba(255,77,106,0.3)",
                        color: "#FF4D6A", borderRadius: 8, padding: "6px 14px", fontSize: 13, cursor: "pointer",
                    }}>Logout</button>
                </div>
            </nav>

            <div style={{ padding: "32px", maxWidth: 1200, margin: "0 auto" }}>

                {/* ── HEADER ── */}
                <div style={{ marginBottom: 28 }}>
                    <h1 style={{ color: "#F0F6FF", fontSize: 24, fontWeight: 700, margin: 0 }}>
                        {dashboard.hospitalName}
                    </h1>
                    <p style={{ color: "#8BA3C7", margin: "4px 0 0", fontSize: 14 }}>
                        Admin Dashboard — Live Bed Occupancy & Staff Management
                    </p>
                </div>

                {/* ── STAT CARDS ── */}
                <div style={{
                    display: "grid",
                    gridTemplateColumns: "repeat(auto-fit, minmax(150px, 1fr))",
                    gap: 14, marginBottom: 24,
                }}>
                    {[
                        { label: "Total Beds", value: dashboard.totalBeds, color: "#8BA3C7" },
                        { label: "Available", value: dashboard.availableBeds, color: "#00D68F" },
                        { label: "Occupied", value: dashboard.occupiedBeds, color: "#FF4D6A" },
                        { label: "Reserved", value: dashboard.reservedBeds, color: "#FF9A3C" },
                        { label: "Active Transfers", value: dashboard.activeTransfersToday, color: "#00C2D4" },
                        { label: "Acceptance Rate", value: `${dashboard.acceptanceRate.toFixed(0)}%`, color: "#9B5DE5" },
                    ].map(s => (
                        <div key={s.label} style={{
                            background: "#112240", border: "1px solid rgba(255,255,255,0.07)",
                            borderRadius: 12, padding: "18px 14px", textAlign: "center",
                        }}>
                            <div style={{ fontSize: 26, fontWeight: 700, color: s.color }}>{s.value}</div>
                            <div style={{ color: "#8BA3C7", fontSize: 12, marginTop: 4 }}>{s.label}</div>
                        </div>
                    ))}
                </div>

                {/* ── OCCUPANCY BAR ── */}
                <div style={{
                    background: "#112240", border: "1px solid rgba(255,255,255,0.07)",
                    borderRadius: 12, padding: 20, marginBottom: 28,
                }}>
                    <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 10 }}>
                        <span style={{ color: "#F0F6FF", fontWeight: 600, fontSize: 14 }}>Overall Occupancy</span>
                        <span style={{ color: "#00C2D4", fontWeight: 700 }}>{occupancyPct}%</span>
                    </div>
                    <div style={{ height: 10, background: "rgba(255,255,255,0.06)", borderRadius: 5, overflow: "hidden" }}>
                        <div style={{
                            height: "100%", width: `${occupancyPct}%`,
                            background: occupancyPct > 80
                                ? "linear-gradient(90deg,#FF9A3C,#FF4D6A)"
                                : "linear-gradient(90deg,#1E5FBF,#00C2D4)",
                            borderRadius: 5, transition: "width 0.8s ease",
                        }} />
                    </div>
                    <div style={{ display: "flex", gap: 20, marginTop: 10, flexWrap: "wrap" }}>
                        {[
                            { label: "Available", color: "#00D68F", count: dashboard.availableBeds },
                            { label: "Occupied", color: "#FF4D6A", count: dashboard.occupiedBeds },
                            { label: "Reserved", color: "#FF9A3C", count: dashboard.reservedBeds },
                            { label: "Maintenance", color: "#8BA3C7", count: dashboard.maintenanceBeds },
                        ].map(l => (
                            <div key={l.label} style={{ display: "flex", alignItems: "center", gap: 6 }}>
                                <div style={{ width: 8, height: 8, borderRadius: "50%", background: l.color }} />
                                <span style={{ color: "#8BA3C7", fontSize: 12 }}>{l.label}: {l.count}</span>
                            </div>
                        ))}
                    </div>
                </div>

                {/* ── TABS ── */}
                <div style={{
                    display: "flex", gap: 0,
                    borderBottom: "1px solid rgba(255,255,255,0.08)", marginBottom: 24,
                }}>
                    {(["beds", "staff", "ambulances"] as ActiveTab[]).map(tab => (
                        <button key={tab} onClick={() => setActiveTab(tab)} style={{
                            background: "none", border: "none", cursor: "pointer",
                            padding: "10px 20px", fontSize: 14, fontWeight: 500,
                            color: activeTab === tab ? "#00C2D4" : "#8BA3C7",
                            borderBottom: activeTab === tab ? "2px solid #00C2D4" : "2px solid transparent",
                            textTransform: "capitalize",
                        }}>{tab === "beds" ? "🛏 Wards & Beds" : tab === "staff" ? "👥 Staff" : "🚑 Ambulances"}</button>
                    ))}
                </div>

                {/* ── BEDS TAB ── */}
                {activeTab === "beds" && (
                    <div>
                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 20 }}>
                            <h2 style={{ color: "#F0F6FF", fontSize: 17, fontWeight: 600, margin: 0 }}>
                                Wards & Beds ({dashboard.wards.length} wards)
                            </h2>
                            <button onClick={() => setShowAddWard(true)} style={primaryBtn}>
                                + Add Ward
                            </button>
                        </div>

                        <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
                            {dashboard.wards.map(ward => (
                                <WardCard
                                    key={ward.id}
                                    ward={ward}
                                    onUpdateBed={handleUpdateBedStatus}
                                    onDeleteWard={handleDeleteWard}
                                />
                            ))}
                            {dashboard.wards.length === 0 && (
                                <EmptyState icon="🏥" message="No wards yet. Click 'Add Ward' to get started." />
                            )}
                        </div>
                    </div>
                )}

                {/* ── STAFF TAB ── */}
                {activeTab === "staff" && (
                    <div>
                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 20 }}>
                            <h2 style={{ color: "#F0F6FF", fontSize: 17, fontWeight: 600, margin: 0 }}>
                                Staff Members ({staff.length})
                            </h2>
                            <button onClick={() => setShowAddStaff(true)} style={primaryBtn}>
                                + Add Staff
                            </button>
                        </div>

                        <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
                            {staff.map(member => (
                                <div key={member.id} style={{
                                    display: "flex", alignItems: "center", justifyContent: "space-between",
                                    background: "#112240", border: "1px solid rgba(255,255,255,0.07)",
                                    borderRadius: 12, padding: "16px 20px",
                                }}>
                                    <div style={{ display: "flex", alignItems: "center", gap: 14 }}>
                                        <div style={{
                                            width: 40, height: 40, borderRadius: "50%",
                                            background: "linear-gradient(135deg,#1E5FBF,#00C2D4)",
                                            display: "flex", alignItems: "center", justifyContent: "center",
                                            fontSize: 16, color: "#fff", fontWeight: 700,
                                        }}>
                                            {member.fullName.charAt(0)}
                                        </div>
                                        <div>
                                            <div style={{ color: "#F0F6FF", fontWeight: 600, fontSize: 15 }}>
                                                {member.fullName}
                                                {!member.isActive && (
                                                    <span style={{
                                                        marginLeft: 8, fontSize: 11, color: "#FF4D6A",
                                                        background: "rgba(255,77,106,0.12)", padding: "1px 8px", borderRadius: 6,
                                                    }}>Inactive</span>
                                                )}
                                            </div>
                                            <div style={{ color: "#8BA3C7", fontSize: 13 }}>{member.email}</div>
                                        </div>
                                    </div>
                                    <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                                        <span style={{
                                            background: "rgba(0,194,212,0.12)", color: "#00C2D4",
                                            padding: "3px 12px", borderRadius: 8, fontSize: 12, fontWeight: 600,
                                        }}>{STAFF_ROLES[member.role]}</span>
                                        {member.isActive ? (
                                            <button
                                                onClick={() => handleDeactivateStaff(member.id)}
                                                style={{ ...dangerBtn, fontSize: 12, padding: "5px 12px" }}
                                            >Deactivate</button>
                                        ) : (
                                            <button
                                                onClick={() => handleReactivateStaff(member.id)}
                                                style={{ ...successBtn, fontSize: 12, padding: "5px 12px" }}
                                            >Reactivate</button>
                                        )}
                                    </div>
                                </div>
                            ))}
                            {staff.length === 0 && (
                                <EmptyState icon="👥" message="No staff members yet. Click 'Add Staff' to create accounts." />
                            )}
                        </div>
                    </div>
                )}

                {/* ── AMBULANCES TAB ── */}
                {activeTab === "ambulances" && (
                    <div>
                        <h2 style={{ color: "#F0F6FF", fontSize: 17, fontWeight: 600, marginBottom: 20 }}>
                            Ambulances ({dashboard.ambulances.length})
                        </h2>
                        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))", gap: 14 }}>
                            {dashboard.ambulances.map(amb => {
                                const statusColor = [
                                    "#00D68F", "#FF9A3C", "#00C2D4", "#8BA3C7"
                                ][amb.status] ?? "#8BA3C7";
                                return (
                                    <div key={amb.id} style={{
                                        background: "#112240", border: `1px solid ${statusColor}33`,
                                        borderRadius: 12, padding: 20,
                                    }}>
                                        <div style={{ fontSize: 32, marginBottom: 10 }}>🚑</div>
                                        <div style={{ color: "#F0F6FF", fontWeight: 700, fontSize: 16, marginBottom: 6 }}>
                                            {amb.unitNumber}
                                        </div>
                                        <div style={{
                                            display: "inline-block",
                                            background: `${statusColor}18`, color: statusColor,
                                            padding: "2px 10px", borderRadius: 8, fontSize: 12, fontWeight: 600,
                                        }}>
                                            {AMB_STATUSES[amb.status]}
                                        </div>
                                    </div>
                                );
                            })}
                            {dashboard.ambulances.length === 0 && (
                                <EmptyState icon="🚑" message="No ambulances registered." />
                            )}
                        </div>
                    </div>
                )}
            </div>

            {/* ── ADD WARD MODAL ── */}
            {showAddWard && (
                <AddWardModal
                    onClose={() => setShowAddWard(false)}
                    onSubmit={handleAddWard}
                />
            )}

            {/* ── ADD STAFF MODAL ── */}
            {showAddStaff && (
                <AddStaffModal
                    onClose={() => setShowAddStaff(false)}
                    onSubmit={handleAddStaff}
                />
            )}
        </div>
    );
}

// ══════════════════════════════════════════════════════════════════
// WARD CARD
// ══════════════════════════════════════════════════════════════════

function WardCard({
    ward, onUpdateBed, onDeleteWard,
}: {
    ward: WardDetailDto;
    onUpdateBed: (bedId: string, status: number) => void;
    onDeleteWard: (wardId: string) => void;
}) {
    const [expanded, setExpanded] = useState(true);

    const occupancyPct = ward.totalBeds > 0
        ? Math.round((ward.occupiedBeds / ward.totalBeds) * 100) : 0;

    return (
        <div style={{
            background: "#112240", border: "1px solid rgba(255,255,255,0.07)",
            borderRadius: 14, overflow: "hidden",
        }}>
            {/* Header */}
            <div style={{
                display: "flex", alignItems: "center", justifyContent: "space-between",
                padding: "16px 20px", cursor: "pointer",
                borderBottom: expanded ? "1px solid rgba(255,255,255,0.06)" : "none",
            }}>
                <div onClick={() => setExpanded(e => !e)} style={{ display: "flex", alignItems: "center", gap: 12, flex: 1 }}>
                    <span style={{
                        background: "rgba(0,194,212,0.12)", color: "#00C2D4",
                        padding: "2px 10px", borderRadius: 8, fontSize: 12, fontWeight: 600,
                    }}>{WARD_TYPES[ward.type]}</span>
                    <span style={{ color: "#F0F6FF", fontWeight: 600 }}>{ward.name}</span>
                </div>
                <div style={{ display: "flex", alignItems: "center", gap: 16 }}>
                    <span style={{ color: "#00D68F", fontSize: 13 }}>{ward.availableBeds} available</span>
                    <span style={{ color: "#8BA3C7", fontSize: 13 }}>{ward.totalBeds} total</span>
                    <div style={{ width: 80, height: 5, background: "rgba(255,255,255,0.06)", borderRadius: 3, overflow: "hidden" }}>
                        <div style={{
                            height: "100%", width: `${occupancyPct}%`,
                            background: occupancyPct > 80 ? "#FF4D6A" : "#00C2D4", borderRadius: 3,
                        }} />
                    </div>
                    <button
                        onClick={() => onDeleteWard(ward.id)}
                        style={{ background: "none", border: "none", color: "#FF4D6A", cursor: "pointer", fontSize: 16, padding: "0 4px" }}
                        title="Delete ward"
                    >🗑</button>
                    <span
                        onClick={() => setExpanded(e => !e)}
                        style={{ color: "#8BA3C7", fontSize: 16, cursor: "pointer" }}
                    >{expanded ? "▾" : "▸"}</span>
                </div>
            </div>

            {/* Beds grid */}
            {expanded && (
                <div style={{ padding: "16px 20px" }}>
                    <div style={{
                        display: "grid",
                        gridTemplateColumns: "repeat(auto-fill, minmax(100px, 1fr))",
                        gap: 8,
                    }}>
                        {ward.beds.map(bed => (
                            <div key={bed.id} style={{
                                background: `${BED_COLORS[bed.status]}12`,
                                border: `1px solid ${BED_COLORS[bed.status]}44`,
                                borderRadius: 10, padding: "10px 8px", textAlign: "center",
                            }}>
                                <div style={{ fontSize: 18, marginBottom: 4 }}>🛏</div>
                                <div style={{ color: "#F0F6FF", fontSize: 11, fontWeight: 600, marginBottom: 6 }}>
                                    {bed.bedNumber}
                                </div>
                                <select
                                    value={bed.status}
                                    onChange={e => onUpdateBed(bed.id, Number(e.target.value))}
                                    style={{
                                        background: "#0A1628",
                                        border: `1px solid ${BED_COLORS[bed.status]}66`,
                                        color: BED_COLORS[bed.status],
                                        borderRadius: 6, padding: "2px 4px",
                                        fontSize: 10, cursor: "pointer", width: "100%",
                                    }}
                                >
                                    {BED_STATUSES.map((s, i) => (
                                        <option key={i} value={i}>{s}</option>
                                    ))}
                                </select>
                            </div>
                        ))}
                        {ward.beds.length === 0 && (
                            <p style={{ color: "#8BA3C7", fontSize: 13, gridColumn: "1/-1" }}>No beds in this ward.</p>
                        )}
                    </div>
                </div>
            )}
        </div>
    );
}

// ══════════════════════════════════════════════════════════════════
// ADD WARD MODAL
// ══════════════════════════════════════════════════════════════════

function AddWardModal({ onClose, onSubmit }: {
    onClose: () => void;
    onSubmit: (data: CreateWardRequest) => void;
}) {
    const [form, setForm] = useState({ name: "", type: 0, totalBeds: 10 });
    const [loading, setLoading] = useState(false);

    const handleSubmit = async () => {
        if (!form.name.trim()) return;
        setLoading(true);
        await onSubmit(form);
        setLoading(false);
    };

    return (
        <Modal title="Add New Ward" onClose={onClose}>
            <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
                <Field label="Ward Name">
                    <input value={form.name} onChange={e => setForm(p => ({ ...p, name: e.target.value }))}
                        placeholder="e.g. ICU Ward B" style={inputStyle} />
                </Field>
                <Field label="Ward Type">
                    <select value={form.type} onChange={e => setForm(p => ({ ...p, type: Number(e.target.value) }))} style={inputStyle}>
                        {WARD_TYPES.map((t, i) => <option key={i} value={i}>{t}</option>)}
                    </select>
                </Field>
                <Field label="Number of Beds">
                    <input type="number" min={1} max={100} value={form.totalBeds}
                        onChange={e => setForm(p => ({ ...p, totalBeds: Number(e.target.value) }))} style={inputStyle} />
                </Field>
                <div style={{ display: "flex", gap: 10, justifyContent: "flex-end", marginTop: 4 }}>
                    <button onClick={onClose} style={cancelBtn}>Cancel</button>
                    <button onClick={handleSubmit} disabled={loading || !form.name.trim()} style={primaryBtn}>
                        {loading ? "Creating..." : "Create Ward"}
                    </button>
                </div>
            </div>
        </Modal>
    );
}

// ══════════════════════════════════════════════════════════════════
// ADD STAFF MODAL
// ══════════════════════════════════════════════════════════════════

function AddStaffModal({ onClose, onSubmit }: {
    onClose: () => void;
    onSubmit: (data: CreateStaffRequest) => void;
}) {
    const [form, setForm] = useState({
        fullName: "", email: "", password: "Test@1234", role: 1,
    });
    const [loading, setLoading] = useState(false);

    const handleSubmit = async () => {
        if (!form.fullName.trim() || !form.email.trim()) return;
        setLoading(true);
        await onSubmit(form);
        setLoading(false);
    };

    return (
        <Modal title="Add Staff Member" onClose={onClose}>
            <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
                <Field label="Full Name">
                    <input value={form.fullName} onChange={e => setForm(p => ({ ...p, fullName: e.target.value }))}
                        placeholder="Dr. John Smith" style={inputStyle} />
                </Field>
                <Field label="Email">
                    <input type="email" value={form.email} onChange={e => setForm(p => ({ ...p, email: e.target.value }))}
                        placeholder="doctor@hospital.com" style={inputStyle} />
                </Field>
                <Field label="Temporary Password">
                    <input value={form.password} onChange={e => setForm(p => ({ ...p, password: e.target.value }))} style={inputStyle} />
                </Field>
                <Field label="Role">
                    <select value={form.role} onChange={e => setForm(p => ({ ...p, role: Number(e.target.value) }))} style={inputStyle}>
                        {STAFF_ROLES.map((r, i) => <option key={i} value={i}>{r}</option>)}
                    </select>
                </Field>
                <div style={{ display: "flex", gap: 10, justifyContent: "flex-end", marginTop: 4 }}>
                    <button onClick={onClose} style={cancelBtn}>Cancel</button>
                    <button onClick={handleSubmit} disabled={loading || !form.fullName.trim() || !form.email.trim()} style={primaryBtn}>
                        {loading ? "Creating..." : "Create Account"}
                    </button>
                </div>
            </div>
        </Modal>
    );
}

// ══════════════════════════════════════════════════════════════════
// SHARED UI HELPERS
// ══════════════════════════════════════════════════════════════════

function Modal({ title, onClose, children }: {
    title: string; onClose: () => void; children: React.ReactNode;
}) {
    return (
        <div style={{
            position: "fixed", inset: 0, background: "rgba(0,0,0,0.7)",
            display: "flex", alignItems: "center", justifyContent: "center", zIndex: 1000,
        }}>
            <div style={{
                background: "#112240", border: "1px solid rgba(255,255,255,0.1)",
                borderRadius: 16, padding: 32, width: "100%", maxWidth: 440,
                boxShadow: "0 24px 80px rgba(0,0,0,0.5)",
            }}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 24 }}>
                    <h3 style={{ color: "#F0F6FF", margin: 0, fontSize: 17 }}>{title}</h3>
                    <button onClick={onClose} style={{ background: "none", border: "none", color: "#8BA3C7", fontSize: 22, cursor: "pointer" }}>×</button>
                </div>
                {children}
            </div>
        </div>
    );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
    return (
        <div>
            <label style={{ color: "#8BA3C7", fontSize: 13, fontWeight: 500, display: "block", marginBottom: 6 }}>{label}</label>
            {children}
        </div>
    );
}

function EmptyState({ icon, message }: { icon: string; message: string }) {
    return (
        <div style={{
            textAlign: "center", padding: 48,
            background: "#112240", borderRadius: 14,
            border: "1px dashed rgba(255,255,255,0.1)",
        }}>
            <div style={{ fontSize: 40, marginBottom: 12 }}>{icon}</div>
            <p style={{ color: "#8BA3C7", fontSize: 14, margin: 0 }}>{message}</p>
        </div>
    );
}

function LoadingScreen() {
    return (
        <div style={{ minHeight: "100vh", background: "#0A1628", display: "flex", alignItems: "center", justifyContent: "center" }}>
            <span style={{ color: "#00C2D4", fontSize: 16, fontFamily: "Inter, sans-serif" }}>Loading dashboard...</span>
        </div>
    );
}

function ErrorScreen({ message, onRetry }: { message: string; onRetry: () => void }) {
    return (
        <div style={{ minHeight: "100vh", background: "#0A1628", display: "flex", alignItems: "center", justifyContent: "center", flexDirection: "column", gap: 16 }}>
            <p style={{ color: "#FF4D6A", fontSize: 15, fontFamily: "Inter, sans-serif" }}>Error: {message}</p>
            <button onClick={onRetry} style={primaryBtn}>Retry</button>
        </div>
    );
}

// ── SHARED STYLES ─────────────────────────────────────────────────
const primaryBtn: React.CSSProperties = {
    background: "linear-gradient(135deg,#1E5FBF,#00C2D4)",
    border: "none", borderRadius: 8, padding: "10px 18px",
    color: "#fff", fontSize: 13, fontWeight: 600, cursor: "pointer",
};
const cancelBtn: React.CSSProperties = {
    background: "rgba(255,255,255,0.06)",
    border: "1px solid rgba(255,255,255,0.1)",
    borderRadius: 8, padding: "10px 18px",
    color: "#8BA3C7", fontSize: 13, cursor: "pointer",
};
const dangerBtn: React.CSSProperties = {
    background: "rgba(255,77,106,0.12)",
    border: "1px solid rgba(255,77,106,0.3)",
    borderRadius: 8, padding: "7px 14px",
    color: "#FF4D6A", fontSize: 13, cursor: "pointer",
};
const successBtn: React.CSSProperties = {
    background: "rgba(0,214,143,0.12)",
    border: "1px solid rgba(0,214,143,0.3)",
    borderRadius: 8, padding: "7px 14px",
    color: "#00D68F", fontSize: 13, cursor: "pointer",
};
const inputStyle: React.CSSProperties = {
    width: "100%", background: "#0A1628",
    border: "1px solid rgba(255,255,255,0.1)",
    borderRadius: 8, padding: "10px 12px",
    color: "#F0F6FF", fontSize: 14,
    outline: "none", boxSizing: "border-box",
};