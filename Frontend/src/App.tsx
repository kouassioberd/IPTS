// src/App.tsx
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import LoginPage from "./pages/LoginPage";
import DashboardPage from "./pages/DashboardPage";
import TransfersPage from "./pages/TransfersPage";
import NewTransferPage from "./pages/NewTransferPage";
import { getToken, getUser } from "./services/api";

function ProtectedRoute({ children, allowedRoles }: {
    children: React.ReactNode;
    allowedRoles?: string[];
}) {
    const token = getToken();
    const user = getUser();

    if (!token || !user) return <Navigate to="/login" replace />;
    if (allowedRoles && !allowedRoles.includes(user.role))
        return <Navigate to="/login" replace />;

    return <>{children}</>;
}

export default function App() {
    return (
        <BrowserRouter>
            <Routes>
                {/* Public */}
                <Route path="/login" element={<LoginPage />} />

                {/* Admin only */}
                <Route
                    path="/dashboard"
                    element={
                        <ProtectedRoute allowedRoles={["Admin"]}>
                            <DashboardPage />
                        </ProtectedRoute>
                    }
                />

                {/* Doctor — transfer list (sent + incoming) */}
                <Route
                    path="/transfers"
                    element={
                        <ProtectedRoute allowedRoles={["Doctor"]}>
                            <TransfersPage />
                        </ProtectedRoute>
                    }
                />

                {/* Doctor — create new transfer request (4-step flow) */}
                <Route
                    path="/transfers/new"
                    element={
                        <ProtectedRoute allowedRoles={["Doctor"]}>
                            <NewTransferPage />
                        </ProtectedRoute>
                    }
                />

                {/* Default redirects */}
                <Route path="/" element={<Navigate to="/dashboard" replace />} />
                <Route path="*" element={<Navigate to="/login" replace />} />
            </Routes>
        </BrowserRouter>
    );
}