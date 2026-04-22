import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { transferRequestsApi, getUser } from "../services/api";
import type { SubmitPatientDataRequest } from "../services/api";

const inputStyle: React.CSSProperties = {
    width: "100%", background: "#0A1628",
    border: "1px solid rgba(255,255,255,0.1)",
    borderRadius: 8, padding: "11px 12px",
    color: "#F0F6FF", fontSize: 14, outline: "none",
    boxSizing: "border-box" as const,
};
const labelStyle: React.CSSProperties = {
    color: "#8BA3C7", fontSize: 13, fontWeight: 500,
    display: "block", marginBottom: 8,
};

function getErrorMessage(error: unknown): string {
    if (error instanceof Error) return error.message;
    if (typeof error === "string") return error;
    return "An unexpected error occurred";
}

export default function SubmitPatientDataPage() {
    const navigate = useNavigate();
    const [params] = useSearchParams();
    const broadcastId = params.get("broadcastId") ?? "";
    const user = getUser();

    const [form, setForm] = useState<SubmitPatientDataRequest>({
        broadcastId,
        patientFullName: "",
        dateOfBirth: "",
        diagnosis: "",
        allergies: "None",
        currentMedications: "None",
        additionalNotes: "None",
        familyContactName: "",
        familyContactPhone: "",
    });
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");

    const set = (k: keyof SubmitPatientDataRequest) =>
        (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
            setForm(p => ({ ...p, [k]: e.target.value }));

    const handleSubmit = async () => {
        if (!form.patientFullName || !form.dateOfBirth || !form.diagnosis) {
            setError("Patient name, date of birth and diagnosis are required.");
            return;
        }
        setLoading(true); setError("");
        try {
            const result = await transferRequestsApi.submitPatientData(form);
            navigate(`/transfers/confirm/${result.id}`);
        } catch (e: unknown) {
            setError(getErrorMessage(e));
        } finally {
            setLoading(false);
        }
    };

    return (
        <div style={{
            minHeight: "100vh", background: "#0A1628",
            fontFamily: "'Inter', sans-serif"
        }}>

            {/* NAVBAR */}
            <nav style={{
                background: "#112240", borderBottom: "1px solid rgba(255,255,255,0.08)",
                padding: "0 32px", display: "flex", alignItems: "center",
                justifyContent: "space-between", height: 60
            }}>
                <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
                    <button onClick={() => navigate("/transfers")} style={{
                        background: "none", border: "none",
                        color: "#8BA3C7", cursor: "pointer", fontSize: 18,
                    }}>←</button>
                    <span style={{ color: "#F0F6FF", fontWeight: 700, fontSize: 17 }}>
                        Submit Patient Data
                    </span>
                </div>
                <span style={{ color: "#8BA3C7", fontSize: 13 }}>{user?.fullName}</span>
            </nav>

            <div style={{ maxWidth: 720, margin: "0 auto", padding: "32px" }}>

                {/* Security notice */}
                <div style={{
                    padding: 16, marginBottom: 24,
                    background: "rgba(146,43,33,0.1)",
                    border: "1px solid rgba(146,43,33,0.35)",
                    borderRadius: 10
                }}>
                    <p style={{ color: "#FF4D6A", fontSize: 13, margin: 0, lineHeight: 1.6 }}>
                        🔒 <strong>Phase 3 — Secure Patient Data Entry</strong><br />
                        This information will be AES-256 encrypted immediately.
                        It will be revealed ONLY to the accepting hospital doctor.
                        Once submitted, it cannot be edited.
                    </p>
                </div>

                <div style={{
                    background: "#112240",
                    border: "1px solid rgba(255,255,255,0.07)",
                    borderRadius: 16, padding: 32
                }}>

                    <h2 style={{ color: "#F0F6FF", fontSize: 20, fontWeight: 700, marginBottom: 24 }}>
                        Patient Information
                    </h2>

                    {error && (<div style={{
                        background: "rgba(255,77,106,0.12)",
                        border: "1px solid rgba(255,77,106,0.3)", borderRadius: 10,
                        padding: "12px 18px", color: "#FF4D6A", marginBottom: 20, fontSize: 14
                    }}>{error}</div>)}

                    <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 20 }}>

                        <div style={{ gridColumn: "1/-1" }}>
                            <label style={labelStyle}>Full Name *</label>
                            <input style={inputStyle} value={form.patientFullName}
                                onChange={set("patientFullName")} placeholder="John Doe" />
                        </div>

                        <div>
                            <label style={labelStyle}>Date of Birth *</label>
                            <input type="date" style={inputStyle}
                                value={form.dateOfBirth} onChange={set("dateOfBirth")} />
                        </div>

                        <div>
                            <label style={labelStyle}>Diagnosis *</label>
                            <input style={inputStyle} value={form.diagnosis}
                                onChange={set("diagnosis")} placeholder="Acute MI" />
                        </div>

                        <div>
                            <label style={labelStyle}>Known Allergies</label>
                            <input style={inputStyle} value={form.allergies}
                                onChange={set("allergies")} placeholder="None" />
                        </div>

                        <div>
                            <label style={labelStyle}>Current Medications</label>
                            <input style={inputStyle} value={form.currentMedications}
                                onChange={set("currentMedications")} placeholder="Aspirin 100mg" />
                        </div>

                        <div style={{ gridColumn: "1/-1" }}>
                            <label style={labelStyle}>Additional Notes</label>
                            <textarea rows={3} style={{ ...inputStyle, resize: "none" }}
                                value={form.additionalNotes}
                                onChange={set("additionalNotes")} placeholder="Any relevant notes..." />
                        </div>

                        <div>
                            <label style={labelStyle}>Family Contact Name</label>
                            <input style={inputStyle} value={form.familyContactName}
                                onChange={set("familyContactName")} placeholder="Jane Doe" />
                        </div>

                        <div>
                            <label style={labelStyle}>Family Contact Phone</label>
                            <input style={inputStyle} value={form.familyContactPhone}
                                onChange={set("familyContactPhone")} placeholder="+1-555-0100" />
                        </div>

                    </div>

                    <div style={{ display: "flex", justifyContent: "flex-end", marginTop: 28 }}>
                        <button onClick={handleSubmit} disabled={loading} style={{
                            background: loading
                                ? "rgba(255,255,255,0.06)"
                                : "linear-gradient(135deg,#1E5FBF,#00C2D4)",
                            border: "none", borderRadius: 10, padding: "13px 28px",
                            color: "#fff", fontSize: 15, fontWeight: 600,
                            cursor: loading ? "not-allowed" : "pointer",
                        }}>
                            {loading ? "Encrypting & Submitting..." : "🔒 Submit Encrypted Patient Data"}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
}
