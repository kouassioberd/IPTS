import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import {
    dispatcherApi,
    clearAuth,
    getUser,
    TRANSFER_STATUS_LABELS,
    TRANSFER_STATUS_COLORS,
} from "../services/api";
import type {
    DispatcherDashboardDto,
    DispatcherTransferDto,
    AmbulanceDetailDto,
} from "../services/api";

function getErrorMessage(error: unknown): string {
    if (error instanceof Error) return error.message;
    if (typeof error === "string") return error;
    return "An unexpected error occurred";
}

// TransferStatus enum values for the update dropdown
const STATUS_TRANSITIONS: Record<number, { label: string; value: number }[]> = {
    1: [{ label: "→ En Route", value: 2 }, { label: "→ Cancelled", value: 6 }],
    2: [{ label: "→ Patient On Board", value: 3 }, { label: "→ Cancelled", value: 6 }],
    3: [{ label: "→ In Transit", value: 4 }, { label: "→ Cancelled", value: 6 }],
    4: [{ label: "→ Delivered", value: 5 }, { label: "→ Cancelled", value: 6 }],
};

//The component function and state:
export default function DispatcherDashboardPage() {
    const navigate = useNavigate();
    const user = getUser();

    const [dashboard, setDashboard] = useState<DispatcherDashboardDto | null>(null);
    const [ambulances, setAmbulances] = useState<AmbulanceDetailDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [assigning, setAssigning] = useState<string | null>(null);
    const [updating, setUpdating] = useState<string | null>(null);
    // Which transfer is expanded for assign/update actions
    const [expanded, setExpanded] = useState<string | null>(null);
    const [selectedAmb, setSelectedAmb] = useState<string>("");
    const [statusNotes, setStatusNotes] = useState("");

    useEffect(() => {
        if (!user) { navigate("/login"); return; }
        loadData();
    }, []);

    const loadData = async () => {
        setLoading(true); setError("");
        try {
            const [dash, ambs] = await Promise.all([
                dispatcherApi.getDashboard(),
                dispatcherApi.getAvailableAmbulances(),
            ]);
            setDashboard(dash);
            setAmbulances(ambs);
        } catch (e) { setError(getErrorMessage(e)); }
        finally { setLoading(false); }
    };

    //The assign ambulance handler:
    const handleAssign = async (transferId: string) => {
        if (!selectedAmb) return;
        setAssigning(transferId);
        try {
            await dispatcherApi.assignAmbulance({
                transferRequestId: transferId,
                ambulanceId: selectedAmb,
            });
            setExpanded(null);
            setSelectedAmb("");
            await loadData();
        } catch (e) { setError(getErrorMessage(e)); }
        finally { setAssigning(null); }
    };

    //The update status handler:
    const handleUpdateStatus = async (transferId: string, newStatus: number) => {
        setUpdating(transferId);
        try {
            await dispatcherApi.updateStatus({
                transferRequestId: transferId,
                newStatus,
                notes: statusNotes || undefined,
            });
            setExpanded(null);
            setStatusNotes("");
            await loadData();
        } catch (e) { setError(getErrorMessage(e)); }
        finally { setUpdating(null); }
    };

    //The render — navbar, summary cards, and transfer list:
    return (
        <div style={{
            minHeight: "100vh", background: "#0A1628",
            fontFamily: "'Inter', sans-serif"
        }}>

            {/* NAVBAR */}
            <nav style={{
                background: "#112240",
                borderBottom: "1px solid rgba(255,255,255,0.08)",
                padding: "0 32px", display: "flex", alignItems: "center",
                justifyContent: "space-between", height: 60,
                position: "sticky", top: 0, zIndex: 50
            }}>
                <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                    <span style={{ fontSize: 22 }}>🚑</span>
                    <span style={{ color: "#F0F6FF", fontWeight: 700, fontSize: 17 }}>IPTS</span>
                    <span style={{
                        background: "rgba(255,154,60,0.2)", color: "#FF9A3C",
                        padding: "2px 10px", borderRadius: 10,
                        fontSize: 12, fontWeight: 600
                    }}>Dispatcher</span>
                </div>
                <div style={{ display: "flex", alignItems: "center", gap: 16 }}>
                    <span style={{ color: "#8BA3C7", fontSize: 13 }}>{user?.fullName}</span>
                    <button onClick={() => { clearAuth(); navigate("/login"); }}
                        style={{
                            background: "rgba(255,77,106,0.12)",
                            border: "1px solid rgba(255,77,106,0.3)",
                            color: "#FF4D6A", borderRadius: 8,
                            padding: "6px 14px", fontSize: 13, cursor: "pointer"
                        }}>
                        Logout
                    </button>
                </div>
            </nav>

            <div style={{ maxWidth: 1100, margin: "0 auto", padding: "32px" }}>

                <h1 style={{
                    color: "#F0F6FF", fontSize: 24,
                    fontWeight: 700, margin: "0 0 8px"
                }}>
                    Dispatcher Dashboard
                </h1>
                <p style={{ color: "#8BA3C7", margin: "0 0 28px", fontSize: 14 }}>
                    Assign ambulances and track active transfers
                </p>

                {error && (
                    <div style={{
                        background: "rgba(255,77,106,0.12)",
                        border: "1px solid rgba(255,77,106,0.3)", borderRadius: 10,
                        padding: "12px 18px", color: "#FF4D6A",
                        marginBottom: 20, fontSize: 14
                    }}>{error}</div>
                )}

                {loading ? (
                    <div style={{
                        textAlign: "center", padding: 60,
                        color: "#8BA3C7"
                    }}>Loading...</div>
                ) : dashboard && (
                    <>
                        {/* SUMMARY CARDS */}
                        <div style={{
                            display: "grid",
                            gridTemplateColumns: "repeat(5,1fr)", gap: 16, marginBottom: 32
                        }}>
                            {[
                                { label: "Awaiting Ambulance", value: dashboard.totalConfirmed, color: "#FF9A3C" },
                                { label: "Ambulance Assigned", value: dashboard.totalAmbulanceAssigned, color: "#00C2D4" },
                                { label: "En Route", value: dashboard.totalEnRoute, color: "#FF4D6A" },
                                { label: "Delivered Today", value: dashboard.totalDeliveredToday, color: "#00D68F" },
                                { label: "Available Ambulances", value: dashboard.availableAmbulances, color: "#7EB9FF" },
                            ].map(card => (
                                <div key={card.label} style={{
                                    background: "#112240",
                                    border: "1px solid rgba(255,255,255,0.07)",
                                    borderRadius: 12, padding: "20px"
                                }}>
                                    <div style={{
                                        color: card.color, fontSize: 32,
                                        fontWeight: 700, lineHeight: 1
                                    }}>{card.value}</div>
                                    <div style={{
                                        color: "#8BA3C7", fontSize: 12,
                                        marginTop: 8
                                    }}>{card.label}</div>
                                </div>
                            ))}
                        </div>

                        {/* TRANSFER LIST */}
                        <div style={{
                            display: "flex",
                            flexDirection: "column", gap: 14
                        }}>
                            {dashboard.activeTransfers.length === 0 ? (
                                <div style={{
                                    textAlign: "center", padding: 60,
                                    background: "#112240", borderRadius: 14,
                                    border: "1px dashed rgba(255,255,255,0.1)"
                                }}>
                                    <div style={{ fontSize: 48, marginBottom: 12 }}>🚑</div>
                                    <p style={{ color: "#8BA3C7", fontSize: 15, margin: 0 }}>
                                        No active transfers. They appear here after the
                                        sending doctor submits patient data.
                                    </p>
                                </div>
                            ) : dashboard.activeTransfers.map(t => (
                                <TransferCard
                                    key={t.id}
                                    transfer={t}
                                    ambulances={ambulances}
                                    expanded={expanded === t.id}
                                    onToggle={() => setExpanded(expanded === t.id ? null : t.id)}
                                    selectedAmb={selectedAmb}
                                    onSelectAmb={setSelectedAmb}
                                    onAssign={() => handleAssign(t.id)}
                                    assigning={assigning === t.id}
                                    onUpdateStatus={(s) => handleUpdateStatus(t.id, s)}
                                    updating={updating === t.id}
                                    statusNotes={statusNotes}
                                    onNotesChange={setStatusNotes}
                                />
                            ))}
                        </div>
                    </>
                )}
            </div>
        </div>
    );
}

//The TransferCard sub - component :
function TransferCard({ transfer, ambulances, expanded, onToggle,
    selectedAmb, onSelectAmb, onAssign, assigning,
    onUpdateStatus, updating, statusNotes, onNotesChange,
}: {
    transfer: DispatcherTransferDto;
    ambulances: AmbulanceDetailDto[];
    expanded: boolean;
    onToggle: () => void;
    selectedAmb: string;
    onSelectAmb: (id: string) => void;
    onAssign: () => void;
    assigning: boolean;
    onUpdateStatus: (status: number) => void;
    updating: boolean;
    statusNotes: string;
    onNotesChange: (v: string) => void;
}) {
    const statusColor = TRANSFER_STATUS_COLORS[transfer.status] ?? "#8BA3C7";
    const statusLabel = TRANSFER_STATUS_LABELS[transfer.status] ?? "Unknown";
    const transitions = STATUS_TRANSITIONS[transfer.status] ?? [];
    const isConfirmed = transfer.status === 0;  // Confirmed — needs ambulance
    const canProgress = transitions.length > 0;

    return (
        <div style={{
            background: "#112240",
            border: "1px solid rgba(255,255,255,0.07)",
            borderRadius: 14, overflow: "hidden"
        }}>

            {/* CARD HEADER — click to expand */}
            <div onClick={onToggle} style={{
                padding: "18px 24px",
                cursor: "pointer", display: "flex",
                justifyContent: "space-between", alignItems: "center"
            }}>

                <div>
                    <div style={{
                        color: "#F0F6FF", fontWeight: 700,
                        fontSize: 15, marginBottom: 4
                    }}>
                        Transfer #{transfer.id.substring(0, 8).toUpperCase()}
                    </div>
                    <div style={{ color: "#8BA3C7", fontSize: 13 }}>
                        → {transfer.receivingHospitalName}
                        {transfer.assignedAmbulanceUnit &&
                            ` · 🚑 ${transfer.assignedAmbulanceUnit}`}
                    </div>
                </div>

                <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                    <span style={{
                        background: `${statusColor}18`,
                        color: statusColor,
                        border: `1px solid ${statusColor}44`,
                        padding: "4px 12px", borderRadius: 8,
                        fontSize: 13, fontWeight: 600
                    }}>
                        {statusLabel}
                    </span>
                    {isConfirmed && (
                        <span style={{
                            background: "rgba(255,154,60,0.14)",
                            color: "#FF9A3C",
                            border: "1px solid rgba(255,154,60,0.3)",
                            padding: "4px 12px", borderRadius: 8,
                            fontSize: 12, fontWeight: 700
                        }}>
                            ⚠ Needs Ambulance
                        </span>
                    )}
                    <span style={{ color: "#8BA3C7", fontSize: 18 }}>
                        {expanded ? "▲" : "▼"}
                    </span>
                </div>
            </div>

            {/* EXPANDED PANEL */}
            {expanded && (
                <div style={{
                    padding: "0 24px 20px",
                    borderTop: "1px solid rgba(255,255,255,0.06)"
                }}>

                    <div style={{ paddingTop: 16, marginBottom: 16 }}>
                        <div style={{ color: "#8BA3C7", fontSize: 13 }}>
                            Confirmed: {new Date(transfer.confirmedAt).toLocaleString()}
                        </div>
                        {transfer.assignedAmbulanceUnit && (
                            <div style={{ color: "#8BA3C7", fontSize: 13, marginTop: 4 }}>
                                Ambulance: {transfer.assignedAmbulanceUnit}
                            </div>
                        )}
                    </div>

                    {/* ASSIGN AMBULANCE — only shown when Confirmed */}
                    {isConfirmed && (
                        <div style={{
                            background: "rgba(255,154,60,0.06)",
                            border: "1px solid rgba(255,154,60,0.2)",
                            borderRadius: 10, padding: 16, marginBottom: 12
                        }}>
                            <p style={{
                                color: "#FF9A3C", fontSize: 13,
                                margin: "0 0 12px", fontWeight: 600
                            }}>
                                Assign an ambulance to begin the transfer
                            </p>
                            {ambulances.length === 0 ? (
                                <p style={{ color: "#8BA3C7", fontSize: 13, margin: 0 }}>
                                    No ambulances available at this time.
                                </p>
                            ) : (
                                <div style={{ display: "flex", gap: 10 }}>
                                    <select
                                        value={selectedAmb}
                                        onChange={e => onSelectAmb(e.target.value)}
                                        style={{
                                            flex: 1, background: "#0A1628",
                                            border: "1px solid rgba(255,255,255,0.1)",
                                            borderRadius: 8, padding: "10px 12px",
                                            color: "#F0F6FF", fontSize: 14,
                                            outline: "none"
                                        }}>
                                        <option value="">Select ambulance...</option>
                                        {ambulances.map(a => (
                                            <option key={a.id} value={a.id}>
                                                {a.unitNumber} — {a.crewCount} crew
                                            </option>
                                        ))}
                                    </select>
                                    <button
                                        onClick={onAssign}
                                        disabled={!selectedAmb || assigning}
                                        style={{
                                            background: selectedAmb
                                                ? "linear-gradient(135deg,#1E5FBF,#00C2D4)"
                                                : "rgba(255,255,255,0.06)",
                                            border: "none", borderRadius: 8,
                                            padding: "10px 20px", color: "#fff",
                                            fontSize: 14, fontWeight: 600,
                                            cursor: selectedAmb ? "pointer" : "not-allowed"
                                        }}>
                                        {assigning ? "Assigning..." : "🚑 Assign"}
                                    </button>
                                </div>
                            )}
                        </div>
                    )}

                    {/* UPDATE STATUS — shown when ambulance is assigned */}
                    {canProgress && (
                        <div style={{
                            background: "rgba(0,194,212,0.06)",
                            border: "1px solid rgba(0,194,212,0.2)",
                            borderRadius: 10, padding: 16
                        }}>
                            <p style={{
                                color: "#00C2D4", fontSize: 13,
                                margin: "0 0 12px", fontWeight: 600
                            }}>
                                Update transfer status
                            </p>
                            <input
                                placeholder="Optional notes (e.g. traffic delay)"
                                value={statusNotes}
                                onChange={e => onNotesChange(e.target.value)}
                                style={{
                                    width: "100%", background: "#0A1628",
                                    border: "1px solid rgba(255,255,255,0.1)",
                                    borderRadius: 8, padding: "10px 12px",
                                    color: "#F0F6FF", fontSize: 14, outline: "none",
                                    marginBottom: 12, boxSizing: "border-box" as const
                                }}
                            />
                            <div style={{ display: "flex", gap: 10, flexWrap: "wrap" }}>
                                {transitions.map(tr => (
                                    <button
                                        key={tr.value}
                                        onClick={() => onUpdateStatus(tr.value)}
                                        disabled={updating}
                                        style={{
                                            background: tr.value === 6
                                                ? "rgba(255,77,106,0.15)"
                                                : "linear-gradient(135deg,#1E5FBF,#00C2D4)",
                                            border: tr.value === 6
                                                ? "1px solid rgba(255,77,106,0.4)"
                                                : "none",
                                            borderRadius: 8, padding: "10px 18px",
                                            color: tr.value === 6 ? "#FF4D6A" : "#fff",
                                            fontSize: 14, fontWeight: 600,
                                            cursor: updating ? "not-allowed" : "pointer"
                                        }}>
                                        {updating ? "Updating..." : tr.label}
                                    </button>
                                ))}
                            </div>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}
