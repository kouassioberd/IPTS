import { useState, useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
    transferRequestsApi, getUser,
    TRANSFER_STATUS_LABELS, TRANSFER_STATUS_COLORS,
} from "../services/api";
import type {
    TransferRequestDto, DecryptedPatientDataDto, AuditLogDto,
} from "../services/api";


function getErrorMessage(error: unknown): string {
    if (error instanceof Error) return error.message;
    if (typeof error === "string") return error;
    return "An unexpected error occurred";
}
export default function TransferConfirmPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const user = getUser();
    const [transfer, setTransfer] = useState<TransferRequestDto | null>(null);
    const [patientData, setPatientData] = useState<DecryptedPatientDataDto | null>(null);
    const [auditLog, setAuditLog] = useState<AuditLogDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [revealing, setRevealing] = useState(false);
    const [error, setError] = useState("");

    useEffect(() => {
        if (!id) return;
        loadData();
    }, [id]);

    const loadData = async () => {
        if (!id) return;
        setLoading(true);
        try {
            const [t, logs] = await Promise.all([
                transferRequestsApi.getById(id),
                transferRequestsApi.getAuditLog(id),
            ]);
            setTransfer(t);
            setAuditLog(logs);
        } catch (e: unknown) {
            setError(getErrorMessage(e));
        } finally {
            setLoading(false);
        }
    };

    const handleRevealPatientData = async () => {
        if (!id) return;
        setRevealing(true); setError("");
        try {
            const data = await transferRequestsApi.getPatientData(id);
            setPatientData(data);
            await loadData(); // refresh audit log
        } catch (e: unknown) {
            setError(getErrorMessage(e));
        } finally {
            setRevealing(false);
        }
    };

    // Compares hospitalId from JWT with the transfer's receivingHospitalId
    const isReceivingHospital = Boolean(
        transfer && user && transfer.receivingHospitalId === user.hospitalId
    );

    if (loading) return (
        <div style={{
            minHeight: "100vh", background: "#0A1628",
            display: "flex", alignItems: "center", justifyContent: "center",
            color: "#8BA3C7", fontFamily: "'Inter', sans-serif"
        }}>
            Loading transfer...
        </div>
    );

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
                justifyContent: "space-between", height: 60
            }}>
                <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
                    <button onClick={() => navigate("/transfers")} style={{
                        background: "none", border: "none",
                        color: "#8BA3C7", cursor: "pointer", fontSize: 18,
                    }}>←</button>
                    <span style={{ color: "#F0F6FF", fontWeight: 700, fontSize: 17 }}>
                        Transfer Confirmation
                    </span>
                </div>
                <span style={{ color: "#8BA3C7", fontSize: 13 }}>{user?.fullName}</span>
            </nav>

            <div style={{ maxWidth: 860, margin: "0 auto", padding: "32px" }}>

                {error && (<div style={{
                    background: "rgba(255,77,106,0.12)",
                    border: "1px solid rgba(255,77,106,0.3)", borderRadius: 10,
                    padding: "12px 18px", color: "#FF4D6A", marginBottom: 20, fontSize: 14
                }}>{error}</div>)}

                {/* Transfer status card */}
                {transfer && (
                    <div style={{
                        background: "#112240",
                        border: "1px solid rgba(255,255,255,0.07)",
                        borderRadius: 16, padding: 24, marginBottom: 24
                    }}>

                        <div style={{
                            display: "flex", justifyContent: "space-between",
                            alignItems: "flex-start"
                        }}>
                            <div>
                                <h2 style={{
                                    color: "#F0F6FF", fontSize: 20,
                                    fontWeight: 700, margin: "0 0 8px"
                                }}>
                                    Transfer #{transfer.id.substring(0, 8).toUpperCase()}
                                </h2>
                                <p style={{ color: "#8BA3C7", fontSize: 14, margin: "0 0 4px" }}>
                                    From: {transfer.sendingHospitalName}
                                </p>
                                <p style={{ color: "#8BA3C7", fontSize: 14, margin: 0 }}>
                                    To: {transfer.receivingHospitalName}
                                </p>
                            </div>
                            <span style={{
                                background: `${TRANSFER_STATUS_COLORS[transfer.status]}18`,
                                color: TRANSFER_STATUS_COLORS[transfer.status],
                                border: `1px solid ${TRANSFER_STATUS_COLORS[transfer.status]}44`,
                                padding: "6px 14px", borderRadius: 8,
                                fontSize: 13, fontWeight: 700,
                            }}>
                                {TRANSFER_STATUS_LABELS[transfer.status]}
                            </span>
                        </div>

                        {/* Receiving hospital: Reveal Patient Data button */}
                        {!patientData && transfer.patientDataSubmitted && isReceivingHospital && (
                            <div style={{
                                marginTop: 20, padding: 16,
                                background: "rgba(0,194,212,0.06)",
                                border: "1px solid rgba(0,194,212,0.2)",
                                borderRadius: 10
                            }}>
                                <p style={{
                                    color: "#8BA3C7", fontSize: 13,
                                    margin: "0 0 12px"
                                }}>
                                    Patient data has been submitted and is ready to be revealed.
                                </p>
                                <button onClick={handleRevealPatientData}
                                    disabled={revealing} style={{
                                        background: revealing
                                            ? "rgba(255,255,255,0.06)"
                                            : "linear-gradient(135deg,#1E5FBF,#00C2D4)",
                                        border: "none", borderRadius: 8,
                                        padding: "11px 20px", color: "#fff",
                                        fontSize: 14, fontWeight: 600,
                                        cursor: revealing ? "not-allowed" : "pointer",
                                    }}>
                                    {revealing ? "Decrypting..." : "🔓 Reveal Patient Data"}
                                </button>
                            </div>
                        )}
                    </div>
                )}

                {/* Decrypted patient data */}
                {patientData && (
                    <div style={{
                        background: "#112240",
                        border: "1px solid rgba(0,214,143,0.3)",
                        borderRadius: 16, padding: 24, marginBottom: 24
                    }}>

                        <div style={{
                            display: "flex", justifyContent: "space-between",
                            alignItems: "center", marginBottom: 20
                        }}>
                            <h3 style={{
                                color: "#00D68F", fontSize: 17,
                                fontWeight: 700, margin: 0
                            }}>
                                🔓 Patient Data Revealed
                            </h3>
                            <span style={{ color: "#8BA3C7", fontSize: 12 }}>
                                Revealed: {new Date(patientData.revealedAt).toLocaleString()}
                            </span>
                        </div>

                        <div style={{
                            display: "grid",
                            gridTemplateColumns: "1fr 1fr", gap: 16
                        }}>
                            {[
                                ["Full Name", patientData.patientFullName],
                                ["Date of Birth", patientData.dateOfBirth],
                                ["Diagnosis", patientData.diagnosis],
                                ["Allergies", patientData.allergies],
                                ["Current Medications", patientData.currentMedications],
                                ["Additional Notes", patientData.additionalNotes],
                                ["Family Contact", patientData.familyContactName],
                                ["Family Phone", patientData.familyContactPhone],
                            ].map(([label, value]) => (
                                <div key={label} style={{
                                    background: "rgba(255,255,255,0.04)",
                                    borderRadius: 10, padding: "12px 16px"
                                }}>
                                    <div style={{
                                        color: "#8BA3C7", fontSize: 11,
                                        marginBottom: 4
                                    }}>{label}</div>
                                    <div style={{
                                        color: "#F0F6FF", fontWeight: 600,
                                        fontSize: 14
                                    }}>{value}</div>
                                </div>
                            ))}
                        </div>
                    </div>
                )}

                {/* Audit log */}
                {auditLog.length > 0 && (
                    <div style={{
                        background: "#112240",
                        border: "1px solid rgba(255,255,255,0.07)",
                        borderRadius: 16, padding: 24
                    }}>
                        <h3 style={{
                            color: "#F0F6FF", fontSize: 16,
                            fontWeight: 700, margin: "0 0 16px"
                        }}>
                            Audit Trail
                        </h3>
                        <div style={{
                            display: "flex",
                            flexDirection: "column", gap: 10
                        }}>
                            {auditLog.map(log => (
                                <div key={log.id} style={{
                                    display: "flex", gap: 16, alignItems: "flex-start",
                                    padding: "12px 0",
                                    borderBottom: "1px solid rgba(255,255,255,0.05)",
                                }}>
                                    <div style={{
                                        width: 2, minHeight: 32,
                                        background: "#00C2D4",
                                        borderRadius: 2, flexShrink: 0
                                    }} />
                                    <div>
                                        <div style={{
                                            color: "#F0F6FF",
                                            fontWeight: 600, fontSize: 14
                                        }}>
                                            {log.action}
                                        </div>
                                        <div style={{
                                            color: "#8BA3C7", fontSize: 12,
                                            marginTop: 2
                                        }}>
                                            {log.performedByRole} ·{" "}
                                            {new Date(log.timestamp).toLocaleString()}
                                        </div>
                                        <div style={{
                                            color: "#5D6D7E", fontSize: 11,
                                            marginTop: 2
                                        }}>
                                            {log.details}
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}
