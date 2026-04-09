// src/pages/LoginPage.tsx
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { authApi, saveAuth } from "../services/api";

export default function LoginPage() {
    const navigate = useNavigate();
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();
        setError("");
        setLoading(true);
        try {
            const data = await authApi.login(email, password);
            saveAuth(data);
            if (data.role === "Admin") navigate("/dashboard");
            else if (data.role === "Doctor") navigate("/transfers");
            else navigate("/dashboard");
        } catch {
            setError("Invalid email or password. Please try again.");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div style={{
            minHeight: "100vh",
            background: "linear-gradient(135deg, #0a1628 0%, #112240 100%)",
            display: "flex", alignItems: "center", justifyContent: "center",
            fontFamily: "'Inter', sans-serif",
        }}>
            <div style={{
                background: "#112240", border: "1px solid rgba(255,255,255,0.08)",
                borderRadius: 16, padding: 40, width: "100%", maxWidth: 420,
                boxShadow: "0 24px 80px rgba(0,0,0,0.4)",
            }}>
                <div style={{ textAlign: "center", marginBottom: 32 }}>
                    <div style={{ fontSize: 40, marginBottom: 8 }}>🏥</div>
                    <h1 style={{ color: "#F0F6FF", fontSize: 24, fontWeight: 700, margin: 0 }}>IPTS</h1>
                    <p style={{ color: "#8BA3C7", fontSize: 14, margin: "4px 0 0" }}>
                        Inter-Hospital Patient Transfer System
                    </p>
                </div>

                {error && (
                    <div style={{
                        background: "rgba(255,77,106,0.12)", border: "1px solid rgba(255,77,106,0.3)",
                        borderRadius: 8, padding: "12px 16px", color: "#FF4D6A", fontSize: 14, marginBottom: 20,
                    }}>{error}</div>
                )}

                <form onSubmit={handleLogin}>
                    <div style={{ marginBottom: 16 }}>
                        <label style={{ color: "#8BA3C7", fontSize: 13, fontWeight: 500, display: "block", marginBottom: 6 }}>
                            Email Address
                        </label>
                        <input
                            type="email" value={email} onChange={e => setEmail(e.target.value)}
                            required placeholder="doctor@hospital.com"
                            style={{
                                width: "100%", background: "#0A1628",
                                border: "1px solid rgba(255,255,255,0.1)", borderRadius: 8,
                                padding: "12px 14px", color: "#F0F6FF", fontSize: 14,
                                outline: "none", boxSizing: "border-box",
                            }}
                        />
                    </div>

                    <div style={{ marginBottom: 24 }}>
                        <label style={{ color: "#8BA3C7", fontSize: 13, fontWeight: 500, display: "block", marginBottom: 6 }}>
                            Password
                        </label>
                        <input
                            type="password" value={password} onChange={e => setPassword(e.target.value)}
                            required placeholder="••••••••"
                            style={{
                                width: "100%", background: "#0A1628",
                                border: "1px solid rgba(255,255,255,0.1)", borderRadius: 8,
                                padding: "12px 14px", color: "#F0F6FF", fontSize: 14,
                                outline: "none", boxSizing: "border-box",
                            }}
                        />
                    </div>

                    <button type="submit" disabled={loading} style={{
                        width: "100%",
                        background: loading ? "#1a3a6b" : "linear-gradient(135deg, #1E5FBF, #00C2D4)",
                        border: "none", borderRadius: 8, padding: "13px",
                        color: "#fff", fontSize: 15, fontWeight: 600,
                        cursor: loading ? "not-allowed" : "pointer",
                    }}>
                        {loading ? "Signing in..." : "Sign In"}
                    </button>
                </form>

                <div style={{
                    marginTop: 24, padding: "12px 16px",
                    background: "rgba(0,194,212,0.06)", border: "1px solid rgba(0,194,212,0.15)",
                    borderRadius: 8,
                }}>
                    <p style={{ color: "#8BA3C7", fontSize: 12, margin: 0, lineHeight: 1.7 }}>
                        <strong style={{ color: "#00C2D4" }}>Test credentials</strong><br />
                        Admin: admin@cityhospital.com<br />
                        Doctor: doctor.send@cityhospital.com<br />
                        Password: Test@1234
                    </p>
                </div>
            </div>
        </div>
    );
}