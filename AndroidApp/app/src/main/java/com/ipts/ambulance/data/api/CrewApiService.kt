package com.ipts.ambulance.data.api

import com.ipts.ambulance.data.model.*
import retrofit2.Response
import retrofit2.http.*


interface CrewApiService {

    @POST("crew/login")
    suspend fun login(
        @Body request: CrewLoginRequest
    ): Response<CrewAuthResponse>

    @GET("crew/active-job")
    suspend fun getActiveJob(
        @Header("Authorization") token: String
    ): Response<CrewActiveJobDto>

    @POST("crew/vitals")
    suspend fun submitVitals(
        @Header("Authorization") token: String,
        @Body request: SubmitVitalsRequest
    ): Response<VitalsResponseDto>

    //  GPS update
    @PATCH("crew/location")
    suspend fun updateLocation(
        @Header("Authorization") token: String,
        @Body request: UpdateLocationRequest
    ): Response<LocationUpdateResponse>

    @PATCH("crew/job-status")
    suspend fun updateJobStatus(
        @Header("Authorization") token: String,
        @Body request: UpdateJobStatusRequest
    ): Response<CrewActiveJobDto>

}