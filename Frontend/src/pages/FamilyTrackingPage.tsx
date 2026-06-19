import { useState, useEffect, useRef } from "react";
import { useParams } from "react-router-dom";
import maplibregl from "maplibre-gl";
import "maplibre-gl/dist/maplibre-gl.css";
import { familyApi } from "../services/api";
import type { FamilyTrackingDto } from "../services/api";

// ── HELPER: converts ISO datetime to human-readable relative time ─────
function timeAgo(dateStr: string): string {
    const seconds = Math.floor(
        (Date.now() - new Date(dateStr).getTime()) / 1000
    );
    if (seconds < 60) return `${seconds} seconds ago`;
    if (seconds < 3600) return `${Math.floor(seconds / 60)} minutes ago`;
    return `${Math.floor(seconds / 3600)} hours ago`;
}

const MAP_STYLE_URL = "https://tiles.openfreemap.org/styles/bright";

// ── HELPER: prefer English map labels when the vector tile provides them ──
function preferEnglishLabels(map: maplibregl.Map) {
    const style = map.getStyle();

    style.layers
        ?.filter((layer) => layer.type === "symbol" && layer.layout?.["text-field"])
        .forEach((layer) => {
            map.setLayoutProperty(layer.id, "text-field", [
                "coalesce",
                ["get", "name:en"],
                ["get", "name_en"],
                ["get", "name:latin"],
                ["get", "name"],
            ]);
        });
}

// ── MAIN COMPONENT ────────────────────────────────────────────────────
export default function FamilyTrackingPage() {
    const { token } = useParams<{ token: string }>();
    const [data, setData] = useState<FamilyTrackingDto | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [copied, setCopied] = useState(false);
    const [lastPoll, setLastPoll] = useState(new Date());

    // Fetch data — called on mount and every 10 seconds
    const fetchData = async (isInitial = false) => {
        if (!token) return;
        if (isInitial) setLoading(true);
        try {
            const result = await familyApi.track(token);
            setData(result);
            setError(null);
        } catch (e: unknown) {
            if (isInitial) {
                setError(
                    e instanceof Error ? e.message : "Invalid or expired tracking link."
                );
            }
        } finally {
            if (isInitial) setLoading(false);
            setLastPoll(new Date());
        }
    };

    useEffect(() => {
        fetchData(true);
        const interval = setInterval(() => fetchData(false), 10_000);
        return () => clearInterval(interval);
    }, [token]);

    // ── LOADING STATE ──────────────────────────────────────────────────
    if (loading) return (
        <div style={{
            minHeight: "100vh", background: "#0A1628",
            display: "flex", alignItems: "center", justifyContent: "center",
            flexDirection: "column", gap: 16,
            fontFamily: "'Inter', sans-serif",
        }}>
            <div style={{ fontSize: 48 }}>🚑</div>
            <p style={{ color: "#8BA3C7", fontSize: 16 }}>
                Loading tracking information...
            </p>
        </div>
    );

    // ── ERROR STATE (invalid / expired token) ─────────────────────────
    if (error) return (
        <div style={{
            minHeight: "100vh", background: "#0A1628",
            display: "flex", alignItems: "center", justifyContent: "center",
            fontFamily: "'Inter', sans-serif",
        }}>
            <div style={{
                background: "#112240",
                border: "1px solid rgba(255,77,106,0.3)",
                borderRadius: 16, padding: "40px", textAlign: "center", maxWidth: 480,
            }}>
                <div style={{ fontSize: 48, marginBottom: 16 }}>❌</div>
                <h2 style={{ color: "#FF4D6A", margin: "0 0 12px", fontSize: 20 }}>
                    Tracking Link Invalid
                </h2>
                <p style={{ color: "#8BA3C7", fontSize: 14, margin: 0 }}>
                    This tracking link is invalid or has expired.
                    Please contact the hospital for assistance.
                </p>
            </div>
        </div>
    );

    if (!data) return null;

    const trackingUrl = window.location.href;

    // ── MAIN TRACKING VIEW ────────────────────────────────────────────
    return (
        <div style={{
            minHeight: "100vh", background: "#0A1628",
            fontFamily: "'Inter', sans-serif",
        }}>

            {/* CSS for pulsing LIVE indicator */}
            <style>{`
                @keyframes livePulse {
                    0%, 100% { opacity: 1; transform: scale(1); }
                    50%       { opacity: 0.4; transform: scale(1.5); }
                }
                .live-dot {
                    width: 10px; height: 10px; border-radius: 50%;
                    background: #00D68F; display: inline-block;
                    animation: livePulse 1.5s ease-in-out infinite;
                    margin-right: 6px; vertical-align: middle;
                }
            `}</style>

            {/* HEADER */}
            <header style={{
                background: "#112240",
                borderBottom: "1px solid rgba(255,255,255,0.08)",
                padding: "16px 24px",
                display: "flex", alignItems: "center", justifyContent: "space-between",
            }}>
                <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                    <span style={{ fontSize: 24 }}>🚑</span>
                    <span style={{ color: "#F0F6FF", fontWeight: 700, fontSize: 18 }}>
                        IPTS — Live Tracking
                    </span>
                </div>
                <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
                    <span className="live-dot" />
                    <span style={{ color: "#00D68F", fontWeight: 700, fontSize: 13 }}>LIVE</span>
                    <span style={{ color: "#8BA3C7", fontSize: 12, marginLeft: 8 }}>
                        Updated {timeAgo(lastPoll.toISOString())}
                    </span>
                </div>
            </header>

            <div style={{ maxWidth: 860, margin: "0 auto", padding: "24px" }}>

                {/* EXPIRED BANNER */}
                {data.isExpired && (
                    <div style={{
                        background: "rgba(255,154,60,0.12)",
                        border: "1px solid rgba(255,154,60,0.3)",
                        borderRadius: 10, padding: "12px 18px",
                        color: "#FF9A3C", marginBottom: 20, fontSize: 14,
                    }}>
                        ⚠️  This tracking link has expired.
                    </div>
                )}

                {/* TRANSFER INFO CARD */}
                <div style={{
                    background: "#112240",
                    border: "1px solid rgba(255,255,255,0.07)",
                    borderRadius: 16, padding: 24, marginBottom: 20,
                }}>
                    <div style={{
                        display: "flex", justifyContent: "space-between",
                        alignItems: "flex-start", marginBottom: 20,
                    }}>
                        <h2 style={{ color: "#F0F6FF", margin: 0, fontSize: 20, fontWeight: 700 }}>
                            Patient Transfer
                        </h2>
                        <span style={{
                            background: "rgba(0,214,143,0.15)",
                            color: "#00D68F",
                            border: "1px solid rgba(0,214,143,0.3)",
                            padding: "4px 12px", borderRadius: 8,
                            fontSize: 12, fontWeight: 700,
                        }}>
                            {data.patientStatus.replace(/([A-Z])/g, " $1").trim()}
                        </span>
                    </div>

                    <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16 }}>
                        <InfoCard label="From" value={data.sendingHospitalName} />
                        <InfoCard label="To" value={data.receivingHospitalName} />
                        <InfoCard label="Address" value={data.receivingHospitalAddress} />
                        <InfoCard label="Ambulance" value={data.ambulanceUnit} />
                    </div>

                    <div style={{
                        marginTop: 16, paddingTop: 16,
                        borderTop: "1px solid rgba(255,255,255,0.07)",
                        color: "#8BA3C7", fontSize: 12,
                    }}>
                        📍 Last GPS update: {timeAgo(data.lastLocationUpdate)}
                    </div>
                </div>

                {/* MAP */}
                <div style={{
                    background: "#112240",
                    border: "1px solid rgba(255,255,255,0.07)",
                    borderRadius: 16, overflow: "hidden", marginBottom: 20,
                }}>
                    <div style={{
                        padding: "12px 20px",
                        borderBottom: "1px solid rgba(255,255,255,0.07)",
                        color: "#8BA3C7", fontSize: 13,
                    }}>
                        🗺️  Ambulance Location
                    </div>
                    <LiveAmbulanceMap
                        latitude={data.ambulanceLatitude}
                        longitude={data.ambulanceLongitude}
                        ambulanceUnit={data.ambulanceUnit}
                    />
                </div>

                {/* SHARE BUTTON */}
                <div style={{
                    background: "#112240",
                    border: "1px solid rgba(255,255,255,0.07)",
                    borderRadius: 16, padding: 20, marginBottom: 20,
                }}>
                    <p style={{ color: "#8BA3C7", fontSize: 13, margin: "0 0 12px" }}>
                        Share this tracking link with other family members:
                    </p>
                    <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
                        <code style={{
                            flex: 1, background: "#0A1628", color: "#F0F6FF",
                            padding: "8px 12px", borderRadius: 6,
                            fontSize: 12, wordBreak: "break-all",
                        }}>
                            {trackingUrl}
                        </code>
                        <button
                            onClick={() => {
                                navigator.clipboard.writeText(trackingUrl);
                                setCopied(true);
                                setTimeout(() => setCopied(false), 2000);
                            }}
                            style={{
                                background: copied ? "#00D68F" : "#1E5FBF",
                                color: "white", border: "none",
                                borderRadius: 8, padding: "8px 18px",
                                fontWeight: 700, cursor: "pointer",
                                whiteSpace: "nowrap", fontSize: 13,
                                transition: "background 0.2s",
                            }}
                        >
                            {copied ? "✓ Copied!" : "Copy Link"}
                        </button>
                    </div>
                </div>

                {/* FOOTER */}
                <p style={{
                    textAlign: "center", color: "#555F7A",
                    fontSize: 12, marginTop: 8,
                }}>
                    This tracking link is valid for 12 hours after the transfer was confirmed.
                    It will automatically stop working once the patient is delivered.
                </p>

            </div>
        </div>
    );
}

// ── LIVE MAP COMPONENT ───────────────────────────────────────────────────
function LiveAmbulanceMap({
    latitude,
    longitude,
    ambulanceUnit,
}: {
    latitude: number;
    longitude: number;
    ambulanceUnit: string;
}) {
    const mapContainerRef = useRef<HTMLDivElement | null>(null);
    const mapRef = useRef<maplibregl.Map | null>(null);
    const markerRef = useRef<maplibregl.Marker | null>(null);

    useEffect(() => {
        if (!mapContainerRef.current || mapRef.current) return;

        const markerElement = document.createElement("div");
        markerElement.style.width = "42px";
        markerElement.style.height = "42px";
        markerElement.style.borderRadius = "50%";
        markerElement.style.background = "#00D68F";
        markerElement.style.border = "3px solid #FFFFFF";
        markerElement.style.boxShadow = "0 8px 20px rgba(0, 0, 0, 0.35)";
        markerElement.style.display = "flex";
        markerElement.style.alignItems = "center";
        markerElement.style.justifyContent = "center";
        markerElement.style.fontSize = "22px";
        markerElement.style.cursor = "pointer";
        markerElement.textContent = "🚑";

        const map = new maplibregl.Map({
            container: mapContainerRef.current,
            style: MAP_STYLE_URL,
            center: [longitude, latitude],
            zoom: 15,
            attributionControl: false,
        });

        map.addControl(new maplibregl.NavigationControl({ showCompass: false }), "top-right");
        map.addControl(
            new maplibregl.AttributionControl({
                compact: true,
                customAttribution:
                    '<a href="https://openfreemap.org/" target="_blank" rel="noreferrer">OpenFreeMap</a>',
            }),
            "bottom-right"
        );

        map.on("load", () => preferEnglishLabels(map));

        const marker = new maplibregl.Marker({ element: markerElement, anchor: "center" })
            .setLngLat([longitude, latitude])
            .setPopup(
                new maplibregl.Popup({ offset: 24 }).setHTML(
                    `<strong>${ambulanceUnit || "Ambulance"}</strong><br/>Live ambulance location`
                )
            )
            .addTo(map);

        mapRef.current = map;
        markerRef.current = marker;

        return () => {
            marker.remove();
            map.remove();
            mapRef.current = null;
            markerRef.current = null;
        };
    }, []);

    useEffect(() => {
        const nextPosition: [number, number] = [longitude, latitude];
        markerRef.current?.setLngLat(nextPosition);
        mapRef.current?.easeTo({
            center: nextPosition,
            duration: 900,
        });
    }, [latitude, longitude]);

    return (
        <div
            ref={mapContainerRef}
            style={{
                width: "100%",
                height: 380,
            }}
        />
    );
}

// ── SUB-COMPONENT ─────────────────────────────────────────────────────
function InfoCard({ label, value }: { label: string; value: string }) {
    return (
        <div style={{
            background: "rgba(255,255,255,0.04)",
            borderRadius: 10, padding: "12px 16px",
        }}>
            <p style={{ color: "#8BA3C7", fontSize: 11, margin: "0 0 4px", textTransform: "uppercase", letterSpacing: 1 }}>
                {label}
            </p>
            <p style={{ color: "#F0F6FF", fontSize: 15, fontWeight: 600, margin: 0 }}>
                {value || "—"}
            </p>
        </div>
    );
}
