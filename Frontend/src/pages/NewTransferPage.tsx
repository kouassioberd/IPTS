// src/pages/NewTransferPage.tsx
// Sending Doctor 4-step flow:
// Step 1 — fill anonymous requirements form
// Step 2 — see ranked matching hospitals
// Step 3 — select and notify hospitals
// Step 4 — waiting room polling for responses

import { useState, useEffect, useRef } from "react";
import type { CSSProperties } from "react";
import { useNavigate } from "react-router-dom";
import {
    transfersApi,
    getUser,
    URGENCY_LABELS,
    URGENCY_COLORS,
    RESPONSE_LABELS,
    RESPONSE_COLORS,
} from "../services/api";
import type {
    CreateBroadcastRequest,
    BroadcastDto,
    HospitalMatchDto,
    NotifyHospitalsRequest,
} from "../services/api";


function getErrorMessage(error: unknown): string {
    if (error instanceof Error) return error.message;
    if (typeof error === "string") return error;
    return "An unexpected error occurred";
}

const WARD_TYPES = ["ICU", "ER", "General", "Surgery", "Pediatric", "Cardiology"];
const EQUIPMENT = ["None", "Ventilator", "Defibrillator", "Dialysis", "ECMO", "Infusion Pump"];
const INSURANCE = ["None", "Medicare", "Medicaid", "BlueCross", "Aetna", "Cigna", "Private"];

type Step = 1 | 2 | 3 | 4;

const labelStyle: CSSProperties = {
    color: "#8BA3C7", fontSize: 13, fontWeight: 500,
    display: "block", marginBottom: 8,
};
const selectStyle: CSSProperties = {
    width: "100%", background: "#0A1628",
    border: "1px solid rgba(255,255,255,0.1)",
    borderRadius: 8, padding: "11px 12px",
    color: "#F0F6FF", fontSize: 14, outline: "none",
};

export default function NewTransferPage() {
    const navigate = useNavigate();
    const user = getUser();

    const [step, setStep] = useState<Step>(1);
    const [form, setForm] = useState<CreateBroadcastRequest>({
        bedTypeRequired: "ICU",
        equipmentNeeded: "None",
        insuranceType: "None",
        maxDistanceMiles: 20,
        urgency: 1,
    });

    // Step 2 — matching results come from MatchingResultDto.matches
    const [matches, setMatches] = useState<HospitalMatchDto[]>([]);
    const [selectedHospitals, setSelected] = useState<Set<string>>(new Set());

    // Step 3–4 — full broadcast with responses
    const [broadcast, setBroadcast] = useState<BroadcastDto | null>(null);

    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");

    // Polling ref — cleaned up on unmount
    const pollRef = useRef<ReturnType<typeof setInterval> | null>(null);

    useEffect(() => {
        if (!user) navigate("/login");
        return () => {
            if (pollRef.current) clearInterval(pollRef.current);
        };
    }, []);

    // Start polling when entering waiting room (Step 4)
    useEffect(() => {
        if (step === 4 && broadcast) {
            pollRef.current = setInterval(async () => {
                try {
                    // GET /api/Transfers/broadcast/{id} returns BroadcastDto
                    const updated = await transfersApi.getById(broadcast.id);
                    setBroadcast(updated);
                } catch { /* silent — keep polling */ }
            }, 5000);
        } else {
            if (pollRef.current) {
                clearInterval(pollRef.current);
                pollRef.current = null;
            }
        }
        return () => {
            if (pollRef.current) clearInterval(pollRef.current);
        };
    }, [step, broadcast?.id]);

    // ── STEP 1 → 2: Create broadcast then get matches ─────────────
    const handleGetMatches = async () => {
        setLoading(true);
        setError("");
        try {
            // POST /api/Transfers/broadcast → BroadcastDto
            const created = await transfersApi.createBroadcast(form);
            setBroadcast(created);

            // GET /api/Transfers/broadcast/{id}/matches → MatchingResultDto
            const result = await transfersApi.getMatches(created.id);
            setMatches(result.matches);   // MatchingResultDto.matches is HospitalMatchDto[]
            setStep(2);
        } catch (e: unknown) {
            setError(getErrorMessage(e));
        } finally {
            setLoading(false);
        }
    };

    // ── STEP 2 → 3: Notify selected hospitals ─────────────────────
    const handleNotify = async () => {
        if (!broadcast || selectedHospitals.size === 0) return;
        setLoading(true);
        setError("");
        try {
            // POST /api/Transfers/broadcast/notify — body: NotifyHospitalsRequest
            const body: NotifyHospitalsRequest = {
                broadcastId: broadcast.id,
                hospitalIds: Array.from(selectedHospitals),
            };
            await transfersApi.notifyHospitals(body);
            setStep(3);
        } catch (e: unknown) {
            setError(getErrorMessage(e));
        } finally {
            setLoading(false);
        }
    };

    const toggleHospital = (hospitalId: string) => {
        setSelected(prev => {
            const next = new Set(prev);
            if (next.has(hospitalId)) {
                next.delete(hospitalId);
            } else {
                next.add(hospitalId);
            }
            return next;
        });
    };

    return (
        <div style={{ minHeight: "100vh", background: "#0A1628", fontFamily: "'Inter', sans-serif" }}>

            {/* NAVBAR */}
            <nav style={{
                background: "#112240", borderBottom: "1px solid rgba(255,255,255,0.08)",
                padding: "0 32px", display: "flex", alignItems: "center",
                justifyContent: "space-between", height: 60,
            }}>
                <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                    <button
                        onClick={() => navigate("/transfers")}
                        style={{ background: "none", border: "none", color: "#8BA3C7", cursor: "pointer", fontSize: 18 }}
                    >←</button>
                    <span style={{ color: "#F0F6FF", fontWeight: 700, fontSize: 17 }}>
                        New Transfer Request
                    </span>
                </div>
                <span style={{ color: "#8BA3C7", fontSize: 13 }}>{user?.fullName}</span>
            </nav>

            <div style={{ maxWidth: 900, margin: "0 auto", padding: "32px" }}>

                {/* STEP INDICATOR */}
                <div style={{ display: "flex", alignItems: "center", marginBottom: 40 }}>
                    {[
                        { n: 1, label: "Requirements" },
                        { n: 2, label: "Matching Results" },
                        { n: 3, label: "Notify Hospitals" },
                        { n: 4, label: "Waiting Room" },
                    ].map(({ n, label }, i) => (
                        <div key={n} style={{ display: "flex", alignItems: "center" }}>
                            <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 6 }}>
                                <div style={{
                                    width: 36, height: 36, borderRadius: "50%",
                                    background: step >= n ? "linear-gradient(135deg,#1E5FBF,#00C2D4)" : "rgba(255,255,255,0.06)",
                                    border: step === n ? "2px solid #00C2D4" : "2px solid transparent",
                                    display: "flex", alignItems: "center", justifyContent: "center",
                                    color: "#fff", fontWeight: 700, fontSize: 14,
                                    boxShadow: step === n ? "0 0 20px rgba(0,194,212,0.4)" : "none",
                                }}>{step > n ? "✓" : n}</div>
                                <span style={{ fontSize: 11, color: step >= n ? "#F0F6FF" : "#8BA3C7", textAlign: "center", maxWidth: 80 }}>
                                    {label}
                                </span>
                            </div>
                            {i < 3 && (
                                <div style={{
                                    width: 80, height: 2, margin: "0 4px", marginBottom: 20,
                                    background: step > n ? "linear-gradient(90deg,#1E5FBF,#00C2D4)" : "rgba(255,255,255,0.08)",
                                }} />
                            )}
                        </div>
                    ))}
                </div>

                {/* ERROR */}
                {error && (
                    <div style={{
                        background: "rgba(255,77,106,0.12)", border: "1px solid rgba(255,77,106,0.3)",
                        borderRadius: 10, padding: "14px 18px", color: "#FF4D6A", marginBottom: 24, fontSize: 14,
                    }}>{error}</div>
                )}

                {/* ── STEP 1: REQUIREMENTS FORM ── */}
                {step === 1 && (
                    <div style={{
                        background: "#112240", border: "1px solid rgba(255,255,255,0.07)",
                        borderRadius: 16, padding: 32,
                    }}>
                        <h2 style={{ color: "#F0F6FF", fontSize: 20, fontWeight: 700, marginBottom: 8 }}>
                            Anonymous Transfer Request
                        </h2>
                        <p style={{ color: "#8BA3C7", fontSize: 14, marginBottom: 28, lineHeight: 1.6 }}>
                            Enter only medical requirements. No patient name or personal data at this stage.
                        </p>

                        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 20 }}>

                            {/* Urgency */}
                            <div style={{ gridColumn: "1/-1" }}>
                                <label style={labelStyle}>Urgency Level</label>
                                <div style={{ display: "flex", gap: 12 }}>
                                    {Object.entries(URGENCY_LABELS).map(([k, label]) => {
                                        const i = Number(k);
                                        return (
                                            <button
                                                key={i}
                                                onClick={() => setForm(p => ({ ...p, urgency: i }))}
                                                style={{
                                                    flex: 1, padding: "12px",
                                                    background: form.urgency === i ? `${URGENCY_COLORS[i]}22` : "rgba(255,255,255,0.04)",
                                                    border: `2px solid ${form.urgency === i ? URGENCY_COLORS[i] : "rgba(255,255,255,0.1)"}`,
                                                    borderRadius: 10, cursor: "pointer",
                                                    color: form.urgency === i ? URGENCY_COLORS[i] : "#8BA3C7",
                                                    fontWeight: form.urgency === i ? 700 : 400,
                                                    fontSize: 14, transition: "all 0.2s",
                                                }}
                                            >{label}</button>
                                        );
                                    })}
                                </div>
                            </div>

                            {/* Bed Type */}
                            <div>
                                <label style={labelStyle}>Bed Type Required</label>
                                <select
                                    value={form.bedTypeRequired}
                                    onChange={e => setForm(p => ({ ...p, bedTypeRequired: e.target.value }))}
                                    style={selectStyle}
                                >
                                    {WARD_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
                                </select>
                            </div>

                            {/* Equipment */}
                            <div>
                                <label style={labelStyle}>Equipment Needed</label>
                                <select
                                    value={form.equipmentNeeded}
                                    onChange={e => setForm(p => ({ ...p, equipmentNeeded: e.target.value }))}
                                    style={selectStyle}
                                >
                                    {EQUIPMENT.map(e => <option key={e} value={e}>{e}</option>)}
                                </select>
                            </div>

                            {/* Insurance */}
                            <div>
                                <label style={labelStyle}>Insurance Type</label>
                                <select
                                    value={form.insuranceType}
                                    onChange={e => setForm(p => ({ ...p, insuranceType: e.target.value }))}
                                    style={selectStyle}
                                >
                                    {INSURANCE.map(ins => <option key={ins} value={ins}>{ins}</option>)}
                                </select>
                            </div>

                            {/* Distance */}
                            <div>
                                <label style={labelStyle}>Max Distance: {form.maxDistanceMiles} miles</label>
                                <input
                                    type="range" min={5} max={100} step={5}
                                    value={form.maxDistanceMiles}
                                    onChange={e => setForm(p => ({ ...p, maxDistanceMiles: Number(e.target.value) }))}
                                    style={{ width: "100%", accentColor: "#00C2D4" }}
                                />
                                <div style={{ display: "flex", justifyContent: "space-between" }}>
                                    <span style={{ color: "#8BA3C7", fontSize: 11 }}>5 miles</span>
                                    <span style={{ color: "#8BA3C7", fontSize: 11 }}>100 miles</span>
                                </div>
                            </div>
                        </div>

                        {/* Privacy notice */}
                        <div style={{
                            marginTop: 24, padding: 16,
                            background: "rgba(0,214,143,0.06)", border: "1px solid rgba(0,214,143,0.2)",
                            borderRadius: 10,
                        }}>
                            <p style={{ color: "#00D68F", fontSize: 13, margin: 0, lineHeight: 1.6 }}>
                                🔒 <strong>Privacy Protected:</strong> No patient name, DOB, diagnosis, or personal
                                data will be shared at this stage. Matching hospitals will only see these
                                medical requirements.
                            </p>
                        </div>

                        <div style={{ display: "flex", justifyContent: "flex-end", marginTop: 24 }}>
                            <button
                                onClick={handleGetMatches}
                                disabled={loading}
                                style={{
                                    background: "linear-gradient(135deg,#1E5FBF,#00C2D4)",
                                    border: "none", borderRadius: 10, padding: "13px 28px",
                                    color: "#fff", fontSize: 15, fontWeight: 600,
                                    cursor: loading ? "not-allowed" : "pointer",
                                }}
                            >
                                {loading ? "Finding matches..." : "Find Matching Hospitals →"}
                            </button>
                        </div>
                    </div>
                )}

                {/* ── STEP 2: MATCHING RESULTS ── */}
                {step === 2 && (
                    <div>
                        <div style={{
                            background: "#112240", border: "1px solid rgba(255,255,255,0.07)",
                            borderRadius: 16, padding: 24, marginBottom: 24,
                        }}>
                            <h2 style={{ color: "#F0F6FF", fontSize: 20, fontWeight: 700, margin: "0 0 6px" }}>
                                {matches.length} Matching Hospital{matches.length !== 1 ? "s" : ""} Found
                            </h2>
                            <p style={{ color: "#8BA3C7", fontSize: 14, margin: 0 }}>
                                Ranked by composite score: distance + beds + response history.
                                Select the hospitals you want to notify.
                            </p>
                        </div>

                        {matches.length === 0 ? (
                            <div style={{
                                textAlign: "center", padding: 60, background: "#112240",
                                borderRadius: 16, border: "1px dashed rgba(255,255,255,0.1)",
                            }}>
                                <div style={{ fontSize: 48, marginBottom: 16 }}>🏥</div>
                                <p style={{ color: "#8BA3C7", fontSize: 16 }}>
                                    No hospitals match your requirements. Try increasing the distance or changing the bed type.
                                </p>
                                <button
                                    onClick={() => setStep(1)}
                                    style={{
                                        marginTop: 16, background: "rgba(255,255,255,0.08)",
                                        border: "1px solid rgba(255,255,255,0.15)", borderRadius: 8,
                                        padding: "10px 20px", color: "#F0F6FF", cursor: "pointer",
                                    }}
                                >← Adjust Requirements</button>
                            </div>
                        ) : (
                            <>
                                <div style={{ display: "flex", flexDirection: "column", gap: 12, marginBottom: 24 }}>
                                    {matches.map((match, index) => (
                                        <div
                                            key={match.hospitalId}
                                            onClick={() => toggleHospital(match.hospitalId)}
                                            style={{
                                                background: selectedHospitals.has(match.hospitalId)
                                                    ? "linear-gradient(135deg,rgba(30,95,191,0.2),rgba(0,194,212,0.1))"
                                                    : "#112240",
                                                border: `2px solid ${selectedHospitals.has(match.hospitalId) ? "#00C2D4" : "rgba(255,255,255,0.07)"}`,
                                                borderRadius: 14, padding: "20px 24px",
                                                cursor: "pointer", transition: "all 0.2s",
                                            }}
                                        >
                                            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
                                                <div style={{ display: "flex", alignItems: "center", gap: 14 }}>
                                                    {/* Rank badge */}
                                                    <div style={{
                                                        width: 36, height: 36, borderRadius: "50%", flexShrink: 0,
                                                        background: index === 0
                                                            ? "linear-gradient(135deg,#FFD700,#FFA500)"
                                                            : index === 1
                                                                ? "linear-gradient(135deg,#C0C0C0,#A0A0A0)"
                                                                : "rgba(255,255,255,0.1)",
                                                        display: "flex", alignItems: "center", justifyContent: "center",
                                                        fontWeight: 700, fontSize: 14, color: "#0A1628",
                                                    }}>#{index + 1}</div>

                                                    <div>
                                                        <div style={{ color: "#F0F6FF", fontWeight: 700, fontSize: 16 }}>
                                                            {match.hospitalName}
                                                            {selectedHospitals.has(match.hospitalId) && (
                                                                <span style={{
                                                                    marginLeft: 10, fontSize: 12, color: "#00C2D4",
                                                                    background: "rgba(0,194,212,0.15)",
                                                                    padding: "2px 8px", borderRadius: 6,
                                                                }}>Selected</span>
                                                            )}
                                                        </div>
                                                        <div style={{ color: "#8BA3C7", fontSize: 13, marginTop: 2 }}>
                                                            📍 {match.address} · {match.distanceMiles.toFixed(1)} miles away
                                                        </div>
                                                    </div>
                                                </div>

                                                {/* Score circle */}
                                                <div style={{
                                                    width: 56, height: 56, borderRadius: "50%", flexShrink: 0,
                                                    background: `conic-gradient(#00C2D4 ${match.score * 3.6}deg, rgba(255,255,255,0.06) 0deg)`,
                                                    display: "flex", alignItems: "center", justifyContent: "center",
                                                }}>
                                                    <div style={{
                                                        width: 42, height: 42, borderRadius: "50%",
                                                        background: "#112240",
                                                        display: "flex", alignItems: "center", justifyContent: "center",
                                                        flexDirection: "column",
                                                    }}>
                                                        <span style={{ color: "#00C2D4", fontWeight: 700, fontSize: 14, lineHeight: 1 }}>
                                                            {match.score}
                                                        </span>
                                                        <span style={{ color: "#8BA3C7", fontSize: 9 }}>/ 100</span>
                                                    </div>
                                                </div>
                                            </div>

                                            {/* Score breakdown — using exact HospitalMatchDto field names */}
                                            <div style={{
                                                display: "grid", gridTemplateColumns: "repeat(4,1fr)",
                                                gap: 10, marginTop: 16,
                                            }}>
                                                {[
                                                    { label: "Distance", score: match.distanceScore, max: 30 },
                                                    { label: "Beds", score: match.bedScore, max: 30 },
                                                    { label: "Response Rate", score: match.responseRateScore, max: 20 },
                                                    // AvgAcceptTimeScore is the exact field name from C# HospitalMatchDto
                                                    { label: "Accept Speed", score: match.avgAcceptTimeScore, max: 20 },
                                                ].map(s => (
                                                    <div key={s.label} style={{
                                                        background: "rgba(255,255,255,0.04)",
                                                        borderRadius: 8, padding: "10px 12px",
                                                    }}>
                                                        <div style={{ color: "#8BA3C7", fontSize: 11, marginBottom: 6 }}>{s.label}</div>
                                                        <div style={{
                                                            height: 4, background: "rgba(255,255,255,0.06)",
                                                            borderRadius: 2, overflow: "hidden",
                                                        }}>
                                                            <div style={{
                                                                height: "100%",
                                                                width: `${(s.score / s.max) * 100}%`,
                                                                background: "linear-gradient(90deg,#1E5FBF,#00C2D4)",
                                                                borderRadius: 2,
                                                            }} />
                                                        </div>
                                                        <div style={{ color: "#00C2D4", fontSize: 12, fontWeight: 600, marginTop: 4 }}>
                                                            {s.score}/{s.max}
                                                        </div>
                                                    </div>
                                                ))}
                                            </div>

                                            {/* Quick stats */}
                                            <div style={{ display: "flex", gap: 20, marginTop: 14 }}>
                                                <span style={{ color: "#00D68F", fontSize: 13 }}>
                                                    🛏 {match.availableBeds} beds available
                                                </span>
                                                <span style={{ color: "#8BA3C7", fontSize: 13 }}>
                                                    ⏱ Avg response: {match.avgResponseTimeMinutes.toFixed(0)} min
                                                </span>
                                                <span style={{ color: "#8BA3C7", fontSize: 13 }}>
                                                    ✅ {match.acceptanceRate.toFixed(0)}% acceptance rate
                                                </span>
                                            </div>
                                        </div>
                                    ))}
                                </div>

                                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                                    <button
                                        onClick={() => setStep(1)}
                                        style={{
                                            background: "rgba(255,255,255,0.06)",
                                            border: "1px solid rgba(255,255,255,0.1)", borderRadius: 10,
                                            padding: "12px 20px", color: "#8BA3C7", cursor: "pointer", fontSize: 14,
                                        }}
                                    >← Adjust Requirements</button>

                                    <button
                                        onClick={handleNotify}
                                        disabled={selectedHospitals.size === 0 || loading}
                                        style={{
                                            background: selectedHospitals.size === 0
                                                ? "rgba(255,255,255,0.06)"
                                                : "linear-gradient(135deg,#1E5FBF,#00C2D4)",
                                            border: "none", borderRadius: 10, padding: "12px 24px",
                                            color: selectedHospitals.size === 0 ? "#8BA3C7" : "#fff",
                                            cursor: selectedHospitals.size === 0 ? "not-allowed" : "pointer",
                                            fontSize: 14, fontWeight: 600,
                                        }}
                                    >
                                        {loading
                                            ? "Notifying..."
                                            : `Notify ${selectedHospitals.size} Hospital${selectedHospitals.size !== 1 ? "s" : ""} →`}
                                    </button>
                                </div>
                            </>
                        )}
                    </div>
                )}

                {/* ── STEP 3: CONFIRMATION ── */}
                {step === 3 && (
                    <div style={{
                        background: "#112240", border: "1px solid rgba(0,214,143,0.3)",
                        borderRadius: 16, padding: 40, textAlign: "center",
                    }}>
                        <div style={{ fontSize: 56, marginBottom: 16 }}>✅</div>
                        <h2 style={{ color: "#00D68F", fontSize: 22, fontWeight: 700, marginBottom: 10 }}>
                            Hospitals Notified
                        </h2>
                        <p style={{ color: "#8BA3C7", fontSize: 15, marginBottom: 28, lineHeight: 1.6 }}>
                            {selectedHospitals.size} hospital{selectedHospitals.size !== 1 ? "s have" : " has"} been
                            notified. They can see only the medical requirements — no patient data yet.
                        </p>
                        <button
                            onClick={() => setStep(4)}
                            style={{
                                background: "linear-gradient(135deg,#1E5FBF,#00C2D4)",
                                border: "none", borderRadius: 10, padding: "13px 28px",
                                color: "#fff", fontSize: 15, fontWeight: 600, cursor: "pointer",
                            }}
                        >Go to Waiting Room →</button>
                    </div>
                )}

                {/* ── STEP 4: WAITING ROOM ── */}
                {step === 4 && broadcast && (
                    <div>
                        <div style={{
                            background: "#112240", border: "1px solid rgba(255,255,255,0.07)",
                            borderRadius: 16, padding: 24, marginBottom: 20,
                        }}>
                            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                                <div>
                                    <h2 style={{ color: "#F0F6FF", fontSize: 20, fontWeight: 700, margin: "0 0 4px" }}>
                                        Waiting for Responses
                                    </h2>
                                    <p style={{ color: "#8BA3C7", fontSize: 13, margin: 0 }}>
                                        Auto-refreshing every 5 seconds ·{" "}
                                        {broadcast.responses.length} hospital{broadcast.responses.length !== 1 ? "s" : ""} notified
                                    </p>
                                </div>
                                {/* Live pulse dot */}
                                <div style={{
                                    width: 12, height: 12, borderRadius: "50%", background: "#00D68F",
                                    animation: "pulse 2s infinite",
                                }} />
                            </div>
                        </div>

                        {/* Response list — BroadcastDto.responses is HospitalResponseDto[] */}
                        <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
                            {broadcast.responses.map(r => (
                                <div key={r.id} style={{
                                    display: "flex", justifyContent: "space-between", alignItems: "center",
                                    background: "#112240",
                                    border: `1px solid ${RESPONSE_COLORS[r.response]}33`,
                                    borderRadius: 12, padding: "18px 24px",
                                }}>
                                    <div>
                                        <div style={{ color: "#F0F6FF", fontWeight: 600, fontSize: 15 }}>
                                            {r.receivingHospitalName}
                                        </div>
                                        {r.declineReason && (
                                            <div style={{ color: "#8BA3C7", fontSize: 13, marginTop: 4 }}>
                                                Reason: {r.declineReason}
                                            </div>
                                        )}
                                        {r.respondedAt && (
                                            <div style={{ color: "#8BA3C7", fontSize: 12, marginTop: 2 }}>
                                                {new Date(r.respondedAt).toLocaleTimeString()}
                                            </div>
                                        )}
                                    </div>
                                    <span style={{
                                        background: `${RESPONSE_COLORS[r.response]}18`,
                                        color: RESPONSE_COLORS[r.response],
                                        border: `1px solid ${RESPONSE_COLORS[r.response]}44`,
                                        padding: "5px 14px", borderRadius: 8, fontSize: 13, fontWeight: 600,
                                    }}>
                                        {RESPONSE_LABELS[r.response]}
                                    </span>
                                </div>
                            ))}

                            {broadcast.responses.length === 0 && (
                                <div style={{
                                    textAlign: "center", padding: 40, color: "#8BA3C7",
                                    background: "#112240", borderRadius: 14,
                                    border: "1px dashed rgba(255,255,255,0.1)",
                                }}>
                                    Waiting for hospitals to respond...
                                </div>
                            )}
                        </div>

                        {/* Banner when at least one hospital accepted */}
                        {broadcast.responses.some(r => r.response === 1) && (
                            <div style={{
                                marginTop: 24, padding: 20,
                                background: "rgba(0,214,143,0.08)", border: "1px solid rgba(0,214,143,0.25)",
                                borderRadius: 12,
                            }}>
                                <p style={{ color: "#00D68F", fontSize: 15, margin: "0 0 12px", fontWeight: 600 }}>
                                    ✅ A hospital has accepted! Proceed to confirm the transfer and reveal patient data.
                                </p>
                                <button
                                    onClick={() => navigate("/transfers")}
                                    style={{
                                        background: "linear-gradient(135deg,#1E5FBF,#00C2D4)",
                                        border: "none", borderRadius: 8, padding: "10px 20px",
                                        color: "#fff", fontSize: 14, fontWeight: 600, cursor: "pointer",
                                    }}
                                >Continue to Transfer Confirmation →</button>
                            </div>
                        )}
                    </div>
                )}
            </div>

            <style>{`
                @keyframes pulse {
                    0%, 100% { opacity: 1; transform: scale(1); }
                    50%       { opacity: 0.5; transform: scale(1.4); }
                }
            `}</style>
        </div>
    );
}