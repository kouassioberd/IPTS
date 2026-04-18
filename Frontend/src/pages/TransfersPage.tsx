// src/pages/TransfersPage.tsx
import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import {
    transfersApi,
    clearAuth,
    getUser,
    URGENCY_LABELS,
    URGENCY_COLORS,
    STATUS_LABELS,
    STATUS_COLORS,
} from "../services/api";
import type { BroadcastSummaryDto, RespondToBroadcastRequest } from "../services/api";

function getErrorMessage(error: unknown): string {
    if (error instanceof Error) return error.message;
    if (typeof error === "string") return error;
    return "An unexpected error occurred";
}

export default function TransfersPage() {
    const navigate = useNavigate();
    const user = getUser();

    const [activeTab, setActiveTab] = useState<"sent" | "incoming">("sent");
    const [broadcasts, setBroadcasts] = useState<BroadcastSummaryDto[]>([]);
    const [incoming, setIncoming] = useState<BroadcastSummaryDto[]>([]);
    const [responding, setResponding] = useState<string | null>(null);
    const [declineReason, setDeclineReason] = useState("");
    const [showDeclineModal, setShowDeclineModal] = useState<string | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    useEffect(() => {
        if (!user) { navigate("/login"); return; }
        loadAll();
    }, []);

    const loadAll = async () => {
        setLoading(true);
        setError("");
        try {
            // Both endpoints return BroadcastSummaryDto[]
            const [sent, inc] = await Promise.all([
                transfersApi.getMyBroadcasts(),   // GET /api/Transfers/my-broadcasts
                transfersApi.getIncoming(),        // GET /api/Transfers/incoming
            ]);
            setBroadcasts(sent);
            setIncoming(inc);
        } catch (e: unknown) {
            setError(getErrorMessage(e));
        } finally {
            setLoading(false);
        }
    };

    const handleAccept = async (broadcastId: string) => {
        setResponding(broadcastId);
        try {
            const body: RespondToBroadcastRequest = {
                response: 1,          // ResponseType.Accepted
                declineReason: null,
            };
            await transfersApi.respond(broadcastId, body);
            await loadAll();
        } catch (e: unknown) {
            setError(getErrorMessage(e));
        } finally {
            setResponding(null);
        }
    };

    const handleDecline = async (broadcastId: string) => {
        setResponding(broadcastId);
        try {
            const body: RespondToBroadcastRequest = {
                response: 2,          // ResponseType.Declined
                declineReason: declineReason.trim() || "No capacity",
            };
            await transfersApi.respond(broadcastId, body);
            setShowDeclineModal(null);
            setDeclineReason("");
            await loadAll();
        } catch (e: unknown) {
            setError(getErrorMessage(e));
        } finally {
            setResponding(null);
        }
    };

    return (
        <div style={{ minHeight: "100vh", background: "#0A1628", fontFamily: "'Inter', sans-serif" }}>

            {/* NAVBAR */}
            <nav style={{
                background: "#112240", borderBottom: "1px solid rgba(255,255,255,0.08)",
                padding: "0 32px", display: "flex", alignItems: "center",
                justifyContent: "space-between", height: 60,
                position: "sticky", top: 0, zIndex: 50,
            }}>
                <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                    <span style={{ fontSize: 22 }}>🏥</span>
                    <span style={{ color: "#F0F6FF", fontWeight: 700, fontSize: 17 }}>IPTS</span>
                    <span style={{
                        background: "rgba(30,95,191,0.2)", color: "#7EB9FF",
                        padding: "2px 10px", borderRadius: 10, fontSize: 12, fontWeight: 600,
                    }}>Doctor</span>
                </div>
                <div style={{ display: "flex", alignItems: "center", gap: 16 }}>
                    <span style={{ color: "#8BA3C7", fontSize: 13 }}>{user?.fullName}</span>
                    <button
                        onClick={() => { clearAuth(); navigate("/login"); }}
                        style={{
                            background: "rgba(255,77,106,0.12)", border: "1px solid rgba(255,77,106,0.3)",
                            color: "#FF4D6A", borderRadius: 8, padding: "6px 14px", fontSize: 13, cursor: "pointer",
                        }}
                    >Logout</button>
                </div>
            </nav>

            <div style={{ maxWidth: 1000, margin: "0 auto", padding: "32px" }}>

                {/* HEADER */}
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 28 }}>
                    <div>
                        <h1 style={{ color: "#F0F6FF", fontSize: 24, fontWeight: 700, margin: 0 }}>
                            Transfer Requests
                        </h1>
                        <p style={{ color: "#8BA3C7", margin: "4px 0 0", fontSize: 14 }}>
                            Manage outgoing broadcasts and respond to incoming requests
                        </p>
                    </div>
                    <button
                        onClick={() => navigate("/transfers/new")}
                        style={{
                            background: "linear-gradient(135deg,#1E5FBF,#00C2D4)",
                            border: "none", borderRadius: 10, padding: "11px 20px",
                            color: "#fff", fontSize: 14, fontWeight: 600, cursor: "pointer",
                        }}
                    >+ New Transfer Request</button>
                </div>

                {/* ERROR */}
                {error && (
                    <div style={{
                        background: "rgba(255,77,106,0.12)", border: "1px solid rgba(255,77,106,0.3)",
                        borderRadius: 10, padding: "12px 18px", color: "#FF4D6A", marginBottom: 20, fontSize: 14,
                    }}>{error}</div>
                )}

                {/* TABS */}
                <div style={{
                    display: "flex", gap: 0,
                    borderBottom: "1px solid rgba(255,255,255,0.08)", marginBottom: 24,
                }}>
                    {[
                        { key: "sent" as const, label: `📤 My Broadcasts (${broadcasts.length})` },
                        { key: "incoming" as const, label: `📥 Incoming Requests${incoming.length > 0 ? ` (${incoming.length})` : ""}` },
                    ].map(tab => (
                        <button
                            key={tab.key}
                            onClick={() => setActiveTab(tab.key)}
                            style={{
                                background: "none", border: "none", cursor: "pointer",
                                padding: "10px 20px", fontSize: 14, fontWeight: 500,
                                color: activeTab === tab.key ? "#00C2D4" : "#8BA3C7",
                                borderBottom: activeTab === tab.key ? "2px solid #00C2D4" : "2px solid transparent",
                            }}
                        >{tab.label}</button>
                    ))}
                </div>

                {loading ? (
                    <div style={{ textAlign: "center", padding: 60, color: "#8BA3C7" }}>Loading...</div>
                ) : activeTab === "sent" ? (

                    // ── SENT BROADCASTS ──────────────────────────────────
                    <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
                        {broadcasts.length === 0 ? (
                            <div style={{
                                textAlign: "center", padding: 60, background: "#112240",
                                borderRadius: 14, border: "1px dashed rgba(255,255,255,0.1)",
                            }}>
                                <div style={{ fontSize: 48, marginBottom: 12 }}>📋</div>
                                <p style={{ color: "#8BA3C7", fontSize: 15, margin: "0 0 16px" }}>
                                    No transfer requests yet.
                                </p>
                                <button
                                    onClick={() => navigate("/transfers/new")}
                                    style={{
                                        background: "linear-gradient(135deg,#1E5FBF,#00C2D4)",
                                        border: "none", borderRadius: 8, padding: "10px 20px",
                                        color: "#fff", fontSize: 14, fontWeight: 600, cursor: "pointer",
                                    }}
                                >Create your first request</button>
                            </div>
                        ) : broadcasts.map(b => (
                            <div key={b.id} style={{
                                background: "#112240", border: "1px solid rgba(255,255,255,0.07)",
                                borderRadius: 14, padding: "20px 24px",
                            }}>
                                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
                                    <div>
                                        {/* Urgency + bed type */}
                                        <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 8 }}>
                                            <span style={{
                                                background: `${URGENCY_COLORS[b.urgency]}18`,
                                                color: URGENCY_COLORS[b.urgency],
                                                border: `1px solid ${URGENCY_COLORS[b.urgency]}44`,
                                                padding: "2px 10px", borderRadius: 8, fontSize: 12, fontWeight: 700,
                                            }}>{URGENCY_LABELS[b.urgency]}</span>
                                            <span style={{ color: "#F0F6FF", fontWeight: 600, fontSize: 15 }}>
                                                {b.bedTypeRequired} bed
                                                {b.equipmentNeeded !== "None" && ` + ${b.equipmentNeeded}`}
                                            </span>
                                        </div>
                                        <div style={{ color: "#8BA3C7", fontSize: 13 }}>
                                            Created: {new Date(b.createdAt).toLocaleString()}
                                        </div>
                                    </div>
                                    {/* Status badge */}
                                    <span style={{
                                        background: `${STATUS_COLORS[b.status]}18`,
                                        color: STATUS_COLORS[b.status],
                                        border: `1px solid ${STATUS_COLORS[b.status]}44`,
                                        padding: "4px 12px", borderRadius: 8, fontSize: 13, fontWeight: 600,
                                    }}>{STATUS_LABELS[b.status]}</span>
                                </div>

                                {/* Response summary — uses BroadcastSummaryDto fields */}
                                <div style={{
                                    display: "flex", gap: 20, marginTop: 14,
                                    padding: "12px 0 0", borderTop: "1px solid rgba(255,255,255,0.06)",
                                }}>
                                    <span style={{ color: "#FF9A3C", fontSize: 13 }}>
                                        ⏳ {b.totalResponses - b.acceptedResponses} pending / not accepted
                                    </span>
                                    <span style={{ color: "#00D68F", fontSize: 13 }}>
                                        ✅ {b.acceptedResponses} accepted
                                    </span>
                                    <span style={{ color: "#8BA3C7", fontSize: 13 }}>
                                        📬 {b.totalResponses} total notified
                                    </span>
                                </div>
                            </div>
                        ))}
                    </div>

                ) : (

                    // ── INCOMING REQUESTS ─────────────────────────────────
                    // GetIncomingRequestsAsync returns BroadcastSummaryDto[]
                    // so we only show the anonymous summary fields
                    <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
                        {incoming.length === 0 ? (
                            <div style={{
                                textAlign: "center", padding: 60, background: "#112240",
                                borderRadius: 14, border: "1px dashed rgba(255,255,255,0.1)",
                            }}>
                                <div style={{ fontSize: 48, marginBottom: 12 }}>📭</div>
                                <p style={{ color: "#8BA3C7", fontSize: 15, margin: 0 }}>
                                    No incoming transfer requests at the moment.
                                </p>
                            </div>
                        ) : incoming.map(b => (
                            <div key={b.id} style={{
                                background: "#112240",
                                border: `1px solid ${URGENCY_COLORS[b.urgency]}33`,
                                borderRadius: 14, overflow: "hidden",
                            }}>
                                {/* Urgency banner */}
                                <div style={{
                                    background: `${URGENCY_COLORS[b.urgency]}15`,
                                    borderBottom: `1px solid ${URGENCY_COLORS[b.urgency]}33`,
                                    padding: "10px 24px",
                                    display: "flex", alignItems: "center", gap: 10,
                                }}>
                                    <span style={{
                                        background: URGENCY_COLORS[b.urgency],
                                        color: "#0A1628", padding: "2px 10px",
                                        borderRadius: 8, fontSize: 12, fontWeight: 700,
                                    }}>{URGENCY_LABELS[b.urgency]}</span>
                                    <span style={{ color: "#8BA3C7", fontSize: 13 }}>
                                        Received: {new Date(b.createdAt).toLocaleString()}
                                    </span>
                                </div>

                                <div style={{ padding: "20px 24px" }}>
                                    {/* Anonymous info only — BroadcastSummaryDto */}
                                    <div style={{
                                        display: "grid", gridTemplateColumns: "1fr 1fr",
                                        gap: 16, marginBottom: 20,
                                    }}>
                                        <InfoBlock label="Bed Type Required" value={b.bedTypeRequired} />
                                        <InfoBlock label="Equipment Needed" value={b.equipmentNeeded || "None"} />
                                    </div>

                                    {/* Privacy notice */}
                                    <div style={{
                                        padding: "10px 14px", marginBottom: 20,
                                        background: "rgba(0,194,212,0.06)",
                                        border: "1px solid rgba(0,194,212,0.15)",
                                        borderRadius: 8,
                                    }}>
                                        <p style={{ color: "#8BA3C7", fontSize: 13, margin: 0 }}>
                                            🔒 <strong style={{ color: "#00C2D4" }}>Privacy Protected</strong> —
                                            Patient name, medical history, and personal data will only be revealed
                                            after you accept this request.
                                        </p>
                                    </div>

                                    {/* Accept / Decline */}
                                    <div style={{ display: "flex", gap: 12 }}>
                                        <button
                                            onClick={() => handleAccept(b.id)}
                                            disabled={responding === b.id}
                                            style={{
                                                flex: 1, background: "rgba(0,214,143,0.12)",
                                                border: "1px solid rgba(0,214,143,0.3)",
                                                borderRadius: 10, padding: "12px",
                                                color: "#00D68F", fontSize: 14, fontWeight: 700,
                                                cursor: responding === b.id ? "not-allowed" : "pointer",
                                            }}
                                        >
                                            {responding === b.id ? "Processing..." : "✓ Accept Transfer"}
                                        </button>
                                        <button
                                            onClick={() => setShowDeclineModal(b.id)}
                                            disabled={responding === b.id}
                                            style={{
                                                flex: 1, background: "rgba(255,77,106,0.12)",
                                                border: "1px solid rgba(255,77,106,0.3)",
                                                borderRadius: 10, padding: "12px",
                                                color: "#FF4D6A", fontSize: 14, fontWeight: 700,
                                                cursor: responding === b.id ? "not-allowed" : "pointer",
                                            }}
                                        >✕ Decline</button>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {/* DECLINE MODAL */}
            {showDeclineModal && (
                <div style={{
                    position: "fixed", inset: 0, background: "rgba(0,0,0,0.7)",
                    display: "flex", alignItems: "center", justifyContent: "center", zIndex: 1000,
                }}>
                    <div style={{
                        background: "#112240", border: "1px solid rgba(255,255,255,0.1)",
                        borderRadius: 16, padding: 32, width: "100%", maxWidth: 420,
                    }}>
                        <h3 style={{ color: "#F0F6FF", margin: "0 0 16px", fontSize: 17 }}>
                            Decline Transfer Request
                        </h3>
                        <label style={{ color: "#8BA3C7", fontSize: 13, display: "block", marginBottom: 8 }}>
                            Reason (optional)
                        </label>
                        <textarea
                            value={declineReason}
                            onChange={e => setDeclineReason(e.target.value)}
                            placeholder="e.g. No ICU capacity available..."
                            rows={3}
                            style={{
                                width: "100%", background: "#0A1628",
                                border: "1px solid rgba(255,255,255,0.1)", borderRadius: 8,
                                padding: "10px 12px", color: "#F0F6FF", fontSize: 14,
                                outline: "none", resize: "none", boxSizing: "border-box",
                            }}
                        />
                        <div style={{ display: "flex", gap: 10, justifyContent: "flex-end", marginTop: 16 }}>
                            <button
                                onClick={() => { setShowDeclineModal(null); setDeclineReason(""); }}
                                style={{
                                    background: "rgba(255,255,255,0.06)",
                                    border: "1px solid rgba(255,255,255,0.1)", borderRadius: 8,
                                    padding: "10px 18px", color: "#8BA3C7", cursor: "pointer", fontSize: 13,
                                }}
                            >Cancel</button>
                            <button
                                onClick={() => handleDecline(showDeclineModal)}
                                disabled={responding === showDeclineModal}
                                style={{
                                    background: "rgba(255,77,106,0.2)",
                                    border: "1px solid rgba(255,77,106,0.4)", borderRadius: 8,
                                    padding: "10px 18px", color: "#FF4D6A", cursor: "pointer",
                                    fontSize: 13, fontWeight: 600,
                                }}
                            >
                                {responding === showDeclineModal ? "Declining..." : "Confirm Decline"}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

function InfoBlock({ label, value }: { label: string; value: string }) {
    return (
        <div style={{ background: "rgba(255,255,255,0.04)", borderRadius: 10, padding: "12px 16px" }}>
            <div style={{ color: "#8BA3C7", fontSize: 11, marginBottom: 4 }}>{label}</div>
            <div style={{ color: "#F0F6FF", fontWeight: 600, fontSize: 15 }}>{value}</div>
        </div>
    );
}