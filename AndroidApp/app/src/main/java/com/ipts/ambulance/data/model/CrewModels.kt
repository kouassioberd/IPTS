package com.ipts.ambulance.data.model

data class CrewLoginRequest(val email: String, val password: String)

data class CrewAuthResponse(
    val accessToken:   String,
    val fullName:      String,
    val role:          String,
    val crewId:        String,
    val ambulanceId:   String,
    val ambulanceUnit: String,
    val expiresAt:     String
)

data class CrewActiveJobDto(
    val transferRequestId:          String,
    val sendingHospitalName:         String,
    val receivingHospitalName:       String,
    val receivingHospitalAddress:    String,
    val receivingHospitalLatitude:   Double,
    val receivingHospitalLongitude:  Double,
    val status:                      Int,
    val ambulanceUnit:               String,
    val confirmedAt:                 String,
    val hasVitalsSubmitted:          Boolean
)

data class SubmitVitalsRequest(
    val transferRequestId: String,
    val bloodPressure:     String,
    val heartRate:         Int,
    val oxygenSaturation:  Int,
    val glasgowComaScale:  Int,
    val notes:             String
)

data class VitalsResponseDto(
    val id:                String,
    val transferRequestId: String,
    val bloodPressure:     String,
    val heartRate:         Int,
    val oxygenSaturation:  Int,
    val glasgowComaScale:  Int,
    val notes:             String,
    val recordedAt:        String
)

// ── GPS location update ──────────────────────
data class UpdateLocationRequest(
    val latitude:  Double,
    val longitude: Double
)

data class LocationUpdateResponse(
    val ambulanceId: String,
    val latitude:    Double,
    val longitude:   Double,
    val updatedAt:   String
)

